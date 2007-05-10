/***************************************************************************
 *  StreamObject.cs
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
using NDesk.DBus;
using ObjectPath = NDesk.DBus.ObjectPath;
using org.freedesktop.DBus;
using org.freedesktop.Telepathy;

namespace Tapioca
{	
	public delegate void StreamObjectErrorHandler (StreamObject sender, uint error, string message);
	public delegate void StreamObjectStateChangedHandler (StreamObject sender, StreamState state);

	public abstract class StreamObject
	{	
		public event StreamObjectErrorHandler Error;
		public event StreamObjectStateChangedHandler StateChanged;
		
		protected StreamChannel channel;
		protected uint stream_id;
		protected Contact contact;
		protected IStreamEngine stream_engine;
		
		private StreamState state;
		
		private string service_name = "org.freedesktop.Telepathy.StreamEngine";
		private string obj_path = "/org/freedesktop/Telepathy/StreamEngine";
			
		public void Play ()
		{
			channel.ContactGroup.AddMembers (channel.ContactGroup.LocalPendingContacts);
		}
		
		public abstract void Pause ();
		
		
		internal uint Id {
			get {
				return stream_id;
			}
		}
		
		public StreamState State { 
			get {
				return state;
			}
		}
		
		public Contact Contact 
		{
			get {
				return contact;
			}
		}
		
		public abstract StreamType Type
		{ get; }
				
		
//protected
		protected StreamObject (StreamChannel channel, uint stream_id, Contact contact)
		{
			this.channel = channel;
			this.stream_id = stream_id;
			this.contact = contact;
			this.stream_engine = Bus.Session.GetObject<IStreamEngine> (this.service_name, new ObjectPath (this.obj_path));			
		}
		
//internal
		internal void UpdateState (StreamState state)
		{
			this.state = state;
			if (StateChanged != null)
				StateChanged (this, state);
		}
		
		internal void EmitError (uint error, string message)
		{
			if (Error != null)
				Error (this, error, message);
		}
		
	}
}
