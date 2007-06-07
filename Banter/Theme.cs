//***********************************************************************
// *  $RCSfile$ - ThemeManager.cs
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
using System.IO;
using Gtk;

namespace Banter
{
	// <summary>
	// Theme represents a combination of MessageStyle, ContactStyle, and AppStyle.
	// </summary>
	public class Theme
	{
		#region Private Types
		private string themePath;
		// private string name;
		#endregion


		#region Public Properties
		///<summary>
		///	Returns the name of the Style
		///</summary>			
		// public string Name
		// {
		// 	get { return name; }
		// }
		#endregion


		#region Constructors
		///<summary>
		///	Constructs a AppStyle from a path.
		/// <param name="path">The full path to a theme file</param>
		///</summary>			
		public Theme(string path)
		{
			this.themePath = path;
			
			// FIXME: Parse the theme file
			throw new Exception ("FIXME: Theme constructor is not implemented yet!");
		}
		#endregion


		#region Public Static Methods
		/// <summary>
		/// Determines if the file specified represents a valid Theme.
		/// <param name="path">The full path to a theme file</param>
		/// <returns>True if the theme file is valid and everything specified
		/// in the file is valid.</returns>
		/// </summary>	
		public static bool IsValid(string path)
		{
			try {
				AppStyle appStyle = new AppStyle(path);
			} catch {
				return false;
			}
			return true;
		}
		#endregion
	}
}
