//***********************************************************************
// *  $RCSfile$ - AppStyle.cs
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
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;

namespace Banter
{
	// <summary>
	// Style (or Theme) used for the GroupWindow and Application in Banter
	// </summary>
	public class AppStyle
	{
		#region Private Types
		private string stylePath;
		private string name;
		#endregion


		#region Public Properties
		///<summary>
		///	Returns the name of the Style
		///</summary>			
		public string Name
		{
			get { return name; }
		}
		#endregion


		#region Constructors
		///<summary>
		///	Constructs a AppStyle from a path
		///</summary>			
		public AppStyle(string path)
		{
			this.stylePath = path;
		}
		#endregion


		#region Public Static Methods
		/// <summary>
		/// Determines if the path passed in is a valid Application Style
		/// </summary>	
		public static bool IsValid(string path)
		{
			try {
				AppStyle appStyle = new AppStyle(path);
			} catch (Exception e) {
				return false;
			}
			return true;
		}
		#endregion

	}
}