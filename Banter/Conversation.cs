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
using System.Net;
using System.Text;

using NDesk.DBus;
using org.freedesktop.DBus;

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
		private ArrayList messages;
		private Tapioca.Contact peerContact;
		private Tapioca.Connection tapConnection;
		private Tapioca.TextChannel tapTextChannel;
		private Tapioca.StreamChannel tapStreamChannel;
		private Tapioca.StreamAudio tapAudioStream;
		private Tapioca.StreamVideo tapVideoStream;
		
		private uint current;
		private uint last;
	
		public Tapioca.UserContact MeContact
		{
			get {return tapConnection.Info;}
		}
		
		public Tapioca.Contact PeerContact
		{
			get {return peerContact;}
		}
		
		public bool CurrentMessageSameAsLast
		{
			get { return (current == last) ? true : false;}
		}
	
		#region Constructors		
		public Conversation (Tapioca.Connection tapiocaConnection, Contact tapiocaPeerContact)
		{
			// TODO
			// Verify these members are from the same connection and valid
			
			this.tapConnection = tapiocaConnection;
			this.peerContact = tapiocaPeerContact;
			this.messages = new ArrayList ();
			last = 999;
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
		
		private void OnTapiocaMessageReceivedHandler (
			Tapioca.TextChannel sender,
			Tapioca.TextChannelMessage message)
		{
			Logger.Debug ("Conversation::OnTextChannelClosed - called");
		
			TextMessage txtMessage = new TextMessage (message.Contents);
			txtMessage.From = this.peerContact.Uri;
			txtMessage.To = this.tapConnection.Info.Uri;
			messages.Add (txtMessage);
			
			if (current != 0)
				last = current;
			current = this.peerContact.Handle.Id;
			
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
		#endregion
		
		#region Public Methods
		public void Dispose()
		{
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
			if (messages.Count == 0)
				return null;
			
			return messages.ToArray (typeof (Message)) as Message[];
		}
		
		public void SendTapiocaMessage (Message message)
		{
			// FIXME throw exception
			if (this.tapConnection == null) return;
			if (this.tapTextChannel == null) return;
			
			Tapioca.TextChannelMessage tm = 
				new Tapioca.TextChannelMessage (TextChannelMessageType.Normal, message.Text);
				
			this.tapTextChannel.SendMessage (tm);

			if (current != 0)
				last = current;
				
			current = this.tapConnection.Info.Handle.Id;
			
			if (MessageSent != null && message != null)
				MessageSent (this, message);
		}

		public void SetTextChannel (Tapioca.TextChannel channel)
		{
			if (tapTextChannel != null) return;
			tapTextChannel = channel;
					
			tapTextChannel.Closed += OnTextChannelClosed;
			tapTextChannel.MessageReceived += OnTapiocaMessageReceivedHandler;
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
		
		public void CreateTextChannel()
		{
			if (tapTextChannel != null) return;
			
			tapTextChannel = 
				tapConnection.CreateChannel (
					Tapioca.ChannelType.Text,
					peerContact) as Tapioca.TextChannel;
					
			tapTextChannel.Closed += OnTextChannelClosed;
			tapTextChannel.MessageReceived += OnTapiocaMessageReceivedHandler;
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
						peerContact) as Tapioca.StreamChannel;
			}
			
			if (this.tapAudioStream == null)
			{
				Logger.Debug ("tapAudioStream == null - creating");
				tapAudioStream =
					this.tapStreamChannel.RequestStream (
						Tapioca.StreamType.Audio, 
						this.PeerContact) as StreamAudio;
						
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
						peerContact) as Tapioca.StreamChannel;
			}
			
			if (this.tapVideoStream == null)
			{
				Logger.Debug ("tapVideoStream == null - creating");
				StreamObject[] streams =
					this.tapStreamChannel.RequestFullStream (this.PeerContact);
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
