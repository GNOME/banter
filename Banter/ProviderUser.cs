//***********************************************************************
// *  $RCSfile$ - ProviderUser.cs
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
using System.Collections.Generic;using org.freedesktop.Telepathy;

namespace Banter
{
	///<summary>
	///	ProtocolName
	/// Class that holds const names for protocols used in ProviderUser
	///</summary>
	public static class ProtocolName
	{
		#region Public Static Types
		public const string Jabber = "jabber";
		public const string Sip	= "sip";
		public const string Aol = "aol";
		public const string Gwim = "gwim";
		#endregion		
	}

	public delegate void ProviderUserPresenceUpdatedHandler (ProviderUser user);
	public delegate void ProviderUserAliasChangedHandler (ProviderUser user);
	public delegate void ProviderUserAvatarUpdatedHandler (ProviderUser user, string newToken);
	public delegate void ProviderUserAvatarReceivedHandler (ProviderUser user, string token, string mimeType, byte[] avatarData);

	/// <summary>
	///	ProviderUserRelationship Enum
	/// represents the relationship of the provider user
	/// </summary>
	public enum ProviderUserRelationship
	{
		Unknown,
		Linked,
		SentInvitation,
		ReceivedInvitation
	}
	
	/// <summary>
	/// Provider User Media Capability
	/// </summary>
	public enum MediaCapability : uint
	{
		None = 0,
		Audio = 1,
		Video = 2
	}
	
	///<summary>
	///	ProviderUser Class
	/// ProviderUser represents buddies from providers in telepathy.
	///</summary>
	public class ProviderUser
	{
		#region Private Types
		private string uri;
		private string alias;
		private string avatarToken;
		private string protocol;
		private uint id;
		private Presence presence;
		private MediaCapability mediaCaps;
		private string accountName;
		private bool isMe;
		private IConnection tlpConnection;
		private ProviderUserRelationship relationship;
		#endregion		
		
		#region Public Events
		public event ProviderUserPresenceUpdatedHandler PresenceUpdated;		
		public event ProviderUserAliasChangedHandler AliasChanged;
		public event ProviderUserAvatarUpdatedHandler AvatarTokenUpdated;
		public event ProviderUserAvatarReceivedHandler AvatarReceived;
		#endregion

		#region Public Properties
		
		/// <summary>
		/// The unique id of the user in the telepathy framework
		/// this id is volatile and only unique during a connection
		/// </summary>		
		public uint ID
		{
			get { return id; }
			set { this.id = value; }
		}

		/// <summary>
		/// Opaque token for detecting changes in the avatar
		/// </summary>		
		public string AvatarToken
		{
			get
			{ 
				if (avatarToken != null)
					return avatarToken;
					
				return String.Empty;
			}
			
			set
			{
				if (avatarToken != null) {
					if (avatarToken.Equals ((string) value) == false) {
						avatarToken = value;
						if (AvatarTokenUpdated != null)
							AvatarTokenUpdated (this, value);
					}
				} else {
					avatarToken = value;
					if (AvatarTokenUpdated != null)
						AvatarTokenUpdated (this, value);
				}	
			}
		}
		
		/// <summary>
		/// The Uri of the ProviderUser from telepathy
		/// </summary>		
		public string Uri
		{
			get { return uri; }
			set { this.uri = value; }
		}
		
		/// <summary>
		/// The Uri of the ProviderUser from telepathy
		/// </summary>		
		public Banter.MediaCapability MediaCapability
		{
			get { return mediaCaps; }
			set { this.mediaCaps = value; }
		}

		/// <summary>
		/// The Alias of the ProviderUser from telepathy
		/// </summary>		
		public string Alias
		{
			get { return alias; }
			set
			{
				if (this.alias != null) {
					if (this.alias.Equals ((string) value) == false) {
						this.alias = value;
						if (this.AliasChanged != null)
							this.AliasChanged (this);
					}
				} else {
					this.alias = value;
					if (this.AliasChanged != null)
						this.AliasChanged (this);
				}	
			}
		}


		/// <summary>
		/// The Protocol of the ProviderUser from telepathy
		/// </summary>		
		public string Protocol
		{
			get { return protocol; }
			set { this.protocol = value; }
		}


		/// <summary>
		/// The Presence of the ProviderUser from telepathy
		/// </summary>		
		public Presence Presence
		{
			get { return presence; }
			set
			{ 
				if (this.presence != null) {
					Banter.Presence newPresence = value;
					if ( (this.presence.Type != newPresence.Type) ||
						this.presence.Message.Equals(newPresence.Message) == false)
					{
						this.presence = newPresence;
						if (this.PresenceUpdated != null)
							PresenceUpdated (this);
					}
				} else {
					this.presence = value;
					if (this.PresenceUpdated != null)
						PresenceUpdated (this);
				}
			}
		}		


		/// <summary>
		/// The Account Name of the ProviderUser from telepathy
		/// </summary>		
		public string AccountName
		{
			get { return accountName; }
			set { this.accountName = value; }
		}		

		/// <summary>
		/// Current user's relationship to the provider user
		/// Linked, SentInvitation, ReceivedInvitation
		/// </summary>		
		public ProviderUserRelationship Relationship
		{
			get { return relationship; }
			set { this.relationship = value; }
		}	
		

		/// <summary>
		/// true if this represents the ProviderUser logged in
		/// </summary>		
		public bool IsMe
		{
			get { return isMe; }
			set { this.isMe = value; }
		}	
		

		/// <summary>
		/// the actual telepathy connection for this provider user
		/// </summary>		
		internal IConnection TlpConnection
		{
			get {return tlpConnection;}
			set {tlpConnection = value;}
		}
		#endregion	
		
		
		#region Constructors
		/// <summary>
		/// Constructs a ProviderUser
		/// </summary>	
		public ProviderUser()
		{
			this.presence = new Presence(PresenceType.Offline);
			this.relationship = ProviderUserRelationship.Unknown;
			this.uri = String.Empty;
			this.accountName = String.Empty;
			this.alias = String.Empty;
			this.isMe = false;
			this.protocol = String.Empty;
			this.avatarToken = String.Empty;
			this.mediaCaps = Banter.MediaCapability.None;
		}
		
		/// <summary>
		/// Constructs a ProviderUser 
		/// </summary>	
		internal ProviderUser(IConnection conn, uint id) : base()
		{
			this.tlpConnection = conn;
			this.id = id;
		}
		#endregion

		#region Private Methods
		#endregion		
		
		#region Public Methods
		/// <summary>
		/// Authorize a user to enable chatting
		/// the user must be in the correct relationship state to authorize
		/// </summary>
        public void Authorize (string message)
        {
        	if (this.relationship != ProviderUserRelationship.ReceivedInvitation)
        		throw new ApplicationException ("User is not in a the correct authorization state");
        		
			// Need to get the account information so we can AddMember
			// to the correct group
			Account account = AccountManagement.GetAccountByName (this.accountName);
			account.AuthorizeUser (true, this.id, message);
			this.relationship = ProviderUserRelationship.Linked;			
        }

		/// <summary>
		/// Deny a user who is requesting authorization to chat
		/// </summary>
        public void DenyAuthorization (string message)
        {
        	if (this.relationship != ProviderUserRelationship.ReceivedInvitation)
        		throw new ApplicationException ("User is not in the ReceivedInvitation state");
        		
			// Need to get the account information so we can AddMember
			// to the correct group
			Account account = AccountManagement.GetAccountByName (this.accountName);
			account.AuthorizeUser (false, this.id, message);
			this.relationship = ProviderUserRelationship.Unknown;	
			
			ProviderUserManager.RemoveProviderUser (this.uri, this.protocol);
        }

		/// <summary>
		/// Method to revoke an outstanding invitation
		/// </summary>
        public void RevokeInvitation ()
        {
        	if (this.relationship != ProviderUserRelationship.SentInvitation)
        		throw new ApplicationException ("User is not in the SentInvitation relationship state");
        		
			// Need to get the account information so we can AddMember
			// to the correct group
			Account account = AccountManagement.GetAccountByName (this.accountName);
			
			account.RemoveUser (this.id, String.Empty);
			this.relationship = ProviderUserRelationship.Unknown;
			ProviderUserManager.RemoveProviderUser (this.uri, this.protocol);
        }

		/// <summary>
		/// Method to remove a user's membership with the current user
		/// </summary>
        public void RemoveUser ()
        {
        	if (this.relationship != ProviderUserRelationship.Linked)
        		throw new ApplicationException ("User is not in the Linked relationship state");
        		
			// Need to get the account information so we can Remove the member
			Account account = AccountManagement.GetAccountByName (this.accountName);
			account.RemoveUser (this.id, String.Empty);
			ProviderUserManager.RemoveProviderUser (this.uri, this.protocol);
        }

		public void RequestAvatarData ()
		{
			if (tlpConnection == null) return;
			
			/*
			if (!connection.SupportAvatars)
				return;
			*/
			
				
			uint[] ids = {id};
			string[] tokens = tlpConnection.GetAvatarTokens(ids);
			if (tokens == null || tokens[0].Length <= 0)
				return;
			org.freedesktop.Telepathy.Avatar avatarData;
			try{
				avatarData= 
				tlpConnection.RequestAvatar (id);
				if (AvatarReceived != null)				
					AvatarReceived (this, tokens[0], avatarData.MimeType, avatarData.Data);
			}catch(Exception e){
				Logger.Error("{0}",e);
				//avatarData = tlpConnection.RequestAvatar (id);
				
			}
			
		}

		public void SetStatus (Presence myPresence)
        {
        	if(tlpConnection.Status == org.freedesktop.Telepathy.ConnectionStatus.Connected) {
				Dictionary<string, IDictionary<string, object>> presence = new Dictionary<string, IDictionary<string, object>>();
				Dictionary<string, object> values = new Dictionary<string, object>();
				values.Add ("message", myPresence.Message);
				presence.Add ( myPresence.Name, values);
				tlpConnection.SetStatus (presence);
				this.Presence = myPresence;
        	}
        }

		public void SetAvatar (string mimeType, byte[] data)
        {
        	if(tlpConnection.Status == org.freedesktop.Telepathy.ConnectionStatus.Connected) {
        		org.freedesktop.Telepathy.Avatar aphoto = new org.freedesktop.Telepathy.Avatar();
        		aphoto.Data = data;
        		aphoto.MimeType = mimeType;
				tlpConnection.SetAvatar(aphoto);
        	}
        }
        

		#endregion
	}
}
