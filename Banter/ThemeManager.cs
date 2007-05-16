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
using System.Threading;
using Gtk;

namespace Banter
{
	// <summary>
	// ThemeManager manages themes, message styles, person styles, and app styles.
	// </summary>
	public class ThemeManager
	{
		const string AppStylesSubdir = "AppStyles";
		const string ContactStylesSubdir = "ContactStyles";
		const string MessageStylesSubdir = "MessageStyles";
		
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
		
		Thread updateThread;

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
			userThemePath = Path.Combine (homeDirectoryPath, ".banter/Themes");
		}

#region Private Methods
		/// <summary>
		/// Load as much information from GConf about themes and styles as
		/// as quickly as possible so we don't slow down the main application.
		/// </summary>
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
					SelectedMessageStyle = messageStyleInfo;
//					selectedMessageStyle = messageStyleInfo;
//					selectedMessageStyleIter = iter;
//					try {
//						messageStyle = new MessageStyle (messageStyleInfo);
//					} catch (Exception e) {
//						Logger.Warn ("Couldn't load the default MessageStyle: {0}\n{1}", e.Message, e.StackTrace);
//					}
				}
			}
		}
		
		private void UpdateThread ()
		{
			// Make sure the user theme and style paths exist
			Utilities.CreateDirectoryIfNeeded (userThemePath);
			Utilities.CreateDirectoryIfNeeded (UserAppStylesPath);
			Utilities.CreateDirectoryIfNeeded (UserContactStylesPath);
			Utilities.CreateDirectoryIfNeeded (UserMessageStylesPath);

			UpdateThemes (systemThemePath);
			UpdateThemes (userThemePath);
			
			UpdateAppStyles (SystemAppStylesPath);
			UpdateAppStyles (UserAppStylesPath);
			
			UpdateContactStyles (SystemContactStylesPath);
			UpdateContactStyles (UserContactStylesPath);
			
			UpdateMessageStyles (SystemMessageStylesPath);
			UpdateMessageStyles (UserMessageStylesPath);
		}
		
		private void UpdateThemes (string path)
		{
			Logger.Debug ("FIXME: Implement ThemeManager.UpdateThemes ()");
			if (Directory.Exists (path) == false)
				return;
		}
		
		private void UpdateAppStyles (string path)
		{
			Logger.Debug ("FIXME: Implement ThemeManager.UpdateAppStyles ()");
			if (Directory.Exists (path) == false)
				return;
		}
		
		private void UpdateContactStyles (string path)
		{
			Logger.Debug ("FIXME: Implement ThemeManager.UpdateContactStyles ()");
			if (Directory.Exists (path) == false)
				return;
		}
		
		private void UpdateMessageStyles (string path)
		{
			if (Directory.Exists (path) == false)
				return;

Logger.Debug ("ThemeManager.UpdateMessageStyles (\"{0}\")", path);
			string selectedMessageStyleString = Preferences.Get (Preferences.SelectedMessageStyle) as string;

			foreach (string stylePath in
					Directory.GetDirectories (path, "*.AdiumMessageStyle")) {
				if (MessageStyleInfo.IsValid (stylePath)) {
					MessageStyleInfo styleInfo = null;
					try {
						styleInfo = new MessageStyleInfo (stylePath);
					} catch {}
					
					if (styleInfo == null)
						continue;
					
					// Check to see if this MessageStyle already exists.  If it
					// does, override it
					TreeIter iter;
					string messageStylePair = styleInfo.ToString ();
					if (messageStyleIters.ContainsKey (messageStylePair)) {
						iter = messageStyleIters [messageStylePair];
					} else {
						iter = messageStyles.Append ();
					}
					
					messageStyles.SetValue (iter, 0, styleInfo);
					messageStyleIters [messageStylePair] = iter;
					
					// If this matches the currently selected style, replace it on
					// the main thread so any attached UI will be able to update correctly
					if (string.Compare (messageStylePair, selectedMessageStyleString) == 0) {
						Gtk.Application.Invoke (delegate {
							SelectedMessageStyle = styleInfo;
						});
					}
				}
			}
		}
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
				
				// Launch a thread to update the list of existing themes and
				// styles in the background.
				try {
					themeManager.updateThread =
						new Thread (new ThreadStart (themeManager.UpdateThread));
					themeManager.updateThread.IsBackground = true;
					themeManager.updateThread.Priority = ThreadPriority.BelowNormal;
					themeManager.updateThread.Start ();
				} catch (Exception e) {
					Logger.Warn ("Could not start the ThemeManager.UpdateThread: {0}\n{1}",
							e.Message, e.StackTrace);
				}

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
		
		public static string SystemThemesPath
		{
			get {
				ThemeManager mgr = ThemeManager.Instance;
				return mgr.systemThemePath;
			}
		}
		
		public static string UserThemesPath
		{
			get {
				ThemeManager mgr = ThemeManager.Instance;
				return mgr.userThemePath;
			}
		}
		
		public static string SystemAppStylesPath
		{
			get {
				ThemeManager mgr = ThemeManager.Instance;
				return Path.Combine (mgr.systemThemePath, AppStylesSubdir);
			}
		}
		
		public static string UserAppStylesPath
		{
			get {
				ThemeManager mgr = ThemeManager.Instance;
				return Path.Combine (mgr.userThemePath, AppStylesSubdir);
			}
		}
		
		public static string SystemContactStylesPath
		{
			get {
				ThemeManager mgr = ThemeManager.Instance;
				return Path.Combine (mgr.systemThemePath, ContactStylesSubdir);
			}
		}
		
		public static string UserContactStylesPath
		{
			get {
				ThemeManager mgr = ThemeManager.Instance;
				return Path.Combine (mgr.userThemePath, ContactStylesSubdir);
			}
		}
		
		public static string SystemMessageStylesPath
		{
			get {
				ThemeManager mgr = ThemeManager.Instance;
				return Path.Combine (mgr.systemThemePath, MessageStylesSubdir);
			}
		}
		
		public static string UserMessageStylesPath
		{
			get {
				ThemeManager mgr = ThemeManager.Instance;
				return Path.Combine (mgr.userThemePath, MessageStylesSubdir);
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
					mgr.messageStyle = null;
				} else {
					string pair = value.ToString ();
					Preferences.Set (Preferences.SelectedMessageStyle, pair);
					if (mgr.messageStyleIters.ContainsKey (pair))
						mgr.selectedMessageStyleIter = mgr.messageStyleIters [pair];
					else
						mgr.selectedMessageStyleIter = TreeIter.Zero;
					
					// Load a MessageStyle so it's ready for any UI that needs it.
					try {
						mgr.messageStyle = new MessageStyle (value);
					} catch (Exception e) {
						Logger.Warn ("Couldn't set the MessageStyle ({0}): {1}\n{2}",
								value.ToString (), e.Message, e.StackTrace);
					}
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
