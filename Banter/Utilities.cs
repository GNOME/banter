//***********************************************************************
// *  $RCSfile$ - Utilities.cs
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
using System.Runtime.InteropServices;
using System.Text;

namespace Banter
{
	internal class Utilities
	{
		[DllImport("libc")]
		private static extern int prctl(int option, byte [] arg2, IntPtr arg3,
		    IntPtr arg4, IntPtr arg5);

		public static void SetProcessName(string name)
		{
			// 15 = PR_SET_NAME
		    if(prctl(15, Encoding.ASCII.GetBytes(name + "\0"), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero) != 0)
		    {
		        throw new ApplicationException("Error setting process name: " +
		            Mono.Unix.Native.Stdlib.GetLastError());
		    }
		}
		
		public static string ReplaceString (string originalString, string searchString, string replaceString)
		{
			return Utilities.ReplaceString (originalString, searchString, replaceString, false);
		}
		
		public static string EscapeForJavaScript (string text)
		{
			// Replace all single quote characters with: &apos;
			text = ReplaceString (text, "'", "&apos;", true);

			// Replace all double quote characters with: &quot;
			text = ReplaceString (text, "\"", "&quot;", true);
			
			return text;
		}
		
		public static string ReplaceString (
				string originalString,
				string searchString,
				string replaceString,
				bool replaceAllOccurrences)
		{
			string replacedString = originalString;
			int pos = replacedString.IndexOf (searchString);
			while (pos >= 0) {
				replacedString = string.Format (
					"{0}{1}{2}",
					replacedString.Substring (0, pos),
					replaceString,
					replacedString.Substring (pos + searchString.Length));
				
				if (!replaceAllOccurrences)
					break;
				
				pos = replacedString.IndexOf (searchString);
			}
			
			return replacedString;
		}
		
		public static Gdk.Pixbuf GetIcon (string iconName, int size)
		{
			try {
				return Gtk.IconTheme.Default.LoadIcon (iconName, size, 0);
			} catch (GLib.GException) {}
			
			try {
				Gdk.Pixbuf ret = new Gdk.Pixbuf (null, iconName + ".png");
				return ret.ScaleSimple (size, size, Gdk.InterpType.Bilinear);
			} catch (ArgumentException) {}
			
			Logger.Debug ("Unable to load icon '{0}'.", iconName);
			return null;
		}
	}
}
