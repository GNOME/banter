/***************************************************************************
 *  ConnectionGroup.cs
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
using org.freedesktop.DBus;
using org.freedesktop.Telepathy;

namespace Tapioca
{
	public delegate void ConnectionGroupSubscriptionAcceptedHandler (Connection sender, ContactList contact_list, Contact contact);
	public delegate void ConnectionGroupAuthorizationRequestedHandler (Connection sender, ContactList contact_list, Contact contact);
	
	public class ConnectionGroup
	{
		public event ConnectionStatusChangedHandler ConnectionStatusChanged;
		public event ConnectionChannelCreatedHandler ConnectionChannelCreated;
		public event ConnectionGroupSubscriptionAcceptedHandler SubscriptionAccepted;
		public event ConnectionGroupAuthorizationRequestedHandler AuthorizationRequested;
		
		System.Collections.ArrayList conn_list;
		System.Collections.Hashtable map;
		
		public ConnectionGroup ()
		{
			conn_list = new System.Collections.ArrayList ();
			map = new System.Collections.Hashtable ();
		}
				
		public void DisconnectAll ()
		{
			foreach (Connection c in conn_list) {
				c.Disconnect ();
			}
		}
		
		public void Clear ()
		{
			foreach (Connection c in conn_list) {
				Remove (c);
			}

			conn_list.Clear ();
			map.Clear ();
		}
		
		public bool Contain (Connection conn)
		{
			return conn_list.Contains (conn);
		}
		
		public void Add (Connection conn)
		{
			if (conn_list.Contains (conn)) return;
			ConnectSignals (conn);	
		}
		
		public void Remove (Connection conn)
		{
			if (!conn_list.Contains (conn)) return;
			DisconnectSignals (conn);
		}
		
		public Connection[] Connections
		{
			get {
				if (conn_list.Count > 0)
					return (Connection[]) conn_list.ToArray (typeof (Connection));
				return new Connection[0];
			}
		}
		
//private methods:
		private void ConnectSignals (Connection conn) 
		{
			conn_list.Add (conn);
			map.Add (conn.ContactList, conn);
			conn.StatusChanged += OnConnectionStatusChanged;
			conn.ChannelCreated += OnConnectionChannelCreated;
			conn.ContactList.SubscriptionAccepted += OnSubscriptionAccepted;
			conn.ContactList.AuthorizationRequested += OnAuthorizationRequested;
		}
		
		private void DisconnectSignals (Connection conn)
		{
			conn.StatusChanged -= OnConnectionStatusChanged;
			conn.ChannelCreated -= OnConnectionChannelCreated;
			conn.ContactList.SubscriptionAccepted -= OnSubscriptionAccepted;
			conn.ContactList.AuthorizationRequested -= OnAuthorizationRequested;
			conn_list.Remove (conn);
			map.Remove (conn.ContactList);
		}
		
		private void OnConnectionStatusChanged (Connection sender, ConnectionStatus status, ConnectionStatusReason reason)
		{
			if ((ConnectionStatusChanged != null) && (conn_list.Contains (sender)))
				ConnectionStatusChanged (sender, status, reason);
		}
		
		private void OnSubscriptionAccepted (ContactList sender, Contact contact)
		{
			if ((SubscriptionAccepted != null) && (map.Contains (sender))) {
				Connection c = (Connection) map[sender];
				SubscriptionAccepted (c, sender, contact);
			}
		}
		
		private void OnAuthorizationRequested (ContactList sender, Contact contact)
		{
			if ((AuthorizationRequested != null) && (map.Contains (sender))) {
				Connection c = (Connection) map[sender];
				AuthorizationRequested (c, sender, contact);
			}
		}
		
		private void OnConnectionChannelCreated (Connection sender, Tapioca.Channel channel)
		{
			if ((ConnectionChannelCreated != null) && (conn_list.Contains (sender)))
				ConnectionChannelCreated (sender, channel);
		}
	}
}
