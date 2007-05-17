//***********************************************************************
// *  $RCSfile$ - Preferences.cs
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
	public class Preferences
	{
		private static IPreferencesProvider provider = null;
		
		public static event PreferenceChangedEventHandler PreferenceChanged;
		
		// This is needed because of a bug in GConf that doesn't allow
		// an empty list.
		static readonly string[] DefaultGroupWindows = {};
		
		static readonly string[] DefaultThemes = {
			"Banter=Banter.theme"
		};
		
		static readonly string[] DefaultAppStyles = {
			"Banter=AppStyles/Banter"
		};
		
		static readonly string[] DefaultContactStyles = {
			"basic=ContactStyles/basic",
			"basic-black=ContactStyles/basic-black"
		};
		
		static readonly string[] DefaultMessageStyles = {
			"PurePlastics=MessageStyles/PurePlastics.AdiumMessageStyle"
		};
		
		public const string GroupWindows = "/apps/banter/group_windows/saved_windows";
		public const string GroupWindowPrefix = "/apps/banter/group_windows";

		// FIXME: Remove this when we start supporting multiple account types
	   	public const string GoogleTalkServer = "/apps/banter/accounts/jabber/google-talk/server";
	   	public const string GoogleTalkPort = "/apps/banter/accounts/jabber/google-talk/port";
	   	public const string SipServer = "/apps/banter/accounts/sip/ekiga/server";
	   	public const string SipPort = "/apps/banter/accounts/sip/ekiga/port";
	   	
	   	// Theme Preferences
	   	public const string SelectedTheme = "/apps/banter/themes/selected_theme";
	   	public const string ValidThemes = "/apps/banter/themes/valid_themes";
	   	
	   	public const string SelectedAppStyle = "/apps/banter/themes/selected_app_style";
	   	public const string ValidAppStyles = "/apps/banter/themes/valid_app_styles";
	   	
	   	public const string SelectedContactStyle = "/apps/banter/themes/selected_contact_style";
	   	public const string ValidContactStyles = "/apps/banter/themes/valid_contact_styles";
	   	
	   	public const string SelectedMessageStyle = "/apps/banter/themes/selected_message_style";
	   	public const string SelectedMessageStyleVariant = "/apps/banter/themes/selected_message_style_variant";
	   	public const string ValidMessageStyles = "/apps/banter/themes/valid_message_styles";

		static Preferences ()
		{
			// Use the GConf provider by default
			provider = new GConfPreferencesProvider (); 
		}
		
		private Preferences()
		{
		}
		
		// <summary>
		// Set a new IPreferencesProvider.  Calls to Get/Set will immediately
		// use the new provider.
		// </summary>
		public static void SetPreferencesProvider (IPreferencesProvider newProvider)
		{
			if (newProvider != null)
				provider = newProvider;
		}
		
		// <summary>
		// Return a default setting for the specified key.  If you add to the
		// defaults, make sure they match up with the defaults specified in
		// the data/rtc.schemas file.
		// </summary>
		public static object GetDefault (string key)
		{
			switch (key) {
			case SelectedTheme:
				return "Banter=Banter.theme";
			case ValidThemes:
				return DefaultThemes;
			
			case SelectedAppStyle:
				return "Banter=AppStyles/Banter";
			case ValidAppStyles:
				return DefaultAppStyles;
			
			case SelectedContactStyle:
				return "basic=ContactStyles/basic";
			case ValidContactStyles:
				return DefaultContactStyles;
			
			case SelectedMessageStyle:
				return "PurePlastics=MessageStyles/PurePlastics.AdiumMessageStyle";
			case ValidMessageStyles:
				return DefaultMessageStyles;
			
			case SelectedMessageStyleVariant:
				return "Blue vs Green";
			case GoogleTalkServer:
				return "talk.google.com";
			case GoogleTalkPort:
				return "5223";
			case SipServer:
				return "ekiga.net";
//			case GroupWindows:
//				return DefaultGroupWindows;
			}
			
			return null;
		}
		
		// <summary>
		// Return the setting object for the specified key.  It's assumed that the caller knows
		// the type object that will be returned.
		// </summary>
		public static object Get (string key)
		{
			object val = null;
			try {
				val = provider.Get (key);
			} catch {
				Logger.Info ("Saved preference not found for {0}.", key);
			}
			
			if (val == null) {
				object defaultValue = GetDefault (key);
				if (defaultValue != null) {
					Logger.Info ("Setting a default value for {0}.", key);
					Set (key, defaultValue);
				}
				
				return defaultValue;
			}
			
			return val;
		}
		
		// <summary>
		// Set a value for the specified key.
		// </summary>
		public static void Set (string key, object value)
		{
			provider.Set (key, value);
		}
		
		// <summary>
		// Unsets the value for the specified key.
		// </summary>
		public static void Unset (string key)
		{
			provider.Unset (key);
		}
		
		// <summary>
		// Recursively unsets the specified key and any of its children
		// </summary>
		public static void RecursiveUnset (string key)
		{
			provider.RecursiveUnset (key);
		}
	}

	public interface IPreferencesProvider
	{
		event PreferenceChangedEventHandler PreferenceChanged;
		void Set (string key, object value);
		object Get (string key);
		void Unset (string key);
		void RecursiveUnset (string key);
	}
	
	public delegate void PreferenceChangedEventHandler (object sender, PreferenceChangedEventArgs args); 
	
	public class PreferenceChangedEventArgs
	{
		private string key;
		private object value;
		
		public PreferenceChangedEventArgs (string key, object value)
		{
			this.key = key;
			this.value = value;
		}
		
		public string Key
		{
			get { return key; }
		}
		
		public object Value
		{
			get { return value; }
		}
	}
}
