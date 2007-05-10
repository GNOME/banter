/***************************************************************************
 *  ConnectionManagerParameter.cs
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

namespace Tapioca
{
	public enum ConnectionManagerParameterFlags : int
	{
		None = 0,
		Required = 1,
		HasDefault = 2,
		Register = 4,
		All = 255
	}

	public class ConnectionManagerParameter
	{
		object val;
		string name;
		ConnectionManagerParameterFlags flags;

		public ConnectionManagerParameter (string name, object val)
		{
			this.name = name;
			this.val = val;
			this.flags = ConnectionManagerParameterFlags.None;
		}

		public object Value
		{
			get {
				object ret = this.val;
				return ret;
			}
			set {		
				val = System.Convert.ChangeType (value, val.GetType ());
			}
		}
		
		public string Name
		{
			get { return name; }
		}

		public ConnectionManagerParameterFlags Flags
		{
			get { return flags; }
		}
		
//internal methods:
		
		internal ConnectionManagerParameter (string name, object val, ConnectionManagerParameterFlags flags)
		{
			this.name = name;
			this.val = val;
			this.flags = flags;
		}
		
		internal void AddFlag (ConnectionManagerParameterFlags flag)
		{
			flags |= flag;
		}
	}
}
