/***************************************************************************
 *  ContactInfo.cs
 *
 *  Copyright (C) 2006 INdT
 *  Written by
 *      Andre Moreira Magalhaes <andre.magalhaes@indt.org.br>
 *      Kenneth Christiansen <kenneth.christiansen@gmail.com>
 *      Renato Araujo Oliveira Filho <renato.filho@indt.org>
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW:
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the Software),
 *  to deal in the Software without restriction, including without limitation
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,
 *  and/or sell copies of the Software, and to permit persons to whom the
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
 *  DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using org.freedesktop.Telepathy;

namespace Tapioca
{
	public delegate void ContactInfoPresenceUpdatedHandler (ContactBase sender, ContactPresence presence);
	public delegate void ContactInfoAliasChangedHandler (ContactBase sender, string new_alias);
	public delegate void ContactInfoAvatarUpdatedHandler (ContactBase sender, string new_token);
	public delegate void ContactInfoAvatarReceivedHandler (ContactBase sender, Avatar avatar);
	
	public abstract class ContactBase : ChannelTarget
	{
		public event ContactInfoPresenceUpdatedHandler PresenceUpdated;		
		public event ContactInfoAliasChangedHandler AliasChanged;
		
		public event ContactInfoAvatarReceivedHandler AvatarReceived;
		public event ContactInfoAvatarUpdatedHandler AvatarUpdated;
		
		string current_avatar_token;
			
		protected string presence_name;
		protected string alias;
		protected ContactPresence presence;
		protected string presence_msg;
		protected Connection connection;

//public methods:		
		public string Alias
		{
			get { 
				return alias; 
			}
			set {	
				if (Handle == null)
					return;	
				if (connection.SupportAliasing && (connection.Status == ConnectionStatus.Connected)) {					
					Dictionary<uint, string> values = new Dictionary<uint, string>();					
					values.Add (Handle.Id, value);					
					connection.TlpConnection.SetAliases (values);					
				}
			}
		}

		public ContactPresence Presence
		{
			get { return presence; }
		}		

		public string PresenceMessage
		{
			get { return presence_msg; }
		}
		
		public static string PresenceName (ContactPresence presence)
		{
			switch (presence)
			{
				case ContactPresence.Offline:
					return "offline";
				case ContactPresence.Available:
					return "available";
				case ContactPresence.Away:
					return "away";
				case ContactPresence.XA:
					return "xa";
				case ContactPresence.Hidden:
					return "hidden";
				case ContactPresence.Busy:
					return "dnd";
			}
			return "";
		}
		
		public string CurrentAvatarToken 
		{
			get {
				return current_avatar_token;
			}
		}
		
		public void RequestAvatar ()
		{
			if (!connection.SupportAvatars)
				return;
				
			uint[] ids = { Handle.Id };
			string[] tokens = connection.TlpConnection.GetAvatarTokens(ids);
			current_avatar_token = tokens[0];
			
			if (current_avatar_token.Length <= 0) {			
				return;
			}
			org.freedesktop.Telepathy.Avatar avatar_info = connection.TlpConnection.RequestAvatar (Handle.Id);			
			if (AvatarReceived != null) {				
				AvatarReceived (this, new Avatar (current_avatar_token, avatar_info));
			}
		}
		
		public ContactCapabilities Capabilities
		{
			get {
				ContactCapabilities caps = 0;
				
				uint[] ids = { Handle.Id };
				CapabilityInfo[] infos = connection.TlpConnection.GetCapabilities (ids);
				foreach (CapabilityInfo info in infos) {
					Console.WriteLine ("Channel {0}/ {1}", info.ChannelType, info.GenericFlags);
					switch (info.ChannelType)
					{
						case org.freedesktop.Telepathy.ChannelType.Text:
							caps = caps | ContactCapabilities.Text;
							break;							
						case org.freedesktop.Telepathy.ChannelType.StreamedMedia:
							if ((info.TypeSpecificFlags & ChannelMediaCapability.Audio) == ChannelMediaCapability.Audio)
								caps = caps | ContactCapabilities.Audio;
							if ((info.TypeSpecificFlags & ChannelMediaCapability.Video) == ChannelMediaCapability.Video)
								caps = caps | ContactCapabilities.Video;
							break;
						default:
							break;
					}
				}
				return caps;
			}
		}

//internal methods:		
		internal void UpdateAlias (string alias)
		{
			this.alias = alias;
			if (AliasChanged != null)
				AliasChanged (this, alias);
		}
		
		internal void UpdatePresence (string presence, string message)
		{
			ContactPresence st = ContactPresence.Offline;
			presence_name = presence;
			switch (presence)
			{
				case "available":
					st = ContactPresence.Available;
					break;
				case "away":
				case "brb":
					st = ContactPresence.Away;
					break;
				case "busy":
				case "dnd":
					st = ContactPresence.Busy;
					break;
				case "xa":
					st = ContactPresence.XA;
					break;
				case "hidden":
					st = ContactPresence.Hidden;
					break;
				case "offline":
				default:
					break;
			}

			this.presence = st;
			this.presence_msg = message;

			if (PresenceUpdated != null)
				PresenceUpdated (this, this.presence);
		}
		
		internal void UpdateAvatarToken (string token)
		{			
			if (current_avatar_token != token) {				
				current_avatar_token = token;
				if (AvatarUpdated != null)
					AvatarUpdated (this, current_avatar_token);
			} 
		}
		
//protected:
		protected ContactBase (Connection connection, Handle handle, ContactPresence presence, string presence_msg)			
			: base (handle) 
		{ 
			this.connection = connection;
			this.presence = presence;
			this.presence_msg = presence_msg;
			
			
			this.current_avatar_token = "";		
			
			//retrive alias
			if (connection.SupportAliasing) {
				uint[] ids = { handle.Id };
				string[] alias = connection.TlpConnection.RequestAliases (ids);
				if (alias.Length > 0)
					this.alias = alias[0];
			}
			
	
		}
	}
}
