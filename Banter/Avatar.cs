//***********************************************************************
// *  $RCSfile$ - Avatar.cs
// *
// *  Copyright (C) 2007 Novell, Inc.
// *
// *  This program is free software; you can redistribute it and/or
// *  modify it under the terms of the GNU General Public
// *  License as published by the Free Software Foundation; either
// *  version 2 of the License, or (at your option) any later version.
// *
// *  This program is distributed in the hope that it will be useful,
// *  but WITHOUT ANY WARRANTY; without even the implied warranty of
// *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// *  General Public License for more details.
// *
// *  You should have received a copy of the GNU General Public
// *  License along with this program; if not, write to the Free
// *  Software Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
// *
// **********************************************************************

using System;
using System.Collections;
using System.Net;
using System.Text;

namespace Banter
{
	/// <summary>
	/// Class to define an Avatar
	/// </summary>
	public class Avatar
	{
		string mimeType;
		byte[] data;
		
		/// <summary>
		/// Property to return the supported avatar mime types
		/// </summary>
		public string MimeType
		{
			get { return mimeType; }
		}
		
		/// <summary>
		/// Property to the actual avatar data
		/// </summary>
		public byte[] Data
		{
			get { return data; }
		}
		
		/// <summary>
		/// Internal constructor for newing up an Avatar
		/// </summary>
		internal Avatar (string mimeType, byte[] data) 
		{
			this.mimeType = mimeType;
			this.data = new byte[data.Length];
			this.data = data;
		}
	}
}	
