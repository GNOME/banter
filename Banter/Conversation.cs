//***********************************************************************
// *  $RCSfile$ - Conversation.cs
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
using System.Net;
using System.Text;

using NDesk.DBus;
using org.freedesktop.DBus;
using org.freedesktop.Telepathy;

namespace Banter
{
	public delegate void MessageSentHandler (Conversation conversation, Message message);
	public delegate void MessageReceivedHandler (Conversation conversation, Message message);
	
	public class Conversation : IDisposable
	{
		public event MessageSentHandler MessageSent;
		public event MessageReceivedHandler MessageReceived;
		
		private uint previewWindowID;
		private uint peerWindowID;
		private List<Message> messages;
		private Account account;
		private org.freedesktop.Telepathy.IConnection tlpConnection;
		private org.freedesktop.Telepathy.IChannelText txtChannel;
		private ProviderUser peerUser;
		private Presence lastPeerPresence;
		
		private uint current;
		private uint last;
	
		public ProviderUser PeerUser
		{
			get {return peerUser;}
		}
		
		public bool CurrentMessageSameAsLast
		{
			get { return (current == last) ? true : false;}
		}
	
		#region Constructors	
		internal Conversation (Account account, Person peer, ProviderUser peerUser, bool initiate)
		{
			this.account = account;
			this.tlpConnection = account.TlpConnection;
			this.peerUser = peerUser;
			this.messages = new List<Message> ();
			last = 999;

			peerUser.PresenceUpdated += OnPeerPresenceUpdated;
			lastPeerPresence = peerUser.Presence;
			
			if (initiate == true)
				this.CreateTextChannel();
		}
		#endregion
		
		#region Private Methods
		/// <summary>
		/// Signal called when presence changes for the peer user.
		/// </summary>
		private void OnPeerPresenceUpdated (ProviderUser user)
		{
			string msg;
			Banter.SystemMessage systemMessage = null;
			
			// no handlers?  exit
			if (MessageReceived == null) return;
			
			string displayName = (user.Alias != null) ? user.Alias.Split (' ')[0] : user.Uri;
			if (user.Presence.Type == Banter.PresenceType.Offline) {
				msg = String.Format("{0} has gone {1}", displayName, user.Presence.Name); 
				systemMessage = new Banter.SystemMessage (msg);
			} else {
				if (user.Presence.Message != null && 
					user.Presence.Message != String.Empty &&
					user.Presence.Message != lastPeerPresence.Message) {
				
					msg = String.Format(
							"{0} is {1} \"{2}\"", 
							displayName, 
							user.Presence.Name,
							user.Presence.Message);
					systemMessage = new Banter.SystemMessage (msg);
					
				} else {
					msg = String.Format("{0} is {1}", displayName, user.Presence.Name); 
					systemMessage = new Banter.SystemMessage (msg);
				}
			}

			lastPeerPresence = user.Presence;
			
			// Indicate the message to registered handlers
			if (systemMessage != null)
				MessageReceived (this, systemMessage);
		}
		
		/// <summary>
		/// Message receive indication called from telepathy
		/// </summary>
		private void OnReceiveMessageHandler (
			uint id,
			uint timeStamp,
			uint sender,
			org.freedesktop.Telepathy.MessageType messageType,
			org.freedesktop.Telepathy.MessageFlag messageFlag,
			string text)
		{
			if (this.peerUser != null)
			{
				Logger.Debug ("Conversation::OnReceiveMessageHandler - called");
				Logger.Debug ("  received message from: {0}", peerUser.Uri);
				Logger.Debug ("  peer id: {0}  incoming id: {1}", peerUser.ID, id);
				
				TextMessage txtMessage = new TextMessage (text);
				messages.Add (txtMessage);
				
				if (current != 0) last = current;
				current = this.peerUser.ID;
			
				// Indicate the message to registered handlers
				if (MessageReceived != null){
					MessageReceived (this, txtMessage);
				}
			}
		}
		
		private void OnTextChannelClosed()
		{
			this.txtChannel = null;
		}
		
		#endregion
		
		#region Public Methods
		public void Dispose()
		{
			if (txtChannel != null)	{
				txtChannel.Close();
				txtChannel = null;
			}
		}
		
		public Banter.Message[] GetReceivedMessages()
		{
			return messages.ToArray();
		}
		
		public void SendMessage (Message message)
		{
			// FIXME::Throw exception
			if (tlpConnection == null) return;
			if (txtChannel == null) return;
			if (message == null) return;
			
			this.txtChannel.Send (org.freedesktop.Telepathy.MessageType.Normal, message.Text);

			if (current != 0)
				last = current;
				
			current = tlpConnection.SelfHandle;
			
			if (MessageSent != null)
				MessageSent (this, message);
		}
		
		public void SetTextChannel (IChannelText channel)
		{
			if (txtChannel != null) return;
			txtChannel = channel;
			
			txtChannel.Received += OnReceiveMessageHandler;
			txtChannel.Closed += OnTextChannelClosed;
			
			// Check for any pending messages and add them to our list
			TextMessage txtMessage;
			PendingMessageInfo[] msgInfos = txtChannel.ListPendingMessages (true);
			foreach (PendingMessageInfo info in msgInfos) {
				Logger.Debug ("Pending Message: {0}", info.Message);
				txtMessage = new TextMessage (info.Message);
				txtMessage.From = peerUser.Uri;
				messages.Add (txtMessage);
			}
		}
		
		public void SetVideoWindows (uint meID, uint peerID)
		{
			this.previewWindowID = meID;
			this.peerWindowID = peerID;
		}
		
		private void CreateTextChannel()
		{
			if (txtChannel != null) return;
			
			ObjectPath op = 
				tlpConnection.RequestChannel (
					org.freedesktop.Telepathy.ChannelType.Text, 
					HandleType.Contact,
					//target.Handle.Type, 
					this.peerUser.ID, 
					true);
				
			txtChannel = Bus.Session.GetObject<IChannelText> (account.BusName, op);
			txtChannel.Received += OnReceiveMessageHandler;
		}
		#endregion
	}
}	
