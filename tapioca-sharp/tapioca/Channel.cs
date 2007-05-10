/***************************************************************************
 *  ISession.cs
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
using org.freedesktop.Telepathy;
using ObjectPath = NDesk.DBus.ObjectPath;

namespace Tapioca
{
	public delegate void ChannelClosedHandler (Channel sender);

	public abstract class Channel : DBusProxyObject
	{
		public event ChannelClosedHandler Closed;
		
		protected IChannel tlp_channel;
		
		bool closed = false;
		ContactGroup contact_group;

//public:		
		public void Close ()
		{
			if (!closed) {
				tlp_channel.Close ();
				closed = true;
			}
			tlp_channel = null;	
		}
		
		public bool IsClosed 
		{
			get { return closed; }
		}
		
		public ContactGroup ContactGroup
		{
			get { return contact_group; }
		}
		
		public abstract ChannelType Type { get; } 

//protected:		
		protected Channel (Tapioca.Connection conn, IChannel tlp_channel, string service_name, ObjectPath object_path) 
			:base (service_name, object_path)
		{
			this.contact_group = new ContactGroup (conn, tlp_channel);
			this.tlp_channel = tlp_channel;
			this.tlp_channel.Closed += OnChannelClosed;
		}

//private:
		private void OnChannelClosed ()
        {
        	closed = true;
        	if (Closed != null)
        		Closed (this);
        }		
	}
}
