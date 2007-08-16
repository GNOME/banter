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
using Mono.Unix;

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
		
		// Telepathy Groups
		protected IChannelGroup publishGroup;
		protected IChannelGroup subscribeGroup;
		protected IChannelGroup denyGroup;

		// Connected Handlers
		protected bool aliasConnected = false;
		protected bool avatarsConnected = false;
		protected bool channelConnected = false;
		protected bool presenceConnected = false;
		protected bool messagingConnected = false;
		protected bool capabilitiesConnected = false;
		
		// Supported interfaces
		protected bool aliasing = false;
		protected bool avatars = false;
		protected bool capabilities = false;
		protected bool forwarding = false;
		protected bool renaming = false;
		protected bool presence = false;
		protected bool privacy = false;

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
			get
			{
				try {
					return connInfo.BusName;
				} catch{}
				return busName;
			}
		}
		
		public ObjectPath BusPath
		{
			get
			{
				try {
					return connInfo.ObjectPath;
				} catch{};
				
				return null;
			}
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
	
		public org.freedesktop.Telepathy.IConnection TlpConnection
		{
			get {return tlpConnection;}
		}

		public string[] ReceivedInvitations
		{
			get
			{
				try {
					return 
						tlpConnection.InspectHandles (
							HandleType.Contact,
							publishGroup.LocalPendingMembers);
				} catch{}
				return null;
			}
		}
		
		public uint[] ReceivedInvitationHandles
		{
			get
			{
				try {return publishGroup.LocalPendingMembers;} catch{}
				return null;
			}
		}
		
		public string[] SentInvitations
		{
			get
			{
				try {
					return 
						tlpConnection.InspectHandles (
							HandleType.Contact,
							subscribeGroup.RemotePendingMembers);
				} catch{}
				return null;
			}
		}
		
		public uint[] SentInvitationHandles
		{
			get
			{
				try {return subscribeGroup.RemotePendingMembers;} catch{}
				return null;
			}
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
		
		/// <summary>
		/// Construct using an existing telepathy connection
		/// </summary>
		public Account (IConnection connection)
		{
			tlpConnection = connection;
		}
		#endregion

		/// <summary>
		/// Method to add or update a user to the provider list
		/// </summary>
		private
		ProviderUser
		AddProviderUser (uint id, string jid, ProviderUserRelationship relationship)
		{
			string key = ProviderUserManager.CreateKey (jid, this.protocol);
			ProviderUser providerUser = null;
			
			try {
				providerUser = ProviderUserManager.GetProviderUser (key);
				if (providerUser != null) {
					providerUser.TlpConnection = tlpConnection;
					providerUser.AccountName = this.Name;
					providerUser.Protocol = this.Protocol;
					providerUser.ID = id;
					providerUser.Relationship = relationship;
				}
			} 
			catch (Exception fff) {
				Logger.Debug ("Failed to get ProviderUser {0}", key);
				Logger.Debug (fff.Message);
			}
				
			if (providerUser == null) {
				try {
					providerUser =
						ProviderUserManager.CreateProviderUser (
							jid, 
							this.protocol, 
							relationship);
					providerUser.TlpConnection = tlpConnection;
					providerUser.AccountName = this.Name;
					providerUser.ID = id;
					Logger.Debug ("Member: {0} ID: {1} - object created", jid, id);
				} catch{}
			} else {
				Logger.Debug ("Member: {0} ID: {1} - object updated", jid, id);
			}
			
			return providerUser;
		}

		/// <summary>
		/// This method should be called immediately
		/// after a successful connect as other private
		/// methods depend on it
		/// </summary>
		protected void SetupSupportedInterfaces ()
		{
			Logger.Debug ("SetupSupportedInterfaces - called");
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
		/// Method to open and maintain outstanding channels
		/// to the publish and subscribe lists
		/// </summary>
		protected void SetupGroupChannels ()
		{
			Logger.Debug ("SetupGroupChannels - called");
			
			string[] pubArgs = {"publish"};
			string[] subArgs = {"subscribe"};
			string[] blockedArgs = {"deny"};
			ObjectPath objectPath;
			
			try {
				// First setup a channel to the publish list
				uint[] pubHandles = tlpConnection.RequestHandles (HandleType.List, pubArgs);
				objectPath = 
					tlpConnection.RequestChannel (
						org.freedesktop.Telepathy.ChannelType.ContactList, 
						HandleType.List, 
						pubHandles[0], 
						true);
						
				publishGroup = 
					Bus.Session.GetObject<IChannelGroup> (connInfo.BusName, objectPath);
				publishGroup.MembersChanged += OnPublishedMembersChanged;
			
				uint groupFlags = publishGroup.GroupFlags;
				Logger.Debug ("Group Flags: {0}", groupFlags);
				if ((((uint) groupFlags & (uint) ChannelGroupFlag.CanAdd) == (uint) ChannelGroupFlag.CanAdd))
					Logger.Debug ("  can add");
				if (((uint) groupFlags & (uint) ChannelGroupFlag.CanRemove) == (uint) ChannelGroupFlag.CanRemove)
					Logger.Debug ("  can remove");
				if (((uint) groupFlags & (uint) ChannelGroupFlag.CanRescind) == (uint) ChannelGroupFlag.CanRescind)
					Logger.Debug ("  can rescind");
				if (((uint) groupFlags & (uint) ChannelGroupFlag.MessageAdd) == (uint) ChannelGroupFlag.MessageAdd)
					Logger.Debug ("  can add messages");
				if (((uint) groupFlags & (uint) ChannelGroupFlag.MessageRemove) == (uint) ChannelGroupFlag.MessageRemove)
					Logger.Debug ("  can remove messages");
				if (((uint) groupFlags & (uint) ChannelGroupFlag.MessageAccept) == (uint) ChannelGroupFlag.MessageAccept)
					Logger.Debug ("  can accept messages");
				if (((uint) groupFlags & (uint) ChannelGroupFlag.MessageReject) == (uint) ChannelGroupFlag.MessageReject)
					Logger.Debug ("  can reject messages");
				if (((uint) groupFlags & (uint) ChannelGroupFlag.MessageRescind) == (uint) ChannelGroupFlag.MessageRescind)
					Logger.Debug ("  can rescind messages");
					
				// Next setup a channel to the subscribe list
				uint[] subHandles = tlpConnection.RequestHandles (HandleType.List, subArgs);
				objectPath = 
					tlpConnection.RequestChannel (
						org.freedesktop.Telepathy.ChannelType.ContactList, 
						HandleType.List, 
						subHandles[0], 
						true);
						
				subscribeGroup = 
					Bus.Session.GetObject<IChannelGroup> (connInfo.BusName, objectPath);
				subscribeGroup.MembersChanged += OnSubscribedMembersChanged;
				
				// Next setup a channel to the blocked list
				uint[] blockedHandles = 
					tlpConnection.RequestHandles (HandleType.List, blockedArgs);
				objectPath = 
					tlpConnection.RequestChannel (
						org.freedesktop.Telepathy.ChannelType.ContactList, 
						HandleType.List, 
						blockedHandles[0], 
						true);
						
				denyGroup = 
					Bus.Session.GetObject<IChannelGroup> (connInfo.BusName, objectPath);
					
				denyGroup.MembersChanged += OnBlockedMembersChanged;
				
			} catch (Exception sgc) {
				Logger.Debug ("Failed setting up group channels");
				Logger.Debug (sgc.Message);
				Logger.Debug (sgc.StackTrace);
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
					try
					{
						SetupSupportedInterfaces ();
						SetupGroupChannels ();
						SetupMe ();
						SetupProviderUsers ();
						AdvertiseCapabilities ();
		                connected = true;
					}
					catch (Exception on)
					{	
						Logger.Debug (on.Message);
						Logger.Debug (on.StackTrace);
					}
					
					if (connectedEvent != null)
						connectedEvent.Set();
						
					break;
				}
				
				case org.freedesktop.Telepathy.ConnectionStatus.Disconnected:
				{
					connected = false;	
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
			Logger.Debug ("SetupMe - called");
			string[] aliasNames = null;
			uint[] meHandles = { tlpConnection.SelfHandle };
	
			try {
				// Get my capabilities
				CapabilityInfo[] caps = tlpConnection.GetCapabilities (meHandles);
				foreach (CapabilityInfo cap in caps)
					Logger.Debug ("Caps - channel type: {0}", cap.ChannelType);
				
				// Request my alias
				if (aliasing == true)
					aliasNames = tlpConnection.RequestAliases (meHandles);

				string uri = options["account"] as string;
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
			Logger.Debug ("SetupProviderUsers - called");
			ProviderUser providerUser;
			string key;
			
			// First add any sent invitation users
			uint[] invitedHandles = this.SentInvitationHandles;
			if (invitedHandles != null && invitedHandles.Length > 0) {
				Logger.Debug ("Adding SentInvitation users");
				string[] invitedUsers = this.SentInvitations;
				for (int i = 0; i < invitedUsers.Length; i++)
					AddProviderUser (
						invitedHandles[i], 
						invitedUsers[i],
						ProviderUserRelationship.SentInvitation);
			}
			
			// Next add received invitation users
			uint[] inviteHandles = this.ReceivedInvitationHandles;
			if (inviteHandles != null && inviteHandles.Length > 0) {
				Logger.Debug ("Adding ReceivedInvitation users");
				string[] inviteUsers = this.ReceivedInvitations;
				for (int i = 0; i < inviteUsers.Length; i++)
					AddProviderUser (
						inviteHandles[i], 
						inviteUsers[i],
						ProviderUserRelationship.ReceivedInvitation);
			}
			
			if (subscribeGroup.Members.Length > 0) {
				Logger.Debug ("Adding Subscribed users");
				// Add subscribed members			
				string[] members = 
					tlpConnection.InspectHandles (HandleType.Contact, subscribeGroup.Members);
				
				string[] aliasNames = null;
				if (aliasing == true)
					aliasNames = tlpConnection.RequestAliases (subscribeGroup.Members);
				
				CapabilityInfo[] caps = null;
				if (this.capabilities == true) {
					caps = tlpConnection.GetCapabilities (subscribeGroup.Members);
				}

				//Logger.Debug ("# of known contacts: {0}", members.Length);
				for (int i = 0; i < members.Length; i++) {
					providerUser =
						AddProviderUser(
							subscribeGroup.Members[i],
							members[i],
							ProviderUserRelationship.Linked);
							
					if (aliasing == true && aliasNames != null)
						providerUser.Alias = aliasNames[i];

					/*						
					public struct CapabilityInfo
					{
						public uint ContactHandle;
						public string ChannelType;
						public CapabilityFlag GenericFlags;
						public ChannelMediaCapability TypeSpecificFlags;
					}
					*/
						
					// Setup the remote contact's media capabilities
					if (capabilities == true && caps != null && caps.Length > 0) {
						foreach (CapabilityInfo info in caps) {
							if (info.ContactHandle == subscribeGroup.Members[i]) {
								if ((info.TypeSpecificFlags & ChannelMediaCapability.Audio) ==
										ChannelMediaCapability.Audio)
									providerUser.MediaCapability = 
										Banter.MediaCapability.Audio;

								if ((info.TypeSpecificFlags & ChannelMediaCapability.Video) ==
										ChannelMediaCapability.Video)
										
									// If video is reported audio is assumed	
									providerUser.MediaCapability = 
										Banter.MediaCapability.Video |
										Banter.MediaCapability.Audio;
								break;
							}
						}
					}
				}
			}
			
			ConnectHandlers ();
			if (presence == true)
				tlpConnection.RequestPresence (subscribeGroup.Members);
		}
		
		private void ConnectHandlers ()
		{
			//Logger.Debug ("Account::ConnectHandlers");
			
			this.tlpConnection.NewChannel += OnNewChannel;
			channelConnected = true;
			
			if (capabilities == true) {
				tlpConnection.CapabilitiesChanged += OnCapabilitiesChanged;
				capabilitiesConnected = true;
			}
			
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
		
		private void DisconnectHandlers ()
		{
			//Logger.Debug ("Account::DisconnectHandlers");
			
			try {
				if (capabilitiesConnected == true) {
					tlpConnection.CapabilitiesChanged -= OnCapabilitiesChanged;
					capabilitiesConnected = false;
				}
				
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
					tlpConnection.NewChannel -= OnNewChannel;
					channelConnected = false;
				}
			} catch (Exception dh) {
				Logger.Debug (dh.Message);
				Logger.Debug (dh.StackTrace);
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
		/// Changing the token property in ProviderUser will force
		/// all registered providers to get called back on the change
		/// </summary>
		private void OnAvatarUpdated (uint id, string token)
		{
			ProviderUser user = ProviderUserManager.GetProviderUser (id);
			if (user != null)
				user.AvatarToken = token;
		}
		
		/// <summary>
		///	Telepathy callback when capabilities change for a contact
		/// </summary>
		private void OnCapabilitiesChanged (CapabilitiesChangedInfo[] caps)
		{
			ProviderUser user;
			foreach (CapabilitiesChangedInfo cap in caps) {
				user = ProviderUserManager.GetProviderUser (cap.ContactHandle);
				
				/*
				if (user != null)
					user.Alias = info.NewAlias;
				*/	
			}
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

			// Set the message after the type so the message is saved off
			if (message != null && message != String.Empty)
				banterPresence.Message = message;

/*			Logger.Debug (
				"Updating presence for: {0} to {1}:{2}", 
				user.Uri, 
				banterPresence.Type.ToString(), 
				banterPresence.Message);
*/				
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
		/// Callback method when members are added or removed from
		/// the blocked list
		/// </summary>
		private
		void
		OnBlockedMembersChanged(
			string message,
			uint[] added,
			uint[] removed,
			uint[] localPending,
			uint[] remotePending,
			uint actor,
			uint reason)
		{
			Logger.Debug ("OnBlockedMembersChanged - called");
			Logger.Debug ("  Message: {0}", message);
			Logger.Debug ("  # added: {0}", added.Length);
			foreach (uint handle in added)
			{
				Logger.Debug ("    contact id: {0}", handle);
				
				/*
				ProviderUser user =
					ProviderUserManager.GetProviderUser (handle);
				user.Relationship = ProviderUserRelationship.Linked;
				*/
			}
				
			Logger.Debug ("  # removed: {0}", removed.Length);
			foreach (uint handle in removed) {
				uint[] handles = {handle};
				string[] names = 
					tlpConnection.InspectHandles (HandleType.Contact, handles);
				Logger.Debug ("    contact id: {0} - {1}", handles[0], names[0]);
			}
				
			Logger.Debug ("Remote Pending List");
			foreach (uint handle in remotePending) {
				uint[] handles = {handle};
				string[] names = 
					tlpConnection.InspectHandles (HandleType.Contact, handles);
				Logger.Debug ("   {0} - {1}", handle, names[0]);
			}
		}
		
		/// <summary>
		/// Callback method when members are added or removed from
		/// the subscribed list
		/// </summary>
		private
		void
		OnSubscribedMembersChanged(
			string message,
			uint[] added,
			uint[] removed,
			uint[] localPending,
			uint[] remotePending,
			uint actor,
			uint reason)
		{
			Logger.Debug ("OnSubscribedMembersChanged - called");
			if (message != null && message != String.Empty)
				Logger.Debug ("  Message: {0}", message);
				
			try {
				Logger.Debug ("  # added: {0}", added.Length);
				
				if (added.Length > 0) {
					ProviderUser providerUser;
					
					string[] names = 
						tlpConnection.InspectHandles (HandleType.Contact, added);
					Logger.Debug ("past getting jid names");
					
					string[] aliasNames = null;
					
					/*
					if (aliasing == true)
						aliasNames = tlpConnection.RequestAliases (added);
					Logger.Debug ("past getting alias");
					*/
					
					CapabilityInfo[] caps = null;
					if (this.capabilities == true)
						caps = tlpConnection.GetCapabilities (added);
					Logger.Debug ("past get caps");
					
					for (int i = 0; i < added.Length; i++) {
						Logger.Debug (
							" adding contact id: {0}  name: {1}", 
							added[i], 
							names[i]);
					
						providerUser =
							AddProviderUser(
								added[i],
								names[i],
								ProviderUserRelationship.Linked);
						if (providerUser != null) {
							if (aliasNames != null && 
								aliasNames.Length >= i+1 &&
								aliasNames[i] != null &&
								aliasNames[i] != String.Empty)
									providerUser.Alias = aliasNames[i];
									
							// Setup the remote contact's media capabilities
							if (capabilities == true && caps != null && caps.Length > 0) {
								foreach (CapabilityInfo info in caps) {
									if (info.ContactHandle == added[i]) {
										if ((info.TypeSpecificFlags & ChannelMediaCapability.Audio) ==
												ChannelMediaCapability.Audio)
											providerUser.MediaCapability = 
												Banter.MediaCapability.Audio;

										if ((info.TypeSpecificFlags & ChannelMediaCapability.Video) ==
												ChannelMediaCapability.Video)
												
											// If video is reported audio is assumed	
											providerUser.MediaCapability = 
												Banter.MediaCapability.Video |
												Banter.MediaCapability.Audio;
										break;
									}
								}
							}
						}	
					}
				}
				
				Logger.Debug ("  # removed: {0}", removed.Length);
				if (removed.Length > 0) {
					for (int i = 0; i < removed.Length; i++) {
						Logger.Debug ("    contact id: {0}", removed[i]);
						ProviderUserManager.RemoveProviderUser (removed[i], this.protocol);
					}
				}

				Logger.Debug ("Remote Pending List");
				if (remotePending.Length > 0) {
					string[] names = 
						tlpConnection.InspectHandles (HandleType.Contact, remotePending);
						
					for (int i = 0; i < names.Length; i++) {
						Logger.Debug ("    contact id: {0} - {1}", remotePending[i], names[i]);
					}
				}
				
				/*
				foreach (uint handle in remotePending) {
					uint[] handles = {handle};
					string[] names = 
						tlpConnection.InspectHandles (HandleType.Contact, handles);
					Logger.Debug ("   {0} - {1}", handle, names[0]);
					//this.remoteInvitationGroup.RemoveMembers (handles, String.Empty);
				}
				*/
			} catch (Exception osmc) {
				Logger.Debug (osmc.Message);
				Logger.Debug (osmc.StackTrace);
			}
		}
		

		/// <summary>
		/// Callback method when members are added or removed from
		/// the published list
		/// </summary>
		private
		void
		OnPublishedMembersChanged(
			string message,
			uint[] added,
			uint[] removed,
			uint[] localPending,
			uint[] remotePending,
			uint actor,
			uint reason)
		{
			Logger.Debug ("OnPublishedMembersChanged - called");
			Logger.Debug ("  Message: {0}", message);
			
			try {
				Logger.Debug ("  # added: {0}", added.Length);
				
				if (added.Length > 0) {
					// Add newly added members
					string[] aliasNames = null;
					string[] names = 
						tlpConnection.InspectHandles (HandleType.Contact, added);
			
					for (int i = 0; i < added.Length; i++) {
						Logger.Debug (
							" adding contact id: {0}  name: {1}", 
							added[i], 
							names[i]);
					
						ProviderUser user =
							ProviderUserManager.GetProviderUser (added[i]);
						if (user != null) {
							user.Relationship = ProviderUserRelationship.Linked;
							if (aliasNames != null && 
								aliasNames.Length >= i+1 &&
								aliasNames[i] != null &&
								aliasNames[i] != String.Empty)
									user.Alias = aliasNames[i];
						}	
					}
				}
			} catch (Exception ae) {
				Logger.Debug (ae.Message);			
			}
				
			if (removed.Length > 0) {
				try {
					Logger.Debug ("  # removed: {0}", removed.Length);
					for (int i = 0; i < removed.Length; i++) {
						Logger.Debug ("    contact id: {0}", removed[i]);
						//ProviderUserManager.RemoveProviderUser (removed[i], this.protocol);
					}
					
				} catch (Exception re) {
					Logger.Debug (re.Message);
				}
			}
				
			Logger.Debug ("Local Pending List");
			foreach (uint handle in localPending) {
				uint[] handles = {handle};
				string[] names = 
					tlpConnection.InspectHandles (HandleType.Contact, handles);
				Logger.Debug ("   {0} - {1}", handle, names[0]);
				
				// Add him to the list	
				ProviderUser providerUser =				
					ProviderUserManager.CreateProviderUser (
						names[0], 
						this.protocol, 
						ProviderUserRelationship.ReceivedInvitation);
				providerUser.TlpConnection = tlpConnection;
				providerUser.AccountName = this.Name;
				providerUser.ID = handle;
				
				if (aliasing == true) {
					string[] aliasNames = null;
					aliasNames = tlpConnection.RequestAliases (handles);
					if (aliasNames != null && aliasNames.Length >= 1)
						providerUser.Alias = aliasNames[0];
				}
			}
			
			Logger.Debug ("Remote Pending List");
			foreach (uint handle in remotePending) {
				uint[] handles = {handle};
				string[] names = 
					tlpConnection.InspectHandles (HandleType.Contact, handles);
				Logger.Debug ("   {0} - {1}", handle, names[0]);
			}
		}
		
		/// <summary>
		/// Telepathy callback when new channels are created
		/// </summary>
		private void OnNewChannel (
						ObjectPath channelPath,
						string channelType,
						HandleType handleType,
						uint handle,
						bool suppressHandler)
		{	
			Logger.Debug ("Account::OnNewChannel - called");
			Logger.Debug ("New Channel {0}", channelType);
			Logger.Debug ("Handle Type: {0}", handleType.ToString());
			Logger.Debug ("Handle: {0}", handle);
			Logger.Debug ("Suppress Handler: {0}", suppressHandler);
			
			if (suppressHandler == false)
				ConversationManager.ProcessNewChannel (
					this,
					channelPath,
					channelType,
					handleType,
					handle,
					suppressHandler);
				
			return;
		}
		
		/// <summary>
		/// Method to add a member
		/// </summary>
		public void AddMember (uint id, string message)
		{
			if (this.publishGroup == null)
				throw new ApplicationException ("Group instance unavailable");
				
			uint[] ids = {id};
			this.publishGroup.AddMembers (ids, message);	
		}


		/// <summary>
		/// Method to authorize a user who is requesting it to
		/// enable chat.  
		/// authorize == false denies the user of authorization
		/// </summary>
		public void AuthorizeUser (bool authorize, uint id, string message)
		{
			if (this.publishGroup == null)
				throw new ApplicationException ("Group instance unavailable");
				
			uint[] ids = {id};
			if (authorize == true)
				publishGroup.AddMembers (ids, message);
			else
				publishGroup.RemoveMembers (ids, message);
		}

		/// <summary>
		/// Method to block an existing user
		/// block == true blocks the user, false unblocks
		/// </summary>
		public void BlockUser (bool block, uint id, string message)
		{
			if (this.denyGroup == null)
				throw new ApplicationException ("Group instance unavailable");
				
			uint[] ids = {id};
			if (block == true) {
				//subscribeGroup.RemoveMembers (ids, message);
				denyGroup.AddMembers (ids, message);
			} else {
				denyGroup.RemoveMembers (ids, message);
				//subscribeGroup.AddMembers (ids, message);
			}	
		}


		/// <summary>
		/// Method to invite a user to enable chat
		/// </summary>
		public void InviteUser (string username)
		{
			Logger.Debug ("InviteUser - called");
			
			if (this.subscribeGroup == null)
				throw new ApplicationException ("Group instance unavailable");
			
			try {
				string[] names = {username};
				uint[] inviteHandles =
					tlpConnection.RequestHandles (HandleType.Contact, names);
				
				subscribeGroup.AddMembers (inviteHandles, String.Empty);
				
				string[] normalizedNames = 
					tlpConnection.InspectHandles (HandleType.Contact, inviteHandles);

				ProviderUser providerUser =				
					ProviderUserManager.CreateProviderUser (
						normalizedNames[0], 
						this.protocol, 
						ProviderUserRelationship.SentInvitation);
				providerUser.TlpConnection = tlpConnection;
				providerUser.AccountName = this.Name;
				providerUser.ID = inviteHandles[0];
				
			} catch (Exception iu ) {
				Logger.Debug (iu.Message);
				Logger.Debug (iu.StackTrace);
			}
		}

		/// <summary>
		/// Method to remove an existing user
		/// </summary>
		public void RemoveUser (uint id, string message)
		{
			Logger.Debug ("Account::RemoveUser - called");
			Logger.Debug ("  removing: {0}", id);
			
			if (this.tlpConnection == null)
				throw new ApplicationException ("Invalid telepathy connection");
				
			try {
				ProviderUser providerUser =
					ProviderUserManager.GetProviderUser (id);
				if (providerUser == null)
					throw new ApplicationException ("Not a valid user");
					
				uint[] ids = {id};
				if (providerUser.Relationship == ProviderUserRelationship.ReceivedInvitation) {
					if (this.publishGroup == null)
						throw new ApplicationException ("Group doesn't exist");
					Logger.Debug ("  removing member from the local invitation group");
					publishGroup.RemoveMembers (ids, message);				
				} else if (providerUser.Relationship == ProviderUserRelationship.Linked ||
							providerUser.Relationship == ProviderUserRelationship.SentInvitation) {
					if (this.subscribeGroup == null)
						throw new ApplicationException ("Group doesn't exist");
						
					Logger.Debug ("  removing member from the subscribed group");
					subscribeGroup.RemoveMembers (ids, message);				
				}
			
			} catch (Exception ru) {
				Logger.Debug ("Exception in RemoveUser");
				Logger.Debug (ru.Message);
			}
				
				
			/*				
//			string[] subArgs = {"subscribe"};
			string[] subArgs = {"known"};
			ObjectPath objectPath;
			IChannelGroup subGroup;
			
			try {
				uint[] subHandles = tlpConnection.RequestHandles (HandleType.List, subArgs);
				objectPath = 
					tlpConnection.RequestChannel (
						org.freedesktop.Telepathy.ChannelType.ContactList, 
						HandleType.List, 
						subHandles[0], 
						true);
						
				subGroup = 
					Bus.Session.GetObject<IChannelGroup> (connInfo.BusName, objectPath);

				Logger.Debug ("Members in \"Known\" group");					
				string[] names = 
					tlpConnection.InspectHandles (HandleType.Contact, subGroup.Members);
				for (int i = 0; i < subGroup.Members.Length; i++) {
					Logger.Debug ("Handle: {0}  Name: {1}", subGroup.Members[i], names[i]);
				}
					
				subGroup.MembersChanged += OnSubscribedMembersChanged;
				uint[] ids = {id};
				subGroup.RemoveMembers (ids, message);				
				subGroup.MembersChanged -= OnSubscribedMembersChanged;
				subGroup = null;
				
			} catch (Exception ru) {
				Logger.Debug ("Exception in RemoveUser");
				Logger.Debug (ru.Message);
			}
			*/
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
				
			connectedEvent.WaitOne (30000, true);
			Thread.Sleep (0);
		}
		
		/// <summary>
		/// Method to disconnect this account
		/// </summary>
		public void Disconnect ()
		{
			Logger.Debug ("Account::Disconnect - called");
			if (connected == true && tlpConnection != null) {
				try {
					//localInvitationGroup.Close ();
					//remoteInvitationGroup.Close ();
					//DisconnectHandlers ();
					Logger.Debug ("Calling telepathy disconnect");
					tlpConnection.Disconnect ();
					Logger.Debug ("out of telepathy disconnect");
				} catch (Exception dis) {
					//Logger.Debug (dis.Message);
					//Logger.Debug (dis.StackTrace);
				}
			}
				
			connected = false;
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
		public JabberAccount (IConnection connection) : base (connection)
		{
		}
		
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
		}
		#endregion
		
		#region Private Methods
		/// <summary>
		/// Check if we have an existing connection in gabble
		/// assumes connection info is valid
		/// </summary>
		private bool CheckForExistingConnection()
		{
			return false;
			
			/*
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
			*/
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