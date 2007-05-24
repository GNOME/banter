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
using org.freedesktop.Telepathy;
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
		protected string busName;
		protected ConnectionInfo connInfo;
		protected IConnection tlpConnection;
		protected IConnectionManager connManager;

		// Connected Handlers
		protected bool aliasConnected = false;
		protected bool avatarsConnected = false;
		protected bool channelConnected = false;
		protected bool presenceConnected = false;
		protected bool messagingConnected = false;
		
		// Supported interfaces
		protected bool aliasing = false;
		protected bool avatars = false;
		protected bool capabilities = false;
		protected bool forwarding = false;
		protected bool renaming = false;
		protected bool presence = false;
		protected bool privacy = false;

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
		
		public string BusName
		{
			get {return busName;}
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
		
		public org.freedesktop.Telepathy.IConnection TlpConnection
		{
			get {return tlpConnection;}
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

		/// <summary>
		/// This method should be called immediately
		/// after a successful connect as other private
		/// methods depend on it
		/// </summary>
		protected void SetupSupportedInterfaces ()
		{
			foreach (string iname in tlpConnection.Interfaces)
			{
				switch (iname)
				{
					case "org.freedesktop.Telepathy.Connection.Interface.Aliasing":
						aliasing = true;
						break;
					case "org.freedesktop.Telepathy.Connection.Interface.Avatars":
						avatars = true;
						break;
					case "org.freedesktop.Telepathy.Connection.Interface.Capabilities":
						capabilities = true;
						break;
					case "org.freedesktop.Telepathy.Connection.Interface.Forwarding":
						forwarding = true;
						break;
					case "org.freedesktop.Telepathy.Connection.Interface.Renaming":
						renaming = true;
						break;
					case "org.freedesktop.Telepathy.Connection.Interface.Presence":
						presence = true;
						break;
					case "org.freedesktop.Telepathy.Connection.Interface.Privacy":
						privacy = true;
						break;
					default:
						Logger.Debug ("Interface {0} is unknown", iname);
						break;
				}
			}
		}

		/// <summary>
		/// Telepathy callback when state is changing on the connection
		/// </summary>
		protected void OnConnectionStateChanged (
			org.freedesktop.Telepathy.ConnectionStatus status, 
			org.freedesktop.Telepathy.ConnectionStatusReason reason)
		{
			Logger.Debug ("Connection state changed, Status: {0}, Reason: {1}", status, reason);
			
			switch (status)
			{
				case org.freedesktop.Telepathy.ConnectionStatus.Connecting:
				{
					break;
				}
				
				case org.freedesktop.Telepathy.ConnectionStatus.Connected:
				{
					Logger.Debug ("  in connected");
					
					try
					{
						SetupSupportedInterfaces ();
						SetupMe ();
						SetupProviderUsers ();
						AdvertiseCapabilities ();
						
						/*
						Logger.Debug ("ME - uri: {0}", tlpConnection.Status);
						Logger.Debug ("ME - alias: {0}", tapConnection.Info.Alias);
						Logger.Debug ("ME - caps: {0}", tapConnection.Info.Capabilities.ToString());
						Logger.Debug ("ME - avatar token: {0}", tapConnection.Info.CurrentAvatarToken);
						
						Logger.Debug ("# Subscribed Contacts: {0}", tapConnection.ContactList.SubscribedContacts.Length);
						Logger.Debug ("# Known Contacts: {0}", tapConnection.ContactList.KnownContacts.Length);
						*/

						// Add me to the list
							
	
						/*
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
						*/
							
		                // Setup handlers for incoming conversations
		                //sender.ChannelCreated += OnNewChannel;
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
				
				case org.freedesktop.Telepathy.ConnectionStatus.Disconnected:
				{
					/*
					DisconnectHandlers ();
					if (tlpConnection != null) {
						tlpConnection.Disconnect();
						tlpConnection = null;
					}
					*/
						
					break;
				}
			}
			
		}

		protected void AdvertiseCapabilities ()
		{
			// FIXME - For now we have all caps
			LocalCapabilityInfo[] info = new LocalCapabilityInfo[2];
			info[0].ChannelType = (string) org.freedesktop.Telepathy.ChannelType.Text;
			info[1].ChannelType = (string) org.freedesktop.Telepathy.ChannelType.StreamedMedia;
			info[1].TypeSpecificFlags = 
				ChannelMediaCapability.Audio | ChannelMediaCapability.Video;
		
			tlpConnection.AdvertiseCapabilities (info, new string[0]);						
		}
		
		protected void SetupMe ()
		{
			string[] aliasNames = null;
	
			try {

				string uri = options["account"] as string;
				if (aliasing == true) {
					uint[] ids = { tlpConnection.SelfHandle };
					aliasNames = tlpConnection.RequestAliases (ids);
				}	

				string meKey = ProviderUserManager.CreateKey (uri, protocol);
				ProviderUser me = null;				
				try {
					me = ProviderUserManager.GetProviderUser (meKey);
					me.TlpConnection = tlpConnection;
					me.ID = tlpConnection.SelfHandle;
					me.Protocol = protocol;
					me.IsMe = true;
					me.Presence = new Banter.Presence (Banter.PresenceType.Available);
					if (aliasNames != null)
						me.Alias = aliasNames[0];
				} 
				catch{}
					
				if (me == null) {
					try {
					
						me = new Banter.ProviderUser (tlpConnection, tlpConnection.SelfHandle);
						me.Uri = uri;
						me.AccountName = this.Name;
						me.Protocol = this.protocol;
						me.IsMe = true;
						me.Presence = new Banter.Presence (Banter.PresenceType.Available);
						if (aliasNames != null)
							me.Alias = aliasNames[0];
						ProviderUserManager.AddProviderUser (meKey, me);
					} catch{}
				}
				
				Logger.Debug ("Me Alias: {0}", me.Alias);
			} catch (Exception sm) {
				Logger.Debug ("Failed in SetupMe");
				Logger.Debug (sm.Message);
				Logger.Debug (sm.StackTrace);
			}
		}
		
		protected void SetupProviderUsers ()
		{
			Logger.Debug ("trying to get the buddy list");
			//string[] args = {"subscribe"};
			string[] args = {"known"};
			uint[] memberHandles = tlpConnection.RequestHandles (HandleType.List, args);
			ObjectPath op = 
				tlpConnection.RequestChannel (
					org.freedesktop.Telepathy.ChannelType.ContactList, 
					HandleType.List, 
					memberHandles[0], 
					true);
					
			Logger.Debug ("#handles: {0}", memberHandles.Length);
					
			IChannelGroup cl = Bus.Session.GetObject<IChannelGroup> (connInfo.BusName, op);
			string[] members = tlpConnection.InspectHandles (HandleType.Contact, cl.Members);
			
			string[] aliasNames = null;
			if (aliasing == true) {
				aliasNames = tlpConnection.RequestAliases (cl.Members);
				Logger.Debug ("# returned aliases: {0}", aliasNames.Length);	
			}	

			Logger.Debug ("# of known contacts: {0}", members.Length);
			for (int i = 0; i < members.Length; i++) {
				Logger.Debug ("MemberID: {0} Member: {1}", cl.Members[i], members[i]);
				
				// update the provider user objects
				string key = ProviderUserManager.CreateKey (members[i], protocol);
						
				ProviderUser providerUser = null;
				try {
					providerUser = ProviderUserManager.GetProviderUser (key);
					providerUser.TlpConnection = tlpConnection;
					providerUser.ID = cl.Members[i];
					if (aliasing == true && aliasNames != null)
						providerUser.Alias = aliasNames[i];
				} 
				catch (Exception fff) {
					Logger.Debug ("Failed to get ProviderUser {0}", key);
					Logger.Debug (fff.Message);
				}
					
				if (providerUser == null) {
					try {
						providerUser = new ProviderUser (tlpConnection, cl.Members[i]);
						providerUser.AccountName = this.Name;
						providerUser.Protocol = this.Protocol;
						providerUser.Uri = members[i];
						if (aliasing == true && aliasNames != null)
							providerUser.Alias = aliasNames[i];
						
						ProviderUserManager.AddProviderUser (key, providerUser);
					} catch{}
				}
			}
			
			ConnectHandlers ();
			if (presence == true)
				tlpConnection.RequestPresence (cl.Members);
		}
		
		protected void ConnectHandlers ()
		{
			Logger.Debug ("Account::ConnectHandlers");
			
			tlpConnection.NewChannel += OnNewTPChannel;
			channelConnected = true;
			
			if (aliasing == true) {
				tlpConnection.AliasesChanged += OnAliasesChanged;
				aliasConnected = true;
			}
			
			if (presence == true) {
				tlpConnection.PresenceUpdate += OnPresenceUpdate;
				presenceConnected = true;
			}
			
			if (avatars == true) {
				tlpConnection.AvatarUpdated += OnAvatarUpdated;
				avatarsConnected = true;
			}
		}
		
		protected void DisconnectHandlers ()
		{
			Logger.Debug ("Account::DisconnectHandlers");
			
			if (avatarsConnected == true) {
				tlpConnection.AvatarUpdated -= OnAvatarUpdated;
				avatarsConnected = false;
			}
			
			if (presenceConnected == true) {
				tlpConnection.PresenceUpdate -= OnPresenceUpdate;
				presenceConnected = false;
			}
			
			if (aliasConnected == true) {
				tlpConnection.AliasesChanged -= OnAliasesChanged;
				aliasConnected = false;
			}
			
			if (channelConnected == true) {
				tlpConnection.NewChannel -= OnNewTPChannel;
				channelConnected = false;
			}
		}
		
		/// <summary>
		///	Telepathy callback when a alias has changed on a contact
		/// </summary>
		private void OnAliasesChanged (AliasInfo[] aliases)
		{
			ProviderUser user;
			foreach (AliasInfo info in aliases) {
				user = ProviderUserManager.GetProviderUser (info.ContactHandle);
				if (user != null)
					user.Alias = info.NewAlias;
			}
		}
		
		/// <summary>
		///	Telepathy callback when an Avatar has changed
		/// </summary>
		private void OnAvatarUpdated (uint id, string token)
		{
			ProviderUser user = ProviderUserManager.GetProviderUser (id);
			if (user != null)
				user.AvatarToken = token;
		}
		
		private void OnAvatarUpdate (string avatarToken)
		{
			/*
			ProviderUser user;
			foreach (AliasInfo info in aliases) {
				user = ProviderUserManager.GetProviderUser (info.ContactHandle);
				if (user != null)
					user.Alias = info.NewAlias;
			}
			*/
		}
		
		/// <summary>
		/// Private method to convert telepathy presence information into
		/// a Banter.Presence object.  The ProviderUser object is updated with
		/// the new presence information which will call all agents registered
		/// for presence update
		/// </summary>
		private void UpdatePresence (ProviderUser user, string presence, string message)
		{
			Banter.Presence banterPresence = new Banter.Presence (Banter.PresenceType.Offline);
			if (message != null && message != String.Empty)
				banterPresence.Message = message;
				
			switch (presence)
			{
				case "available":
					banterPresence.Type = Banter.PresenceType.Available;
					break;
				case "away":
				case "brb":
					banterPresence.Type = Banter.PresenceType.Away;
					break;
				case "busy":
				case "dnd":
					banterPresence.Type = Banter.PresenceType.Busy;
					break;
				case "xa":
					banterPresence.Type = Banter.PresenceType.XA;
					break;
				case "hidden":
					banterPresence.Type = Banter.PresenceType.Hidden;
					break;
				case "offline":
				default:
					break;
			}

			user.Presence = banterPresence;
		}
	
		/// <summary>
		/// Telepathy callback when presence has changed
		/// </summary>
		private void OnPresenceUpdate (IDictionary<uint, PresenceUpdateInfo> infos)
		{
			ProviderUser user;
			foreach (KeyValuePair<uint, PresenceUpdateInfo> entry in infos)
			{
				user = ProviderUserManager.GetProviderUser (entry.Key);
				if (user == null) continue;
				
				foreach (KeyValuePair<string, IDictionary<string, object>> info in entry.Value.info)
				{
					string message = String.Empty;
					foreach (KeyValuePair<string, object> val in info.Value)
					{
						if (val.Key == "message")
							message = val.Value as String;
					}
					
					UpdatePresence (user, info.Key, message);
				}
			}
		}
		
		/// <summary>
		/// Callback when new channels are created
		/// </summary>
		private void OnNewTPChannel (
						ObjectPath channelPath,
						string channelType,
						HandleType handleType,
						uint handle,
						bool suppressHandler)
		{	
			Logger.Debug ("New Channel {0}", channelType);
			Logger.Debug ("Handle Type: {0}", handleType.ToString());
			Logger.Debug ("Handle: {0}", handle);
			Logger.Debug ("Suppress Handler: {0}", suppressHandler);
			
			Conversation conversation = null;
			switch (channelType)
			{
				case org.freedesktop.Telepathy.ChannelType.Text:
				{
					if (handle == 0)
						return;
						
					// Check if we have an existing conversation with the peer user
					ProviderUser pu = null;
					try {
						pu = ProviderUserManager.GetProviderUser (handle);
					} catch{}
					
					if (pu == null) return;
					
					if (ConversationManager.Exist (pu) == true) {
						Logger.Debug ("An existing conversation with {0} already exists", pu.Uri);
						return;
					}
					
					Person peer = PersonStore.GetPersonByJabberId (pu.Uri);
					ChatWindow cw = null;
					
					Logger.Debug ("Peer: {0}", peer.Id);
					Logger.Debug ("Peer Name: {0}", peer.EDSContact.GivenName);
					
					if (ChatWindow.AlreadyExist (peer.Id) == true) { 
						Logger.Debug ("ChatWindow already exists with this peer");
						ChatWindow.PresentWindow (peer.Id);
					} else {
						try
						{
							Logger.Debug ("creating conversation object");
							conversation = ConversationManager.Create (this, peer, false);
							IChannelText txtChannel = 
								Bus.Session.GetObject<IChannelText> (busName, channelPath);
							
							conversation.SetTextChannel (txtChannel);
							Logger.Debug ("created new conversation object");
						
							cw = new ChatWindow (conversation);
							cw.Present();
						}
						catch (Exception es)
						{
							Logger.Debug (es.Message);
							Logger.Debug (es.StackTrace);
						}
					}
					break;
				}
				
				case org.freedesktop.Telepathy.ChannelType.StreamedMedia:
				{
					/*
					IChannelStreamedMedia ichannel = 
						Bus.Session.GetObject<IChannelStreamedMedia> (
							account.TelepathyBusName, 
							channel);
					
					if (handle == 0) {
						if (ichannel.RemotePendingMembers.Length > 0)
							handle = ichannel.RemotePendingMembers[0];
						else if (ichannel.Members.Length > 0)
							handle = ichannel.Members[0];
						else
							return;
					}
						
					uint[] ids = {handle};
					Contact contact = contact_list.ContactLookup (ids[0]);
					if (contact == null)
						return;
						
					StreamChannel stream_channel = new StreamChannel (this, ichannel, contact, ServiceName, channel);	
					if ((stream_channel != null) && (!channel_list.Contains (stream_channel)))
						channel_list.Add (stream_channel);

					if (ChannelCreated != null)
						ChannelCreated (this, (Channel) stream_channel);
					*/
					break;
				}	
				default:
					break;
			}
		}

		/*		
		/// <summary>
		/// Callback from Tapioca when connection status changes
		/// </summary>
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
							// END OF FIXME
							
							// update the provider user objects
							string key = 
								ProviderUserManager.CreateKey (c.Uri, this.Protocol);
								
							ProviderUser providerUser = null;
							try {
								providerUser = ProviderUserManager.GetProviderUser (key);
								UpdateProviderUserFromContact (providerUser, c);
							} 
							catch (Exception fff) {
								Logger.Debug ("Failed to get ProviderUser {0}", key);
								Logger.Debug (fff.Message);
							}
							
							if (providerUser == null) {
								try {
									providerUser = CreateProviderUserFromContact (c);
									ProviderUserManager.AddProviderUser (key, providerUser);
									
								} catch{}
							}
							
							providerUser.Contact = c;
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
		*/
		
		/*
		private void UpdateProviderUserFromContact (ProviderUser pu, Tapioca.Contact contact)
		{
			pu.AccountName = this.Name;
			pu.Alias = contact.Alias;
			pu.Protocol = this.protocol;
			pu.Uri = contact.Uri;
			
			switch (contact.Presence)
			{
				case Tapioca.ContactPresence.Available:
				{
					pu.Presence.Type = Banter.PresenceType.Available;
					break;
				}
			
				case Tapioca.ContactPresence.Away:
				{
					pu.Presence.Type = Banter.PresenceType.Away;
					break;
				}
				case Tapioca.ContactPresence.Busy:
				{
					pu.Presence.Type = Banter.PresenceType.Busy;
					break;
				}
				case Tapioca.ContactPresence.Hidden:
				{
					pu.Presence.Type = Banter.PresenceType.Hidden;
					break;
				}
				case Tapioca.ContactPresence.Offline:
				{
					pu.Presence.Type = Banter.PresenceType.Offline;
					break;
				}
				
				case Tapioca.ContactPresence.XA:
				{
					pu.Presence.Type = Banter.PresenceType.XA;
					break;
				}
			}
			
			if (contact.PresenceMessage != null && pu.Presence != null)
				pu.Presence.Message = contact.PresenceMessage;
		}
		
		/// <summary>
		/// method to create a provider user object from a 
		/// tapioca contact object
		/// </summary>
		private ProviderUser CreateProviderUserFromContact (Tapioca.Contact contact)
		{
			ProviderUser pu = new ProviderUser();
			UpdateProviderUserFromContact (pu, contact);
			return pu;
		}
		*/
		
		/// <summary>
		/// Tapioca callback when a new channel is created on a connection
		/// </summary>
		private void OnNewChannel (Tapioca.Connection sender, Tapioca.Channel channel)
		{
			Logger.Debug ("Account::OnNewChannel - called");
			
			Logger.Debug ("  incoming channel");
			Logger.Debug ("  type: {0}", channel.Type.ToString());
				
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
						ProviderUser pu = null;
						try {
							string key = ProviderUserManager.CreateKey (contact.Uri, sender.Protocol);
							pu = ProviderUserManager.GetProviderUser (key);
						} catch{}
						
						if (pu == null) return;
						
						if (ConversationManager.Exist (pu) == true)
							return;
						
						Person peer = PersonStore.GetPersonByJabberId (pu.Uri);
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
								//conversation.SetTextChannel (txtChannel);
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
		
		/// <summary>
		///	Method to connect this account
		/// </summary>
		public void Connect (bool async)
		{
			// An instance of a telepathy connection should exist
			// it just won't be connected.
			if (tlpConnection != null )
				tlpConnection.Connect ();
				
			/*	
			else
				tapConnection.Connect (Tapioca.ContactPresence.Available);
			*/
			connectedEvent.WaitOne (30000, true);
			Thread.Sleep (0);
		}
		
		/// <summary>
		/// Method to disconnect this account
		/// </summary>
		public void Disconnect ()
		{
			if (connected == true && tlpConnection != null) {
				DisconnectHandlers ();
				Logger.Debug ("Calling telepathy disconnect");
				tlpConnection.Disconnect ();
				Logger.Debug ("out of telepathy disconnect");
			} else if (connected == true && tapConnection != null)
				tapConnection.Disconnect ();
				
			connected = false;
			tapConnection = null;
			tlpConnection = null;
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
			
			TelepathyConnectionSetup ();
			//TapiocaSetup();
		}
		#endregion
		
		#region Private Methods
		private void TapiocaSetup()
		{
			ConnectionManagerFactory cmFactory = new ConnectionManagerFactory ();
              
            Logger.Debug ("user account: {0}", options["account"]);
              
			List<ConnectionManagerParameter> ps = new List<ConnectionManagerParameter> ();
			ps.Add (new ConnectionManagerParameter ("account", options["account"]));
			ps.Add (new ConnectionManagerParameter ("password", options["password"]));
			ps.Add (new ConnectionManagerParameter ("server", options["server"]));
			ps.Add (new ConnectionManagerParameter ("old-ssl", true));
			ps.Add (new ConnectionManagerParameter ("ignore-ssl-errors", true));
			ps.Add (new ConnectionManagerParameter ("port", (uint) 5223));

			ConnectionManagerParameter[] parameters = ps.ToArray ();

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
			//tapConnection.StatusChanged += OnConnectionChanged;
		}

		/// <summary>
		/// Check if we have an existing connection in gabble
		/// assumes connection info is valid
		/// </summary>
		private bool CheckForExistingConnection()
		{
			return false;
			
			IConnection connection = null;
			string existingBusName = 
				"org.freedesktop.Telepathy.ConnectionManager.gabble/";
			string objectPath = existingBusName.Replace ('.', '/');	
				
			existingBusName += options["account"];
			objectPath += options["account"];
			
			Logger.Debug ("Existing BusName:{0}", existingBusName);
			Logger.Debug ("Object Path: {0}", objectPath);
			ObjectPath op = new ObjectPath (objectPath);
			
			connection = 
				Bus.Session.GetObject<IConnection> (existingBusName, op);
				
			if (connection != null) {
				Logger.Debug ("Found an existing connection");
				tlpConnection = connection;
				
				Logger.Debug ("Connection Status: {0}", tlpConnection.Status.ToString());
				//if (tlpConnection.Status == org.freedesktop.Telepathy.IConnection.Status.Connected) {
					SetupSupportedInterfaces ();
					SetupProviderUsers ();
					AdvertiseCapabilities ();
				//}	
				
			} else {
				Logger.Debug ("Failed to find an existing connection");
			}
			
			return (connection != null) ? true : false;
		}
		
		private bool TelepathyConnectionSetup ()
		{
			if (this.protocol == Banter.ProtocolName.Jabber) {
				busName = "org.freedesktop.Telepathy.ConnectionManager.gabble";
			}
		
			string objectPath = "/" + busName.Replace('.', '/');
			Logger.Debug ("connectionPath = " + busName);
			
			try {
				
				if (CheckForExistingConnection() == false) {
					//get connection manager from dbus
					connManager = 
						Bus.Session.GetObject<IConnectionManager> (
							busName,
							new ObjectPath (objectPath));

				    if (connManager == null) {
			    	  	Logger.Debug ("Unable to establish a connection with the telepathy-sharp connection manager");
						return false;
					}

					connInfo = connManager.RequestConnection (this.protocol, this.options);
					tlpConnection = 
						Bus.Session.GetObject<IConnection> (connInfo.BusName, connInfo.ObjectPath);
					tlpConnection.StatusChanged += OnConnectionStateChanged;
				}
				
				connectedEvent = new ManualResetEvent (false);
				return true;
			} catch (Exception e) {
				Logger.Debug ("Exception while connecting to: " + busName);
				Logger.Debug (e.Message);
			}
			
			return false;
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