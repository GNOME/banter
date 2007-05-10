//***********************************************************************
// *  $RCSfile$ - Connection.cs
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

namespace Novell.Rtc
{
	public enum ConnectionStatus : uint
	{
		Connected = 0,
		Connecting = 1,
		Disconnected = 2
	}
	
	public enum ConnectionStatusReason : uint
	{
		NoneSpecified = 0,
		Requested = 1,
		NetworkError = 2,
		AuthenticationFailed = 3,
		EncryptionError = 4,
		NameInUse = 5,
		CertificateNotProvided = 6,
		CertificateUntrusted = 7,
		CertificateExpired = 8,
		CertificateNotActivated = 9,
		CertificateHostnameMismatch = 10,
		CertificateFingerPrintMismatch = 11,
		CertificateSelfSigned = 12,
		CertificateOtherError = 13
	}
	
	public enum ConnectionPrivacy : uint
	{
		AllowAll = 1,
		AllowSpecified,
		AllowSubscribed
	}
	
	public delegate void ConnectionStatusChangedHandler (Connection sender, ConnectionStatus status, ConnectionStatusReason reason);
	public delegate void IncomingConversationHandler (Conversation conversation);	
	public delegate void IncomingMediaConversationHandler (
								Member member, 
								IChannelStreamedMedia ichannel,
								ObjectPath channelPath);

	//public delegate void ConnectionChannelCreatedHandler (Connection sender, Novell.Rtc.Channel channel);

	public class Connection : IDisposable
	{
		// Public Events
		public event ConnectionStatusChangedHandler StatusChanged;
		public event IncomingConversationHandler IncomingConversation;
		public event IncomingMediaConversationHandler IncomingMediaConversation;
		
		//public event ConnectionChannelCreatedHandler ChannelCreated;

		bool connected = false;
		
		// Handlers
		bool aliasConnected = false;
		bool avatarConnected = false;
		bool channelConnected = false;
		bool presenceConnected = false;
		
		// Supported interfaces
		bool aliasing = false;
		bool avatars = false;
		bool capabilities = false;
		bool forwarding = false;
		bool renaming = false;
		bool presence = false;
		bool privacy = false;
		
		private IConnectionManager connManager;
		private string myAlias = String.Empty;
		private Account account;
		private IConnection tlpConnection;
		private ConnectionInfo connectionInfo;
		private ManualResetEvent connectedEvent;
		
		private System.Collections.Hashtable activeMembers;
		// private System.Collections.Hashtable bannedMembers;
		private System.Collections.Generic.Dictionary <uint, Conversation> conversations = null;
		
		// fix
		public Member me;
		
		//System.Collections.ArrayList channelList;
		ConnectionStatus status;
		//MemberPresence memberPresence;

		#region Internal Properties
		internal IConnection TlpConnection
		{
			get { return this.tlpConnection; }
		}
		
		internal ConnectionInfo TlpConnectionInfo
		{
			get { return this.connectionInfo;}
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Property to get the user's Alias name
		/// Note: not valid unless the connection is
		/// in the connected state
		/// </summary>
		public string Alias
		{
			get {return myAlias;}
		}
	
		/// <summary>
		/// Property to get the connection's 
		/// "connected" state
		/// </summary>
		public bool Connected
		{
			get {return connected;}
		}
		
		public Account Account
		{
			get {return account;}
		}

		/// <summary>
		/// Property to get the user or screenname
		/// for this connection.  The name is valid
		/// in all connection states since the name
		/// comes forward from the configured account.
		/// </summary>
		public string Username
		{
			get {return this.Account.Username;}
		}

		public ConnectionStatus Status
		{
			get {return status;}
		}
		
		public bool SupportAliasing
		{
			get { return aliasing; }
		}

		public bool SupportAvatars
		{
			get { return avatars; }
		}

		public bool SupportCapabilities
		{
			get { return capabilities; }
		}

		public bool SupportForwarding
		{
			get { return forwarding; }
		}

		public bool SupportRenaming
		{
			get { return renaming; }
		}

		public bool SupportPresence
		{
			get {return presence; }
		}

		public bool SupportPrivacy
		{
			get {return privacy; }
		}
		#endregion

		/*
		public Channel CreateChannel (ChannelType type, ChannelTarget target)
		{
			if (status != ConnectionStatus.Connected) return null;

			switch (type)
			{
				case ChannelType.Text:
				{
					ObjectPath op = tlpConnection.RequestChannel (org.freedesktop.Telepathy.ChannelType.Text, target.Handle.Type, target.Handle.Id, true);
					IChannelText channel = Bus.Session.GetObject<IChannelText> (ServiceName, op);
					TextChannel textChannel = new TextChannel (this, channel, (Contact) target, ServiceName, op);

					if (textChannel != null)
						channelList.Add (textChannel);

					if (ChannelCreated != null)
						ChannelCreated (this, (Channel) text_channel);

					return (Channel) textChannel;
				}
				case ChannelType.StreamedMedia:
				{
					ObjectPath op = 
						tlpConnection.RequestChannel (
							org.freedesktop.Telepathy.ChannelType.StreamedMedia, 
							target.Handle.Type, 
							target.Handle.Id,
							true);
							
					IChannelStreamedMedia channel = Bus.Session.GetObject<IChannelStreamedMedia> (ServiceName, op);
					
					StreamChannel streamChannel = 
						new StreamChannel (this, channel, (Contact) target, ServiceName, op);
					if (streamChannel != null)
						channelList.Add (streamChannel);

					if (ChannelCreated != null)
						ChannelCreated (this, (Channel) streamChannel);

					return (Channel) streamChannel;
				}
				default:
					throw new NotImplementedException("Still needs to be designed");
			}
		}
		*/

		internal Connection (Account account)
		{
			this.account = account;
			this.connectedEvent = new ManualResetEvent (false);
			this.activeMembers = new System.Collections.Hashtable();
			this.conversations = new System.Collections.Generic.Dictionary<uint, Conversation>();

			if (this.account.TelepathyBusName == null ||
					this.account.TelepathyObjectPath == null ||
					this.account.Options == null) {
		    	throw new ApplicationException ("Connectection constructor failed: No configured Telepathy provider");
			}
			
			//get connection manager from dbus
			connManager = 
				Bus.Session.GetObject<IConnectionManager> (
					account.TelepathyBusName,
					new ObjectPath (account.TelepathyObjectPath));

		    if (connManager == null) {
		    	throw new ApplicationException (
		    		String.Format (
		    			"Failed to get a dbus connection {0}",
		    			account.TelepathyBusName));
			}
			
			connectionInfo = connManager.RequestConnection (account.Protocol, account.Options);
			tlpConnection = 
				Bus.Session.GetObject<IConnection> (connectionInfo.BusName, connectionInfo.ObjectPath);
				
			if (tlpConnection == null) {
		    	throw new ApplicationException (
		    		String.Format (
		    			"Failed to get a telepath connection to {0}",
		    			account.TelepathyBusName));
			}
		}

		#region Private Methods
		/// <summary>
		/// Delegate method called when connection state changes
		/// </summary>
		private void OnConnectionStateChanged (
						org.freedesktop.Telepathy.ConnectionStatus status,
						org.freedesktop.Telepathy.ConnectionStatusReason reason)
		{
			Console.WriteLine ("OnConnectionStateChanged - called");
			this.status = (ConnectionStatus) status;
			switch (status) {
				case org.freedesktop.Telepathy.ConnectionStatus.Connected:
					Logger.Debug ("  connected");
					SetupSupportedInterfaces ();
					ConnectHandlers ();
					SetMeUp ();
					connected = true;
					DebugDisplayAllowedMembers ();
					LoadActiveMembers ();
					connectedEvent.Set ();
					break;
				case org.freedesktop.Telepathy.ConnectionStatus.Disconnected:
					Logger.Debug ("  disconnected");
					DisconnectHandlers ();
					Close ();
					connected = false;
					break;
				case org.freedesktop.Telepathy.ConnectionStatus.Connecting:
					Logger.Debug ("  connecting");
					break;
				default:
					break;
			}

			// Call all registered handlers
			if (StatusChanged != null) {
				StatusChanged (this, (ConnectionStatus) status, (ConnectionStatusReason) reason);
			}			
		}

		private void Close ()
		{
			Console.WriteLine ("Connection::Close - called");
			//channelList.Clear ();
			//memberList.Dispose ();
			me.UpdatePresence ("offline", String.Empty);
			
			aliasing = false;
			avatars = false;
			capabilities = false;
			forwarding = false;
			renaming = false;
			presence = false;
			privacy = false;
		}
		
		/// <summary>
		/// Callback when new channels are created
		/// </summary>
		private void OnNewChannel (
						ObjectPath channelPath,
						string channelType,
						HandleType handleType,
						uint handle,
						bool suppressHandler)
		{	
			Console.WriteLine ("New Channel {0}", channelType);
			Novell.Rtc.Conversation conversation;
			switch (channelType)
			{
				case org.freedesktop.Telepathy.ChannelType.Text:
				{
					Console.WriteLine ("Handle Type: {0}", handleType.ToString());
					Console.WriteLine ("Handle: {0}", handle);
					Console.WriteLine ("Suppress Handler: {0}", suppressHandler);
					if (handle == 0)
						return;
						
					if (conversations.TryGetValue (handle, out conversation) == false) {
						Member peer = this.MemberLookup (handle);
						if (peer != null) {
							conversation = new Conversation (this, this.me, peer);
							if (conversation != null) {
								conversations [handle] = conversation;
							}
							if (this.IncomingConversation != null) {
								this.IncomingConversation (conversation);
							}
						}
					} else {
						conversation = conversations[handle];
						if (this.IncomingConversation != null) {
							this.IncomingConversation (conversation);
						}
					}
						
					/*
					IChannelText ichannel = Bus.Session.GetObject<IChannelText> (ServiceName, channelPath);
					
					uint[] ids = {handle};
					Contact contact = contactList.ContactLookup (ids[0]);
					if (contact == null)
						return;
					TextChannel textChannel = new TextChannel (this, ichannel, contact, ServiceName, channel);	
					if ((textChannel != null) && (!channelList.Contains (textChannel)))
						channelList.Add (textChannel);

					if (ChannelCreated != null)
						ChannelCreated (this, (Channel) textChannel);						
					*/
					break;
				}
				
				case org.freedesktop.Telepathy.ChannelType.StreamedMedia:
				{
					if (handle != 0) return;
					
					IChannelStreamedMedia ichannel = 
						Bus.Session.GetObject<IChannelStreamedMedia> (
							connectionInfo.BusName,
							channelPath);
							
					if (ichannel.LocalPendingMembers.Length == 0) return;
					
					Logger.Debug ("Connection.OnNewChannel - Incoming media channel");
					
					// Handle should be myself
					if (ichannel.LocalPendingMembers[0] == this.me.Handle)
					{
						// Get the callers handle and look him
						Member peer = this.MemberLookup (ichannel.Members[0]);
						if (peer != null)
						{
							uint[] meHandles = {ichannel.LocalPendingMembers[0]};
							ichannel.AddMembers(meHandles, String.Empty);
							
							if (this.IncomingMediaConversation != null)
								this.IncomingMediaConversation (peer, ichannel, channelPath);
						}
						else
							Logger.Debug ("couldn't find peer member - Contact: {0}", handle);
					}
					break;
				}
				
				default:
					break;
			}
		}

		private void SetMeUp ()
		{
			try
			{
				// Get my alias
				uint[] myHandles = {tlpConnection.SelfHandle};
				string[] myAliasNames = tlpConnection.RequestAliases (myHandles);
				if (myAliasNames != null && myAliasNames.Length > 0) {
					myAlias = myAliasNames[0];
				}
				
				// Advertise my capabilities
				org.freedesktop.Telepathy.LocalCapabilityInfo[] info = 
	           		new org.freedesktop.Telepathy.LocalCapabilityInfo[2];
				info[0].ChannelType = 
					(string) org.freedesktop.Telepathy.ChannelType.Text;
				info[1].ChannelType = 
					(string) org.freedesktop.Telepathy.ChannelType.StreamedMedia;
				info[1].TypeSpecificFlags = 
					org.freedesktop.Telepathy.ChannelMediaCapability.Audio |
					org.freedesktop.Telepathy.ChannelMediaCapability.Video;
				this.TlpConnection.AdvertiseCapabilities (info, new string[0]);			
				
				// Create the me member object and we're done
				this.me = 
					new Member (
							this, 
							tlpConnection.SelfHandle, 
							this.account.Username, 
							myAlias, 
							MemberPresence.Available, 
							String.Empty);
			} catch (Exception sm) {
				Logger.Debug (sm.Message);
			} finally {
				Logger.Debug (me.ToString());
			}
		}
	
		private void ConnectHandlers ()
		{
			Logger.Debug ("Connection::ConnectHandlers");
			
			tlpConnection.NewChannel += this.OnNewChannel;
			channelConnected = true;
			
			if (aliasing == true) {
				tlpConnection.AliasesChanged += OnAliasesChanged;
				aliasConnected = true;
			}
			
			if (presence == true) {
				tlpConnection.PresenceUpdate += OnPresenceUpdate;
				presenceConnected = true;
			}
			
			if ( avatars == true) {
				tlpConnection.AvatarUpdated += OnAvatarUpdate;
				avatarConnected = true;
			}
		}
		
		private void DisconnectHandlers ()
		{
			Logger.Debug ("Connection::DisconnectHandlers");
			
			if (avatarConnected == true) {
				tlpConnection.AvatarUpdated -= OnAvatarUpdate;
				avatarConnected = false;
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
				tlpConnection.NewChannel -= OnNewChannel;
				channelConnected = false;
			}
		}
	
		private void DebugDisplayAllowedMembers()
		{
			Logger.Debug ("Connection::DebugDisplayAllowMembers");
			
			try {
				// Now retrieve all the members or buddies
				string[] args = {"allow"};
				uint[] handles = tlpConnection.RequestHandles (HandleType.List, args);
				ObjectPath op = 
					tlpConnection.RequestChannel (ChannelType.ContactList, HandleType.List, handles[0], true);
				IChannelGroup cl = 
					Bus.Session.GetObject<IChannelGroup> (account.TelepathyBusName, op);
					
				uint[] memberHandles = cl.Members;
				string[] allowedMembers = 
					tlpConnection.InspectHandles (HandleType.Contact, memberHandles);
					
				for (int i = 0; i < memberHandles.Length; i++ ) {
					Logger.Debug ("  {0} - {1}", memberHandles[i], allowedMembers[i]);			
				}
				} catch (Exception ddam) {
					Logger.Debug (ddam.Message);
					Logger.Debug (ddam.StackTrace);
			}
		}
	
		private void LoadActiveMembers ()
		{
			Logger.Debug ("Connection::LoadActiveMembers");
			try
			{
				// Now retrieve all the members or buddies
				string[] args = {"subscribe"};
				uint[] handles = tlpConnection.RequestHandles (HandleType.List, args);
				ObjectPath op = tlpConnection.RequestChannel (ChannelType.ContactList, HandleType.List, handles[0], true);
				IChannelGroup cl = 
					Bus.Session.GetObject<IChannelGroup> (account.TelepathyBusName, op);
					
				// FIX - need to verify this code
				// what happens when a user has a screen name and no alias
				uint[] memberHandles = cl.Members;
				string[] memberNames = tlpConnection.InspectHandles (HandleType.Contact, memberHandles);
				string[] aliasNames;
				
				if (avatars == true && aliasing == true) {
					string[] avatarTokens;
					avatarTokens = tlpConnection.GetAvatarTokens (memberHandles);
					aliasNames = tlpConnection.RequestAliases (memberHandles);
					for (int i = 0; i < memberNames.Length; i++) {
						activeMembers.Add( 
							memberHandles[i], 
							new Member (
									this, 
									memberHandles[i], 
									memberNames[i], 
									aliasNames[i], 
									MemberPresence.Offline, 
									String.Empty,
									avatarTokens[i]));
									
						uint[] oneHandles = { memberHandles[i] };			
						tlpConnection.RequestPresence (oneHandles);			
					}
				} else if (aliasing == true) {
					aliasNames = tlpConnection.RequestAliases (memberHandles);
					for (int i = 0; i < memberNames.Length; i++) {
						activeMembers.Add( 
							memberHandles[i], 
							new Member (
									this, 
									memberHandles[i], 
									memberNames[i], 
									aliasNames[i], 
									MemberPresence.Offline, 
									String.Empty));
									
						uint[] oneHandles = { memberHandles[i] };			
						tlpConnection.RequestPresence (oneHandles);			
					}
				} else {
					for (int i = 0; i < memberNames.Length; i++) {
						activeMembers.Add ( 
							memberHandles[i], 
							new Member (
									this, 
									memberHandles[i], 
									memberNames[i], 
									String.Empty, 
									MemberPresence.Offline, 
									String.Empty));
					}
					
					// Now request presence for all discovered members
					tlpConnection.RequestPresence (memberHandles);
				}
				
				Console.WriteLine ("Added {0} members to the active list", activeMembers.Count);
				
			} catch (Exception gmf) {
				Console.WriteLine ("Exception getting active - message: {0}", gmf.Message);
			}
		}
	
		/// <summary>
		/// This method should be called immediately
		/// after a successful connect as other private
		/// methods depend on it
		/// </summary>
		private void SetupSupportedInterfaces ()
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
						Console.WriteLine ("Interface {0} is unknown", iname);
						break;
				}
			}
		}
		
		private void OnAliasesChanged (AliasInfo[] aliases)
		{
			Logger.Debug ("Connection::OnAliasesChanged - called");
			foreach (AliasInfo info in aliases) {
				if (activeMembers.ContainsKey (info.ContactHandle)) {
					Member member = activeMembers[info.ContactHandle] as Member;
					member.UpdateAlias (info.NewAlias);
				}
			}
		}

		/// <summary>
		/// Delegate called when an avatar is changed remotely
		/// </summary>
		private void OnAvatarUpdate (uint contact, string token)
		{
			Logger.Debug ("Connection::OnAvatarUpdate - called");
			if (activeMembers.ContainsKey (contact ) == true ) {
			
				Member member = activeMembers[contact] as Member;
				member.UpdateAvatar (token );
			}
		}
		
		private void OnPresenceUpdate (IDictionary<uint, PresenceUpdateInfo> infos)
		{
			Console.WriteLine ("Connection::OnPresenceUpdate - called");
			foreach (KeyValuePair<uint, PresenceUpdateInfo> entry in infos)
			{
				//Logger.Debug ("  key: {0}", entry.Key);
				Member member = MemberLookup (entry.Key);
				if (member == null ) return;
				
				foreach (KeyValuePair<string, IDictionary<string, object>> info in entry.Value.info)
				{
					string message = String.Empty;
					foreach (KeyValuePair<string, object> val in info.Value)
					{
						if (val.Key == "message")
							message = val.Value as String;
					}
					
					member.UpdatePresence (info.Key, message);
				}
			}
		}
		
		/// <summary>
		/// Look up a member in the internal 
		/// hashtable based on their id or handle
		/// </summary>
		internal Member MemberLookup (uint id)
		{
			if (activeMembers.ContainsKey (id) == true)
				return activeMembers[id] as Member;
			return null;
		}
		#endregion
		
		#region Public Methods
		/// <summary>
		/// Method to connect/authenticate against the
		/// configured telepathy connection manager
		/// </summary>
		public void Connect()
		{
			
			Console.WriteLine (
				"Attempting to authenticate {0} on {1}", 
				this.Account.Username, 
				this.Account.TelepathyBusName);
			Console.WriteLine ("Server: {0}", this.Account.Server);
			Console.WriteLine ("Port: {0}", this.Account.Port);
			
			/*
			// Verify the connection manager supports Jabber
			bool supportsJabber = false;
			try {
				foreach( string proto in connManager.ListProtocols() ) {
					if (proto.ToLower() == "jabber") {
						supportsJabber = true;
						break;
					}
				}

			} catch (Exception e1) {
				Console.WriteLine("Exception getting protocols");
				throw e1;
			}
			
			if (supportsJabber == false) {
		    	// log authentication failure
		    	throw new ApplicationException (
		    		String.Format (
		    			"Authentication failed: connection manager \"{0}\" does not support Jabber",
		    			this.TelepathyBusName));
			}
			*/
			
			try
			{
				tlpConnection.StatusChanged += OnConnectionStateChanged;
				connectedEvent.Reset ();
				Console.WriteLine ("calling tlpConnection.Connect");
				tlpConnection.Connect ();
				//Bus.Session.Iterate ();
				connectedEvent.WaitOne (10000, true);

				if (tlpConnection.Status != org.freedesktop.Telepathy.ConnectionStatus.Connected) {
					throw new ApplicationException (
								String.Format ("Failed to connect \"{0}\" on \"{1}\"", 
									this.Account.Username, 
									this.Account.TelepathyBusName));
				}
				
				// Temporary Debug Code
				// To display what channels are available on this connection
				Logger.Debug ("Listing available channels");
				ChannelInfo[] cls = tlpConnection.ListChannels();
				foreach (ChannelInfo ci in cls ) {
					Logger.Debug ("  {0}", ci.InterfaceName );
				}
				
				// Temporary code
				// Get all privacy modes
				if (privacy == true) {
					Logger.Debug ("Listing privacy modes");
					foreach (string pmode in tlpConnection.PrivacyModes)
						Logger.Debug ("  {0}", pmode);
				} else {
					Logger.Debug ("Privacy not supported on this connection");
				}
				
				// Temporary code to test avie requirements
				Novell.Rtc.AvatarRequirements avie =
					this.GetAvatarRequirements ();
				if (avie != null) {
					Logger.Debug ("Avatar Requirements");
					if (avie.MimeTypes != null)
						Logger.Debug ("  Mime Type: {0}", avie.MimeTypes[0]);
					Logger.Debug ("internal size: {0}", tlpConnection.AvatarRequirements.MaxSize);
					Logger.Debug ("  Max Image Size: {0}", avie.MaximumImageSize);
				}
					
				Console.WriteLine ("ConnectionStatus: {0}", tlpConnection.Status.ToString ());
			}
			catch (Exception aa) 
			{
				Console.WriteLine (aa.Message);
				throw aa;
			}
			return;
		}
		
		/// <summary>
		/// Method to disconnect a previously connected connection
		/// </summary>
		public void Disconnect()
		{
			Console.WriteLine ("Connection::Disconnect - called");
			if (tlpConnection != null && 
					tlpConnection.Status == org.freedesktop.Telepathy.ConnectionStatus.Connected) {
				tlpConnection.Disconnect();
			}	
		}
		
		public void Dispose ()
		{
			/*
			if (status == ConnectionStatus.Connected) 
				throw new Exceptions.ConnectionOpened();
			*/

			//channelList.Clear ();
			//channelList = null;
			GC.SuppressFinalize (this);
		}
		
		public Member[] GetActiveMembers ()
		{
			Logger.Debug ("Connection::GetActiveMembers - called");
			Logger.Debug ("Members in the hash table: {0}", activeMembers.Count);
			int i = 0;
			Member[] members = new Member[activeMembers.Count];
			foreach (Member member in activeMembers.Values) {
				members[i] = member;
				i++;
			}
			
			return members;
		}
		
		public AvatarRequirements GetAvatarRequirements ()
		{
			Logger.Debug ("Connection::GetAvatarRequirements - called");
			
			return 
				new Novell.Rtc.AvatarRequirements (
						tlpConnection.AvatarRequirements.MimeTypes,
						tlpConnection.AvatarRequirements.MaxSize,
						(int) tlpConnection.AvatarRequirements.MinWidth,
						(int) tlpConnection.AvatarRequirements.MinHeight,
						(int) tlpConnection.AvatarRequirements.MaxWidth,
						(int) tlpConnection.AvatarRequirements.MaxHeight);
		}
		
		/// <summary>
		/// Method to lookup up a member by screen name
		/// </summary>
		public Member LookupMemberByName (string name)
		{
			Member member = null;
			string nameLower = name.ToLower();
			
			foreach (Member m in activeMembers.Values) {
				if (m.ScreenName.ToLower() == nameLower) {
					member = m;
					break;
				}
			}
			
			if (member == null)
				throw new ApplicationException (String.Format ("{0} not found", name));
				
			return member;
		}
		
		public Member GetSelf()
		{
			Member returnedMember = null;
			
			if (connected == true && me != null) {
				Console.WriteLine ("Connection::GetSelf - called");
				Console.WriteLine (me.ToString());

				returnedMember = 
					new Member (
							this, 
							this.me.Handle, 
							this.me.ScreenName, 
							this.me.Alias, 
							this.me.Presence, 
							this.me.PresenceMessage);
				
			} else {
				returnedMember = new Member (this, 0, this.Username, String.Empty, MemberPresence.Offline, String.Empty);
			}
			
			return returnedMember;
		}
		
		public void AddConversation (Conversation conv)
		{
			conversations [conv.Peer.Handle] = conv;
		}
		
		#endregion
	}
}