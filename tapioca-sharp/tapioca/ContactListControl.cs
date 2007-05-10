/***************************************************************************
 *  ContactListControl.cs
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

namespace Tapioca
{
	public class ContactListControl
	{	
		private enum CLNames 
		{
			subscribe = 0,
			publish = 1,
			known = 2,
			hide = 3,
			deny = 4,
			allow = 5,
			last = 6
		};
		
		PrivContactList[] cl;	
		Tapioca.Connection connection;

		public ContactListControl(Tapioca.Connection connection)
		{
			this.connection = connection;
		}		
		
		public uint[] Handles
		{
			get {
				//TODO
				return new uint[0];
			}			
		}
		
		public void Remove (Handle handle)
		{
			for (int i= (int) CLNames.subscribe; i < (int) CLNames.last; i++)
			{
				if ((cl[i] != null) && (cl[i].Contains (handle)))
					cl[i].RemoveMember (handle);
			}		
		}
		
		public void Subscribe (Handle handle, bool status)
		{
			if (status) 
				cl[(int) CLNames.subscribe].AddMember (handle);
			else
				cl[(int) CLNames.subscribe].RemoveMember (handle);
		}
		
		public void Authorize (Handle handle, bool status)
		{
			if (status) 
				cl[(int) CLNames.publish].AddMember (handle);
			else
				cl[(int) CLNames.publish].RemoveMember (handle);		
		}
		
		public void HideFrom (Handle handle, bool status)
		{
			//TODO
		}
		
		public void Block (Handle handle, bool status)
		{
			if (cl[(int) CLNames.deny] == null) {
				Console.WriteLine ("CM: Not support block");
				return;
			}
			
			if (status)
				cl[(int) CLNames.deny].AddMember (handle);
			else
				cl[(int) CLNames.deny].RemoveMember (handle);				
		}


//internal:
		internal PrivContactList[] Lists 
		{
			get {
				return cl;
			}
		}

		internal Contact[] Load (string service_name)
		{
			bool blocked;
			System.Collections.Hashtable lst = new System.Collections.Hashtable ();
			
			cl = new PrivContactList[(int)CLNames.last];			
			// Loading contacts
			for(int i= (int) CLNames.subscribe; i < (int)CLNames.last; i++)
			{
				try {					
					cl[i] = new PrivContactList (connection.TlpConnection, System.Enum.GetName (typeof (CLNames), i), service_name);					
					if (i == (int)CLNames.deny)
						blocked = true;
					else
						blocked = false;
						
					foreach (Handle handle in cl[i].Members)
					{	
						Contact contact;
						if (!lst.Contains (handle.Id)) {
							contact = new Contact (this, 
													connection, handle,
													ContactSubscriptionStatus.Subscribed,
													ContactAuthorizationStatus.Authorized,
													ContactPresence.Offline, "");
							lst.Add (handle.Id, contact);
						} else
							contact = (Contact) lst[handle.Id];
						contact.Blocked = blocked;						
					}					
					foreach (Handle handle in cl[i].LocalPending)
					{	
						Contact contact;
						if (!lst.Contains (handle.Id)) {
							contact = new Contact (this,
													connection, handle,
													ContactSubscriptionStatus.NotSubscribed,
													ContactAuthorizationStatus.LocalPending,
													ContactPresence.Offline, "");
							lst.Add (handle.Id, contact);
						} else {
							contact = (Contact) lst[handle.Id];
						}
													
						contact.Blocked = blocked;						
					}
					
					foreach (Handle handle in cl[i].RemotePending)
					{
						Contact contact;
						if (!lst.Contains (handle.Id)) {
							contact = new Contact (this, 
													connection, handle, 
													ContactSubscriptionStatus.RemotePending,
													ContactAuthorizationStatus.NonExistent,
													ContactPresence.Offline, "");
							lst.Add (handle.Id, contact);						
						} else {
							contact = (Contact) lst[handle.Id];
						}
						
						contact.Blocked = blocked;
					}
				}
				catch (Exception cs) { 
					Console.WriteLine ("Invalid list");
					Console.WriteLine (cs.Message);
					Console.WriteLine (cs.StackTrace);
					cl[i] = null;
					continue;
				}
			}
			
			Contact[] ret  = new Contact[lst.Values.Count];
			int pos = 0;
			
			foreach (Contact c in lst.Values)
			{
				ret[pos] = c;
				pos++;
			}
			
			return ret;
		}
		
		internal void Unload () 
		{
			if (cl != null) {
				for(int i= (int) CLNames.subscribe; i < (int)CLNames.last; i++)
				{
					if (cl[i] != null)
						cl[i].Dispose ();
				}
				cl = null;
			}
		}
	}
}
