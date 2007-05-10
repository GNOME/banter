/***************************************************************************
 *  Avatar.cs
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
using ObjectPath = NDesk.DBus.ObjectPath;
using Gdk;

namespace Tapioca
{	
	public class Avatar
	{			
		org.freedesktop.Telepathy.Avatar avatar_info;
		string token;
				
		public string MimeType
		{
			get {
				return avatar_info.MimeType;
			}
		}
		
		public string Token
		{
			get {
				return token;
			}
		}
		
		public byte[] Data 
		{
			get {
				return avatar_info.Data;
			}
		}
		
		
//intenarl methods:
		internal Avatar (string token, org.freedesktop.Telepathy.Avatar avatar_info)
		{
			this.token = token;
			this.avatar_info = avatar_info;
		}
	}
}
