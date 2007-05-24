//***********************************************************************
// *  $RCSfile$ - AccountManagement.cs
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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

using NDesk.DBus;
using org.freedesktop.DBus;

//using GConf;
using Gnome.Keyring;

namespace Banter
{

	public delegate void AccountInitializing (Account account);
	public delegate void AccountConnecting (Account account);
	public delegate void AccountConnected (Account account);
	public delegate void AccountDisconnected (Account account);
	
	public delegate void IAmUpHandler ();
	
	/// NOTE: This code is temporary - For now we're just going
	/// to gather credentials and authenticate to GoogleTalk
	///
	/// <summary>
	///	Class to manage provider accounts such as Jabber and SIP
	/// </summary>
	public class AccountManagement
	{
		static private System.Object locker;
		static private bool initialized = false;
		static private Thread startThread = null;
		static private Thread shutdownThread = null;
		static private IList <Account> accounts = null;
		static public IAmUpHandler IAmUpEvent;

	   	static AccountManagement()
	   	{
	   		locker = new System.Object ();
        	accounts = new List<Account> ();	
	   	}
	   	
	   	static internal IList <Account> GetAccounts ()
	   	{
	   		return accounts;
	   	}
	   	
	   	static internal bool InitializedFinished ()
	   	{
	   		return initialized;
	   	}
	   	
		/// <summary>
		/// Internal method for starting up and initializing any
		/// current configured accounts.
		/// Any preconfigured accounts that contain full credential sets
		/// and are marked for automatic login with connect and authenticate.
		/// </summary>
        static internal void Initialize ()
        {
        	if (initialized == true) return;
        	
        	if (!AccountInformationFilled ()) {
        		Logger.Debug ("Aborting AccountManagement.Initialize() because the server, port, username, and port aren't all set.");
        		Application.ActionManager ["ShowPreferencesAction"].Activate ();
        		return;
        	}
        	
        	Logger.Debug ("AccountManagement initializing");
        	
			try
			{
				if (startThread == null) {
					lock (locker)
					{
						if (startThread == null)
						{
							AccountManagement.startThread = 
								new Thread (new ThreadStart (AccountManagement.StartupThread));
							startThread.IsBackground = true;
							startThread.Priority = ThreadPriority.Normal;
							startThread.Start();
						}
					}
				}
			}
			catch (Exception e)
			{
				Logger.Debug (e.Message);
				Logger.Debug (e.StackTrace);
				throw e;
			}
        }
        
		/// <summary>
		/// Internal method for shutting down and cleaning up 
		/// all account objects
		/// </summary>
        static internal void Shutdown ()
        {
        	if (initialized == false) return;
        	
        	Logger.Debug ("AccountManagement::Shutdown - called");
        	
			try
			{
				if (shutdownThread == null) {
					lock (locker)
					{
						if (shutdownThread == null)
						{
							AccountManagement.shutdownThread = 
								new Thread (new ThreadStart (AccountManagement.ShutdownThread));
							shutdownThread.IsBackground = true;
							shutdownThread.Priority = ThreadPriority.Normal;
							shutdownThread.Start();
							
							Logger.Debug ("Waiting for accounts to cleanup");
							Thread.Sleep (5000);
							
							Logger.Debug ("Aborting shutdown thread");
							shutdownThread.Abort ();
						}
					}
				}
			}
			catch (Exception e)
			{
				Logger.Debug (e.Message);
				Logger.Debug (e.StackTrace);
				throw e;
			}
        }
        
		/// <summary>
		/// Account Management Shutdown Thread.
		/// </summary>
		static private void ShutdownThread()
		{
			foreach (Banter.Account account in AccountManagement.GetAccounts())
			{
				account.Disconnect();
			}
		}

		/// <summary>
		/// Account Management Startup Thread.
		/// </summary>
		static private void StartupThread()
		{
			Logger.Debug ("AccountManagement::StartupThread - running");
			
			// For now just instantiate our one default account
			AccountManagement.CreateAccountObject ("Google Talk", true);
			
			if (IAmUpEvent != null)
				IAmUpEvent();
			
			AccountManagement.initialized = true;	
		}
		
		// <summary>
		// Temporary hack to determine whether we have non-empty fields for
		// server, port, username, and password for a GoogleTalk account.
		// </summary>
		static private bool AccountInformationFilled ()
		{
			string username;
			string password;

			if (!AccountManagement.GetGoogleTalkCredentialsHack (out username, out password)) {
				Logger.Debug ("Could not get GoogleTalk credentials from GNOME Keyring.");
				return false;
			}
			
			if (username == null || username.Trim ().Length == 0) {
				Logger.Debug ("GoogleTalk Username is empty");
				return false;
			}
			
			if (password == null || password.Trim ().Length == 0) {
				Logger.Debug ("GoogleTalk Password is empty");
				return false;
			}

			string server = Preferences.Get (Preferences.GoogleTalkServer) as String;
			string port = Preferences.Get (Preferences.GoogleTalkPort) as String;

			if (server == null || server.Trim ().Length == 0) {
				Logger.Debug ("GoogleTalk Server Address is empty");
				return false;
			}
			
			if (port == null || port.Trim ().Length == 0) {
				Logger.Debug ("GoogleTalk Server Port is empty");
				return false;
			}
			
			// Everything appears to have a value
			return true;
		}
		
		private static bool GetCredentialsHack (string type, out string username, out string password)
		{
			username = null;
			password = null;
			
			Hashtable attributes = new Hashtable ();
			attributes["name"] = type;
			try {
				ItemData [] results = Ring.Find (ItemType.GenericSecret, attributes);
				if (results != null && results.Length > 0) {
					attributes = results [0].Attributes;
					if (attributes != null) {
						username = attributes ["username"] as String;
						password = results [0].Secret;
					}
				}
			} catch (Exception e) {
				Logger.Warn ("Error storing {0} Credentials for {1}: {2}", type, username, e.Message);
			}
			
			// Return false if a username or password was not retrieved
			if (username == null || password == null)
				return false;
			
			// Successfully retrieved credentials
			return true;
		}
		
		private static void SetCredentialsHack (string type, string username, string password)
		{
			if (username == null || username.Trim ().Length == 0) {
				Logger.Error ("SetCredentialsHack called with null/empty username.");
				throw new ArgumentNullException ("username");
			}
			
			if (password == null || password.Trim ().Length == 0) {
				Logger.Error ("SetCredentialsHack called with null/empty password.");
				throw new ArgumentNullException ("password");
			}
			
			string keyring = Ring.GetDefaultKeyring ();
			Hashtable attributes = new Hashtable ();
			attributes ["name"] = type;
			attributes ["username"] = username.Trim ();
			
			Ring.CreateItem (
					keyring,
					ItemType.GenericSecret,
					"RtcGoogleTalkAccountName",
					attributes,
					password,
					true);
			
			Logger.Info ("Stored credentials for {0}/{1} into the GNOME Keyring.", type, username);
		}
		
#region Public Methods
		// <summary>
		// This is a temporary method hard-coded for GoogleTalk credentials
		// and should eventually be replaced so that Rtc deals with multiple
		// account types.
		// </summary>
		public static bool GetGoogleTalkCredentialsHack (out string username, out string password)
		{
			return GetCredentialsHack ("RtcGoogleTalkAccountName", out username, out password);
		}
		
		// <summary>
		// This is a temporary method hard-coded for GoogleTalk credentials
		// and should eventually be replaced so that Rtc deals with multiple
		// account types.
		// </summary>
		public static void SetGoogleTalkCredentialsHack (string username, string password)
		{
			SetCredentialsHack ("RtcGoogleTalkAccountName", username, password);
		}
		
		public static bool GetSipCredentialsHack (out string username, out string password)
		{
			return GetCredentialsHack ("RtcEkigaSipAccountName", out username, out password);
		}
		
		public static void SetSipCredentialsHack (string username, string password)
		{
			SetCredentialsHack ("RtcEkigaSipAccountName", username, password);
		}
		
		/// <summary>
		/// Method to instantiate and possibly connect/authenticate if
		/// all the credentials are available
		/// <summary>
		public static void CreateAccountObject (string accountname, bool connect)
		{
			string username = null;
			string password = null;
			string server;
			string port;
			
			// hack - for now we support "Google Talk"
			if (accountname.ToLower() != "google talk")
			{
				throw new ApplicationException (String.Format ("{0} account is not configured", accountname));
			}
			
        	// FIXME: Temporary - read our hard coded Google Talk settings
			if (!GetGoogleTalkCredentialsHack (out username, out password)) {
				Logger.Error ("Could not retrieve GoogleTalk account information from Gnome.Keyring.  Probably gonna crash!");
			} else {
				Logger.Info ("Successfully retrieved GoogleTalk credentials from Gnome.Keyring for {0}.", username);
			}
			
			server = Preferences.Get (Preferences.GoogleTalkServer) as string;
			port = Preferences.Get (Preferences.GoogleTalkPort) as string;

        	Banter.JabberAccount account = 
        		new Banter.JabberAccount (
        				"Google Talk",
        				Banter.ProtocolName.Jabber,
        				username,
        				password,
        				server,
        				port,
        				false,
        				true,
        				false);
			account.Default = true;
				
			try
			{
				accounts.Add (account);
				
				if (connect == true)
					account.Connect (true);
					
			} catch (Exception es){
				Logger.Debug (es.Message);
				Logger.Debug (es.StackTrace);
			} finally {
			
			}
		}
#endregion
 	}
}
