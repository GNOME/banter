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

using Gtk;
using Gnome;
using Mono.Unix;
using Mono.Unix.Native;
using Notifications;

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
		private PersonSync personSync;
		private Egg.TrayIcon trayIcon;
		//private NotificationArea tray;
		private Gdk.Pixbuf appPixBuf;
		private Gnome.Program program = null;
		private PreferencesDialog preferencesDialog;
		
		private Dictionary<GroupWindow, GroupWindow> groupWindows;
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
		
		public static Gdk.Pixbuf AppIcon
		{
			get { return Instance.appPixBuf; }
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
			Logger.Debug ("Banter::Application::Init - called");
			initialized = false;
			Gtk.Application.Init ();
			program = 
				new Gnome.Program (
						"banter",
						Defines.Version,
						Gnome.Modules.UI,
						args);

			// Initialize the singleton classes to be used in the app
			PersonManager.Instance.Init();
			ChatWindowManager.Instance.Init();
			NotificationManager.Instance.Init();
			
			// Create and start up the Person Sync
			personSync = new PersonSync();
			personSync.Start();
			
			actionManager = new ActionManager ();
			actionManager.LoadInterface ();
			SetUpGlobalActions ();
			
			// Initialize the Theme Manager
			ThemeManager.Init ();

			//tray = new NotificationArea("RtcApplication");
			SetupTrayIcon();

			application = this;
			
			groupWindows = new Dictionary<GroupWindow, GroupWindow> ();

			GLib.Idle.Add(InitializeIdle);
		}
	
		private void SetupTrayIcon ()
		{
			Logger.Debug ("Creating TrayIcon");
			
			EventBox eb = new EventBox();
			appPixBuf = GetIcon ("banter-22", 22);
			eb.Add(new Image (appPixBuf)); 
			//new Image(Gtk.Stock.DialogWarning, IconSize.Menu)); // using stock icon

			// hooking event
			eb.ButtonPressEvent += new ButtonPressEventHandler (this.OnImageClick);
			trayIcon = new Egg.TrayIcon("RtcApplication");
			trayIcon.Add(eb);
			// showing the trayicon
			trayIcon.ShowAll();			
		}
		
		private bool InitializeIdle ()
		{
			Logger.Debug ("Initialize_Idle - called");

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
			AccountManagement.Initialize ();
			
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
			Logger.Debug ("OnAccountManagementReady() called");
			initialized = true;
			
			GLib.Idle.Add (OpenSavedGroupWindows);
		}
		
		private bool OpenSavedGroupWindows ()
		{
			GroupWindow gw;
			
			try {
				string[] groupWindowIds = null;
				try {
					// Use a try/catch here because gconf can puke on zero-length string[] settings.
					groupWindowIds = Preferences.Get (Preferences.GroupWindows) as String[];
				} catch {}
				if (groupWindowIds == null || groupWindowIds.Length == 0) {
					// Create a new Group Window with Everyone selected
					gw = new GroupWindow ();
					groupWindows [gw] = gw;
					gw.DeleteEvent += OnGroupWindowDeleted;
					gw.ShowAll ();
				} else {
					foreach (string id in groupWindowIds) {
						gw = new GroupWindow (id);
						
						groupWindows [gw] = gw;
						gw.DeleteEvent += OnGroupWindowDeleted;
						gw.ShowAll ();
					}
				}
			} catch (Exception osgw) {
				Logger.Debug (osgw.Message);
				Logger.Debug (osgw.StackTrace);
			}
			
			return false; // Prevent GLib.Idle from calling this method again
		}

		private void OnImageClick (object o, ButtonPressEventArgs args) // handler for mouse click
		{
			if(!initialized)
				return;
				
			if (args.Event.Button == 1) {
				Logger.Debug ("left button clicked");
			} else if (args.Event.Button == 3) //right click
   			{
   				// FIXME: Eventually get all these into UIManagerLayout.xml file
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
			Logger.Debug ("Selected Accounts");
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

			// stop the personSync
			personSync.Stop();
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
			
			AccountManagement.Shutdown();
			Logger.Debug ("Finished Disconnecting accounts");

			// Shutdown the messaging engine
			//MessageEngine.Stop ();
			
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
			if (!initialized && AccountManagement.InitializedFinished() == false)
			{
				AccountManagement.Initialize ();
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
		
		
		// <summary>
		// Connects a Notification to the application icon in the notification area and shows it.
		// </summary>
		public static void ShowAppNotification(Notification notification)
		{
			notification.AttachToWidget(Banter.Application.Instance.trayIcon);
			notification.Show();
		}
		#endregion


		#region Public Methods			
		public void StartMainLoop ()
		{
			program.Run ();
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
