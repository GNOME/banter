//***********************************************************************
// *  $RCSfile$ - VideoConversation.cs
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

namespace Novell.Rtc
{
	public class VideoConversation : IDisposable
	{
		private IChannelStreamedMedia mediaChannel;
		private Connection conn;
//		private IConnection tlpConnection;
		private ConnectionStatus status;
		private uint previewWindowID, feedWindowID;
		private IStreamEngine stream_engine;
		private ObjectPath mcObjectPath;
		private IChannelHandler channel_handler;
		private Member targetMember;
		private Tapioca.Connection tapiocaConnection;
		private Tapioca.Contact targetContact;
		private bool incomingCall;
		
		// Whenever you create/add a new stream, make sure to stuff it in one of these
		// dictionaries so that handlers later can determine what type of stream they're
		// dealing with.
		private System.Collections.Generic.Dictionary<uint, uint> audioStreams;
		private System.Collections.Generic.Dictionary<uint, uint> videoStreams;
	
		public IChannelStreamedMedia MediaChannel
		{
			get {return mediaChannel;}
		}

		// <summary>
		// This constructor should only be used for placing outgoing calls
		// </summary>
		public VideoConversation (Member targetMember, uint previewWindowID,
									uint feedWindowID)
		{
			this.incomingCall = false;
			ConstructorInit (targetMember, previewWindowID, feedWindowID);
		}
		
		// <summary>
		// This constructor should only be used for placing outgoing calls
		// </summary>
		public VideoConversation (
					Tapioca.Connection tapiocaConnection,
					Tapioca.Contact targetContact, 
					uint previewWindowID,
					uint feedWindowID)
		{
			this.incomingCall = false;
			TapiocaConstructorInit (tapiocaConnection, targetContact, previewWindowID, feedWindowID);
		}
		
		// <summary>
		// This constructor should only be used for incoming calls
		// </summary>
		public VideoConversation (Member targetMember, IChannelStreamedMedia incomingMediaChannel,
									ObjectPath mediaChannelObjectPath,
									uint previewWindowID, uint feedWindowID)
		{
			this.incomingCall = true;
			this.mediaChannel = incomingMediaChannel;
			this.mcObjectPath = mediaChannelObjectPath;
			ConstructorInit (targetMember, previewWindowID, feedWindowID);
		}
		
		private void TapiocaConstructorInit(
						Tapioca.Connection tapiocaConnection,
						Tapioca.Contact targetContact,
						uint previewWindowID,
						uint peerWindowID)
		{
			this.previewWindowID = previewWindowID;
			this.feedWindowID = peerWindowID;
			this.targetContact = targetContact;
			this.audioStreams = new Dictionary<uint,uint> ();
			this.videoStreams = new Dictionary<uint,uint> ();

			this.tapiocaConnection = tapiocaConnection;
				
			if (conn.TlpConnection == null) 
			{
		    	throw new ApplicationException (
		    		String.Format (
		    			"Failed to get a telepathy connection from the member: {0}",
		    			targetMember.Alias));
			}
			
			if (SetupMediaChannel () == false) {
				throw new ApplicationException ("Failed to set up the media channel");
			}
		}
		
		
		private void ConstructorInit (Member targetMember, uint previewWindowID, uint feedWindowID)
		{
			this.previewWindowID = previewWindowID;
			this.feedWindowID = feedWindowID;
			this.targetMember = targetMember;
			this.audioStreams = new Dictionary<uint,uint> ();
			this.videoStreams = new Dictionary<uint,uint> ();

			conn = targetMember.Connection;
				
			if (conn.TlpConnection == null) 
			{
		    	throw new ApplicationException (
		    		String.Format (
		    			"Failed to get a telepathy connection from the member: {0}",
		    			targetMember.Alias));
			}
			
			if (SetupMediaChannel () == false) {
				throw new ApplicationException ("Failed to set up the media channel");
			}
		}
		
		private bool SetupMediaChannel()
		{
			Logger.Debug ("SetupMediaChannel entered");
			uint[] handles = new uint [] {targetMember.Handle};
			try
			{
				if (this.incomingCall == false) {
					// Outgoing call and need to create the media channel ourself
Logger.Debug ("Using member handle: {0}", targetMember.Handle);

					Logger.Debug("Setting up Media Channel");
					mcObjectPath = 
						conn.TlpConnection.RequestChannel (
							org.freedesktop.Telepathy.ChannelType.StreamedMedia,
							HandleType.Contact,
							handles[0],
							true);
					Logger.Debug("Have the Media Channel Object Path");
					mediaChannel = 
						Bus.Session.GetObject<IChannelStreamedMedia> (conn.TlpConnectionInfo.BusName, mcObjectPath);
					
					if (mediaChannel == null) {
						Logger.Debug ("mediaChannel is null!");
						throw new ApplicationException ("Cannot proceed with a null media channel");
					}
				}

				Logger.Debug("Have the Media Channel");
				
				Logger.Debug("Initializing media channel with ChannelHandler");						
		       //initialize media channel
		       channel_handler = Bus.Session.GetObject<IChannelHandler> ("org.freedesktop.Telepathy.StreamEngine",
		           new ObjectPath ("/org/freedesktop/Telepathy/StreamEngine"));
		           
		       if (channel_handler != null) 
		       {
		       	ConnectionInfo connectionInfo = conn.TlpConnectionInfo;
		       	Logger.Debug("Have the channelHandler... telling it to handle the channel");
		       	channel_handler.HandleChannel (connectionInfo.BusName, connectionInfo.ObjectPath,
		                                           mediaChannel.ChannelType, mcObjectPath,
		                                           0, 0);
		       }
		       else
		       	Logger.Debug("Didn't get a channel handler");

	       
		       StreamInfo[] lst = ((IChannelStreamedMedia) mediaChannel).ListStreams();

				Logger.Debug("StreamInfo List Length: {0}", lst.Length);
				
		       foreach (StreamInfo info in lst)
		       {
		       		Logger.Debug("Stream Info: Id:{0}, Type:{1}, ContactHandle:{2}, Direction: {3}", 
		       				info.Id, info.Type, info.ContactHandle, info.Direction);
		       				
		       		// Save the type of the stream so we can reference it later
		       		SaveStream (info.Type, info.Id);
		       }
		       
		       mediaChannel.StreamAdded += OnStreamAdded;
		       mediaChannel.StreamDirectionChanged += OnStreamDirectionChanged;
		       mediaChannel.StreamError += OnStreamError;
		       mediaChannel.StreamRemoved += OnStreamRemoved;
		       mediaChannel.StreamStateChanged += OnStreamStateChanged;
		       
		       mediaChannel.MembersChanged += OnMembersChanged;

//					Logger.Debug("Adding Members to the Media Channel");
					
//					mediaChannel.AddMembers(handles, "Welcome to my video chat");

//					Logger.Debug("Added Members.");

//		       Logger.Debug("Sleeping 'cause we don't know what else to do...");
//		       System.Threading.Thread.Sleep(4000);	


				Logger.Debug("Getting the stream_engine");
				
				stream_engine = Bus.Session.GetObject<IStreamEngine> ("org.freedesktop.Telepathy.StreamEngine",
		           new ObjectPath ("/org/freedesktop/Telepathy/StreamEngine"));

		        Logger.Debug("have the stream engine");
		        
				Logger.Debug("Adding Preview Window");
			    stream_engine.AddPreviewWindow(previewWindowID);

				stream_engine.Receiving += OnStreamEngineReceiving;

				Logger.Debug("The numder of members is: {0}", mediaChannel.Members.Length);

				if (this.incomingCall == false) {
					uint[] stream_type = new uint[1];
//					uint[] stream_type = new uint[2];

					stream_type[0] = (uint) StreamType.Video;
//					stream_type[1] = (uint) StreamType.Audio;


					Logger.Debug("Requesting streams from media channel");
					
					
	//				if (this.incomingCall == false) {
	//					// Outgoing call
	//					uint [] myself = new uint [] {conn.TlpConnection.SelfHandle};
	//					mediaChannel.AddMembers (myself, String.Empty);
	//				} else {
	//					// Incoming call
	//				}
					
					StreamInfo[] infos = mediaChannel.RequestStreams (handles[0], stream_type);
					
					Logger.Debug("Number of Streams Received: {0}", infos.Length);
					
					Logger.Debug("Stream Info: Id{0} State{1} Direction{2} ContactHandle{3}", infos[0].Id, infos[0].State, infos[0].Direction, infos[0].ContactHandle);
				}
			}
			catch(Exception e)
			{
				Logger.Debug("Exception in SetupMediaChannel: {0}\n{1}", e.Message, e.StackTrace);
			}
//			mediaChannel.Received += OnReceiveMessageHandler;

			return true;
		}

		public void Dispose()
		{
			if (mediaChannel != null) {
				mediaChannel.Close();
				mediaChannel = null;
			}
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
       	
       	private void RemoveStream (uint id)
       	{
       		if (audioStreams.ContainsKey (id)) {
       			Logger.Debug ("Removing audio stream: {0}", id);
       			audioStreams.Remove (id);
       		} else if (videoStreams.ContainsKey (id)) {
       			Logger.Debug ("Removing video stream: {0}", id);
       			videoStreams.Remove (id);
       		}
       	}
		
        private void OnStreamAdded (uint stream_id, uint contact_handle, org.freedesktop.Telepathy.StreamType stream_type)
		{
			Logger.Debug("OnStreamAdded of type: {0} with id:{1}, for contact {2}", stream_type, stream_id, contact_handle);
			SaveStream (stream_type, stream_id);

		}

		private void OnStreamDirectionChanged (uint stream_id, StreamDirection stream_direction, StreamPendingFlags pending_flags)
        {
			Logger.Debug("OnStreamDirectionChanged called");
            //TODO
        }

        private void OnStreamError (uint stream_id, uint errno, string message)
        {
 			Logger.Debug("OnStreamError called with message: {0}:{1}", errno, message);
        }

        private void OnStreamRemoved (uint stream_id)
        {
			Logger.Debug("OnStreamRemoved called on stream {0}", stream_id);
			RemoveStream (stream_id);
        }

        private void OnStreamStateChanged (uint stream_id, org.freedesktop.Telepathy.StreamState stream_state)
        {
            Logger.Debug ("OnStreamStateChanged called : {0}/{1}", stream_id, stream_state);
            
            // Make sure that this stream is a Video stream
            if (videoStreams.ContainsKey (stream_id) && stream_state == StreamState.Connecting)
			{
	   			try
				{
			    	Logger.Debug("Adding Output Window");
					stream_engine.SetOutputWindow (mcObjectPath, stream_id, feedWindowID);
					Logger.Debug("Preview and Output Windows are set");
				}
				catch(Exception e)
				{
					Logger.Debug(e.Message);
					Logger.Debug(e.StackTrace);
				}
			
			}

        }

        private void OnStreamEngineReceiving (ObjectPath channel_path, uint stream_id, bool state)      
        {   
        	Logger.Debug("OnStreamEngineReceiving: stream id: {0}", stream_id);
        }
        
        private void OnMembersChanged (string message, uint[] added, uint[] removed, uint[] local_pending, uint[] remote_pending, uint actor, uint reason)
        {
        	Logger.Debug ("OnMembersChanged: {0}", message);
        	
        	Logger.Debug ("\tAdded {0}: {1}", added.Length, PrintHandles (added));
        	Logger.Debug ("\tRemoved {0}: {1}", removed.Length, PrintHandles (removed));
        	Logger.Debug ("\tLocal Pending {0}: {1}", local_pending.Length, PrintHandles (local_pending));
        	Logger.Debug ("\tRemote Pending {0}: {1}", remote_pending.Length, PrintHandles (remote_pending));
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
	}
}	