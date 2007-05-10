/***************************************************************************
 *  ConnectionManager.cs
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
using System.Collections.Generic;
using GLib;
using NDesk.DBus;
using ObjectPath = NDesk.DBus.ObjectPath;
using org.freedesktop.DBus;
using org.freedesktop.Telepathy;
using Tapioca.Util;

namespace Tapioca
{
	public class ConnectionManager : DBusProxyObject
	{
		string name;
		bool initialized = false;
		System.Collections.ArrayList connection_list;
		System.Collections.Hashtable protocols;
		IConnectionManager tlp_conn_manager;

//public methods:

		public bool IsRunning
		{
			get { return this.ServiceRunning; }
		}

		public string Name
		{
			get { return name; }
		}

		public Connection[] Connections
		{
			get {
				Init ();

				if (connection_list.Count > 0)
					return  (Connection[]) connection_list.ToArray (typeof (Connection));
				else
					return new Connection[0];
			}
		}

		public string [] SupportedProtocols
		{
			get {
				string [] ret = new string [protocols.Values.Count];
				int i = 0;
				foreach (string s in protocols.Keys) {
					ret[i] = s;
					i++;
				}
				return ret;
			}
		}

		public bool Supports (string protocol)
		{
			return protocols.Contains (protocol);
		}

		public ConnectionManagerParameter[] ProtocolConnectionManagerParameters (string protocol)
		{			
			if (protocols.Contains (protocol))
				return (ConnectionManagerParameter []) protocols[protocol];
			return new ConnectionManagerParameter [0];
		}

		public Connection RequestConnection (string protocol, ConnectionManagerParameter[] parameters)
		{
			if (!Init ()) 
				return null;
				
			Dictionary<string, object> p = ParseConnectionManagerParameters (parameters);
			
			org.freedesktop.Telepathy.ConnectionInfo conn = tlp_conn_manager.RequestConnection (protocol, p);			
			IConnection tlp_connection = Bus.Session.GetObject<IConnection> (conn.BusName, conn.ObjectPath);

			if (tlp_connection == null) {
				Console.WriteLine ("Error geting connection {0}", protocol);
				return null;
			}

			Connection connection = new Connection (protocol, tlp_connection, conn.BusName, conn.ObjectPath);
			if (connection == null) {
				Console.WriteLine ("Error creating connection {0}", protocol);
				return null;			
			}
			connection_list.Add (connection);
			return connection;
		}
		
		public Connection RequestConnection (string service_name, string object_path)
		{			
			ObjectPath obj = new ObjectPath (object_path);
			IConnection tlp_connection = Bus.Session.GetObject<IConnection> (service_name, obj);
			if (tlp_connection == null)
				return null;
			Connection connection = Connection.Load (tlp_connection, service_name, obj);
			if (connection == null) {
				Console.WriteLine ("Error creating connection");
				return null;			
			}
			connection_list.Add (connection);
			return connection;			
		}


//internal methods:

		internal IConnectionManager TConnectionManager
		{
			get { return tlp_conn_manager; }
		}

		internal ConnectionManager (string name,
			string service_name,
			string object_path,
			System.Collections.Hashtable protocols)
				:base (service_name, new ObjectPath (object_path))
		{
			this.name = name;
			this.protocols = protocols;

			connection_list = new System.Collections.ArrayList ();
		}


//private methods:

		private bool Init ()
		{
			if (initialized) 
				return true;
	
			tlp_conn_manager = Bus.Session.GetObject<IConnectionManager> (this.ServiceName,
			this.ObjectPath);
								
			if (tlp_conn_manager != null)
				return true;
				
			return false;
		}

		private Dictionary<string, object> ParseConnectionManagerParameters (ConnectionManagerParameter[] parameters)
		{
			Dictionary<string, object>  ret = new Dictionary<string, object> ();
			foreach (ConnectionManagerParameter param in parameters)
				ret.Add (param.Name, param.Value);

			return ret;
		}
	}
}
