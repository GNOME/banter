/***************************************************************************
 *  PrivContactList.cs
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
using System.Threading;
using System.Collections;
using NDesk.DBus;
using ObjectPath = NDesk.DBus.ObjectPath;
using org.freedesktop.DBus;
using org.freedesktop.Telepathy;

namespace Tapioca
{	
	namespace Exceptions
	{
		class InvalidHandle : System.Exception { }
	}

	internal delegate void MemberChangedHandler (PrivContactList sender, Handle handle);
	
	internal class PrivContactList : DBusProxyObject, IDisposable
	{
		public event MemberChangedHandler MemberAdded;
		public event MemberChangedHandler MemberRemoved;
		public event MemberChangedHandler MemberRemotePending;
		public event MemberChangedHandler MemberLocalPending;
		
		Handle handle;
		//IChannelContactList tlp_cl;
		IChannelGroup tlp_cl;
		IConnection tlp_connection;
		
		bool closed = false;
		
		public PrivContactList(IConnection tlp_connection, string name, string service_name)
			: base (service_name, new ObjectPath (String.Empty))
		{
			Console.WriteLine ("PrivContactList::Constructor - called");
			Console.WriteLine ("  Name: {0}", name);
			Console.WriteLine ("  Service Name: {0}", service_name);
			
			this.tlp_connection = tlp_connection;
			this.handle = new Handle (this.tlp_connection, HandleType.List, name);
			if (this.handle.Request () == false)
			{
				Console.WriteLine ("  holding handle: {0}", this.handle);
				this.handle.Hold ();
			}	
			
			UpdateObjectPath (
				this.tlp_connection.RequestChannel (
					org.freedesktop.Telepathy.ChannelType.ContactList, 
					HandleType.List, 
					this.handle.Id, 
					true));
			
			//tlp_cl = Bus.Session.GetObject<IChannelContactList> (ServiceName, ObjectPath);
			tlp_cl = Bus.Session.GetObject<IChannelGroup> (ServiceName, ObjectPath);
			
			//tlp_cl.Closed += OnChannelClosed;
			tlp_cl.MembersChanged += OnMembersChanged;
			Console.WriteLine ("PrivContactList::Constructor - exit");
		}
		
		public string Name
		{
			get {
				return handle.Name;
			}
		}
		
		public bool Contains (Handle handle)
		{
			foreach (uint id in tlp_cl.Members)
				if (handle.Id == id)
					return true;
			return false;
		}
		
		public void AddMember (Handle handle)
		{
			uint[] ids = { handle.Id };
			tlp_cl.AddMembers (ids, "");
		}
		
		public void RemoveMember (Handle handle)
		{
			uint[] ids = { handle.Id };
			tlp_cl.RemoveMembers (ids, "");
		}
		
		public Handle[] Members
		{
			get {
				uint[] ids = tlp_cl.Members;
				Handle[] ret = new Handle[ids.Length];
				int i = 0;
				foreach (uint id in ids) { 
					ret[i] = new Handle(this.tlp_connection, HandleType.Contact, id);
					i++;
				}
				return ret;
			}
		}
		
		public Handle[] LocalPending
		{
			get {
				uint[] ids = tlp_cl.LocalPendingMembers;
				Handle[] ret = new Handle[ids.Length];
				int i = 0;
				foreach (uint id in ids) { 
					ret[i] = new Handle(this.tlp_connection, HandleType.Contact, id);
					i++;
				}
				return ret;
			}
		}
		
		public Handle[] RemotePending
		{
			get {
				uint[] ids = tlp_cl.RemotePendingMembers;
				Handle[] ret = new Handle[ids.Length];
				int i = 0;
				foreach (uint id in ids) { 
					ret[i] = new Handle(this.tlp_connection, HandleType.Contact, id);
					i++;
				}
				return ret;
			}
		}
		
		public void Dispose ()
		{
			if (!closed) {
				tlp_cl.MembersChanged -= OnMembersChanged;
				//tlp_cl.Close ();
			}
			
			while (!closed) {}; //TODO: change to mutex
			handle = null;
			tlp_connection = null;
			GC.SuppressFinalize (this);
		}
		
//private methods:
		/*
		FIXME reenable if ChannelContactList works again
		private void OnChannelClosed ()
		{
			closed = true;
		}
		*/

		private void OnMembersChanged (string message, uint[] added, uint[] removed, uint[] local_pending, uint[] remote_pending, uint actor, uint reason)
		{
			if (MemberAdded != null) {
				foreach (uint id in added) {
					Handle handle = new Handle(this.tlp_connection, HandleType.Contact, id);
					MemberAdded (this, handle);
				}
			}
			
			if (MemberRemoved != null) {
				foreach (uint id in removed) {
					Handle handle = new Handle(this.tlp_connection, HandleType.Contact, id);
					MemberRemoved (this, handle);
				}
			}

			if (MemberLocalPending != null) {
				foreach (uint id in local_pending) {
					Handle handle = new Handle(this.tlp_connection, HandleType.Contact, id);
					MemberLocalPending (this, handle);
				}
			}

			if (MemberRemotePending != null) {
				foreach (uint id in remote_pending) {
					Handle handle = new Handle(this.tlp_connection, HandleType.Contact, id);
					MemberRemotePending (this, handle);
				}
			}
		}
	}
}
