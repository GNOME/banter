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
using Tapioca;

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
		private Tapioca.Connection tapConnection;
		private Tapioca.TextChannel tapTextChannel;
		private Tapioca.StreamChannel tapStreamChannel;
		private Tapioca.StreamAudio tapAudioStream;
		private Tapioca.StreamVideo tapVideoStream;
		private org.freedesktop.Telepathy.IConnection tlpConnection;
		private org.freedesktop.Telepathy.IChannelText txtChannel;
		private ProviderUser peerUser;
		private Presence lastPeerPresence;
		
		private uint current;
		private uint last;
	
		public Tapioca.UserContact MeContact
		{
			get {return tapConnection.Info;}
		}
		
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
		private void OnTextChannelClosed (Tapioca.Channel sender)
		{
			Logger.Debug ("Conversation::OnTextChannelClosed - called");
			if ( tapTextChannel != null)
			{
				tapTextChannel.Close();
				tapTextChannel = null;
			}
		}
		
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
					
					/*
					MessageReceived (this, systemMessage);
					msg = String.Format("{0}'s new status \"{1}\"", displayName, user.Presence.Message); 
					systemMessage = new Banter.SystemMessage (msg);
					*/
				} else {
					msg = String.Format("{0} is {1}", displayName, user.Presence.Name); 
					systemMessage = new Banter.SystemMessage (msg);
				}
			}

			/*
			if (user.Presence.Type != lastPeerPresence.Type) {
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
			
			} else {
				
				if (user.Presence.Message != null && user.Presence.Message != String.Empty) {
					msg = String.Format("{0}'s new status \"{1}\"", displayName, user.Presence.Message); 
					systemMessage = new Banter.SystemMessage (msg);
				}
			}
			*/
			
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

		/*
		private void OnMessageReceivedHandler (
			ProviderUser sender,
			TextMessage txtMessage)
		{
			Logger.Debug ("Conversation::OnMessageReceivedHandler - called");
	
			messages.Add (txtMessage);
			
			if (current != 0) last = current;
			current = this.peerUser.ID;
			
			// Indicate the message to registered handlers
			if (MessageReceived != null){
				MessageReceived (this, txtMessage);
			}
		}
		*/
		
		private void OnTapiocaMessageReceivedHandler (
			Tapioca.TextChannel sender,
			Tapioca.TextChannelMessage message)
		{
			Logger.Debug ("Conversation::OnTapiocaMessageReceiveHandler - called");
		
			TextMessage txtMessage = new TextMessage (message.Contents);
			txtMessage.From = this.peerUser.Uri;
			txtMessage.To = this.tapConnection.Info.Uri;
			messages.Add (txtMessage);
			
			if (current != 0)
				last = current;
			current = this.peerUser.Contact.Handle.Id;
			
			// Indicate the message to registered handlers
			if (MessageReceived != null){
				MessageReceived (this, txtMessage);
			}
		}
		
		private void SetVideoStream (StreamVideo video)
        {
			if (video == null)
			{
				if (tapVideoStream != null)
				{
					tapVideoStream.StateChanged -= OnVideoStreamStateChanged;
					tapVideoStream.Error -= OnMediaStreamError;
					tapVideoStream = null;
				}	
			} 
			else
			{
				if (tapVideoStream != null) return;
				
				Logger.Debug ("Setting up video stream - once per channel");
				tapVideoStream = video;
				tapVideoStream.WindowPreviewID = this.previewWindowID;
				tapVideoStream.StateChanged += OnVideoStreamStateChanged;
				tapVideoStream.Error += OnMediaStreamError;
			}
		}

		private void SetAudioStream (StreamAudio audio)
		{
			if (audio == null)
			{
				if (tapAudioStream != null)
				{
					tapAudioStream.StateChanged -= OnAudioStreamStateChange;
					tapAudioStream.Error -= OnMediaStreamError;
					tapAudioStream = null;
				}
				return;
			}
			else
			{
				if (this.tapAudioStream != null) return;
				tapAudioStream = audio;
				tapAudioStream.StateChanged += OnAudioStreamStateChange;
				tapAudioStream.Error += OnMediaStreamError;
			}
		}
		
		private void OnStreamChannelClosed (Tapioca.Channel channel)
		{
			Logger.Debug ("Conversation::OnStreamChannelClosed");

			if (channel == tapStreamChannel)
			{
				if (tapAudioStream != null)
				{
					Logger.Debug ("  releasing audio stream");
					tapStreamChannel.ReleaseStream (tapAudioStream);
					tapAudioStream = null;
				}
				
				if (tapVideoStream != null)
				{
					Logger.Debug ("  releasing video stream");
					tapStreamChannel.ReleaseStream (tapVideoStream);
					tapVideoStream = null;
				}
			}
		}
		
		private void OnMediaStreamError (StreamObject stream, uint error, string message)
		{
			Logger.Debug ("Conversation::OnMediaStreamError - called");
			Logger.Debug ("  Error: {0}", error);
			Logger.Debug ("  Message: {0}", message);
		}
		
		private void OnStreamLost (StreamChannel streamChannel, StreamObject stream)
		{
			Logger.Debug ("Conversation::OnStreamLost - called");
			switch (stream.Type)
			{
				case Tapioca.StreamType.Audio:
				{
					Logger.Debug ("  audio stream lost");
					SetAudioStream (null);
					break;
				}
				
				case Tapioca.StreamType.Video:
				{
					Logger.Debug ("  video stream lost");
					SetVideoStream (null);
					break;
				}
			}
		}
		
		private void OnNewStream (StreamChannel streamChannel, StreamObject stream)
		{
			Logger.Debug ("Conversation::OnNewStream - called");

			switch (stream.Type)
			{
				case Tapioca.StreamType.Audio:
				{
					SetAudioStream (stream as StreamAudio);
					break;
				}
				
				case Tapioca.StreamType.Video:
				{
					SetVideoStream (stream as StreamVideo);
					break;
				}
			}
		}

		private void OnAudioStreamStateChange (StreamObject sender, Tapioca.StreamState state)
		{
			switch (state)
			{
				case Tapioca.StreamState.Connecting:
				{
					Logger.Debug ("  audio stream connecting");
					break;
				}
				
				case Tapioca.StreamState.Connected:
				{
					Logger.Debug ("  audio stream connected");
					break;
				}
				
				case Tapioca.StreamState.Stopped:
				{
					Logger.Debug ("  audio stream stopped");
					break;
				}
			}
		}
	
		private void OnVideoStreamStateChanged (StreamObject sender, Tapioca.StreamState state)
		{
			switch (state)
			{
				case Tapioca.StreamState.Connecting:
				{
					Logger.Debug ("  video stream connecting");
					//tapVideoStream.WindowPreviewID = previewWindowID;
					tapVideoStream.WindowID = peerWindowID;
					break;
				}
				
				case Tapioca.StreamState.Connected:
				{
					Logger.Debug ("  video stream connected");
					break;
				}
				
				case Tapioca.StreamState.Stopped:
				{
					Logger.Debug ("  video stream stopped");
					break;
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
			
			if (tapStreamChannel != null)
			{
				StopVideoChat ();
				this.tapStreamChannel.Close();
				tapStreamChannel = null;
			}
			
			if (tapTextChannel != null)
			{
				tapTextChannel.Close();
				tapTextChannel = null;
			}
		}
		
		public Banter.Message[] GetReceivedMessages()
		{
//			if (messages.Count == 0)
//				return null;
			
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
		
		public void SetStreamedMediaChannel (Tapioca.StreamChannel channel)
		{
			if (this.tapStreamChannel != null) return;
			tapStreamChannel = channel;
			
			tapStreamChannel.Closed += OnStreamChannelClosed;
			tapStreamChannel.LostStream += OnStreamLost;
			tapStreamChannel.NewStream += OnNewStream;
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
			
			/*
			ObjectPath op = tlpConnection.RequestChannel (
				org.freedesktop.Telepathy.ChannelType.Text,
				org.freedesktop.Telepathy.dfd,
				this.peerUser.ID,
				true);
			*/	
				
				
				
			/*
			if (tapTextChannel != null) return;
			tapTextChannel = 
				tapConnection.CreateChannel (
					Tapioca.ChannelType.Text,
					peerUser.Contact) as Tapioca.TextChannel;
					
			tapTextChannel.Closed += OnTextChannelClosed;
			tapTextChannel.MessageReceived += OnTapiocaMessageReceivedHandler;
			*/
		}
		
		public void StartAudioChat ()
		{
			Logger.Debug ("Conversation::StartAudioChat - called");
			if (this.tapStreamChannel == null)
			{
				Logger.Debug ("tapStreamChannel == null - creating");
				this.tapStreamChannel =
					tapConnection.CreateChannel (
						Tapioca.ChannelType.StreamedMedia,
						peerUser.Contact) as Tapioca.StreamChannel;
			}
			
			if (this.tapAudioStream == null)
			{
				Logger.Debug ("tapAudioStream == null - creating");
				tapAudioStream =
					this.tapStreamChannel.RequestStream (
						Tapioca.StreamType.Audio, 
						this.PeerUser.Contact) as StreamAudio;
						
				tapAudioStream.Play();
			}
		}
		
		public void StartVideoChat ()
		{
			Logger.Debug ("Conversation::StartVideoChat - called");
			if (this.tapStreamChannel == null)
			{
				Logger.Debug ("tapStreamChannel == null - creating");
				this.tapStreamChannel =
					tapConnection.CreateChannel (
						Tapioca.ChannelType.StreamedMedia,
						peerUser.Contact) as Tapioca.StreamChannel;
			}
			
			if (this.tapVideoStream == null)
			{
				Logger.Debug ("tapVideoStream == null - creating");
				StreamObject[] streams =
					this.tapStreamChannel.RequestFullStream (this.PeerUser.Contact);
				foreach (StreamObject so in streams)
				{
					switch (so.Type)
					{
						case Tapioca.StreamType.Audio:
						{
							SetAudioStream (so as StreamAudio);
							break;
						}
						
						case Tapioca.StreamType.Video:
						{
							SetVideoStream (so as StreamVideo);
							break;
						}
					}
				}
				
				if (tapVideoStream != null)
					tapVideoStream.Play();
					
				if (tapAudioStream != null)
					tapAudioStream.Play();
			}
		}
		
		public void StopAudioChat()
		{
			try
			{
				if (tapStreamChannel != null)
				{
					if (tapAudioStream != null)
					{
						tapStreamChannel.ReleaseStream (tapAudioStream);
						tapAudioStream = null;
					}
				}
			}
			catch (Exception svc)
			{
				Logger.Debug (svc.Message);
				Logger.Debug (svc.StackTrace);
			}
		}
		
		public void StopVideoChat()
		{
			try
			{
				if (tapStreamChannel != null)
				{
					StopAudioChat ();
					if (tapVideoStream != null)
					{
						tapStreamChannel.ReleaseStream (tapVideoStream);
						tapVideoStream = null;
					}
				}
			}
			catch (Exception svc)
			{
				Logger.Debug (svc.Message);
				Logger.Debug (svc.StackTrace);
			}
		}
		#endregion
	}
}	
