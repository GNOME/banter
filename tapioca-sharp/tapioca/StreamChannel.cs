/***************************************************************************
 *  ChatSession.cs
 *
 *  Copyright (C) 2006 INdT
 *  Written by
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
using System.Collections;
using NDesk.DBus;
using org.freedesktop.DBus;
using org.freedesktop.Telepathy;
using ObjectPath = NDesk.DBus.ObjectPath;

namespace Tapioca
{
	public delegate void StreamChannelNewStreamHandler (StreamChannel sender, StreamObject stream_object);
	public delegate void StreamChannelStreamLostHandler (StreamChannel sender, StreamObject stream_object);
	
	public class StreamChannel : Channel
	{
		public event StreamChannelNewStreamHandler NewStream;
		public event StreamChannelStreamLostHandler LostStream;
		
		private Tapioca.Connection connection;
		private System.Collections.Hashtable hash_stream;	
		private bool requesting;
		private ChannelTarget remoteTarget;
		
//public methods:		
		public override ChannelType Type {
			get {
				return ChannelType.StreamedMedia;
			}
		}
		
		public ChannelTarget RemoteTarget
		{
			get { return remoteTarget; }
		}
		
		public StreamObject[] Streams 
		{
			get {
				StreamObject[] ret = new StreamObject[hash_stream.Count];
				int count = 0;
				foreach (StreamObject obj in hash_stream.Values)
				{
					ret[count++] = obj;
				}
				return ret;
			}
		}
		
		public void ReleaseStream (StreamObject obj)
		{
			if (!hash_stream.ContainsValue (obj))
				return;
				
			IChannelStreamedMedia ch = (IChannelStreamedMedia) this.tlp_channel;
			uint[] ids = {obj.Id};
			ch.RemoveStreams (ids);			
		}
		
		public StreamObject[] RequestFullStream (Contact contact)
		{	
			requesting = true;
			IChannelStreamedMedia ch = (IChannelStreamedMedia) this.tlp_channel;
			uint[] stream_type = new uint[2];
			
			stream_type[0] = (uint) org.freedesktop.Telepathy.StreamType.Audio;
			stream_type[1] = (uint) org.freedesktop.Telepathy.StreamType.Video;
						
			StreamInfo[] infos = ch.RequestStreams (contact.Handle.Id, stream_type);
			StreamObject[] ret = new StreamObject[infos.Length];
			int i = 0;
			foreach (StreamInfo info in infos) {
				StreamObject stream = CreateStream (info);
				hash_stream.Add (info.Id, stream);
				ret[i++] = stream;
			}
			requesting = false;
			return ret;
		}
		
		public StreamObject RequestStream (StreamType type, Contact contact)
		{
			requesting = true;
			IChannelStreamedMedia ch = (IChannelStreamedMedia) this.tlp_channel;
			uint[] stream_type = new uint[1];
			
			switch (type) 
			{
				case StreamType.Audio:
					stream_type[0] = (uint) org.freedesktop.Telepathy.StreamType.Audio;
					break;
				case StreamType.Video:
					stream_type[0] = (uint) org.freedesktop.Telepathy.StreamType.Video;
					break;
			}
			
			StreamInfo[] infos = ch.RequestStreams (contact.Handle.Id, stream_type);
			if (infos.Length < 1) {
				requesting = false;
				return null;
			}
			
			StreamObject stream = CreateStream (infos[0]);	
			if (stream != null)
				hash_stream.Add (stream.Id, stream);
				
			requesting = false;
			return stream;
		}
		
//internal methods:
		internal StreamChannel (Tapioca.Connection connection, IChannelStreamedMedia channel, ChannelTarget contact, string service_name, ObjectPath obj_path)
			:base (connection, channel, service_name, obj_path)
		{
			requesting = false;
			this.connection = connection;
			this.remoteTarget = contact;
			hash_stream = new System.Collections.Hashtable ();
			
			//initialize media channel
			IChannelHandler channel_handler = Bus.Session.GetObject<IChannelHandler> ("org.freedesktop.Telepathy.StreamEngine", 
				new ObjectPath ("/org/freedesktop/Telepathy/StreamEngine"));
			
			if (channel_handler != null) {				
				channel_handler.HandleChannel (connection.ServiceName, connection.ObjectPath, 
												this.tlp_channel.ChannelType, this.ObjectPath, 
												0, 0);
			}
			
			StreamInfo[] lst = ((IChannelStreamedMedia) this.tlp_channel).ListStreams();
			
			foreach (StreamInfo info in lst)
			{
				StreamObject stream = CreateStream (info);
				hash_stream.Add (stream.Id, stream);
			}
			
			((IChannelStreamedMedia) this.tlp_channel).StreamAdded += OnStreamAdded;
			((IChannelStreamedMedia) this.tlp_channel).StreamDirectionChanged += OnStreamDirectionChanged;
			((IChannelStreamedMedia) this.tlp_channel).StreamError += OnStreamError;
			((IChannelStreamedMedia) this.tlp_channel).StreamRemoved += OnStreamRemoved;
			((IChannelStreamedMedia) this.tlp_channel).StreamStateChanged += OnStreamStateChanged;			
		}
		
		private void OnStreamAdded (uint stream_id, uint contact_handle, org.freedesktop.Telepathy.StreamType stream_type)
		{	
			
			if (this.hash_stream.Contains (stream_id)) return;
			
			StreamObject stream;
			Contact contact = this.connection.ContactList.ContactLookup (contact_handle);
			
			switch (stream_type) 
			{
				case org.freedesktop.Telepathy.StreamType.Audio:
					stream = new StreamAudio (this, stream_id, contact);
					break;
				case org.freedesktop.Telepathy.StreamType.Video:
					stream = new StreamVideo (this, stream_id, contact);
					break;
				default:
					throw new NotImplementedException("Still needs to be designed");
			}
			
			if (!requesting) 
				hash_stream.Add (stream_id, stream);
				
			if (NewStream != null)
				NewStream (this, stream);
		}
		
		private StreamObject CreateStream(StreamInfo info)
		{			
			StreamObject stream;
			Contact c = this.connection.ContactList.ContactLookup (info.ContactHandle);
			
			switch (info.Type) 
			{
				case org.freedesktop.Telepathy.StreamType.Audio:
					stream = new StreamAudio (this, info.Id, c);
					break;
				case org.freedesktop.Telepathy.StreamType.Video:
					stream = new StreamVideo (this, info.Id, c);
					break;
				default: 
					stream = null;
					break;
			}	
			return stream;	
		}
		
		private void OnStreamDirectionChanged (uint stream_id, StreamDirection stream_direction, StreamPendingFlags pending_flags)
		{
			//TODO
		}
		
		private void OnStreamError (uint stream_id, uint errno, string message)
		{
			if (!hash_stream.Contains (stream_id))
				return;
				
			StreamObject obj = (StreamObject) hash_stream[stream_id];
			obj.EmitError (errno, message);
		}
		
		private void OnStreamRemoved (uint stream_id)
		{
			if (!hash_stream.Contains (stream_id))
				return;
				
			StreamObject obj = (StreamObject) hash_stream[stream_id];
			if (LostStream != null)
				LostStream (this, obj);
				
			hash_stream.Remove (stream_id);				
		}
		
		private void OnStreamStateChanged (uint stream_id, org.freedesktop.Telepathy.StreamState stream_state)
		{
			Console.WriteLine ("{0}/{1}", stream_id, stream_state);
			if (!hash_stream.Contains (stream_id))
				return;
				
			StreamObject obj = (StreamObject) hash_stream[stream_id];
			obj.UpdateState ((Tapioca.StreamState) stream_state);
		}
	}
}		
