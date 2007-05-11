//***********************************************************************
// *  $RCSfile$ - ProviderUserManager.cs
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


namespace Banter
{
	///<summary>
	///	ProviderUserManager Class
	/// ProviderUserManager is a singleton that manages all ProviderUsers in Banter.
	///</summary>	
	public class ProviderUserManager
	{
		#region Private Static Types
		private static Banter.ProviderUserManager manager = null;
		private static System.Object locker = new System.Object();
		#endregion	
		
		
		#region Private Types
		private Dictionary<string, ProviderUser> users;
		#endregion
		

		#region Public Static Properties
		/// <summary>
		/// Obtain the singleton for ProviderUserManager
		/// </summary>		
		public static ProviderUserManager Instance
		{
			get
			{
				lock(locker) {
					if(manager == null) {
						lock(locker) {
							manager = new ProviderUserManager();
						}
					}
					return manager;
				}
			}
		}
		#endregion



		#region Constructors
		/// <summary>
		/// A private constructor used when obtaining the Singleton by using the static property Instance.
		/// </summary>			
		private ProviderUserManager ()
		{
			users = new Dictionary<string, ProviderUser> ();		
		}
		#endregion


		#region Public Static Methods
		/// <summary>
		/// Gets the ProviderUser for the specified key
		/// The key is basically "Uri:Provider"
		/// </summary>	
		public static ProviderUser GetProviderUser(string key)
		{
			ProviderUser user = null;

			lock(locker) {			
				if(ProviderUserManager.Instance.users.ContainsKey(key))
					user = ProviderUserManager.Instance.users[key];
			}
			return user;
		}


		/// <summary>
		/// Adds the ProviderUser for the given key
		/// The key is basically "Uri:Provider"
		/// </summary>	
		public static void AddProviderUser(string key, ProviderUser user)
		{
			lock(locker) {
				if(!ProviderUserManager.Instance.users.ContainsKey(key)) {
					ProviderUserManager.Instance.users[key] = user;
				}
				else {
					throw new ApplicationException("key already exists");
				}
			}
		}


		/// <summary>
		/// Creates a new ProviderUser and adds them in one atomic operation
		/// </summary>	
		public static ProviderUser CreateProviderUser(string uri, string protocol)
		{
			lock(locker) {
				string key = CreateKey(uri, protocol);
				
				if(!ProviderUserManager.Instance.users.ContainsKey(key)) {
					ProviderUser user = new ProviderUser();
					user.Uri = uri;
					user.Protocol = protocol;
					ProviderUserManager.Instance.users[key] = user;
					return user;
				}
				else
					throw new ApplicationException("key already exists");
			}
		}		
		
		
		/// <summary>
		/// Gets the ProviderUser for the specified key
		/// The key is basically "Uri:Provider"
		/// </summary>	
		public static ProviderUser[] GetMe()
		{
			return null;
		}		


		/// <summary>
		/// Generates a key to locate a ProviderUser
		/// </summary>	
		public static string CreateKey(string uri, string protocol)
		{
			return uri + ":" + protocol;
		}
		#endregion		
		
	}
}