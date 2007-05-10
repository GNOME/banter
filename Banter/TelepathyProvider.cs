//***********************************************************************
// *  $RCSfile$ - TelepathyProviders.cs
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
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

using Mono.Unix;

using NDesk.DBus;
using org.freedesktop.DBus;
using org.freedesktop.Telepathy;

namespace Novell.Rtc
{
	/// NOTE: This code is temporary - For now we're just going
	/// to discover if Gabble is loaded and launch it.
	///
	/// <summary>
	///	Class to discover what telepathy providers have been
	/// loaded on the system and to then launch the responsible process
	/// </summary>
	public class TelepathyProvider
	{
		private string busName;
		private string objectPath;
		private string providerName;
		private bool video;
		private bool im;
		private bool voice;

		System.Diagnostics.Process gabbleProcess = null;
		
		/// <summary>
		/// Return the telepathy based object path
		/// </summary>
		public string ObjectPath
		{
			get {return objectPath;}
		}
		
		/// <summary>
		/// Return the provider's d-bus name
		/// </summary>
		public string BusName
		{
			get {return busName;}
		}
		
		
		public TelepathyProvider (string managerFile )
		{
			im = true;
			voice = false;
			video = false;
			
			this.providerName = managerFile;
			
			// temp check for managers
			bool foundManagerFile = false;
			foreach (string dir in Defines.TelepathyManagerLocations) {
				if (Directory.Exists (dir) == true) {
					if (File.Exists (String.Format ("{0}/{1}", dir, managerFile)) == true) {
						foundManagerFile = true;
						break;
					}
				}
			}
			
			
			if (foundManagerFile == false )
				throw new ApplicationException	(
					String.Format ("Telepathy provider {0} is not installed on the this system"));

			// For now hard code Gabble
			objectPath = "/org/freedesktop/Telepathy/ConnectionManager/gabble";
			busName = "org.freedesktop.Telepathy.ConnectionManager.gabble";
		}
		
		public bool IsProviderRunning ()
		{
			//get connection manager from dbus
			IConnectionManager connManager = 
				Bus.Session.GetObject<IConnectionManager> (this.busName, new ObjectPath (this.objectPath));

		    if (connManager == null) {
	    	  	Console.WriteLine ("Unable to establish a connection with: {0}", this.providerName);
	    	  	return false;
			}
			
			Console.WriteLine ("{0} is already running", providerName);
			return true;
		
		}
		
		public bool SupportsIM ()
		{
			return im;
		}
		
		public bool SupportsVoice ()
		{
			return voice;
		}
		
		public bool SupportsVideo ()
		{
			return video;
		}
		
 	}
}