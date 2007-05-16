//***********************************************************************
// *  $RCSfile$ - ContactStyleInfo.cs
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

namespace Banter
{
	public class ContactStyleInfo
	{
		#region Private Types
		private bool systemStyle;
		private string systemPath;
		private string name;
		private string path;
		#endregion
		
		#region Constructors
		
		/// <summary>
		/// ContactStyleInfo constructor that should only be called by the
		/// ThemeManager for constructing ContactStyles that are known to
		/// be installed and valid (stored in GConf).
		/// <param name="name">The name of the Contact Style</param>
		/// <param name="path">The path to the Contact Style</param>
		/// </summary>
		public ContactStyleInfo(string name, string path)
		{
			if (name == null || name.Trim ().Length == 0
					|| path == null || path.Trim ().Length == 0)
				throw new ArgumentException ("Exception constructing a ContactStyleInfo because one of the arguments to the constructor is invalid.");
			
Logger.Debug ("ContactStyleInfo (\"{0}\", \"{1}\")", name, path);
			
			this.name = name;
			this.systemStyle = true;
			this.systemPath = path;
			this.path = path;
		}
		
		/// <summary>
		/// ContactStyleInfo constructor
		/// <param name="path">Directory path to a ContactStyle</param>
		/// </summary>
		public ContactStyleInfo(string path, bool systemStyle)
		{
			if (path == null || path.Trim ().Length == 0 ||
					System.IO.Directory.Exists (path) == false)
				throw new ArgumentException (
					"The path specified is invalid.  \n" +
					"It must point to a valid directory.");
			
			// Parse the name of the ContactStyle
			this.name = System.IO.Path.GetFileName (path);
Logger.Debug ("ContactStyleInfo: Parsed name as: {0}", name);

			this.systemStyle = systemStyle;
			if (systemStyle) {
				string sysThemePath = ThemeManager.SystemThemesPath;
				int len = sysThemePath.Length + 1;
				
				this.systemPath = path.Substring (len);
			}
			this.path = path;
		}
		#endregion
		
		#region Public Methods
		/// <summary>
		/// Checks to see if the specified path points to a valid ContactStyle.
		/// <param name="path">Directory path to a ContactStyle</param>
		/// </summary>
		public static bool IsValid (string path)
		{
Logger.Debug ("ContactStyleInfo.IsValid (\"{0}\")", path);
			// Attempt to load and create a ContactStyle
			ContactStyleInfo contactStyleInfo;
			ContactStyle contactStyle;
			try {
				contactStyleInfo = new ContactStyleInfo (path, false);
			} catch (Exception e) {
				Logger.Debug ("Exception in ContactStyleInfo (\"{0}\"): {1}", path, e.Message);
				return false;
			}
			
			try {
				contactStyle = new ContactStyle (contactStyleInfo);
			} catch (Exception e) {
				Logger.Debug ("Exception in ContactStyle (\"{0}\"): {1}", path, e.Message);
				return false;
			}
			
			return true;
		}

		public override string ToString ()
		{
			return string.Format ("{0}={1}", name,
					systemStyle ? systemPath : path);
		}

		#endregion
		
		#region Public Properties
		public string Name
		{
			get { return name; }
		}
		
		public string Path
		{
			get { return path; }
		}
		#endregion
	}
}
