//***********************************************************************
// *  $RCSfile$ - AvatarRequirements.cs
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
	/// Class to define the avatar requirements based on
	/// a connection.
	/// </summary>
	public class AvatarRequirements
	{
		int minWidth;
		int minHeight;
		int maxWidth;
		int maxHeight;
		uint maxImageSize;
		
		string[] mimeTypes = null;
		
		/// <summary>
		/// Property to return the supported avatar mime types
		/// </summary>
		public string[] MimeTypes
		{
			get { return null; }
		}
		
		/// <summary>
		/// Property to return the maximum image size supported in bytes
		/// </summary>
		public uint MaximumImageSize
		{
			get { return maxImageSize; }
		}
		
		/// <summary>
		/// Property to return the minimum image width in pixels
		/// </summary>
		public int MinimumImageWidth
		{
			get { return minWidth; }
		}
		
		/// <summary>
		/// Property to return the minimum image height in pixels
		/// </summary>
		public int MinimumImageHeight
		{
			get { return minHeight; }
		}
		
		/// <summary>
		/// Property to return the maximum image width in pixels
		/// </summary>
		public int MaximumImageWidth
		{
			get { return maxWidth; }
		}
		
		/// <summary>
		/// Property to return the maximum image height in pixels
		/// </summary>
		public int MaximumImageHeight
		{
			get { return maxHeight; }
		}
		
		/// <summary>
		/// Internal constructor calledProperty to return the supported avatar mime types
		/// </summary>
		internal AvatarRequirements ( 
					string[] mimeTypes, 
					uint maxSize, 
					int minWidth, 
					int minHeight, 
					int maxWidth, 
					int maxHeight)
		{
			if (mimeTypes != null || mimeTypes.Length > 0) { 
				this.mimeTypes = new string[mimeTypes.Length];
				for (int i = 0; i < mimeTypes.Length; i++)
					this.mimeTypes[i] = mimeTypes[i];
			}
			
			this.maxImageSize = maxSize;
			this.minWidth = minWidth;
			this.minHeight = minHeight;
			this.maxWidth = maxWidth;
			this.maxHeight = maxHeight;
		}
	}
}	
