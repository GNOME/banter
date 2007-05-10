//***********************************************************************
// *  $RCSfile$ - XmlFilePreferencesProvider.cs
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
	public class XmlFilePreferencesProvider : IPreferencesProvider
	{
		private XmlDocument doc;

		public event PreferenceChangedEventHandler PreferenceChanged;
		
		public XmlFilePreferencesProvider (string file_path)
		{
			doc = new XmlDocument ();
			try {
				doc.Load (file_path);
			} catch (Exception e) {
				Logger.Debug ("Could not load {0}: {1}", file_path, e.Message);
				throw e;
			}
		}
		
		public void Set (string key, object value)
		{
			// For now, set is not going to be supported
			Logger.Debug ("FIXME: Implement XmlFilePreferencesProvider.Set ()");
		}
		
		public object Get (string key)
		{
			XmlNode node = doc.SelectSingleNode (key);
			if (node == null)
				return null;
			
			string inner_text = node.InnerText;
			if (inner_text == String.Empty)
				return String.Empty;
			
			string lower_case_text = inner_text.ToLower ();
			if (lower_case_text == "true")
				return true;
			
			if (lower_case_text == "false")
				return false;
			
			return inner_text;			
		}

		public void Unset (string key)
		{
			Logger.Debug ("FIXME: Implement XmlFilePreferencesProvider.Unset ()");
		}
		
		public void RecursiveUnset (string key)
		{
			Logger.Debug ("FIXME: Implement XmlFilePreferencesProvider.RecursiveUnset ()");
		}
	}
}
