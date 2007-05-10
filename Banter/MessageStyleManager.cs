//***********************************************************************
// *  $RCSfile$ - MessageStyleManager.cs
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
	
	
	public class MessageStyleManager
	{
		private static MessageStyleManager instance = null;
		private static object locker = new object ();
		
		ListStore messageStyles;
		MessageStyle selectedStyle;
		
		// Path where the default/system styles are stored
		//string systemStylesPath;
		
		// Path where user styles are stored
		string userStylesPath;
		
		private MessageStyleManager()
		{
			messageStyles = new ListStore (typeof (MessageStyle));
			
			// FIXME: Set up the systemStylePath
			
			string homeDirectoryPath =
				Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments);
			userStylesPath = Path.Combine (homeDirectoryPath, ".banter/Themes/MessageStyles");
		}

#region Private Methods
		private void Init ()
		{
			LoadMessageStylesFromPath (userStylesPath);
		}
		
		private void LoadMessageStylesFromPath (string path)
		{
			if (!Directory.Exists (path)) {
				Console.WriteLine ("{0} does not exist");
				return;
			}
			
			foreach (string stylePath in Directory.GetDirectories (path, "*.AdiumMessageStyle")) {
				MessageStyle style;
				
				try {
					Console.WriteLine ("About to load: {0}", stylePath);
					style = MessageStyle.CreateFromPath (stylePath);
					AddMessageStyle (style);
				} catch (Exception e) {
					Console.WriteLine ("Error loading style: {0}", e.Message);
				}
			}
			
			// Set the selectedStyle based on what is found in the preferences
			string selectedStyleName = Preferences.Get (Preferences.MessageStyleName) as String;
			if (selectedStyleName != null) {
				TreeIter iter;
				if (messageStyles.GetIterFirst (out iter)) {
					do {
						MessageStyle style = messageStyles.GetValue (iter, 0) as MessageStyle;
						if (String.Compare (style.Name, selectedStyleName) == 0) {
							selectedStyle = style;
							Logger.Info ("Selected MessageStyle is {0}.", style.Name);
							break;
						}
					} while (messageStyles.IterNext (ref iter));
				} else {
					Logger.Warn ("The MessageStyleManager was unable to load any MessageStyles from ~/.banter/Themes/MessageStyles.  Strange things may happen.");
				}
			}
			
			if (selectedStyle == null) {
				Logger.Warn ("Unable to load/set a selected MessageStyle in LoadMessageStylesFromPath.");
			}
		}
#endregion
		
#region Public Methods
		public static MessageStyleManager GetInstance ()
		{
			lock (locker) {
				if (instance == null) {
					lock (locker) {
						instance = new MessageStyleManager ();
						instance.Init ();
					}
				}
			}
			
			return instance;
		}
		
		public void AddMessageStyle (MessageStyle style)
		{
			// FIXME: Check for a duplicate
			
			// FIXME: If this style is not already installed into the
			// user's area, load it up.
			TreeIter iter = messageStyles.Append ();
			messageStyles.SetValue (iter, 0, style);
		}
#endregion
		
#region Public Properties
		
		public static MessageStyle SelectedMessageStyle
		{
			get {
				MessageStyleManager mgr = MessageStyleManager.GetInstance ();
				return mgr.selectedStyle;
			}
			set {
				MessageStyleManager mgr = MessageStyleManager.GetInstance ();
				mgr.selectedStyle = value;
				Preferences.Set (Preferences.MessageStyleName, value.Name);
			}
		}
		
		public static TreeIter SelectedMessageStyleIter
		{
			get {
				Console.WriteLine ("FIXME: MessageStyleManager.SelectedMessageStyleIter: We really should store the TreeIters in a dictionary, but for now, just loop through the ListStore");
				MessageStyleManager mgr = MessageStyleManager.GetInstance ();
				TreeIter iter;
				if (mgr.messageStyles.GetIterFirst (out iter)) {
					do {
						MessageStyle style = mgr.messageStyles.GetValue (iter, 0) as MessageStyle;
						if (style == mgr.selectedStyle) {
							Logger.Debug ("MessageStyleManager.SelectedMessageStyleIter/Get found selected style");
							return iter;
						}
					} while (mgr.messageStyles.IterNext (ref iter));
				}
				
				Logger.Debug ("MessageStyleManager.SelectedMessageStyleIter/Get did NOT find anything.");
				return TreeIter.Zero;
			}
		}
		
		public static ListStore MessageStyles
		{
			get {
				MessageStyleManager mgr = MessageStyleManager.GetInstance ();
				return mgr.messageStyles;
			}
		}
#endregion
	}
}
