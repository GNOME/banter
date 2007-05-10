/***************************************************************************
 *  ConnectionManagerFactory.cs
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
using System.IO;
using System.Collections.Generic;
using GLib;
using NDesk.DBus;
using ObjectPath = NDesk.DBus.ObjectPath;
using org.freedesktop.DBus;
using org.freedesktop.Telepathy;
using Tapioca.Util;

namespace Tapioca
{
	public class ConnectionManagerFactory
	{
		string [] TelepathyDataDirs = {
			Path.Combine (Environment.GetFolderPath (System.Environment.SpecialFolder.Personal), ".telepathy/managers/"),
			"/usr/share/telepathy/managers/",
			//Path.Combine (ConfigureDefines.PREFIX, "share/telepathy/managers/"),
			"/usr/local/share/telepathy/managers/",
			"/usr/share/telepathy/managers/",
			Path.Combine (Environment.GetFolderPath (System.Environment.SpecialFolder.CommonApplicationData), "telepathy/managers/")			
		};

// private methods:
		private System.Collections.Hashtable cm_hash;

		public ConnectionManager[] AllConnectionManagers
		{
			get {
				ConnectionManager[] ret = new ConnectionManager [cm_hash.Count];
				int i = 0;
				foreach (ConnectionManager cm in cm_hash.Values)
				{
					ret[i] = cm;
					i++;
				}
				return ret;
			}
		}

		public ConnectionManager[] GetConnectionManagers (string protocol)
		{
			System.Collections.ArrayList ret = new System.Collections.ArrayList ();
			foreach (ConnectionManager cm in cm_hash.Values) {
				if (cm.Supports (protocol))
					ret.Add (cm);
			}
			if (ret.Count == 0)
				return null;
			else
				return (ConnectionManager[]) ret.ToArray (typeof (ConnectionManager));
		}
		
		public ConnectionManager GetConnectionManager (string protocol)
		{
			foreach (ConnectionManager cm in cm_hash.Values) {
				if (cm.Supports (protocol))
					return cm;
			}
			return null;
		}
		

		public ConnectionManager GetConnectionManagerByName (string name) {
			if (cm_hash.Contains (name))
				return cm_hash[name] as ConnectionManager;

			return null;
		}

		public ConnectionManagerFactory ()
		{
			cm_hash = new System.Collections.Hashtable ();

			Load ();
		}

		private void Load ()
		{
			foreach (string path in TelepathyDataDirs) {
				if (Directory.Exists (path)) {
				 	string[] files = Directory.GetFiles (path);
				 	foreach (string name in files) {
				 		string server_name = Path.GetFileNameWithoutExtension (name);
				 		if (cm_hash.Contains (server_name))
				 			continue;
				 		ManagerFileReader rd;
				 		try {
				 			 rd = new ManagerFileReader (name);
				 		}
				 		catch {
				 			continue;
				 		}
				 		if (!rd.IsValid ())
				 			continue;
				 			
				 		System.Collections.Hashtable protocols = new System.Collections.Hashtable ();
				 		foreach (string proto in rd.Protocols) {
				 			protocols.Add (proto, rd.GetProtolsParameters (proto));
				 		}				 		

				 		cm_hash.Add (server_name, new ConnectionManager (server_name, rd.BusName, rd.ObjectPath, protocols));
				 	}
				}
			}
		}
	}
}
