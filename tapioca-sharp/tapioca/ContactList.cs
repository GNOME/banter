/***************************************************************************
 *  ContactList.cs
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
using System.Collections;
using System.Collections.Generic;
using NDesk.DBus;
using ObjectPath = NDesk.DBus.ObjectPath;
using org.freedesktop.DBus;
using org.freedesktop.Telepathy;

namespace Tapioca
{
	public delegate void ContactListAuthorizationRequestedHandler (ContactList sender, Contact contact);
	public delegate void ContactListSubscriptionAcceptedHandler (ContactList sender, Contact contact);
	
	public class ContactList : DBusProxyObject
	{
		public event ContactListAuthorizationRequestedHandler AuthorizationRequested;
		public event ContactListSubscriptionAcceptedHandler SubscriptionAccepted;
		
		Tapioca.Connection connection;
		System.Collections.Hashtable contacts;
		ContactListControl control;
		
		bool avatar_connected, presence_connected, alias_connected;

//public methods:	
		public Contact AddContact (string contact_uri)
		{
			Handle contact_h = new Handle (connection.TlpConnection, HandleType.Contact, contact_uri);
			contact_h.Request ();
			contact_h.Hold ();
			
			if (contacts.ContainsKey (contact_h.Id)) {
				contact_h.Dispose ();
				return (Contact) contacts[contact_h.Id];
			}
			
			Contact contact = new Contact (control, 
											connection, contact_h,
											ContactSubscriptionStatus.NotSubscribed, 
											ContactAuthorizationStatus.NonExistent, 
											ContactPresence.Offline, "");
			contacts.Add (contact_h.Id, contact);			
			return contact;
		}

		public void RemoveContact (Contact contact)
		{
			if (!contacts.ContainsKey (contact.Handle.Id))
				return;
				
			contacts.Remove (contact.Handle.Id);
			control.Remove (contact.Handle);
		}
						
		public Contact GetContact (string uri)
		{
			foreach (Contact c in contacts.Values) {
				if (c.Uri == uri)
					return c;
			}
			return null;
		}
		
		public Contact[] KnownContacts
		{
			get {
				Contact[] ret = new Contact[contacts.Values.Count];
				int i = 0;								
				foreach (Contact contact in contacts.Values)
				{
					ret[i] = contact;
					i++;
				}
				return ret;
			}
		}
		
		public Contact[] SubscribedContacts
		{
			get {
				System.Collections.ArrayList lst = new System.Collections.ArrayList ();								
				foreach (Contact contact in contacts.Values)
				{ 
					if (contact.SubscriptionStatus == ContactSubscriptionStatus.Subscribed)
						lst.Add (contact);
				}
				
				if (lst.Count > 0)
					return (Contact[]) lst.ToArray (typeof (Contact));
				else
					return new Contact[0];
			}
		}
		
		public Contact[] AuthorizedContacts
		{
			get {
				System.Collections.ArrayList lst = new System.Collections.ArrayList ();								
				foreach (Contact contact in contacts.Values)
				{ 
					if (contact.AuthorizationStatus == ContactAuthorizationStatus.Authorized)
						lst.Add (contact);
				}
				
				if (lst.Count > 0)
					return (Contact[]) lst.ToArray (typeof (Contact));
				else
					return new Contact[0];
			}
		}	

		public Contact[] BlockedContacts
		{
			get {
				System.Collections.ArrayList lst = new System.Collections.ArrayList ();								
				foreach (Contact contact in contacts.Values)
				{ 
					if (contact.IsBlocked)
						lst.Add (contact);
				}
				
				if (lst.Count > 0)
					return (Contact[]) lst.ToArray (typeof (Contact));
				else
					return new Contact[0];
			}
		}
		
		public Contact[] HiddenFromContacts
		{
			get {
				System.Collections.ArrayList lst = new System.Collections.ArrayList ();								
				foreach (Contact contact in contacts.Values)
				{ 
					if (contact.IsHiddenFrom)
						lst.Add (contact);
				}
				
				if (lst.Count > 0)
					return (Contact[]) lst.ToArray (typeof (Contact));
				else
					return new Contact[0];
			}
		}		

//internal methods:
		
		internal ContactList (Tapioca.Connection connection)
			: base (connection.ServiceName, new ObjectPath (String.Empty))
		{
			contacts = new System.Collections.Hashtable ();
			this.connection = connection;
			this.control = new ContactListControl (connection);
		}

		internal Contact ContactLookup (uint handle)
		{
			if (contacts.ContainsKey (handle))
				return  (Contact) contacts[(uint) handle];
				
			return null;
		}
		
		internal void Dispose ()
		{
			UnloadContacts ();
		}
		
		internal void Clear ()
		{
			UnloadContacts ();
		}
		
		internal void LoadContacts ()
		{	
			ConnectConnectionSignals ();
			Contact[] lst = control.Load (this.ServiceName);
			
			foreach (Contact c in lst) {
				this.contacts.Add (c.Handle.Id, c);
				if (c.AuthorizationStatus == ContactAuthorizationStatus.LocalPending) {
					if (AuthorizationRequested != null)				
						AuthorizationRequested (this, c);
				} else {					
					this.RequestPresence (c.Handle.Id);
				}					
			}
			
			foreach (PrivContactList cl in control.Lists) {
				if (cl != null) {
					cl.MemberAdded += OnMemberAdded;
					cl.MemberRemoved += OnMemberRemoved;
					cl.MemberLocalPending += OnMemberLocalPending;
					cl.MemberRemotePending += OnMemberRemotePending;
				}
			}
		}		
		
//private methods:		
		private void UnloadContacts ()
		{	
			DiscconectConnectionSignals ();
			
			foreach (Contact contact in contacts.Values) {
				contact.Release ();
			}
			contacts.Clear ();
			control.Unload ();
		}						

		private void RequestPresence (uint id)
		{
			if (!connection.SupportPresence)
				return;
				
			uint[] ids = {id};
			connection.TlpConnection.RequestPresence (ids);
		}

		private void OnPresenceUpdate (IDictionary<uint, PresenceUpdateInfo> statuses)
		{
			foreach (KeyValuePair<uint,PresenceUpdateInfo>  entry in statuses)
			{
				Contact contact = ContactLookup (entry.Key);

				if (contact == null)
					return;

				foreach (KeyValuePair<string, IDictionary<string,object>> info in entry.Value.info)
				{
					string message = "";
					foreach (KeyValuePair<string,object> val in info.Value) {						
						if (val.Key == "message")
							message = (string) val.Value;
					}
					contact.UpdatePresence (info.Key, message);
				}
			}
		}
		
		private Contact GetContact (Handle handle)
		{
			if (contacts.ContainsKey (handle.Id))
				return (Contact) contacts[handle.Id];
			return null;
		}
		
		private void OnMemberAdded (PrivContactList cl, Handle handle)
		{					
			Contact contact = GetContact (handle);			
			if (contact == null) {
				Console.WriteLine ("Invalid contact {0}", handle);
			}				
			
//			Console.WriteLine ("OnMemberAdded {0} / {1}", contact.Uri, cl.Name);
			
			switch (cl.Name) {
				case "deny":
					contact.Blocked = true;
					break;
				case "subscribe":					
					contact.UpdateStatus (ContactSubscriptionStatus.Subscribed);
					RequestPresence (contact.Handle.Id);					
					if (SubscriptionAccepted != null) 
						SubscriptionAccepted (this, contact);
					break;
				case "publish":
					contact.UpdateAuthorize (ContactAuthorizationStatus.Authorized);
					break;						
				default:
					break;
			}
		}
		
		private void OnMemberRemoved (PrivContactList cl, Handle handle)
		{
			Contact contact = GetContact (handle);
			if (contact != null) {
				switch (cl.Name) {
					case "deny":
						contact.Blocked = false;
						break;
					case "subscribe":
						contacts.Remove (contact.Handle.Id);
						contact.Release ();
						contact.UpdatePresence ("offline", "");
						contact.UpdateStatus (ContactSubscriptionStatus.NotSubscribed);
						break;
					default:
						break;
				}
			}
		}

		private void OnMemberLocalPending (PrivContactList cl, Handle handle)
		{	
			if (cl.Name != "publish") return;
			
			Contact contact = GetContact (handle);
			if (contact != null) {
				contact.UpdateAuthorize (ContactAuthorizationStatus.LocalPending);
			} 
			else {
				contact = new Contact (control, 
										connection, handle,
										ContactSubscriptionStatus.NotSubscribed,
										ContactAuthorizationStatus.LocalPending,
										ContactPresence.Offline, "");				
				contacts.Add (handle.Id, contact);
				
				if (AuthorizationRequested != null)				
					AuthorizationRequested (this, contact);
			}
		}

		private void OnMemberRemotePending (PrivContactList cl, Handle handle)
		{			
			if (cl.Name != "publish") return;
			
			Contact contact = GetContact (handle);
			if (contact != null)  
				contact.UpdateStatus (ContactSubscriptionStatus.RemotePending);
			else {
				contact = new Contact (control, 
										connection, handle, 
										ContactSubscriptionStatus.RemotePending,
										ContactAuthorizationStatus.NonExistent,
										ContactPresence.Offline, "");
				contacts.Add (handle.Id, contact);
			}
		}
		
		private void OnAliasesChanged (AliasInfo[] aliases)
		{
			foreach (AliasInfo info in aliases) {
				if (contacts.ContainsKey (info.ContactHandle)) {
					Contact contact = (Contact) contacts[info.ContactHandle];
					contact.UpdateAlias (info.NewAlias);
				}
			}
		}		
		
		private void ConnectConnectionSignals ()
		{
			if (this.connection.SupportAliasing) {
				this.connection.TlpConnection.AliasesChanged += OnAliasesChanged;
				alias_connected = true;
			}
				
			if (this.connection.SupportPresence) {
				this.connection.TlpConnection.PresenceUpdate += OnPresenceUpdate;
				presence_connected = true;
			}
			
			if (this.connection.SupportAvatars) {
				this.connection.TlpConnection.AvatarUpdated += OnAvatarUpdated;
				avatar_connected = true;
			}
		}
		
		private void DiscconectConnectionSignals ()
		{ 
			if (avatar_connected) {
				this.connection.TlpConnection.AvatarUpdated -= OnAvatarUpdated;
				avatar_connected = false;
			}
			
			if (presence_connected) {
				this.connection.TlpConnection.PresenceUpdate -= OnPresenceUpdate;
				presence_connected = false;
			}
			
			if (alias_connected) {
				this.connection.TlpConnection.AliasesChanged -= OnAliasesChanged;
				alias_connected = false;
			}	
		}
		
		private void OnAvatarUpdated (uint contact_id, string token)
		{
			Contact c = ContactLookup (contact_id);
			if (c != null)
				c.UpdateAvatarToken (token);
		}
	}
}
