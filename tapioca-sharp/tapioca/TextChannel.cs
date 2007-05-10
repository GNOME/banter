/***************************************************************************
 *  ChatSession.cs
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
using org.freedesktop.DBus;
using org.freedesktop.Telepathy;
using ObjectPath = NDesk.DBus.ObjectPath;

namespace Tapioca
{
	public delegate void TextChannelMessageReceivedHandler (TextChannel sender, TextChannelMessage message);
	public delegate void TextChannelDeliveryErrorOccurredHandler (TextChannel sender, TextChannelMessageDeliveryError error, TextChannelMessage message);

	public class TextChannel : Channel
	{	
		private ChannelTarget remoteTarget;
		
		public event TextChannelMessageReceivedHandler MessageReceived;
		public event TextChannelDeliveryErrorOccurredHandler MessageDeliveryError;
		
		
//public methods:
		public void SendMessage (TextChannelMessage message)
		{
			if (!IsClosed)
				((IChannelText) this.tlp_channel).Send ((org.freedesktop.Telepathy.MessageType) message.Type, message.Contents);
		}
		
		public override ChannelType Type {
			get {
				return ChannelType.Text;
			}
		}
		
		public ChannelTarget RemoteTarget
		{
			get { return remoteTarget; }
		}
				

//internal methods:

		internal TextChannel (Tapioca.Connection connection, IChannelText channel, ChannelTarget contact, string service_name, ObjectPath obj_path)
			:base (connection, channel, service_name, obj_path)
		{
			remoteTarget = contact;
			((IChannelText) this.tlp_channel).Received += OnMessageReceived;
			((IChannelText) this.tlp_channel).SendError += OnMessageSendError;		
		}

//private methods:        

		private void OnMessageReceived (uint id, uint time_stamp, uint sender,
			org.freedesktop.Telepathy.MessageType type, org.freedesktop.Telepathy.MessageFlag flags, string text)
		{
			if (MessageReceived != null) {
				TextChannelMessage msg = new TextChannelMessage ((TextChannelMessageType) type, text, time_stamp);
				MessageReceived (this, msg);
			}
		}

		private void OnMessageSendError (org.freedesktop.Telepathy.MessageSendError error, uint time_stamp,
			org.freedesktop.Telepathy.MessageType type, string text)
		{
			if (MessageDeliveryError != null) {
				TextChannelMessage msg = new TextChannelMessage ((TextChannelMessageType) type, text, time_stamp);
				MessageDeliveryError (this, (TextChannelMessageDeliveryError) error, msg);
			}
		}
	}
}
