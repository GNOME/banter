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
	public delegate void MediaChannelOpenedHandler (Conversation conversation);
	public delegate void MediaChannelClosedHandler (Conversation conversation);
	public delegate void TextChannelOpenedHandler (Conversation conversation);
	public delegate void TextChannelClosedHandler (Conversation conversation);
	public delegate void VideoStreamUpHandler (Conversation conversation);
	public delegate void VideoStreamDownHandler (Conversation conversation);
	public delegate void VideoStreamPlayingHandler (Conversation conversation);
	public delegate void VideoStreamStoppedHandler (Conversation conversation);
	public delegate void VideoStreamErrorHandler (Conversation conversation, string message);
	public delegate void AudioStreamUpHandler (Conversation conversation);
	public delegate void AudioStreamDownHandler (Conversation conversation);
	public delegate void AudioStreamPlayingHandler (Conversation conversation);
	public delegate void AudioStreamStoppedHandler (Conversation conversation);
	public delegate void AudioStreamErrorHandler (Conversation conversation, string message);
	
	public class Conversation : IDisposable
	{
		public event MessageSentHandler MessageSent;
		public event MessageReceivedHandler MessageReceived;
		public event MediaChannelOpenedHandler MediaChannelOpened;
		public event MediaChannelClosedHandler MediaChannelClosed;
		public event TextChannelOpenedHandler TextChannelOpened;
		public event TextChannelClosedHandler TextChannelClosed;
		public event AudioStreamUpHandler AudioStreamUp;
		public event AudioStreamDownHandler AudioStreamDown;
		public event AudioStreamPlayingHandler AudioStreamPlaying;
		public event AudioStreamStoppedHandler AudioStreamStopped;
		public event AudioStreamErrorHandler AudioStreamError;
		public event VideoStreamUpHandler VideoStreamUp;
		public event VideoStreamDownHandler VideoStreamDown;
		public event VideoStreamPlayingHandler VideoStreamPlaying;
		public event VideoStreamStoppedHandler VideoStreamStopped;
		public event VideoStreamErrorHandler VideoStreamError;
		
		// True the conversation was initiated locally
		// False the conversation was initiated by an incoming
		// request from a peer.
		private bool initiated;
		
		private uint previewWindowID;
		private uint peerWindowID;
		private bool outputWindowIsSet = false;
		private List<Message> messages;
		private Account account;
		
		// Telepathy connection and channels		
		private org.freedesktop.Telepathy.IConnection tlpConnection;
		private IChannelHandler channelHandler;
		private ObjectPath txtChannelObjectPath;
		private ObjectPath mediaChannelObjectPath;
		private IChannelText txtChannel;
		private IChannelStreamedMedia mediaChannel;
		private IStreamEngine streamEngine;
		
		/*
		private uint textStreamId = 0;
		private uint audioStreamId = 0;
		private uint videoStreamId = 0;
		*/
		
		private ProviderUser peerUser;
		private Presence lastPeerPresence;
		private uint current;
		private uint last;
		
		private System.Collections.Generic.Dictionary<uint, uint> videoStreams;
		private System.Collections.Generic.Dictionary<uint, uint> audioStreams;
	
		public bool ActiveTextChannel
		{
			get {return (txtChannel != null) ? true : false;}			
		}

		public bool ActiveMediaChannel
		{
			get {return (mediaChannel != null) ? true : false;}
		}
		
		public bool ActiveAudioStream
		{
			get {return (audioStreams.Count > 0) ? true : false;}			
		}
		
		public bool ActiveVideoStream
		{
			get {return (videoStreams.Count > 0) ? true : false;}			
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
		internal Conversation (ProviderUser providerUser)
		{
			this.Init (
				AccountManagement.GetAccountByName (providerUser.AccountName),			
				providerUser);
			
			this.initiated = true;
		}
		
		/// <summary>
		/// Constructor for a conversation initiated
		/// from a media channel via a remote user
		/// </summary>
		internal Conversation (
			Account account, 
			ProviderUser providerUser,
			ObjectPath objectpath,
			IChannelStreamedMedia channel)
		{
			this.Init (account, providerUser);
			this.initiated = false;

			// Figure out the streamed media type
			mediaChannelObjectPath = objectpath;
			mediaChannel = channel;
			mediaChannel.Closed += OnMediaChannelClosed;
			mediaChannel.StreamAdded += OnStreamAdded;
			mediaChannel.StreamDirectionChanged += OnStreamDirectionChanged;
			mediaChannel.StreamError += OnStreamError;
			mediaChannel.StreamRemoved += OnStreamRemoved;
			mediaChannel.StreamStateChanged += OnStreamStateChanged;
			mediaChannel.MembersChanged += OnMembersChanged;
		}


		/// <summary>
		/// Constructor for a conversation initiated
		/// from a text channel via a remote user
		/// </summary>
		internal Conversation (
			Account account, 
			ProviderUser providerUser,
			ObjectPath objectpath,
			IChannelText channel)
		{
			this.Init (account, providerUser);
			
			this.txtChannelObjectPath = objectpath;
			this.initiated = false;
			
			txtChannel = channel;
			txtChannel.Received += OnReceiveMessageHandler;
			txtChannel.Closed += OnTextChannelClosed;
			
			AddPendingMessages ();
		}
		#endregion
		
		#region Private Methods
		private void AddPendingMessages ()
		{
			// Check for any pending messages and add them to our list
			try {
				TextMessage txtMessage;
				PendingMessageInfo[] msgInfos = txtChannel.ListPendingMessages (true);
				foreach (PendingMessageInfo info in msgInfos) {
					Logger.Debug ("Pending Message: {0}", info.Message);
					txtMessage = new TextMessage (info.Message, peerUser);
					messages.Add (txtMessage);
				}
			} catch{}
		}
		
		private void Init (Account account, ProviderUser providerUser)
		{
			this.account = account;
			this.tlpConnection = this.account.TlpConnection;
			this.peerUser = providerUser;
			this.messages = new List<Message> ();
			this.last = 999;
			this.videoStreams = new Dictionary<uint,uint> ();
			this.audioStreams = new Dictionary<uint,uint> ();
			this.peerUser.PresenceUpdated += OnPeerPresenceUpdated;
			this.lastPeerPresence = this.peerUser.Presence;
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
				
				TextMessage txtMessage = new TextMessage (text, peerUser);
				messages.Add (txtMessage);
				
				if (current != 0) last = current;
				current = this.peerUser.ID;
			
				// Indicate the message to registered handlers
				if (MessageReceived != null)
					MessageReceived (this, txtMessage);
			}
		}
		
		private void OnMediaChannelClosed ()
		{
			if (MediaChannelClosed != null)
				MediaChannelClosed (this);
				
			this.mediaChannel = null;
		}
		
		private void OnTextChannelClosed()
		{
			if (TextChannelClosed != null)
				TextChannelClosed (this);
			this.txtChannel = null;
		}
		
		
		// Save stream types and ids off for later so handlers can know
        // what type of streams they are.
        private void SaveStream (StreamType type, uint id)
        {
            switch (type) {
            case StreamType.Audio:
                if (audioStreams.ContainsKey (id) == false) {
                    Logger.Debug ("Saving audio stream id: {0}", id);
                    audioStreams [id] = id;
                }
                break;
                
            case StreamType.Video:
                if (videoStreams.ContainsKey (id) == false) {
                    Logger.Debug ("Saving video stream id: {0}", id);
                    videoStreams [id] = id;
                }
                break;
            }
        }

        private void OnStreamAdded (uint streamid, uint contacthandle, org.freedesktop.Telepathy.StreamType streamtype)
		{
			Logger.Debug(
				"OnStreamAdded of type: {0} with id:{1}, for contact {2}", 
				streamtype, 
				streamid, 
				contacthandle);

			SaveStream (streamtype, streamid);
				
			if (streamtype == StreamType.Video) {
				if (VideoStreamUp != null)
					VideoStreamUp (this);
			} else if (streamtype == StreamType.Audio) {
				if (AudioStreamUp != null)
					AudioStreamUp (this);
			}
		}

		private void OnStreamDirectionChanged (uint streamid, StreamDirection streamdirection, StreamPendingFlags pendingflags)
        {
			Logger.Debug("OnStreamDirectionChanged called");
        }

        private void OnStreamError (uint streamid, uint errno, string message)
        {
 			Logger.Debug("OnStreamError called with message: {0}:{1}", errno, message);

            if (videoStreams.ContainsKey (streamid)) {
            	if (VideoStreamError != null)
            		VideoStreamError (this, message);
            } else if (audioStreams.ContainsKey (streamid)) {
            	if (AudioStreamError != null)
            		AudioStreamError (this, message);
            }
        }

        private void OnStreamRemoved (uint streamid)
        {
            if (videoStreams.ContainsKey (streamid)) {
                Logger.Debug ("Removing video stream: {0}", streamid);
                videoStreams.Remove (streamid);
                
            	if (VideoStreamDown != null)
            		VideoStreamDown (this);
            } else if (audioStreams.ContainsKey (streamid)) {
                Logger.Debug ("Removing audio stream: {0}", streamid);
                audioStreams.Remove (streamid);
                
				IndicateSystemMessage ("Audio chat stopped");	
            	if (AudioStreamDown != null)
            		AudioStreamDown (this);
            }
        }

        private void OnStreamStateChanged (uint streamid, org.freedesktop.Telepathy.StreamState streamstate)
        {
            Logger.Debug ("OnStreamStateChanged called - ID: {0} State: {1}", streamid, streamstate);
            
	       	switch (streamstate ) {
	       		case StreamState.Connecting:
	       		{
					if (videoStreams.ContainsKey(streamid) && (outputWindowIsSet == false) ) {
						Logger.Debug("Stream State: Connecting and we found the streamid {0}", streamid);
						Logger.Debug("Setting output window {0}", peerWindowID);
						streamEngine.SetOutputWindow (
							mediaChannelObjectPath, 
							streamid,
							this.peerWindowID);
						// we should only do this once or we blow
						outputWindowIsSet = true;
	       			} else if (audioStreams.ContainsKey (streamid)) {
						Logger.Debug("Stream State: Connecting and we found the streamid {0}", streamid);
	       			}
	       			break;
	       		}
	       		
	       		case StreamState.Connected:
	       		{
	       			//IndicateSystemMessage ("Video chat connected!");
					if (videoStreams.ContainsKey(streamid)) {
						if (VideoStreamUp != null) 
							VideoStreamUp (this);
	       			} else if (audioStreams.ContainsKey (streamid)) {
						if (AudioStreamUp != null) 
							AudioStreamUp (this);
	       			}
	       			break;
	       		}
	       		
	       		case StreamState.Playing:
	       		{
					if (videoStreams.ContainsKey(streamid)) {
						if (VideoStreamPlaying != null)
							VideoStreamPlaying (this);
						//IndicateSystemMessage ("Video chat started");	
	       			} else if (audioStreams.ContainsKey (streamid)) {
						if (AudioStreamPlaying != null) 
							AudioStreamPlaying (this);
	       			}
	       			break;
        		}
	       		
	       		case StreamState.Stopped:
	       		{
					if (videoStreams.ContainsKey(streamid)) {
						if (VideoStreamStopped != null) 
							VideoStreamStopped (this);
						IndicateSystemMessage ("Video chat stopped");	
	       			} else if (audioStreams.ContainsKey (streamid)) {
						if (AudioStreamStopped != null) 
							AudioStreamStopped (this);
	       			}
	       			break;
	       		}
	       	}
        }

        private void OnStreamEngineReceiving (ObjectPath channelpath, uint streamid, bool state)      
        {   
        	Logger.Debug("OnStreamEngineReceiving: stream id: {0}", streamid);
        }
        
        private void OnMembersChanged (string message, uint[] added, uint[] removed, uint[] localpending, uint[] remotepending, uint actor, uint reason)
        {
        	Logger.Debug ("OnMembersChanged: {0}", message);
        	
        	Logger.Debug ("\tAdded {0}: {1}", added.Length, PrintHandles (added));
        	Logger.Debug ("\tRemoved {0}: {1}", removed.Length, PrintHandles (removed));
        	Logger.Debug ("\tLocal Pending {0}: {1}", localpending.Length, PrintHandles (localpending));
        	Logger.Debug ("\tRemote Pending {0}: {1}", remotepending.Length, PrintHandles (remotepending));
        	Logger.Debug ("\tActor: {0}", actor);
        	Logger.Debug ("\tReason: {0}", reason);
        }
		
        private string PrintHandles (uint[] handles)
        {
        	string str = string.Empty;
        	foreach (uint handle in handles) {
        		if (str.Length > 0)
        			str += ", ";
        		str += string.Format ("0", handle);
        	}
        	
        	return str;
        }
        
        private void SetupMediaChannel ()
        {
			mediaChannelObjectPath = 
				tlpConnection.RequestChannel (
					org.freedesktop.Telepathy.ChannelType.StreamedMedia, 
					HandleType.Contact,
					this.peerUser.ID, 
					true);
				
			mediaChannel = 
				Bus.Session.GetObject<IChannelStreamedMedia> (
					account.BusName, 
					mediaChannelObjectPath);
				
			mediaChannel.StreamAdded += OnStreamAdded;
			mediaChannel.StreamDirectionChanged += OnStreamDirectionChanged;
			mediaChannel.StreamError += OnStreamError;
			mediaChannel.StreamRemoved += OnStreamRemoved;
			mediaChannel.StreamStateChanged += OnStreamStateChanged;
			mediaChannel.MembersChanged += OnMembersChanged;
        }
		
		#endregion
		
		#region Internal Methods
		internal void AddMediaChannel (ObjectPath objectPath, IChannelStreamedMedia media)
		{
			mediaChannel = media;
			mediaChannelObjectPath = objectPath;
			mediaChannel.Closed += OnMediaChannelClosed;
			mediaChannel.StreamAdded += OnStreamAdded;
			mediaChannel.StreamDirectionChanged += OnStreamDirectionChanged;
			mediaChannel.StreamError += OnStreamError;
			mediaChannel.StreamRemoved += OnStreamRemoved;
			mediaChannel.StreamStateChanged += OnStreamStateChanged;
			mediaChannel.MembersChanged += OnMembersChanged;
			
			if (MediaChannelOpened != null)
				MediaChannelOpened (this);
		}
		
		#endregion
		
		#region Public Methods
		public void Dispose()
		{
			if (txtChannel != null)	{
				try {txtChannel.Close();} catch{}
				//txtChannel.Close();
				txtChannel = null;
			}
		}
		
		public Banter.Message[] GetReceivedMessages()
		{
			return messages.ToArray();
		}
		
		internal void IndicateReceivedMessages ()
		{
			if (this.MessageReceived != null) {
				AddPendingMessages ();
				Banter.Message[] messages =
					this.GetReceivedMessages ();
				if (messages.Length > 0) {
					foreach (Message msg in messages)
						this.MessageReceived (this, msg);
				}
			} else {
				Logger.Debug ("No registered receive handler");
			}
		}
		
		/// <summary>
		/// Method to indicate a local system message
		/// </summary>
		public void IndicateSystemMessage (string message)
		{
			if (message == null || message == String.Empty)
				return;

			if (MessageSent == null)
				return;
				
			MessageSent (this, new SystemMessage (message));
		}
		
		public void SendMessage (Message message)
		{
			// FIXME::Throw exception
			if (tlpConnection == null) return;
			if (message == null) return;
			
			try {
				// If a text channel doesn't exist attempt to create one
				if (txtChannel == null)
					AddTextChannel ();
				this.txtChannel.Send (org.freedesktop.Telepathy.MessageType.Normal, message.Text);

				if (current != 0)
					last = current;
					
				current = tlpConnection.SelfHandle;
				
				if (MessageSent != null)
					MessageSent (this, message);
				} catch (Exception sm) {
					Logger.Debug ("Conversation::SendMessage failed");
					Logger.Debug (sm.Message);
			}
		}
		
		/// New methods 6/7
		
		/// <summary>
		/// Method to open and setup a text channel
		/// </summary>
		public void AddTextChannel ()
		{
			if (txtChannel != null) return;
			
			txtChannelObjectPath = 
				tlpConnection.RequestChannel (
					org.freedesktop.Telepathy.ChannelType.Text, 
					HandleType.Contact,
					this.peerUser.ID, 
					true);
				
			txtChannel = 
				Bus.Session.GetObject<IChannelText> (account.BusName, txtChannelObjectPath);
			txtChannel.Received += OnReceiveMessageHandler;
			txtChannel.Closed += OnTextChannelClosed;
		}

		/// <summary>
		/// Method to open and setup a text channel
		/// </summary>
		public void AddTextChannel (IChannelText existingTxtChannel)
		{
			if (txtChannel != null) return;
			txtChannel = existingTxtChannel;				
			txtChannel.Received += OnReceiveMessageHandler;
			txtChannel.Closed += OnTextChannelClosed;
		}

		/// <summary>
		/// Method to open and connect and audio
		/// channel with a peer
		/// </summary>
		public void AddAudioChannel ()
		{
			if (mediaChannel == null)
				SetupMediaChannel ();
		}
		
		/// <summary>
		/// Method to open and connect an
		/// audio and open and connect a video
		/// channel with a peer
		/// </summary>
		public void AddAudioVideoChannels ()
		{
			if (mediaChannel == null)
				SetupMediaChannel ();
		}
		
		/// <summary>
		/// Method to close an existing text channel
		/// </summary>
		public void RemoveTextChannel ()
		{
			if (txtChannel == null) return;
			txtChannel.Received -= OnReceiveMessageHandler;
			try {txtChannel.Close();} catch{}
			txtChannel = null;
		}
		
		/// <summary>
		/// Method to close an existing media channel
		/// Note: all outstanding streams will be closed
		/// </summary>
		public void RemoveMediaChannel ()
		{
			if (mediaChannel == null) return;
			outputWindowIsSet = false;

			
			uint i = 0;
			try {
				uint[] streams = new uint[videoStreams.Count + audioStreams.Count];
				
      			foreach (KeyValuePair<uint,uint> kvp in videoStreams)
      				streams[i++] = kvp.Value;
      				
      			foreach (KeyValuePair<uint,uint> kvp in audioStreams)
      				streams[i++] = kvp.Value;
					
				((IChannelStreamedMedia) mediaChannel).RemoveStreams (streams);					
	
				audioStreams.Clear();
				videoStreams.Clear();
				mediaChannel.Close ();
				Logger.Debug ("Completed Media Channel cleanup");
			} catch{}
			
			IndicateSystemMessage ("Video chat stopped");	
			mediaChannel = null;
		}
		
		
		/// <summary>
		/// Method to start streaming the audio channel
		/// </summary>
		public void StartAudioStream ()
		{
			Logger.Debug ("StartAudioStream - called");
			
			if (mediaChannel == null)
				throw new ApplicationException ("Streamed media channel does not exist");
				
			IChannelHandler	channelHandler = 
				Bus.Session.GetObject<IChannelHandler> (
			    	"org.freedesktop.Telepathy.StreamEngine",
			    	new ObjectPath ("/org/freedesktop/Telepathy/StreamEngine"));
			    	
			if (channelHandler == null)
				throw new ApplicationException ("Failed get a channel handler");
		        
			Logger.Debug("Have the channelHandler... telling it to handle the channel");
			channelHandler.HandleChannel (
				account.BusName,
				account.BusPath,
			    mediaChannel.ChannelType, 
			    mediaChannelObjectPath,
			    0,
			    0);

			StreamInfo[] lst = ((IChannelStreamedMedia) mediaChannel).ListStreams();
			Logger.Debug("StreamInfo List Length: {0}", lst.Length);
				
			uint tempStreamId;
	       	foreach (StreamInfo info in lst) {
	       		Logger.Debug("Stream Info: Id:{0}, Type:{1}, ContactHandle:{2}, Direction: {3}",
                           info.Id, info.Type, info.ContactHandle, info.Direction);

				// Save the type of the stream so we can reference it later
                SaveStream (info.Type, info.Id);	       	
			}
				
			streamEngine = 
				Bus.Session.GetObject<IStreamEngine> (
					"org.freedesktop.Telepathy.StreamEngine",
	           		new ObjectPath ("/org/freedesktop/Telepathy/StreamEngine"));
			streamEngine.Receiving += OnStreamEngineReceiving;
			
			// Startup an audio stream
			if (this.initiated == true) {
				uint[] streamtypes = new uint[1];
				streamtypes[0] = (uint) StreamType.Audio;
				uint[] handles = new uint [] {peerUser.ID};
				StreamInfo[] infos = mediaChannel.RequestStreams (handles[0], streamtypes);
			}
		}
		
		/// <summary>
		/// Method to start streaming the audio and 
		/// video channels
		/// </summary>
		public void StartAudioVideoStreams (uint previewWindowId, uint peerwindowId)
		{
			Logger.Debug ("StartAudioVideoStreams - called");
			
			if (mediaChannel == null)
				throw new ApplicationException ("Media stream channel does not exist");
				
			this.peerWindowID = peerwindowId;
			this.previewWindowID = previewWindowId;
			
			IChannelHandler	channelHandler = 
				Bus.Session.GetObject<IChannelHandler> (
			    	"org.freedesktop.Telepathy.StreamEngine",
			    	new ObjectPath ("/org/freedesktop/Telepathy/StreamEngine"));
			    	
			if (channelHandler == null)
				throw new ApplicationException ("Failed get a channel handler");
		        
			Logger.Debug("Have the channelHandler... telling it to handle the channel");
			channelHandler.HandleChannel (
				account.BusName,
				account.BusPath,
			    mediaChannel.ChannelType, 
			    mediaChannelObjectPath,
			    0,
			    0);

			StreamInfo[] lst = ((IChannelStreamedMedia) mediaChannel).ListStreams();
			Logger.Debug("StreamInfo List Length: {0}", lst.Length);
				
			uint tempStreamId;
	       	foreach (StreamInfo info in lst) {
	       		Logger.Debug("Stream Info: Id:{0}, Type:{1}, ContactHandle:{2}, Direction: {3}",
                           info.Id, info.Type, info.ContactHandle, info.Direction);

				// Save the type of the stream so we can reference it later
                SaveStream (info.Type, info.Id);
			}
		       
			streamEngine = 
				Bus.Session.GetObject<IStreamEngine> (
					"org.freedesktop.Telepathy.StreamEngine",
	           		new ObjectPath ("/org/freedesktop/Telepathy/StreamEngine"));

			if (this.videoStreams.Count > 0) {
				Logger.Debug("Adding Preview Window {0}", previewWindowId);
			    streamEngine.AddPreviewWindow(previewWindowId);
				IndicateSystemMessage ("Video chat started");
			} else
				IndicateSystemMessage ("Audio chat started");

			streamEngine.Receiving += OnStreamEngineReceiving;
			
			if (this.initiated == true) {
				uint[] streamtypes;
				if (this.videoStreams.Count > 0 &&
				    this.audioStreams.Count > 0) {
					streamtypes = new uint[2];
					streamtypes[0] = (uint) StreamType.Audio;
					streamtypes[1] = (uint) StreamType.Video;
				} else {
					streamtypes = new uint[1];
					streamtypes[0] = (uint) StreamType.Audio;
				}

				Logger.Debug("Requesting streams from media channel");
				uint[] handles = new uint [] {peerUser.ID};
				StreamInfo[] infos = mediaChannel.RequestStreams (handles[0], streamtypes);
			}
		}
		#endregion
	}
}	
