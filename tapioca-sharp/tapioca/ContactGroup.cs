/***************************************************************************
 *  ContactGroup.cs
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
using org.freedesktop.Telepathy;

namespace Tapioca
{
	public delegate void ContactGroupContactEnteredHandler (ContactGroup sender, Contact contact);
	public delegate void ContactGroupContactLeftHandler (ContactGroup sender, Contact contact);
	public delegate void ContactGroupNewContactPendingHandler (ContactGroup sender, Contact contact);
	
	public class ContactGroup
	{
		public event ContactGroupContactEnteredHandler ContactEntered;
		public event ContactGroupContactLeftHandler ContactLeft;
		public event ContactGroupNewContactPendingHandler NewContactPending;
		
		private bool group_support;
		private IChannel tlp_channel;
		private Connection connection;
				
		public void InviteContact (Contact contact)
		{
			return;
		}
		
		public void ExpelContact (Contact contact)
		{
			return;
		}
		
		public bool CanInvite ()
		{
			return group_support;
		}
		
		public bool CanExpel ()
		{
			return group_support;
		}
		
		public Contact[] Contacts
		{
			get {
				System.Collections.ArrayList contacts = new System.Collections.ArrayList ();
				foreach (uint handle in this.tlp_channel.Members)
				{
					Contact c = connection.ContactList.ContactLookup (handle);
					if (c != null)
						contacts.Add (c);
				}
				if (contacts.Count > 0)
					return (Contact[]) contacts.ToArray (typeof (Contact));
				else
					return new Contact[0];
			}
		}
		
		public Contact[] PendingContacts
		{
			get {
				System.Collections.ArrayList contacts = new System.Collections.ArrayList ();
				foreach (uint handle in this.tlp_channel.RemotePendingMembers)
				{
					Contact c = connection.ContactList.ContactLookup (handle);
					if (c != null)
						contacts.Add (c);
				}
				if (contacts.Count > 0)
					return (Contact[]) contacts.ToArray (typeof (Contact));
				else
					return new Contact[0];			
			}
		}

//internal
		internal ContactGroup (Connection connection, IChannel tlp_channel)
		{
			this.connection = connection;
			this.tlp_channel = tlp_channel;
			group_support = false;
			
			foreach (string iface in this.tlp_channel.Interfaces) {
				if (iface == "org.freedesktop.Telepathy.Channel.Interface.Group") {
					Console.WriteLine ("supports groups");
					group_support = true;
					break;
				}
			}
			
			this.tlp_channel.MembersChanged += OnMembersChanged;
		}
		
		internal void AddMembers (uint[] ids)
		{
			tlp_channel.AddMembers (ids, "");
		}
		
		public uint[] LocalPendingContacts
		{
			get {
				return this.tlp_channel.LocalPendingMembers;
			}
		}

//private
		private ContactGroup ()
		{
		}
		
		private void OnMembersChanged (string message, uint[] added, uint[] removed, uint[] local_pending, uint[] remote_pending, uint actor, uint reason)
		{
			if (ContactEntered != null) {
				foreach (uint handle in added) {
					Contact c = connection.ContactList.ContactLookup (handle);
					if (c != null)
						ContactEntered (this, c);											
				}
			}
			
			if (ContactLeft != null) {
				foreach (uint handle in removed) {
					Contact c = connection.ContactList.ContactLookup (handle);
					if (c != null)
						ContactLeft (this, c);											
				}			
			}
			
			if (NewContactPending != null) {
				foreach (uint handle in remote_pending) {
					Contact c = connection.ContactList.ContactLookup (handle);
					if (c != null)
						NewContactPending (this, c);											
				}			
			}

		}
	}
}
