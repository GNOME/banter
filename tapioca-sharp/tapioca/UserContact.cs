/***************************************************************************
 *  PersonalInfo.cs
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
	public class UserContact : ContactBase
	{
//public methods:		
		public Avatar Avatar
		{
			set { 
				//TODO: 
			}
		}
		
		public void SetCapabilities (ContactCapabilities caps)
		{
		
			ChannelMediaCapability media_caps = 0;
			bool has_media = false;
						
			if ((caps & ContactCapabilities.Audio) == ContactCapabilities.Audio) {
				media_caps = ChannelMediaCapability.Audio;
				has_media = true;
			}
				
			if ((caps & ContactCapabilities.Video) == ContactCapabilities.Video) {			
				media_caps = media_caps | ChannelMediaCapability.Video;
				has_media = true;
			}
			
			LocalCapabilityInfo[] info = new LocalCapabilityInfo[(has_media?2:1)];
			
			
			if ((caps & ContactCapabilities.Text) == ContactCapabilities.Text) {				
				info[0].ChannelType = (string) org.freedesktop.Telepathy.ChannelType.Text;			
			}
			
			if (has_media) {
				info[1].ChannelType = (string) org.freedesktop.Telepathy.ChannelType.StreamedMedia;
				info[1].TypeSpecificFlags = media_caps;
			}
			
			connection.TlpConnection.AdvertiseCapabilities (info, new string[0]);
		}
		
		public new ContactPresence Presence
		{
			set {
				if (connection.SupportPresence && (connection.Status == ConnectionStatus.Connected)) {
					Dictionary<string, IDictionary<string, object>> presence = new Dictionary<string, IDictionary<string, object>>();					
					Dictionary<string, object> values = new Dictionary<string, object>();
					values.Add ("message", presence_msg);
					presence.Add (PresenceName (value), values);
					connection.TlpConnection.SetStatus (presence);
					this.presence = value;
				}
			}
		}
		
		public new string PresenceMessage
		{
			set {
				if (connection.SupportPresence && (connection.Status == ConnectionStatus.Connected)) {
					Dictionary<string, IDictionary<string, object>> presence = new Dictionary<string, IDictionary<string, object>>();					
					Dictionary<string, object> values = new Dictionary<string, object>();
					values.Add ("message", value);
					presence.Add (PresenceName (this.presence), values);
					connection.TlpConnection.SetStatus (presence);
				}
			}
		}
		
		public UserContact(Connection connection, Handle handle, ContactPresence presence, string presence_msg)
			: base (connection, handle, presence, presence_msg)
		{
		}		
	}
}
