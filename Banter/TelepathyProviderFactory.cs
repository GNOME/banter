//***********************************************************************
// *  $RCSfile$ - TelepathyProviderFactory.cs
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
	public class TelepathyProviderFactory
	{
		static private string locker = "lckr";
		static private bool started = false;
		//static private bool stop = false;
		//static private TelepathyProviders tpProviders;
		static public ArrayList Providers;
		static System.Diagnostics.Process gabbleProcess = null;
		
		/// <summary>
		/// Internal method to startup the message store database
		/// This method should only be called by the rtc monitor application
		/// </summary>
        static internal void Start ()
        {
        	Console.WriteLine ("Telepathy providers starting up");

			try {
				if (started == false) {
					lock (locker) {
						if (started == false)
						{
							if (Providers == null)
								Providers = new ArrayList();
								
							// For now just support gabble
							TelepathyProvider tp = new TelepathyProvider("gabble.manager");
							Console.WriteLine ("Gabble installed!");
								
							if (tp.IsProviderRunning () == false) {
									
								// Startup the Gabble process
								Environment.SetEnvironmentVariable ("GABBLE_PERSIST", "1");
								ProcessStartInfo psi = new ProcessStartInfo();
								psi.FileName = "telepathy-gabble";
								psi.UseShellExecute = false;
								gabbleProcess = Process.Start (psi);
			
								Console.WriteLine ("Found and launched Gabble");
							} 
							
							TelepathyProviderFactory.Providers.Add (tp);
							started = true;
						}
					}
				}
			} catch (Exception e) {
				Console.WriteLine (e.Message);
				throw e;
			}
        }

		/// <summary>
		/// Internal method to shutdown and close the database
		/// This method should only be called the rtc monitor application
		/// </summary>
        static internal void Stop ()
        {
        	Console.WriteLine ("Telepathy Providers shutting down");
        	
			try {
				if (started == true && Providers != null) {
					lock (locker) {
						if (gabbleProcess != null) {
							gabbleProcess.Kill ();
						}
						
						Providers.Clear();
						started = false;
					}
				}
				
			} catch (Exception es) {
				Console.WriteLine ("Exception shutting down the TelepathyProviders");
				Console.WriteLine (es.Message);
				throw es;
			}
        }
 	}
}