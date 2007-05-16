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
using System.Collections.Generic;
using System.IO;
using Gtk;

namespace Banter
{
	// <summary>
	// ThemeManager manages themes, message styles, person styles, and app styles.
	// </summary>
	public class ThemeManager
	{
		private static ThemeManager instance = null;
		private static object locker = new object ();
		
		ListStore themes;
		ThemeInfo selectedTheme;
		TreeIter selectedThemeIter;
		Dictionary<string, TreeIter> themeIters;
		
		ListStore appStyles;
		AppStyleInfo selectedAppStyle;
		TreeIter selectedAppStyleIter;
		Dictionary<string, TreeIter> appStyleIters;
		
		ListStore contactStyles;
		ContactStyleInfo selectedContactStyle;
		TreeIter selectedContactStyleIter;
		Dictionary<string, TreeIter> contactStyleIters;
		
		ListStore messageStyles;
		MessageStyleInfo selectedMessageStyle;
		TreeIter selectedMessageStyleIter;
		Dictionary<string, TreeIter> messageStyleIters;
		MessageStyle messageStyle;
		
		// Path where the system themes are stored
		string systemThemePath;
		
		// Path where user themes are stored
		string userThemePath;
		
		private ThemeManager()
		{
			themes = new ListStore (typeof (ThemeInfo));
			themeIters = new Dictionary<string, TreeIter> ();
		
			appStyles = new ListStore (typeof (AppStyleInfo));
			appStyleIters = new Dictionary<string, TreeIter> ();

			contactStyles = new ListStore (typeof (ContactStyleInfo));
			contactStyleIters = new Dictionary<string, TreeIter> ();

			messageStyles = new ListStore (typeof (MessageStyleInfo));
			messageStyleIters = new Dictionary<string, TreeIter> ();

			systemThemePath = Defines.ThemeDir;
			
			string homeDirectoryPath =
				Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments);
			userThemePath = Path.Combine (homeDirectoryPath, ".banter/Themes/MessageStyles");
		}

#region Private Methods
		private void LoadFromGConf ()
		{
			LoadThemesFromGConf ();
			LoadAppStylesFromGConf ();
			LoadContactStylesFromGConf ();
			LoadMessageStylesFromGConf ();
		}
		
		private void LoadThemesFromGConf ()
		{
			string selectedThemePath = Preferences.Get (Preferences.SelectedTheme) as string;
			string [] validThemePaths = Preferences.Get (Preferences.ValidThemes) as string [];
		}

		private void LoadAppStylesFromGConf ()
		{
			string selectedAppStylePath = Preferences.Get (Preferences.SelectedAppStyle) as string;
			string [] validAppStyleStrings = Preferences.Get (Preferences.ValidAppStyles) as string [];
		}

		private void LoadContactStylesFromGConf ()
		{
			string selectedContactStyleString = Preferences.Get (Preferences.SelectedContactStyle) as string;
			string [] validContactStyleStrings = Preferences.Get (Preferences.ValidContactStyles) as string [];
		}
		
		private void LoadMessageStylesFromGConf ()
		{
			TreeIter iter;
			
			string selectedMessageStyleString = Preferences.Get (Preferences.SelectedMessageStyle) as string;
			string [] validMessageStyleStrings = Preferences.Get (Preferences.ValidMessageStyles) as string [];
			
			foreach (string messageStylePair in validMessageStyleStrings) {
				string name;
				string path;
				
				if (Utilities.ParseNameValuePair (messageStylePair, out name, out path) == false)
					continue;
				
				MessageStyleInfo messageStyleInfo = new MessageStyleInfo (name, path);
				
				iter = messageStyles.Append ();
				messageStyles.SetValue (iter, 0, messageStyleInfo);
				
				messageStyleIters [messageStyleInfo.ToString ()] = iter;
				
				if (string.Compare (messageStylePair, selectedMessageStyleString) == 0) {
					selectedMessageStyle = messageStyleInfo;
					selectedMessageStyleIter = iter;
					try {
						messageStyle = new MessageStyle (messageStyleInfo);
					} catch {}
				}
			}
		}
		
		private void LaunchUpdateThread ()
		{
			Logger.Debug ("FIXME: Implement ThemeManager.LaunchUpdateThread");
		}

/*		
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
					style = new MessageStyle (stylePath);
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
					Logger.Warn ("The ThemeManager was unable to load any MessageStyles from ~/.banter/Themes/MessageStyles.  Strange things may happen.");
				}
			}
			
			if (selectedStyle == null) {
				Logger.Warn ("Unable to load/set a selected MessageStyle in LoadMessageStylesFromPath.");
			}
		}
*/
#endregion
		
#region Public Methods
		/// <summary>
		/// This method should be called almost as soon as possible when the
		/// application is started.  It will start loading and validating all
		/// of the themes.
		/// </summary>
		public static void Init ()
		{
			ThemeManager themeManager = ThemeManager.Instance;
			lock (locker) {
				try {
				// Load cached information about installed styles from GConf
				// so that we don't block the main application process and
				// things can move right along.
				themeManager.LoadFromGConf ();
				
				// Spawn a thread to update installed styles.
				themeManager.LaunchUpdateThread ();
				} catch (Exception e) {
					Logger.Debug ("Exception in ThemeManager.Init: {0}\n{1}", e.Message, e.StackTrace);
				}
			}
		}
		
//		public void AddMessageStyle (MessageStyle style)
//		{
//			// FIXME: Check for a duplicate
//			
//			// FIXME: If this style is not already installed into the
//			// user's area, load it up.
//			TreeIter iter = messageStyles.Append ();
//			messageStyles.SetValue (iter, 0, style);
//		}
#endregion
		
#region Public Properties
		public static ThemeManager Instance
		{
			get {
				lock (locker) {
					if (instance == null) {
						lock (locker) {
							instance = new ThemeManager ();
						}
					}
				}
				
				return instance;
			}
		}
		
		public static string SystemThemePath
		{
			get {
				ThemeManager mgr = ThemeManager.Instance;
				return mgr.systemThemePath;
			}
		}
		
		public static string UserThemePath
		{
			get {
				ThemeManager mgr = ThemeManager.Instance;
				return mgr.userThemePath;
			}
		}
		
		public static ListStore Themes
		{
			get {
				ThemeManager mgr = ThemeManager.Instance;
				return mgr.themes;
			}
		}
		
		public static ListStore AppStyles
		{
			get {
				ThemeManager mgr = ThemeManager.Instance;
				return mgr.appStyles;
			}
		}
		
		public static ListStore ContactStyles
		{
			get {
				ThemeManager mgr = ThemeManager.Instance;
				return mgr.contactStyles;
			}
		}
		
		public static ListStore MessageStyles
		{
			get {
				ThemeManager mgr = ThemeManager.Instance;
				return mgr.messageStyles;
			}
		}

		public static MessageStyleInfo SelectedMessageStyle
		{
			get {
				ThemeManager mgr = ThemeManager.Instance;
				return mgr.selectedMessageStyle;
			}
			set {
				ThemeManager mgr = ThemeManager.Instance;
				mgr.selectedMessageStyle = value;
				if (value == null) {
					Preferences.Set (Preferences.SelectedMessageStyle,
							string.Empty);
					mgr.selectedMessageStyleIter = TreeIter.Zero;
				} else {
					string pair = value.ToString ();
					Preferences.Set (Preferences.SelectedMessageStyle, pair);
					if (mgr.messageStyleIters.ContainsKey (pair))
						mgr.selectedMessageStyleIter = mgr.messageStyleIters [pair];
					else
						mgr.selectedMessageStyleIter = TreeIter.Zero;
				}
			}
		}
		
		public static TreeIter SelectedMessageStyleIter
		{
			get {
				ThemeManager mgr = ThemeManager.Instance;
				return mgr.selectedMessageStyleIter;
			}
		}
		
		public static MessageStyle MessageStyle
		{
			get {
				ThemeManager mgr = ThemeManager.Instance;
				return mgr.messageStyle;
			}
		}
#endregion
	}
}
