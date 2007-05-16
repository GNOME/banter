//***********************************************************************
// *  $RCSfile$ - MessageStyleInfo.cs
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
using System.Xml;

namespace Banter
{
	/// <summary>
	/// A MessageFileInfo provides minimal information about a MessageStyle
	/// without having to load everything in the world and keep it around in
	/// memory.
	/// </summary>
	public class MessageStyleInfo
	{
		#region Private Types
		private string name;
		private string path;
		#endregion
		
		#region Constructors
		
		/// <summary>
		/// MessageFileInfo constructor that should only be called by the
		/// ThemeManager for constructing MessageStyles that are known to
		/// be installed and valid (stored in GConf).
		/// <param name="name">The name of the Message Style</param>
		/// <param name="path">The path to the Message Style</param>
		/// </summary>
		public MessageStyleInfo(string name, string path)
		{
			if (name == null || name.Trim ().Length == 0
					|| path == null || path.Trim ().Length == 0)
				throw new ArgumentException ("Exception constructing a MessageStyleInfo because one of the arguments to the constructor is invalid.");
			
Logger.Debug ("MessageStyleInfo (\"{0}\", \"{1}\")", name, path);
			
			this.name = name;
			this.path = path;
		}
		
		/// <summary>
		/// MessageFileInfo constructor
		/// <param name="path">Directory path to a MessageStyle</param>
		/// </summary>
		public MessageStyleInfo(string path)
		{
			if (path == null || path.Trim ().Length == 0 ||
					System.IO.Directory.Exists (path) == false)
				throw new ArgumentException (
					"The path specified is invalid.  \n" +
					"It must point to a valid directory.");
			
			// Parse the plist file enough to grab the name
			string infoPlistPath = System.IO.Path.Combine (path, "Contents/Info.plist");
			XmlDocument doc = new XmlDocument ();
			doc.Load (infoPlistPath);
			
			name = MessageStyle.GetPlistStringValue (doc, "CFBundleName");
			
			if (name == null || name.Trim ().Length == 0)
				throw new Exception ("MessageStyle does not have a valid name");
			
			this.path = path;
		}
		#endregion
		
		#region Public Methods
		/// <summary>
		/// Checks to see if the specified path points to a valid MessageStyle.
		/// <param name="path">Directory path to a MessageStyle</param>
		/// </summary>
		public static bool IsValid (string path)
		{
Logger.Debug ("MessageStyleinfo.IsValid (\"{0}\")", path);
			// Attempt to load and create a MessageStyle
			MessageStyleInfo messageStyleInfo;
			MessageStyle messageStyle;
			try {
				messageStyleInfo = new MessageStyleInfo (path);
			} catch (Exception e) {
				Logger.Debug ("Exception in MessageStyleInfo (\"{0}\"): {1}", path, e.Message);
				return false;
			}
			
			try {
				messageStyle = new MessageStyle (messageStyleInfo);
			} catch (Exception e) {
				Logger.Debug ("Exception in MessageStyle (\"{0}\"): {1}", path, e.Message);
				return false;
			}
			
			return true;
		}

		public static string GetPlistStringValue (XmlDocument doc, string keyName)
		{
			XmlNode node = GetPlistValueNode (doc, keyName, "string");
			if (node != null)
				return node.InnerText;
			
			return null;
		}
		
		public static int GetPlistIntValue (XmlDocument doc, string keyName)
		{
			int val = 0;
			XmlNode node = GetPlistValueNode (doc, keyName, "integer");
			if (node != null)
				val = Int32.Parse (node.InnerText);
			
			return val;
		}
		
		public static bool GetPlistBoolValue (XmlDocument doc, string keyName)
		{
			bool val = false;
			XmlNode node = GetPlistValueNode (doc, keyName, "true");
			if (node != null)
				val = true;
			
			return val;
		}
		
		public static XmlNode GetPlistValueNode (XmlDocument doc, string keyName, string valueType)
		{
			XmlNode node = null;
			
			string xPathExpression = string.Format (
					"//{0}[preceding-sibling::key[.='{1}']]",
					valueType,
					keyName);
			
			node = doc.SelectSingleNode (xPathExpression);
			
			return node;
		}
		
		public override string ToString ()
		{
			return string.Format ("{0}={1}", name, path);
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
