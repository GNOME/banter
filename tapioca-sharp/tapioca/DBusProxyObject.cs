/***************************************************************************
 *  DBusProxyObject.cs
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
using NDesk.DBus;
//using NDesk.GLib;
using ObjectPath = NDesk.DBus.ObjectPath;
using org.freedesktop.DBus;

namespace Tapioca
{
	public class DBusProxyObject
	{
		ObjectPath object_path;
		string service_name;

		public DBusProxyObject(string service_name, ObjectPath object_path)
		{
			this.object_path = object_path;
			this.service_name = service_name;
		}

		public ObjectPath ObjectPath
		{
			get {
				return object_path;
			}
		}

		public string ServiceName
		{
			get {
				return service_name;
			}
		}

		public bool ServiceRunning
		{
			get {
				//return Bus.Session.NameHasOwner (service_name);
				return false;
			}
		}
		
//protect methods:
		protected void UpdateObjectPath (ObjectPath object_path)
		{
			this.object_path = object_path;
		}
	}
}
