/***********************************************************************
 *  $RCSfile$ - PersonList.cs
 *
 *  Copyright (C) 2007 Novell, Inc.
 *
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU General Public
 *  License as published by the Free Software Foundation; either
 *  version 2 of the License, or (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 *  General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public
 *  License along with this program; if not, write to the Free
 *  Software Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
 *
 ***********************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

using NDesk.DBus;
using org.freedesktop.DBus;
using org.freedesktop.Telepathy;

namespace Novell.Rtc
{
	///<summary>
	///	PersonList Class
	/// Class used for enumerating all known people, friends 
	/// and banned friends.
	/// 
	/// Also the client interface for clients that wish to discover
	/// the coming and going of friends.
	///</summary>
	public class PersonList
	{
		#region Private Types
		private bool online = false;
		private IList <Account> accounts;
		private Account defaultAccount;
		
		// Private types that must be changed when we 
		// support more than one account
		private uint tpHandle;
		private string username;
		ObjectPath contactListPath;
		IChannelGroup contactListGroup;
		#endregion

		#region Constructors	
		/// <summary>
		///
		/// </summary>
		public PersonList()
		{
		
			// First get all the active authenticated accounts
			IList <Account> myAccounts = AccountManagement.GetAccounts();
			if (myAccounts == null || myAccounts.Count == 0) {
				throw new ApplicationException ("no active accounts");
			}	
			
			// Take a snapshot of my current self
			foreach (Account account in myAccounts ) {
				if (account.Authenticated && account.TPConnection != null) {
					this.online = true;
					
					// Get status from the primary or default account
					if (account.Default == true) {
						this.username = account.Username;
						this.defaultAccount = account;
						this.tpHandle = account.TPConnection.SelfHandle;
						
						// Setup a channel for the contact list
						string[] args = {"subscribe"};
						uint[] handles = account.TPConnection.RequestHandles (HandleType.List, args);
						contactListPath = 
							account.TPConnection.RequestChannel (ChannelType.ContactList, HandleType.List, handles[0], true);
						contactListGroup = 
							Bus.Session.GetObject<IChannelGroup> (defaultAccount.TelepathyBusName, contactListPath);
								
						
						SetupPresenceNotification (contactListGroup);
					}
				}
			}	
		}
		#endregion
		
		#region Private Methods
		private void SetupPresenceNotification (IChannelGroup cg)
		{
		
		}
		#endregion
		
		
		#region Public Methods
		/// <summary>
		/// Method to retrieve all known friends of self
		/// </summary>
		public Person[] GetMyFriends ()
		{
			Console.WriteLine ("GetMyFriends - called");
			
			if (this.online == false)
				throw new ApplicationException ("No authenticated accounts");
				
			ArrayList people = new ArrayList();
			
			try
			{
				IConnection conn = defaultAccount.TPConnection;
				
				string[] args = {"subscribe"};
				uint[] handles = conn.RequestHandles (HandleType.List, args);
				ObjectPath op = conn.RequestChannel (ChannelType.ContactList, HandleType.List, handles[0], true);
				IChannelGroup cl = 
					Bus.Session.GetObject<IChannelGroup> (defaultAccount.TelepathyBusName, op);
					
				// FIX - need to verify this code
				// what happens when a user has a screen name and no alias
				uint[] memberHandles = cl.Members;
				string[] memberNames = conn.InspectHandles (HandleType.Contact, memberHandles);
				string[] aliasNames = conn.RequestAliases (memberHandles);
				
				for (int i = 0; i < memberNames.Length; i++) {
					people.Add ( new Person (false, defaultAccount, memberHandles[i], memberNames[i], aliasNames[i]));
				}
				
			} catch (Exception gmf) {
				Console.WriteLine ("Exception getting subscribed members - message: {0}", gmf.Message);
				throw gmf;
			}
			
			return people.ToArray (typeof(Person)) as Person[];
		}
		
		#endregion
	}
}	
