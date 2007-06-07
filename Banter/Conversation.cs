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
	public delegate void VideoChannelInitializedHandler (Conversation conversation);
	public delegate void VideoChannelConnectedHandler (Conversation conversation, uint streamId);
	public delegate void VideoStreamAddedHandler (Conversation conversation, uint streamId);
	public delegate void AudioChannelInitializedHandler (Conversation conversation);

	public delegate void NewAudioChannelHandler (Conversation conversation);
	public delegate void NewVideoChannelHandler (Conversation conversation);
	public delegate void TextChannelOpenedHandler (Conversation conversation);
	public delegate void TextChannelClosedHandler (Conversation conversation);
	
	public class Conversation : IDisposable
	{
		public event MessageSentHandler MessageSent;
		public event MessageReceivedHandler MessageReceived;
		public event VideoChannelInitializedHandler VideoChannelInitialized;
		public event VideoChannelConnectedHandler VideoChannelConnected;
		public event VideoStreamAddedHandler VideoStreamAdded;
		public event AudioChannelInitializedHandler AudioChannelInitialized;
		
		public event NewAudioChannelHandler NewAudioChannel;
		public event NewVideoChannelHandler NewVideoChannel;
		public event TextChannelOpenedHandler NewTextChannel;
		public event TextChannelClosedHandler TextChannelClosed;

		// True the conversation was initiated locally
		// False the conversation was initiated by an incoming
		// request from a peer.
		private bool initiated;
		
		private uint previewWindowID;
		private uint peerWindowID;
		private List<Message> messages;
		private Account account;
		
		// Telepathy connection and channels		
		private org.freedesktop.Telepathy.IConnection tlpConnection;
		private IChannelHandler channelHandler;
		private ObjectPath videoChannelObjectPath;
		private ObjectPath audioChannelObjectPath;
		private org.freedesktop.Telepathy.IChannelText txtChannel;
		private IChannelStreamedMedia videoChannel;
		private IChannelStreamedMedia audioChannel;
		private IStreamEngine streamEngine;
		private uint videoInputStreamId = 0;
		
		private uint textStreamId = 0;
		private uint audioStreamId = 0;
		private uint videoStreamId = 0;
		
		private ProviderUser peerUser;
		private Presence lastPeerPresence;
		
		private uint current;
		private uint last;
		
		private System.Collections.Generic.Dictionary<uint, uint> videoStreams;
	
		public bool ActiveTextChannel
		{
			get {return (textStreamId != 0) ? true : false;}			
		}

		public bool ActiveAudioChannel
		{
			get {return (audioStreamId != 0) ? true : false;}			
		}
		
		public bool ActiveVideoChannel
		{
			get {return (videoStreamId != 0) ? true : false;}			
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
			this.account = AccountManagement.GetAccountByName (providerUser.AccountName);
			this.tlpConnection = this.account.TlpConnection;
			this.peerUser = providerUser;
			this.messages = new List<Message> ();
			last = 999;
			this.videoStreams = new Dictionary<uint,uint> ();

			peerUser.PresenceUpdated += OnPeerPresenceUpdated;
			lastPeerPresence = peerUser.Presence;
			this.initiated = true;
		}
		
		
		internal Conversation (Account account, ProviderUser providerUser, IChannelStreamedMedia channel)
		{
			this.account = account;
			this.tlpConnection = this.account.TlpConnection;
			this.peerUser = providerUser;
			this.messages = new List<Message> ();
			last = 999;
			this.videoStreams = new Dictionary<uint,uint> ();

			peerUser.PresenceUpdated += OnPeerPresenceUpdated;
			lastPeerPresence = peerUser.Presence;
			this.initiated = false;

			// Figure out the streamed media type
			videoChannel = channel;
			videoChannel.StreamAdded += OnStreamAdded;
			videoChannel.StreamDirectionChanged += OnStreamDirectionChanged;
			videoChannel.StreamError += OnStreamError;
			videoChannel.StreamRemoved += OnStreamRemoved;
			videoChannel.StreamStateChanged += OnStreamStateChanged;
			videoChannel.MembersChanged += OnMembersChanged;
		}


		internal Conversation (Account account, ProviderUser providerUser, IChannelText channel)
		{
			this.account = account;
			this.tlpConnection = this.account.TlpConnection;
			this.peerUser = providerUser;
			this.messages = new List<Message> ();
			last = 999;
			this.videoStreams = new Dictionary<uint,uint> ();

			peerUser.PresenceUpdated += OnPeerPresenceUpdated;
			lastPeerPresence = peerUser.Presence;
			this.initiated = false;
			
			txtChannel = channel;
			txtChannel.Received += OnReceiveMessageHandler;
			txtChannel.Closed += OnTextChannelClosed;
			
			// Check for any pending messages and add them to our list
//			TextMessage txtMessage;
//			PendingMessageInfo[] msgInfos = txtChannel.ListPendingMessages (true);
//			foreach (PendingMessageInfo info in msgInfos) {
//				Logger.Debug ("Pending Message: {0}", info.Message);
//				txtMessage = new TextMessage (info.Message);
//				txtMessage.From = peerUser.Uri;
//				messages.Add (txtMessage);
//			}
			
		}

		
		internal Conversation (Account account, Person peer, ProviderUser peerUser, bool initiate)
		{
			this.account = account;
			this.tlpConnection = account.TlpConnection;
			this.peerUser = peerUser;
			this.messages = new List<Message> ();
			last = 999;
			this.videoStreams = new Dictionary<uint,uint> ();

			peerUser.PresenceUpdated += OnPeerPresenceUpdated;
			lastPeerPresence = peerUser.Presence;
			this.initiated = initiate;
			
			/*
			if (initiate == true)
				this.CreateTextChannel ();
			*/
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
 				Logger.Debug("SaveStream called on a non-Video Stream");
                break;
            case StreamType.Video:
                if (videoStreams.ContainsKey (id) == false) {
                    Logger.Debug ("Saving video stream id: {0}", id);
                    videoStreams [id] = id;
                }
                break;
            }
        }

        private void RemoveStream (uint id)
        {
            if (videoStreams.ContainsKey (id)) {
                Logger.Debug ("Removing video stream: {0}", id);
                videoStreams.Remove (id);
            }
        }
		#endregion
		
		#region Public Methods
		public void SetVideoWindows (uint meID, uint peerID)
		{
			this.previewWindowID = meID;
			this.peerWindowID = peerID;
		}
		
		/*
		private void CreateTextChannel ()
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
		*/
		
		private bool SetupVideoChannel ()
		{
			if (initiated == true && videoChannel != null) return true;
			
			Logger.Debug ("SetupVideoChannel entered");

			uint[] handles = new uint [] {peerUser.ID};
			try
			{
				if (this.initiated == true) {
				
					try {
						videoChannelObjectPath = 
							tlpConnection.RequestChannel (
								org.freedesktop.Telepathy.ChannelType.StreamedMedia,
								HandleType.Contact,
								handles[0],
								true);
								
						Logger.Debug("Have the Video Channel Object Path");
						videoChannel = 
							Bus.Session.GetObject<IChannelStreamedMedia> (
								account.BusName, videoChannelObjectPath);
						
					} catch (Exception vrequest) {
					
						switch (vrequest.Message.Split (':')[0]) {
							case "org.freedesktop.Telepathy.Error.NotAvailable":
							{
								IndicateSystemMessage (
									String.Format (
										"{0} does not have video capability", 
										this.PeerUser.Alias));
								break;
							}
							
							default:
							{
								Logger.Debug ("Caught an unsuspected exception");
								break;
							}
						}
						
						Logger.Debug (vrequest.Message);
						Logger.Debug (vrequest.StackTrace);
					}
					
					if (videoChannel == null) {
						Logger.Debug ("videoChannel is null!");
						throw new ApplicationException ("Cannot proceed with a null video channel");
					}
				}

				Logger.Debug("Have a Video Channel");

				videoChannel.StreamAdded += OnStreamAdded;
				videoChannel.StreamDirectionChanged += OnStreamDirectionChanged;
				videoChannel.StreamError += OnStreamError;
				videoChannel.StreamRemoved += OnStreamRemoved;
				videoChannel.StreamStateChanged += OnStreamStateChanged;
				videoChannel.MembersChanged += OnMembersChanged;

				Logger.Debug("Initializing video channel with ChannelHandler");
				
				channelHandler = 
			       	Bus.Session.GetObject<IChannelHandler> (
			       		"org.freedesktop.Telepathy.StreamEngine",
			           	new ObjectPath ("/org/freedesktop/Telepathy/StreamEngine"));
		           
				if (channelHandler != null) 
				{
					Logger.Debug("Have the channelHandler... telling it to handle the channel");
					channelHandler.HandleChannel (
						account.BusName,
						account.BusPath,
					    videoChannel.ChannelType, 
					    this.videoChannelObjectPath,
					    0,
					    0);
				}
				else
		       		Logger.Debug("Didn't get a channel handler");

				StreamInfo[] lst = ((IChannelStreamedMedia) videoChannel).ListStreams();
				Logger.Debug("StreamInfo List Length: {0}", lst.Length);
				
				uint tempStreamId;
		       	foreach (StreamInfo info in lst)
		       	{
		       		Logger.Debug("Stream Info: Id:{0}, Type:{1}, ContactHandle:{2}, Direction: {3}",
                            info.Id, info.Type, info.ContactHandle, info.Direction);

                    // Save the type of the stream so we can reference it later
                    SaveStream (info.Type, info.Id);	       	

/*		       		Logger.Debug ("Setting peer Window ID - Object Path: {0}", this.videoChannelObjectPath.ToString());
		       		Logger.Debug ("info ID: {0}  Contact Handle: {1}", info.Id, info.ContactHandle);
		       		tempStreamId = info.Id;
		       	
		       		Logger.Debug(
		       			"Stream Info: Id:{0}, Type:{1}, ContactHandle:{2}, Direction: {3}",
		       			info.Id,
		       			info.Type,
		       			info.ContactHandle,
		       			info.Direction);
		       				
		       		// Save the type of the stream so we can reference it later
		       		//SaveStream (info.Type, info.Id);
		       */
		       }
		       

				Logger.Debug("Getting the stream_engine");
				
				streamEngine = 
					Bus.Session.GetObject<IStreamEngine> (
						"org.freedesktop.Telepathy.StreamEngine",
		           		new ObjectPath ("/org/freedesktop/Telepathy/StreamEngine"));

		        Logger.Debug("have the stream engine");
		        
				Logger.Debug("Adding Preview Window {0}", previewWindowID);
			    streamEngine.AddPreviewWindow(previewWindowID);
			    
				streamEngine.Receiving += OnStreamEngineReceiving;

				Logger.Debug("The numder of members is: {0}", videoChannel.Members.Length);

				if (this.initiated == true) {
	//				uint[] stream_type = new uint[2];
					uint[] streamtype = new uint[1];
					
	//				stream_type[0] = (uint) StreamType.Audio;
					streamtype[0] = (uint) StreamType.Video;

					Logger.Debug("Requesting streams from video channel");
					StreamInfo[] infos = videoChannel.RequestStreams (handles[0], streamtype);
					
					Logger.Debug("Number of Streams Received: {0}", infos.Length);
					Logger.Debug("Stream Info: Id{0} State{1} Direction{2} ContactHandle{3}", infos[0].Id, infos[0].State, infos[0].Direction, infos[0].ContactHandle);
				}
				
				if (VideoChannelInitialized != null)
					VideoChannelInitialized (this);
			}
			catch(Exception e)
			{
				Logger.Debug("Exception in StartVideoChannel: {0}\n{1}", e.Message, e.StackTrace);
			}

			return true;
		}	
		
		
        private void OnStreamAdded (uint streamid, uint contacthandle, org.freedesktop.Telepathy.StreamType streamtype)
		{
			Logger.Debug(
				"OnStreamAdded of type: {0} with id:{1}, for contact {2}", 
				streamtype, 
				streamid, 
				contacthandle);

			SaveStream (streamtype, streamid);
				
			if (streamtype == StreamType.Video )
				if (VideoStreamAdded != null)
					VideoStreamAdded (this, streamid);
		}

		private void OnStreamDirectionChanged (uint streamid, StreamDirection streamdirection, StreamPendingFlags pendingflags)
        {
			Logger.Debug("OnStreamDirectionChanged called");
        }

        private void OnStreamError (uint streamid, uint errno, string message)
        {
 			Logger.Debug("OnStreamError called with message: {0}:{1}", errno, message);
        }

        private void OnStreamRemoved (uint streamid)
        {
			Logger.Debug("OnStreamRemoved called on stream {0}", streamid);
			RemoveStream (streamid);
        }

        private void OnStreamStateChanged (uint streamid, org.freedesktop.Telepathy.StreamState streamstate)
        {
            Logger.Debug ("OnStreamStateChanged called - ID: {0} State: {1}", streamid, streamstate);
 
            // Audio or Video
            videoInputStreamId = streamid;
            
	       	switch (streamstate ) {
	       		case StreamState.Connecting:
	       		{
					if(videoStreams.ContainsKey(streamid)) {
						Logger.Debug("Stream State: Connecting and we found the streamid {0}", streamid);
						Logger.Debug("Setting output window {0}", peerWindowID);
						streamEngine.SetOutputWindow (
							this.videoChannelObjectPath, 
							streamid,
							this.peerWindowID);
	       			}
	       			else
						Logger.Debug("Stream State: Connecting but stream ID NOT found {0}", streamid);	       			
	       			break;
	       		}
	       		
	       		case StreamState.Connected:
	       		{
	       			//IndicateSystemMessage ("Video chat connected!");
						if (VideoChannelConnected != null)
							VideoChannelConnected (this, streamid);
	       			break;
	       		}
	       		
	       		case StreamState.Playing:
	       		{
	       			//IndicateSystemMessage ("Video chat playing");
	       			break;
	       		}
	       		
	       		case StreamState.Stopped:
	       		{
	       			//IndicateSystemMessage ("Video chat stopped");
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
			if (txtChannel == null) return;
			if (message == null) return;
			
			this.txtChannel.Send (org.freedesktop.Telepathy.MessageType.Normal, message.Text);

			if (current != 0)
				last = current;
				
			current = tlpConnection.SelfHandle;
			
			if (MessageSent != null)
				MessageSent (this, message);
		}
		
		public void SetMediaChannel (IChannelStreamedMedia channel, ObjectPath op)
		{
			Logger.Debug ("SetMediaChannel - called");
			Logger.Debug ("  Object Path: {0}", op.ToString() );
			videoChannelObjectPath = op;
			videoChannel = channel;
		}
		
		public void SetPreviewWindow (uint windowId)
		{
			previewWindowID = windowId;
		}
		
		public void SetPeerWindow (uint windowId)
		{
			this.peerWindowID = windowId;
			Logger.Debug ("peer window ID private: {0}", this.peerWindowID);
		}
		
		public void SetPeerWindow (uint windowId, uint streamId)
		{
			Logger.Debug("SetPeerWindow was called with windowID {0}", windowId);
			
			this.peerWindowID = windowId;
			
			if (streamEngine != null && 
				videoChannelObjectPath != null &&
				videoChannel != null) {
				streamEngine.SetOutputWindow (
					videoChannelObjectPath, 
					streamId,
					windowId);
			}
		}
		
		/*
		public void StartVideo (bool initiatedChat)
		{
			this.initiated = initiatedChat;
			if (tlpConnection == null) 
				throw new ApplicationException (String.Format ("No telepathy connection exists"));
				
			// FIXME::localize
			//this.IndicateSystemMessage ("Starting video chat");
			
			// Create the video channel
			SetupVideoChannel ();
		}
		*/
		
		
		/// New methods 6/7
		
		/// <summary>
		/// Method to open and setup a text channel
		/// </summary>
		public void AddTextChannel ()
		{
			if (txtChannel != null) return;
			
			ObjectPath op = 
				tlpConnection.RequestChannel (
					org.freedesktop.Telepathy.ChannelType.Text, 
					HandleType.Contact,
					this.peerUser.ID, 
					true);
				
			txtChannel = Bus.Session.GetObject<IChannelText> (account.BusName, op);
			txtChannel.Received += OnReceiveMessageHandler;
			txtChannel.Closed += OnTextChannelClosed;
		}

		/// <summary>
		/// Method to open and connect and audio
		/// channel with a peer
		/// </summary>
		public void AddAudioChannel ()
		{
		}
		
		/// <summary>
		/// Method to open and connect an
		/// audio and open and connect a video
		/// channel with a peer
		/// </summary>
		public void AddAudioVideoChannels ()
		{
		}
		
		/// <summary>
		/// Method to close an existing text channel
		/// </summary>
		public void RemoveTextChannel ()
		{
			if (txtChannel == null) return;
			txtChannel.Received -= OnReceiveMessageHandler;
			txtChannel = null;
		}
		
		/// <summary>
		/// Method to close an existing audio channel
		/// </summary>
		public void RemoveAudioChannel ()
		{
		}
		
		/// <summary>
		/// Method to close existing audio and
		/// video channels
		/// </summary>
		public void RemoveAudioVideoChannels ()
		{
		}
		
		/// <summary>
		/// Method to start streaming the audio channel
		/// </summary>
		public void StartAudioStream ()
		{
		}
		
		/// <summary>
		/// Method to start streaming the audio and 
		/// video channels
		/// </summary>
		public void StartAudioVideoStreams (uint windowId)
		{
			if (videoChannel == null)
				throw new ApplicationException ("Video Channel does not exist");
				
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
			    videoChannel.ChannelType, 
			    this.videoChannelObjectPath,
			    0,
			    0);

			StreamInfo[] lst = ((IChannelStreamedMedia) videoChannel).ListStreams();
			Logger.Debug("StreamInfo List Length: {0}", lst.Length);
				
			uint tempStreamId;
	       	foreach (StreamInfo info in lst) {
	       		Logger.Debug("Stream Info: Id:{0}, Type:{1}, ContactHandle:{2}, Direction: {3}",
                           info.Id, info.Type, info.ContactHandle, info.Direction);

				// Save the type of the stream so we can reference it later
                SaveStream (info.Type, info.Id);	       	

				/*
	       		Logger.Debug ("Setting peer Window ID - Object Path: {0}", this.videoChannelObjectPath.ToString());
	       		Logger.Debug ("info ID: {0}  Contact Handle: {1}", info.Id, info.ContactHandle);
	       		tempStreamId = info.Id;
	       	
	       		Logger.Debug(
	       			"Stream Info: Id:{0}, Type:{1}, ContactHandle:{2}, Direction: {3}",
	       			info.Id,
	       			info.Type,
	       			info.ContactHandle,
	       			info.Direction);
	       				
	       		// Save the type of the stream so we can reference it later
	       		//SaveStream (info.Type, info.Id);
		       */
			}
		       
			Logger.Debug("Getting the stream_engine");
				
			streamEngine = 
				Bus.Session.GetObject<IStreamEngine> (
					"org.freedesktop.Telepathy.StreamEngine",
	           		new ObjectPath ("/org/freedesktop/Telepathy/StreamEngine"));

	        Logger.Debug("have the stream engine");
		        
			Logger.Debug("Adding Preview Window {0}", windowId);
		    streamEngine.AddPreviewWindow(windowId);
			streamEngine.Receiving += OnStreamEngineReceiving;

			Logger.Debug("The numder of members is: {0}", videoChannel.Members.Length);

			if (this.initiated == true) {
	//				uint[] stream_type = new uint[2];
				uint[] streamtype = new uint[1];
					
	//				stream_type[0] = (uint) StreamType.Audio;
				streamtype[0] = (uint) StreamType.Video;

				Logger.Debug("Requesting streams from video channel");
				uint[] handles = new uint [] {peerUser.ID};
				StreamInfo[] infos = videoChannel.RequestStreams (handles[0], streamtype);
					
				Logger.Debug("Number of Streams Received: {0}", infos.Length);
				Logger.Debug("Stream Info: Id{0} State{1} Direction{2} ContactHandle{3}", infos[0].Id, infos[0].State, infos[0].Direction, infos[0].ContactHandle);
			}
			
			/*
			if (VideoChannelInitialized != null)
				VideoChannelInitialized (this);
			*/
		}
		#endregion
	}
}	
