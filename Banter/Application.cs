//***********************************************************************
// *  $RCSfile$ - Application.cs
// *
// *  Copyright (C) 2007 Novell, Inc.
// *
// *  This program is free software; you can redistribute it and/or
// *  modify it under the terms of the GNU General Public
// *  License as published by the Free Software Foundation; either
// *  version 2 of the License, or (at your option) any later version.
// *
// *  This program is distributed in the hope that it will be useful,
// *  but WITHOUT ANY WARRANTY; without even the implied warranty of
// *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// *  General Public License for more details.
// *
// *  You should have received a copy of the GNU General Public
// *  License along with this program; if not, write to the Free
// *  Software Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
// *
// **********************************************************************

using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml;

using NDesk.DBus;
using org.freedesktop.DBus;
//using org.freedesktop.Telepathy;
using Tapioca;

using Gtk;
using Gnome;
using Mono.Unix;
using Mono.Unix.Native;

namespace Banter
{
	class Application
	{
		#region Private Static Types
		private static Banter.Application application = null;
		private static System.Object locker = new System.Object();
		#endregion
		
		#region Private Types
		private bool initialized = false;
		private ActionManager actionManager;
		//private NotificationArea tray;
		private Gdk.Pixbuf appPixBuf;
		private Gnome.Program program = null;
		private Dictionary <uint, Person> persons;
		private Dictionary <string, ChatWindow> chatWindows;
		//private Person me;
		private PreferencesDialog preferencesDialog;
		//VideoConversation videoConversation;
		private Conversation conversation = null;
		private bool initiatedConversation = false;
		
		private Dictionary<GroupWindow, GroupWindow> groupWindows;

		// Major temporary hack
		//private Novell.Rtc.Connection conn = null;
		
		// Extend the hack to tapioca
		//Tapioca.UserContact meContact = null;
		Tapioca.Connection tapConnection = null;
		ManualResetEvent connectedEvent = null;
		
		#endregion
	
		#region Public Static Properties
		public static Application Instance
		{
			get
			{
				lock(locker)
				{
					if(application == null)
					{
						lock(locker)
						{
							application = new Application();
						}
					}
					return application;
				}
			}
		}
		
		public static ActionManager ActionManager
		{
			get { return Instance.actionManager; }
		}
		#endregion

		#region Constructors	
		private Application ()
		{
			Init(null);
		}
	
		private Application (string[] args)
		{
			Init(args);
		}
		#endregion


		#region Private Methods
		private void Init(string[] args)
		{
			Console.WriteLine ("Banter.Application constructor");
			initialized = false;
			Gtk.Application.Init ();
			program = 
				new Gnome.Program (
						"novell-rtc-application",
						Defines.Version,
						Gnome.Modules.UI,
						args);

			// Call Init before anything so the people objects are in place
			PersonStore.Instance.Init();
			
			actionManager = new ActionManager ();
			actionManager.LoadInterface ();
			SetUpGlobalActions ();

			//tray = new NotificationArea("RtcApplication");
			SetupTrayIcon();

			application = this;
			
			groupWindows = new Dictionary<GroupWindow, GroupWindow> ();
			chatWindows = new Dictionary<string,ChatWindow> ();

			GLib.Idle.Add(InitializeIdle);
		}
	
		private void SetupTrayIcon ()
		{
			Console.WriteLine ("Creating TrayIcon");
			
			EventBox eb = new EventBox();
			appPixBuf = GetIcon ("banter-22", 22);
			eb.Add(new Image (appPixBuf)); 
			//new Image(Gtk.Stock.DialogWarning, IconSize.Menu)); // using stock icon

			// hooking event
			eb.ButtonPressEvent += new ButtonPressEventHandler (this.OnImageClick);
			Egg.TrayIcon icon = new Egg.TrayIcon("RtcApplication");
			icon.Add(eb);
			// showing the trayicon
			icon.ShowAll();			
		}
		
		private bool InitializeIdle ()
		{
			Console.WriteLine ("Initialize_Idle - called");

			//GConfPreferencesProvider prefs = new GConfPreferencesProvider ();
			//Preferences.Init (prefs);
			//Preferences.SettingChanged += CompDirApplication.SettingChanged;

			// Startup the message log store
			// Note! not persisting messages yet so don't start
			//MessageStore.Start ();
			
			// Startup the messaging engine
			//MessageEngine.Start ();
			
			// Launch the configured telepathy providers
			//TelepathyProviderFactory.Start ();
			
			// Start account management and authenticate to any auto-login accounts
			AccountManagement.IAmUpEvent += OnAccountManagementReadyEvent;
			AccountManagement.Start ();
			
			/*
			if ((bool) Preferences.Get (Preferences.ENABLE_TRAY_ICON))
				ShowTrayIcon ();
			else
				ShowCompDirWindow ();
			*/
			return false;
		}
		
		// <summary>
		// Set up the ActionManager actions that can be called from anywhere.
		// </summary>
		private void SetUpGlobalActions ()
		{
			actionManager ["QuitAction"].Activated += OnQuitAction;
			actionManager ["ShowPreferencesAction"].Activated += OnShowPreferencesAction;
		}
		
		private void OnAccountManagementReadyEvent ()
		{
			Logger.Debug("OnAccountManagementReady() called");
			InitializePersons();
			initialized = true;
			
			GLib.Idle.Add (OpenSavedGroupWindows);
		}
		
		private bool OpenSavedGroupWindows ()
		{
			GroupWindow gw;
			string[] groupWindowIds = Preferences.Get (Preferences.GroupWindows) as String[];
			if (groupWindowIds == null) {
				// Create a new Group Window with Everyone selected
				gw = new GroupWindow ();
				groupWindows [gw] = gw;
				gw.DeleteEvent += OnGroupWindowDeleted;
				gw.ShowAll ();
				return false;
			}
			
			foreach (string id in groupWindowIds) {
				gw = new GroupWindow (id);
				
				groupWindows [gw] = gw;
				gw.DeleteEvent += OnGroupWindowDeleted;
				gw.ShowAll ();
			}
			
			return false; // Prevent GLib.Idle from calling this method again
		}

		private void OnNewChannel (Tapioca.Connection sender, Tapioca.Channel channel)
		{
			Logger.Debug ("Application::OnNewChannel - called");
			
			if (initiatedConversation == true)
				return;
				
			/*
			Logger.Debug ("Checking for contacts");
			Logger.Debug (
				"# contacts in channel: {0}",
				channel.ContactGroup.Contacts.Length);
			foreach (Contact ct in channel.ContactGroup.Contacts)
				Logger.Debug (ct.Uri);
			Logger.Debug (
				"# pending contacts in channel: {0}", 
				channel.ContactGroup.PendingContacts.Length);
			*/	
		
			try
			{
				Conversation conversation = null;
				
				switch (channel.Type)
				{
					case Tapioca.ChannelType.Text:
					{
						TextChannel txtChannel = channel as TextChannel;
						conversation = 
							new Conversation (sender, txtChannel.RemoteTarget as Contact);
						conversation.SetTextChannel (txtChannel);

						Person peer = GetPersonFromContact (txtChannel.RemoteTarget as Contact);
						ChatWindow cw = null;
						
						if (chatWindows.ContainsKey (peer.Id)) {
							chatWindows[peer.Id].Present();
						}
						else {
							try
							{
								cw = new ChatWindow ();
								cw.Conversation = conversation;
								cw.DeleteEvent += OnChatWindowDelete;
								chatWindows[peer.Id] = cw;
								cw.Present();
							}
							catch (Exception es)
							{
								Console.WriteLine (es.Message);
								Console.WriteLine (es.StackTrace);
							}
						}
						break;
					}
					
					case Tapioca.ChannelType.StreamedMedia:
					{
						VideoWindow meWindow = new VideoWindow();
						meWindow.Title = "Me";
						meWindow.Show();
				
						VideoWindow youWindow = new VideoWindow();
						youWindow.Title = "You";
						youWindow.Show();
						
						StreamChannel strmChannel =	channel as StreamChannel;

						conversation = 
							new Conversation (sender, strmChannel.RemoteTarget as Contact);
					
						conversation.SetVideoWindows (meWindow.WindowId, youWindow.WindowId);
						conversation.SetStreamedMediaChannel (strmChannel);
						break;
					}
				}
			
			}
			catch (Exception onc)
			{
				Logger.Debug (onc.Message);
				Logger.Debug (onc.StackTrace);
			}
			
		}

		private void OnConnectionChanged ( 
			Tapioca.Connection sender,
			Tapioca.ConnectionStatus status,
			Tapioca.ConnectionStatusReason reason)
		{
			Logger.Debug ("OnConnectionChanged - called");
			Logger.Debug ("  {0}", status.ToString());
			
			switch (status)
			{
				case Tapioca.ConnectionStatus.Connecting:
				{
					break;
				}
				
				case Tapioca.ConnectionStatus.Connected:
				{
					Logger.Debug ("  in connected");
					
					try
					{
						Logger.Debug ("ME - uri: {0}", tapConnection.Info.Uri);
						Logger.Debug ("ME - alias: {0}", tapConnection.Info.Alias);
						Logger.Debug ("ME - caps: {0}", tapConnection.Info.Capabilities.ToString());
						Logger.Debug ("ME - avatar token: {0}", tapConnection.Info.CurrentAvatarToken);
						
						/*
						Person person = new Person(tapConnection.Info, true);
						persons[tapConnection.Info.Handle.Id] = person;
						this.me = person;
						*/
						
						Logger.Debug ("# Subscribed Contacts: {0}", tapConnection.ContactList.SubscribedContacts.Length);
						Logger.Debug ("# Known Contacts: {0}", tapConnection.ContactList.KnownContacts.Length);

						Logger.Debug ("Connection jabber requested");
						
						// Loop through and add all of the subscribed contacts
		 				foreach (Contact c in tapConnection.ContactList.KnownContacts) //.SubscribedContacts)
						{
		                 	Logger.Debug (
		                 		"Contact Retrieved\n\t{0}/{4} - {1}/{2} - {3}",
		                 		c.Uri, 
		                 		c.Presence, 
		                 		c.PresenceMessage, 
		                 		c.SubscriptionStatus, 
		                 		c.Alias);

							if(!persons.ContainsKey (c.Handle.Id)) {
								Person person = PersonStore.GetPersonByJabberId(c.Handle.Name);
								if(person == null) {
									person = new Person(c.Alias);
									person.JabberId = c.Handle.Name;
									PersonStore.AddPerson(person);
								}
								
								person.Contact = c;
								persons[c.Handle.Id] = person;
							}
		                }
		                
						// FIXME - For now we have all caps		                
						tapConnection.Info.SetCapabilities (
							ContactCapabilities.Text |
							ContactCapabilities.Audio |
							ContactCapabilities.Video);
		                
		                // Setup handlers for incoming conversations
		                sender.ChannelCreated += OnNewChannel;
					}
					catch (Exception on)
					{	
						Logger.Debug (on.Message);
						Logger.Debug (on.StackTrace);
					}
					
					if (this.connectedEvent != null)
						this.connectedEvent.Set();
						
					break;
				}
				
				case Tapioca.ConnectionStatus.Disconnected:
				{
					break;
				}
			}
		}
		
		private void InitializePersons()
		{
			Logger.Debug("InitializePersons called");

			persons = new Dictionary<uint,Person> ();

			try
			{
				Banter.Account rtcAccount = null;
				foreach (Banter.Account account in AccountManagement.GetAccounts())
				{
					rtcAccount = account;
					break;
				}
	
				System.Collections.Generic.Dictionary <string, object> options =
					rtcAccount.Options;
			
				ConnectionManagerFactory cmFactory = new ConnectionManagerFactory ();
	              
	            Logger.Debug ("user account: {0}", options["account"]);
	              
				System.Collections.ArrayList ps = new System.Collections.ArrayList ();
				ps.Add (new ConnectionManagerParameter ("account", options["account"]));
				ps.Add (new ConnectionManagerParameter ("password", options["password"]));
				ps.Add (new ConnectionManagerParameter ("server", options["server"]));
				//ps.Add (new ConnectionManagerParameter ("server", "talk.google.com"));
				ps.Add (new ConnectionManagerParameter ("old-ssl", true));
				ps.Add (new ConnectionManagerParameter ("ignore-ssl-errors", true));
				ps.Add (new ConnectionManagerParameter ("port", (uint) 5223));

				ConnectionManagerParameter[] parameters = 
					(ConnectionManagerParameter[]) ps.ToArray (typeof (ConnectionManagerParameter));

				Logger.Debug ("Creating connection");
				ConnectionManager cm = cmFactory.GetConnectionManager ("jabber");
				if (cm == null) {
				       Logger.Debug ("Error geting CM");
				       return;
				}
				Console.WriteLine ("Connection created");
				
				/*
				Logger.Debug ("#parameters: {0}", parameters.Length);
				foreach (ConnectionManagerParameter pm in parameters)
					Logger.Debug ("name: {0} - value: {1}", pm.Name, pm.Value);
				*/

				tapConnection = cm.RequestConnection ("jabber", parameters);
				if (tapConnection == null) {
				       Logger.Debug ("Error on RequestConnection");
				       return;
				}
			
				connectedEvent = new ManualResetEvent (false);
				tapConnection.StatusChanged += OnConnectionChanged;
				tapConnection.Connect (Tapioca.ContactPresence.Available);
				
				connectedEvent.WaitOne (20000, true);
				Thread.Sleep(2000);

				if (tapConnection.Status != Tapioca.ConnectionStatus.Connected)
				{
					throw new ApplicationException (
								String.Format ("Failed to connect \"{0}\"", 
									rtcAccount.Username));
									//rtcAccount.TelepathyBusName));
				}
				
				Logger.Debug ("Connection Status: {0}", tapConnection.Status.ToString());
				

			
				/*
				foreach(Account account in AccountManagement.GetAccounts())
				{
					conn = new Novell.Rtc.Connection (account);
					conn.Connect ();
					Member me = conn.GetSelf();
					Console.WriteLine ("Me - Name: {0}  Alias: {1}", me.ScreenName, me.Alias);
					Person person = new Person(me);
					person.IsSelf = true;
					persons[me.Handle] = person;
					this.me = person;
					
							
					Member[] members = conn.GetActiveMembers();
							
					foreach (Member member in members)
					{
						if(!persons.ContainsKey(member.Handle))
							persons[member.Handle] = new Person(member);
						
						Console.WriteLine (
							"Member - Handle: {0} Name: {1}  Alias: {2} Presence: {3}", 
							member.Handle, 
							member.ScreenName, 
							member.Alias,
							member.Presence.ToString());
					}
					
					conn.IncomingConversation += OnIncomingConversation;
					conn.IncomingMediaConversation += OnIncomingMediaConversation;
				}
				*/
			}
			catch (Exception es)
			{
				Console.WriteLine (es.Message);
				Console.WriteLine (es.StackTrace);
			}
			finally
			{
					/*
					if (friends != null)
						foreach (Person friend in friends)
							friend.Dispose();
					*/
			}
		}

		
		private void OnImageClick (object o, ButtonPressEventArgs args) // handler for mouse click
		{
			if(!initialized)
				return;
				
			if (args.Event.Button == 1) {
				Console.WriteLine ("left button clicked");
			} else if (args.Event.Button == 3) //right click
   			{
   				// FIXME: Eventually get all these into UIManagerLayout.xml file
   				Console.WriteLine ("Right button clicked");
      			Menu popupMenu = new Menu();
      			
      			if (groupWindows.Count > 0) {
	      			// List all the opened GroupWindows here
	      			foreach (GroupWindow groupWindow in groupWindows.Values) {
	      				GroupWindowMenuItem item = new GroupWindowMenuItem (
	      						groupWindow);
	      				item.Activated += OnGroupWindowMenuItem;
	      				popupMenu.Add (item);
	      			}
      			
      				popupMenu.Add (new SeparatorMenuItem ());
      			}
 
      			ImageMenuItem people = new ImageMenuItem (
						Catalog.GetString ("New Group Window ..."));
      			people.Activated += OnPeople;
      			popupMenu.Add (people);
      			
//      			ImageMenuItem everyone = new ImageMenuItem ("Everyone");
//      			everyone.Activated += OnEveryone;
//      			popupMenu.Add (everyone);

      			ImageMenuItem accounts = new ImageMenuItem (
      					Catalog.GetString ("Accounts"));
      			accounts.Activated += OnAccounts;
      			popupMenu.Add (accounts);
      			
      			SeparatorMenuItem separator = new SeparatorMenuItem ();
      			popupMenu.Add (separator);
      			
      			ImageMenuItem preferences = new ImageMenuItem (Gtk.Stock.Preferences, null);
      			preferences.Activated += OnPreferences;
      			popupMenu.Add (preferences);

      			separator = new SeparatorMenuItem ();
      			popupMenu.Add (separator);

      			ImageMenuItem quit = new ImageMenuItem (
      					Catalog.GetString ("Quit"));
      			quit.Activated += OnQuit;
      			popupMenu.Add (quit);
      			
				popupMenu.ShowAll(); // shows everything
      			//popupMenu.Popup(null, null, null, IntPtr.Zero, args.Event.Button, args.Event.Time);
      			popupMenu.Popup(null, null, null, args.Event.Button, args.Event.Time);
   			}
		}		
		

		private void OnAccounts (object o, System.EventArgs args)
		{
			Console.WriteLine ("Selected Accounts");
		}
		
		private void OnPreferences (object sender, EventArgs args)
		{
			actionManager ["ShowPreferencesAction"].Activate ();
		}
		
		private void OnGroupWindowMenuItem (object sender, EventArgs args)
		{
			GroupWindowMenuItem item = sender as GroupWindowMenuItem;
			
			item.GroupWindow.Present ();
		}

		private void OnPeople (object o, System.EventArgs args)
		{
			GroupWindow gw = new GroupWindow ();
			
			groupWindows [gw] = gw;
			gw.DeleteEvent += OnGroupWindowDeleted;
			gw.ShowAll ();
		}
		
		private void OnQuit (object o, System.EventArgs args)
		{
			actionManager ["QuitAction"].Activate ();
		}
		
		private void OnQuitAction (object sender, EventArgs args)
		{
			Logger.Info ("OnQuitAction called - terminating application");

			// Save off the GroupWindow states
			if (groupWindows.Count > 0) {
				Logger.Info ("Saving the state of all group windows...");
				foreach (GroupWindow gw in groupWindows.Values) {
					gw.SaveState ();
				}
			}
			
			/*
			// Temporary disconnect from our single account			
			if (conn != null && conn.Connected == true) {
				conn.Disconnect ();
				conn = null;
			}
			*/

			if (tapConnection != null)			
				tapConnection.Disconnect ();

			// Unauthenticate all accounts
			AccountManagement.Stop ();
			
			// Shutdown the messaging engine
			//MessageEngine.Stop ();

			// Shutdown all the telepathy providers that were loaded
			//TelepathyProviderFactory.Stop ();
			
			// Shutdown the message log store
			//MessageStore.Stop ();
			
			Gtk.Main.Quit ();
			//program.Quit (); // Should this be called instead?
		}
		
		private void OnShowPreferencesAction (object sender, EventArgs args)
		{
			if (preferencesDialog == null) {
				preferencesDialog = new PreferencesDialog ();
				preferencesDialog.Response += OnPreferencesDialogResponse;
			}
			preferencesDialog.Present ();
		}
		
		private void OnPreferencesDialogResponse (object sender, ResponseArgs args)
		{
			PreferencesDialog dialog = sender as PreferencesDialog;
			string username = dialog.GoogleTalkUsername;
			string password = dialog.GoogleTalkPassword;
			
			string sipUsername = dialog.SipUsername;
			string sipPassword = dialog.SipPassword;
			
			((Gtk.Widget)sender).Destroy ();
			preferencesDialog = null;

			// If the username and password fields are not null, store them.
			if (username != null && password != null) {
				AccountManagement.SetGoogleTalkCredentialsHack (username, password);
			} else {
				Logger.Debug ("Cannot save empty credentials.  Reverting to old credential state.");
			}
			
			if (sipUsername != null && sipPassword != null) {
				AccountManagement.SetSipCredentialsHack (sipUsername, sipPassword);
			} else {
				Logger.Debug ("Cannot save empty SIP credentials.  Reverting to old credential state.");
			}

			// FIXME: Remove this eventually.  It's a hack to retry starting the AccountManager assuming the user entered credentials
			if (!initialized) {
				AccountManagement.Start ();
			}
		}
		
		private void OnGroupWindowDeleted (object sender, DeleteEventArgs args)
		{
Logger.Debug ("Application.OnGroupWindowDeleted");
			GroupWindow gw = sender as GroupWindow;
			gw.Hide ();
			
			if (groupWindows.ContainsKey (gw))
				groupWindows.Remove (gw);
			
			gw.Destroy ();
		}
		#endregion		


		#region Public Static Methods	
		public static void Main(string[] args)
		{
			try 
			{
				Utilities.SetProcessName ("Banter");

				BusG.Init ();

				application = GetApplicationWithArgs(args);

				Banter.Application.RegisterSessionManagerRestart (
					Environment.GetEnvironmentVariable ("RTC_PATH"),
					args);

				application.StartMainLoop ();
			} 
			catch (Exception e)
			{
				//TODO log
				Console.WriteLine (e.Message);
				Exit (-1);
			}
		}

		public static Application GetApplicationWithArgs(string[] args)
		{
			lock(locker)
			{
				if(application == null)
				{
					lock(locker)
					{
						application = new Application(args);
					}
				}
				return application;
			}
		}
		
		public static void RegisterSessionManagerRestart (string path, string[] args)
		{
			if (path == null) return;
			
			// If the session ends or the application crashes - restart
			Gnome.Client client = Gnome.Global.MasterClient ();
			client.RestartStyle = 
				Gnome.RestartStyle.IfRunning | Gnome.RestartStyle.Immediately;
			client.Die += OnSessionManagerDie;
			
			// Get the args for session restart... 	 
			string [] restart_args = new string [args.Length + 1]; 	 
			restart_args [0] = path;
			args.CopyTo (restart_args, 1);
			client.SetRestartCommand (restart_args.Length, restart_args);
		}
		
		public static void OnSessionManagerDie (object sender, EventArgs args)
		{
			// Don't let the exit signal run, which would cancel
			// session management.
			Application.Instance.QuitMainLoop ();
		}

		public static void CancelSessionManagerRestart ()
		{
			Gnome.Client client = Gnome.Global.MasterClient ();
			client.RestartStyle = Gnome.RestartStyle.IfRunning;
			client.Flush ();
		}
		
		public static void OnExitSignal (int signal)
		{
			// Don't auto-restart after exit/kill
			CancelSessionManagerRestart ();
			
			if (ExitingEvent != null) ExitingEvent (null, EventArgs.Empty);
			if (signal >= 0) System.Environment.Exit (0);
		}
		
		public static event EventHandler ExitingEvent = null;
		
		public static void Exit (int exitcode)
		{
			OnExitSignal (-1);
			System.Environment.Exit (exitcode);
		}
		
		public static Gdk.Pixbuf GetIcon (string resource_name, int size)
		{
			try {
				return Gtk.IconTheme.Default.LoadIcon (resource_name, size, 0);
			} catch (GLib.GException e) {
				Console.WriteLine (e.Message);
			}
			
			try {
				Gdk.Pixbuf ret = new Gdk.Pixbuf (null, resource_name + ".png");
				return ret.ScaleSimple (size, size, Gdk.InterpType.Bilinear);
			} catch {}
			
			return null;
		}
		#endregion


		#region Public Methods			
		public void StartMainLoop ()
		{
			program.Run ();
		}
		
		
		public Person GetPersonFromContact(Tapioca.Contact contact)
		{	
			Logger.Debug ("Application.GetPerson ({0})", contact.Uri);
			if(persons.ContainsKey(contact.Handle.Id)) {
				Person person = persons[contact.Handle.Id];
				Logger.Debug("Found the person: {0}", person.DisplayName);
				return person;
			}
			
			throw new ApplicationException("Fixme: The dude is not found, create real exceptions");
		}
		
		public void InitiateTapiocaChat(Person person)
		{
			Console.WriteLine("Called to initiate chat with: " + person.DisplayName);
			initiatedConversation = true;
			
			if(chatWindows.ContainsKey(person.Id))
				chatWindows[person.Id].Present();
			else
			{
				try
				{
					ChatWindow cw = new ChatWindow();
					cw.DeleteEvent += OnChatWindowDelete;
					chatWindows[person.Id] = cw;

					Conversation conversation = 
						new Conversation(tapConnection, person.Contact);
					conversation.CreateTextChannel();
					cw.Conversation = conversation;
					cw.Present();
				}
				catch (Exception es)
				{
					Console.WriteLine (es.Message);
					Console.WriteLine (es.StackTrace);
				}
			}
		}

		public void InitiateTapiocaVideoChat(Person person)
		{
			Console.WriteLine("Called to initiate Video chat with: " + person.DisplayName);
			initiatedConversation = true;
			
			VideoWindow meWindow = new VideoWindow();
			meWindow.Title = "Me";
			meWindow.Show();
			
			VideoWindow youWindow = new VideoWindow();
			youWindow.Title = "You";
			youWindow.Show();
			
			if (this.conversation == null)
			{
				this.conversation = new Conversation (tapConnection, person.Contact);
				this.conversation.SetVideoWindows (meWindow.WindowId, youWindow.WindowId);
				this.conversation.StartVideoChat ();
			}
		}		

		public void InitiateTapiocaAudioChat (Person person)
		{
			Console.WriteLine("Called to initiate Audio chat with: " + person.DisplayName);
			initiatedConversation = true;
			
			if (this.conversation == null)
			{
				this.conversation = new Conversation (tapConnection, person.Contact);
				this.conversation.StartAudioChat ();
			}
		}		
		
		private void OnChatWindowDelete (object sender, DeleteEventArgs args)
		{
			ChatWindow cw = sender as ChatWindow;
			if (chatWindows.ContainsValue (cw))
			{
				Person person = persons[cw.Conversation.PeerContact.Handle.Id];
				if(person != null)
					chatWindows.Remove (person.Id);
			}
		}

		public void QuitMainLoop ()
		{
			actionManager ["QuitAction"].Activate ();
		}
		#endregion
		
		class GroupWindowMenuItem : ImageMenuItem
		{
			GroupWindow groupWindow;
			
			public GroupWindowMenuItem (GroupWindow groupWindow) :
				base (groupWindow.SelectedGroup == null ?
						Catalog.GetString ("Everyone") :
						groupWindow.SelectedGroup.DisplayName)
			{
				this.groupWindow = groupWindow;
			}
			
			public GroupWindow GroupWindow
			{
				get { return groupWindow; }
			}
		}
	}
}
