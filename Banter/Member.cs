//***********************************************************************
// *  $RCSfile$ - Member.cs
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
// ***********************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace Novell.Rtc
{
	public delegate void PresenceUpdatedHandler (Member sender, MemberPresence presence);
	public delegate void AliasChangedHandler (Member sender, string newAlias);
	public delegate void AvatarChangedHandler (Member member);
	
	public enum MemberPresence : uint
	{
		Offline = 1,
		Available = 2,
		Away = 3,
		XA = 4,
		Hidden = 5,
		Busy = 6
	}
	
	public enum MemberSubscriptionStatus : uint
	{
		NotSubscribed = 0,
		RemotePending = 1,
		Subscribed = 2
	}
	
	public enum MemberAuthorizationStatus : uint
	{
		NonExistent = 0,
		LocalPending = 1,
		Authorized = 2,
	}
	
	public enum MemberCapabilities : uint
	{
		None = 0,
		Text = 1,
		Audio = 2,
		Video = 4
	}
	
	///<summary>
	///	Member Class
	/// In memory representation of a member or buddy which
	/// is returned from the account management methods
	///</summary>
	public class Member
	{
		public event PresenceUpdatedHandler PresenceUpdated;
		public event AliasChangedHandler AliasChanged;
		public event AvatarChangedHandler AvatarChanged;
		
		private string alias;
		private string avatarToken;
		private string presenceMessage;
		private string screenName;
		private MemberPresence presence;
		private MemberCapabilities capabilities;
		private uint id;
		private Connection connection;
		
		public string ScreenName
		{
			get {return screenName;}
		}
		
		public uint Handle
		{
			get {return id;}
		}
		
		public string Alias
		{
			get {return alias;}
			
			set
			{
				if (connection.SupportAliasing && 
					(connection.Status == ConnectionStatus.Connected)){
					Dictionary<uint, string> values = new Dictionary<uint, string>();
					values.Add (id, value);
					connection.TlpConnection.SetAliases (values);
				}
			}
		}
		
		public Connection Connection
		{
			get { return connection; }
		}
		
		public MemberPresence Presence
		{
			get {return presence;}
		}
		
		public string PresenceMessage
		{
			get {return presenceMessage;}
		}
		
		public MemberCapabilities Capabilities
		{
			get {return capabilities;}
		}
		
		public string AvatarToken
		{
			get {return avatarToken;}
		}
		
		internal Member (Connection connection, uint id, string screenName, string alias, MemberPresence presence, string presenceMessage)
		{
			this.connection = connection;
			this.id = id;
			this.screenName = screenName;
			this.alias = alias;
			this.presence = presence;
			this.presenceMessage = presenceMessage;
		}

		internal Member (
					Connection connection, 
					uint id, 
					string screenName, 
					string alias, 
					MemberPresence presence, 
					string presenceMessage,
					string avatarToken) //: 
//				base (connection, id, screenName, alias, presence, presenceMessage)
		{
			this.avatarToken = avatarToken;

			this.connection = connection;
			this.id = id;
			this.screenName = screenName;
			this.alias = alias;
			this.presence = presence;
			this.presenceMessage = presenceMessage;
		}
		
		internal void UpdateAlias (string newAlias)
		{
			Console.WriteLine ("UpdateAlias - called");
			this.alias = newAlias;
			
			// Call any registered handlers
			if (AliasChanged != null)
				AliasChanged (this, newAlias);
		}
		
		internal void UpdateAvatar (string newToken)
		{
			this.avatarToken = newToken;
			if (AvatarChanged != null)
				AvatarChanged (this);
		}
		
		internal void UpdatePresence (string presence, string message)
		{
			Logger.Debug ("UpdatePresence called for {0}", this.screenName);
			this.presenceMessage = message;
			MemberPresence mp = MemberPresence.Offline;
			switch (presence.ToLower()) {
				case "available":
					mp = MemberPresence.Available;
					break;
				case "away":
				case "brb":
					mp = MemberPresence.Away;
					break;
				case "busy":
				case "dnd":
					mp = MemberPresence.Busy;
					break;
				case "xa":
					mp = MemberPresence.XA;
					break;
				case "hidden":
					mp = MemberPresence.Hidden;
					break;
				default:
					break;
			}
			
			this.presence = mp;
			if (message != null && message != String.Empty)
				Logger.Debug("  Presence: {0}  Message: {1}", this.presence.ToString(), message);
			else
				Logger.Debug("  Presence: {0}", this.presence.ToString());
			
			// Call any registered handlers
			if (PresenceUpdated != null) {
				PresenceUpdated (this, this.presence);
			}
		}
		
		public override string ToString ()
		{
			return
				String.Format (
					"\nScreenName: {0}\nAlias: {1}\nHandle: {2}\nPresence: {3}",
					screenName,
					alias,
					id,
					presence.ToString());
		}
		
		/// <summary>
		/// Method to get a member's avatar
		/// </summary>
		public Novell.Rtc.Avatar GetAvatar ()
		{
			org.freedesktop.Telepathy.Avatar av =
				connection.TlpConnection.RequestAvatar (this.id);
			
			return new Novell.Rtc.Avatar (av.MimeType, av.Data );
		}
	}	
}