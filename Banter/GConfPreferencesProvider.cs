//***********************************************************************
// *  $RCSfile$ - GConfPreferencesProvider.cs
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
using GConf;

namespace Banter
{
	public class GConfPreferencesProvider : IPreferencesProvider
	{
		public event PreferenceChangedEventHandler PreferenceChanged;
		static HackedGConfClient client;
		static GConf.NotifyEventHandler changed_handler;
		
		public GConfPreferencesProvider ()
		{
		}

		public void Set (string key, object value)
		{
			try {
				Client.Set (key, value);
			} catch {}
		}
		
		// <summary>
		// Returns the object setting for the specified key.  If the setting
		// does not exist, null will be returned.
		// </summary>
		public object Get (string key)
		{
			try {
				return Client.Get (key);
			} catch (GConf.NoSuchKeyException) {
				return null;
			}
		}

		public void Unset (string key)
		{
			try {
				Client.Unset (key);
			} catch {}
		}
		
		public void RecursiveUnset (string key)
		{
			try {
				Client.RecursiveUnset (key);
			} catch {}
		}

		private HackedGConfClient Client
		{
			get
			{
				if (client == null) {
					client = new HackedGConfClient ();
					
					changed_handler = new GConf.NotifyEventHandler (OnSettingChanged);
					client.AddNotify ("/apps/rtc", changed_handler);
				}
				
				return client;
			}
		}

		void OnSettingChanged (object sender, GConf.NotifyEventArgs args)
		{
			if (PreferenceChanged != null)
				PreferenceChanged (this, new PreferenceChangedEventArgs (args.Key, args.Value));
		}

		[DllImport("gconf-2")]
		static extern uint gconf_client_add_dir (IntPtr client, string dir, int preload, out IntPtr err);

		[DllImport("gconf-2")]
		private static extern bool gconf_client_unset (IntPtr client, string key, out IntPtr err);

		[DllImport("gconf-2")]
		private static extern bool gconf_client_recursive_unset (IntPtr client, string key, int flags, out IntPtr err);
		
		[DllImport("gconf-2")]
		private static extern IntPtr gconf_client_get_default ();
		
		class HackedGConfClient : GConf.Client
		{
			IntPtr RawPtr = IntPtr.Zero;
			
			public HackedGConfClient () : base ()
			{
				RawPtr = gconf_client_get_default ();
			}
			
			public void Unset (string key)
			{
				IntPtr err;
				gconf_client_unset (RawPtr, key, out err);
			}
			
			public void RecursiveUnset (string key)
			{
				IntPtr err;
				gconf_client_recursive_unset (RawPtr, key, 0, out err);
			}
		}
	}
}
