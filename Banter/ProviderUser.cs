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
using System.Collections.Generic;

using Tapioca;
using org.freedesktop.Telepathy;

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
		private string accountName;
		private bool isMe;
		private Tapioca.Contact contact;
		private IConnection tlpConnection;
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
					if (this.presence.Type != newPresence.Type ||
						this.presence.Message.Equals (newPresence.Message) == false)
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
		
		/// <summary>
		/// the actual tapioca contact for this provider user
		/// </summary>		
		internal Tapioca.Contact Contact
		{
			get { return contact; }
			set
			{ 
				if (this.contact != null) {
					this.contact.AliasChanged -= OnAliasChanged;
					this.contact.AuthorizationStatusChanged -= OnAuthorizationStatusChanged;
					this.contact.PresenceUpdated -= OnPresenceUpdated;
					this.contact.AvatarUpdated -= OnAvatarUpdated;
					this.contact.AvatarReceived -= OnAvatarReceived;
				}
				
				this.contact = value;
				this.contact.AliasChanged += OnAliasChanged;
				this.contact.AuthorizationStatusChanged += OnAuthorizationStatusChanged;
				this.contact.PresenceUpdated += OnPresenceUpdated;
				this.contact.AvatarUpdated += OnAvatarUpdated;
				this.contact.AvatarReceived += OnAvatarReceived;
			}
		}	
		#endregion	
		
		
		#region Constructors
		/// <summary>
		/// Constructs a ProviderUser
		/// </summary>	
		public ProviderUser()
		{
			this.presence = new Presence(PresenceType.Offline);
			this.uri = String.Empty;
			this.accountName = String.Empty;
			this.alias = String.Empty;
			this.isMe = false;
			this.protocol = String.Empty;
			this.avatarToken = String.Empty;
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
		private void OnAliasChanged (ContactBase sender, string newAlias)
		{
			this.alias = newAlias;
			
			// Call any registered handlers
			if (this.AliasChanged != null)
				this.AliasChanged (this);
		}
		
		private void OnAuthorizationStatusChanged (Tapioca.Contact sender, Tapioca.ContactAuthorizationStatus status)
		{
		}
		
		private void OnAvatarUpdated (ContactBase sender, string newToken)
		{
			if (this.AvatarTokenUpdated != null)
				this.AvatarTokenUpdated (this, newToken);
		}
		
		private void OnAvatarReceived (ContactBase sender, Tapioca.Avatar avatar)
		{
			if (this.AvatarReceived != null)
				this.AvatarReceived (this, avatar.Token, String.Empty, avatar.Data);
		}
		
		private void OnPresenceUpdated (ContactBase sender, Tapioca.ContactPresence contactPresence)
		{
			if (this.Presence != null) {
				this.Presence.Message = this.contact.PresenceMessage;

				switch (contactPresence)
				{
					case Tapioca.ContactPresence.Available:
					{
						this.Presence.Type = Banter.PresenceType.Available;
						break;
					}
					case Tapioca.ContactPresence.Away:
					{
						this.Presence.Type = Banter.PresenceType.Away;
						break;
					}
					case Tapioca.ContactPresence.Busy:
					{
						this.Presence.Type = Banter.PresenceType.Busy;
						break;
					}
					case Tapioca.ContactPresence.Hidden:
					{
						this.Presence.Type = Banter.PresenceType.Hidden;
						break;
					}
					case Tapioca.ContactPresence.Offline:
					{
						this.Presence.Type = Banter.PresenceType.Offline;
						break;
					}
					
					case Tapioca.ContactPresence.XA:
					{
						this.Presence.Type = Banter.PresenceType.XA;
						break;
					}
				}
			
				// call registered handlers
				if (this.PresenceUpdated != null)
					this.PresenceUpdated (this); 
			}
		}
		#endregion		
		
		#region Public Methods
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
				
			org.freedesktop.Telepathy.Avatar avatarData	= 
				tlpConnection.RequestAvatar (id);
				
			if (AvatarReceived != null)				
				AvatarReceived (this, tokens[0], avatarData.MimeType, avatarData.Data);
		}
		#endregion
	}
}
