/***************************************************************************
 *  Handle.cs
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

namespace Tapioca
{
	public class Handle : IDisposable
	{
		private IConnection tlp_connection;
		private HandleType type;
		private uint id;
		private string name;
		private bool requested = false;

//public methods:
		public Handle (IConnection tlp_connection, HandleType type, string name)
		{
			Init (tlp_connection, type);
			id = 0;
			this.name = name;
		}

		public Handle (IConnection tlp_connection, HandleType type, uint id)
		{
			Init (tlp_connection, type);
			requested = true;
			this.id = id;
			this.name = Inspect ();
		}

		public bool Request ()
		{
			if (requested) return false;

			try
			{
				string[] names = {name};
				uint[] handles = tlp_connection.RequestHandles (type, names);
			
				if (handles == null || handles.Length == 0)
				{
					return false;
				}

				requested = true;
				id = handles[0];
				Console.WriteLine ("Handle::Request - exit");
				return true;
			} catch{}
			
			return false;
		}

		public string Inspect ()
		{
			if (!requested) return String.Empty;

			uint[] ids = {id};
			string[] names = tlp_connection.InspectHandles (type, ids);
			if (names.Length != 1) return "";
			return names[0];
		}

		public uint Id
		{
			get { return id; }
		}

		public string Name
		{
			get { return name;	}
		}

		public HandleType Type
		{
			get { return type;	}
		}

		public void Hold ()
		{
			if (!requested) return;
			uint[] ids = {id};
			tlp_connection.HoldHandles (type, ids);
		}

		public void Release ()
		{
			if (tlp_connection.Status == org.freedesktop.Telepathy.ConnectionStatus.Disconnected) {
				Console.WriteLine ("Disconnected not need release");
				requested = false;
			}
			else {
				if (!requested) return;
				uint[] ids = {id};
				tlp_connection.ReleaseHandles (type, ids);
				requested = false;
			}
		}

		public bool Equals (Handle handle)
		{
			return (this.id == handle.Id);
		}

		public void Dispose ()
		{
			//Release (); //TODO: verify if connected
			tlp_connection = null;
			GC.SuppressFinalize (this);
		}

//private methods:
		private void Init (IConnection tlp_connection, HandleType type)
		{
			this.tlp_connection = tlp_connection;
			this.type = type;
		}

	}
}
