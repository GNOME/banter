//***********************************************************************
// *  $RCSfile$ - Account.cs
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

using NDesk.DBus;
using org.freedesktop.DBus;
using Tapioca;

namespace Banter
{
	///<summary>
	/// Proxy Setting
	/// Note: valid for jabber accounts 
	/// need to research other account types such as SIP
	///</summary>
	public enum AccountProxySetting
	{
		NoProxy = 1,
		GlobalSetting,
		Http,
		Socks4,
		Socks5,
		EnvironmentSettings
	}
	
	///<summary>
	///	Abstract Account Class
	///</summary>
	public abstract class Account
	{
		protected string name;
		protected string username;
		protected string password;
		protected string server;
		protected string port;
		protected string protocol;
		protected Tapioca.Connection tapConnection = null;
		protected ManualResetEvent connectedEvent = null;
		protected Dictionary<string, object> options;	
		
		private bool autoLogin = false;
		private bool remember = false;
		private bool primary = false;
		private bool connected = false;

		public bool Connected
		{
			get {return connected;}
		}
		
		public bool AutoLogin
		{
			get {return autoLogin;}
			set {autoLogin = value;}
		}
		
		public bool RemberPassword
		{
			get {return remember;}
			set {remember = value;}
		}
		
		public bool Default
		{
			get {return primary;}
			set {primary = value;}
		}
		
		public Dictionary<string, object> Options
		{
			get {return options;}
		}
		
		public string Name
		{
			get {return name;}
			set {name = value;}
		}
		
		public string Username
		{
			get {return username;}
			set {username = value;}
		}
		
		public string Password
		{
			get {return password;}
			set {password = value;}
		}
		
		public string Protocol
		{
			get {return protocol;}
		}
		
		public string Server
		{
			get {return server;}
			set {server = value;}
		}

		public string Port
		{
			get {return port;}
			set {port = value;}
		}
		
		public Tapioca.Connection TapiocaConnection
		{
			get {return tapConnection;}
		}
		
		#region Constructors
		public Account ()
		{
			autoLogin = false;
			
			
		}
		
		public Account (string protocol)
		{
			this.protocol = protocol;
			
		}
		#endregion
		
		
		protected void OnConnectionChanged ( 
			Tapioca.Connection sender,
			Tapioca.ConnectionStatus status,
			Tapioca.ConnectionStatusReason reason)
		{
			Logger.Debug ("Account::OnConnectionChanged - called");
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
						
						Logger.Debug ("# Subscribed Contacts: {0}", tapConnection.ContactList.SubscribedContacts.Length);
						Logger.Debug ("# Known Contacts: {0}", tapConnection.ContactList.KnownContacts.Length);

						// Add me to the list
							
						try {
							string meKey = 
								ProviderUserManager.CreateKey (tapConnection.Info.Uri, this.protocol);
							ProviderUser me = new Banter.ProviderUser ();
							me.AccountName = this.Name;
							me.Alias = tapConnection.Info.Alias;
							me.Protocol = this.protocol;
							me.Uri = tapConnection.Info.Uri;
							me.IsMe = true;
							me.Presence = new Banter.Presence (Banter.PresenceType.Available);
							ProviderUserManager.AddProviderUser (meKey, me);
						} catch{}

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

							// FIXME:: this all needs to be take out when the PersonManager
							// is complete - it will handle all of this
							Person person = PersonStore.GetPersonByJabberId(c.Handle.Name);
							if(person == null) {
								person = new Person(c.Alias);
								person.JabberId = c.Handle.Name;
								PersonStore.AddPerson(person);
							}
								
							person.Contact = c;
							// END OF FIXME
							
							// update the provider user objects
							string key = 
								ProviderUserManager.CreateKey (c.Uri, Banter.ProtocolName.Jabber);
							ProviderUser providerUser =	CreateProviderUserFromContact (c);
							
							try {
								ProviderUserManager.AddProviderUser (key, providerUser);
							} catch{}
		                }
		                
						// FIXME - For now we have all caps		                
						tapConnection.Info.SetCapabilities (
							ContactCapabilities.Text |
							ContactCapabilities.Audio |
							ContactCapabilities.Video);
		                
		                // Setup handlers for incoming conversations
		                sender.ChannelCreated += OnNewChannel;
		                this.connected = true;
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
		
		private ProviderUser CreateProviderUserFromContact (Tapioca.Contact contact)
		{
			ProviderUser pu = new ProviderUser();
			pu.AccountName = this.Name;
			pu.Alias = contact.Alias;
			pu.Protocol = this.protocol;
			pu.Uri = contact.Uri;
			
			switch (contact.Presence)
			{
				case Tapioca.ContactPresence.Available:
				{
					pu.Presence = new Banter.Presence (Banter.PresenceType.Available);
					break;
				}
			
				case Tapioca.ContactPresence.Away:
				{
					pu.Presence = new Banter.Presence (Banter.PresenceType.Away);
					break;
				}
				case Tapioca.ContactPresence.Busy:
				{
					pu.Presence = new Banter.Presence (Banter.PresenceType.Busy);
					break;
				}
				case Tapioca.ContactPresence.Hidden:
				{
					pu.Presence = new Banter.Presence (Banter.PresenceType.Hidden);
					break;
				}
				case Tapioca.ContactPresence.Offline:
				{
					pu.Presence = new Banter.Presence (Banter.PresenceType.Offline);
					break;
				}
				
				case Tapioca.ContactPresence.XA:
				{
					pu.Presence = new Banter.Presence (Banter.PresenceType.XA);
					break;
				}
			}
			
			if (contact.PresenceMessage != null && pu.Presence != null)
				pu.Presence.Message = contact.PresenceMessage;

			return pu;
		}
		
		private void OnNewChannel (Tapioca.Connection sender, Tapioca.Channel channel)
		{
			Logger.Debug ("Account::OnNewChannel - called");
			
			Logger.Debug ("  incoming channel");
			Logger.Debug ("  type: {0}", channel.Type.ToString());
				
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
						Contact contact = txtChannel.RemoteTarget as Contact;
						Logger.Debug ("got contact: {0}", contact.Uri);
						
						// Do we already have a conversation setup with contact
						if (ConversationManager.Exist (contact) == true)
							return;
						
						Person peer = PersonStore.GetPersonByJabberId (contact.Uri);
						Logger.Debug ("got person");
						ChatWindow cw = null;
						
						Logger.Debug ("Peer: {0}", peer.Id);
						Logger.Debug ("Peer Name: {0}", peer.EDSContact.GivenName);
						
						if (ChatWindow.AlreadyExist (peer.Id) == true) { 
							Logger.Debug ("ChatWindow already exists with this peer");
							ChatWindow.PresentWindow (peer.Id);
						}	
						else {
							try
							{
								Logger.Debug ("creating conversation object");
								conversation = ConversationManager.Create (this, peer, false);
								conversation.SetTextChannel (txtChannel);
								Logger.Debug ("created new conversation object");
							
								cw = new ChatWindow (conversation);
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

						/*
						conversation = 
							new Conversation (sender, strmChannel.RemoteTarget as Contact);
					
						conversation.SetVideoWindows (meWindow.WindowId, youWindow.WindowId);
						conversation.SetStreamedMediaChannel (strmChannel);
						*/
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
		
		
		/*
		public virtual Member[] GetMembers ()
		{
			return null;
		}
		
		public virtual Member[] GetBannedMembers ()
		{
			return null;
		}
		*/
		
		/// <summary>
		///	Method to connect this account
		/// </summary>
		public void Connect (bool async)
		{
			tapConnection.Connect (Tapioca.ContactPresence.Available);
			connectedEvent.WaitOne (30000, true);
			Thread.Sleep (0);
		}
		
		/// <summary>
		/// Method to disconnect this account
		/// </summary>
		public void Disconnect ()
		{
			if (connected == true && tapConnection != null)
				tapConnection.Disconnect ();
				
			connected = false;
			tapConnection = null;
		}
	}
	
	/// <summary>
	/// Jabber Account
	/// </summary>
	public class JabberAccount : Account
	{
		private string resource = "Banter";
		private bool useTls = true;
		private bool forceOldSsl = false;
		private AccountProxySetting proxySetting = AccountProxySetting.GlobalSetting;
		
		public AccountProxySetting ProxySetting
		{
			get {return proxySetting;}
		}
		
		public string Resource
		{
			get {return resource;}
			set {resource = value;}
		}
		
		public bool Tls
		{
			get {return useTls;}
			set {useTls = value;}
		}
		
		public bool OldSsl
		{
			get {return forceOldSsl;}
			set {forceOldSsl = value;}
		}
		
		#region Constructors
		public JabberAccount (
				//TelepathyProvider provider,
				string accountname,
				string protocol,
				string username,
				string password,
				string server,
				string port,
				bool tls,
				bool oldSsl,
				bool ignoreSslErrors) : base (protocol)
		{
			this.name = accountname;
			this.username = username;
			this.password = password;
			this.server = server;
			this.port = port;
			this.useTls = tls;
			options = new Dictionary<string, object>();
			options.Add ("account", username);
			options.Add ("password", password);
			options.Add ("server", server);
			options.Add ("port", (uint) UInt32.Parse (port));
			
			if (oldSsl == true ) {
				options.Add ("old-ssl", true);
			} else {
				options.Add ("tls", tls);
			}
				
//			optionList.Add ("ignore-ssl-errors", ignoreSslErrors);
			options.Add ("ignore-ssl-errors", true);
			
			TapiocaSetup();
		}
		#endregion
		
		#region Private Methods
		private void TapiocaSetup()
		{
			ConnectionManagerFactory cmFactory = new ConnectionManagerFactory ();
              
            Logger.Debug ("user account: {0}", options["account"]);
              
			System.Collections.ArrayList ps = new System.Collections.ArrayList ();
			ps.Add (new ConnectionManagerParameter ("account", options["account"]));
			ps.Add (new ConnectionManagerParameter ("password", options["password"]));
			ps.Add (new ConnectionManagerParameter ("server", options["server"]));
			ps.Add (new ConnectionManagerParameter ("old-ssl", true));
			ps.Add (new ConnectionManagerParameter ("ignore-ssl-errors", true));
			ps.Add (new ConnectionManagerParameter ("port", (uint) 5223));

			ConnectionManagerParameter[] parameters = 
				(ConnectionManagerParameter[]) ps.ToArray (typeof (ConnectionManagerParameter));

			Logger.Debug ("Creating connection");
			ConnectionManager cm = cmFactory.GetConnectionManager (this.protocol);
			if (cm == null) {
				throw new ApplicationException ("Could not get a factory for this protocol");
			}
			
			tapConnection = cm.RequestConnection (this.protocol, parameters);
			if (tapConnection == null) {
				throw new ApplicationException ("Could not establish a telepathy connection for the specified protocol");
			}
		
			connectedEvent = new ManualResetEvent (false);
			tapConnection.StatusChanged += OnConnectionChanged;
		}
		#endregion
		
		#region Public Methods
		#endregion
	}

	/// <summary>
	/// Sip Account
	/// </summary>
	public class SipAccount : Account
	{
		private int registrationTimeout = 3600;
		private string domain;
		
		public string Domain
		{
			get {return domain;}
			set {domain = value;}
		}
		
		public string Registrar
		{
			get {return Server;}
			set {Server = value;}
		}
		
		public int RegistrationTimeout
		{
			get {return registrationTimeout;}
			set {registrationTimeout = value;}
		}
	}
}