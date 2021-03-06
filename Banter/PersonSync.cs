//***********************************************************************
// *  $RCSfile$ - PersonSync.cs
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
	///<summary>
	///	PersonSync Class
	/// Synchronizes the Telepathy contacts with EDS contacts.  This is done using
	/// the ProviderUserManager and the PersonManager.
	///</summary>
	public class PersonSync
	{

		#region Constructors
		/// <summary>
		/// Creates a new PersonSync
		/// </summary>			
		public PersonSync()
		{
		}
		#endregion
		
		
		#region Public Methods
		/// <summary>
		/// This should perform an initial sync of telepathy contacts and EDS contacts then listen to
		/// events on both sides and keep things in sync
		/// </summary>			
		public void Start()
		{
			ProviderUserManager.ProviderUserAdded += ProviderUserAdded;
			ProviderUserManager.ProviderUserRemoved += ProviderUserRemoved;
		}
		
		
		/// <summary>
		/// This should deregister from all events and clean up
		/// </summary>			
		public void Stop()
		{
			ProviderUserManager.ProviderUserAdded -= ProviderUserAdded;
			ProviderUserManager.ProviderUserRemoved -= ProviderUserRemoved;
		}
		#endregion
		
		
		#region Private Methods
		public void ProviderUserAdded (ProviderUser user)
		{
			Person person = PersonManager.GetPerson(user);
			if(person == null) {
				person = new Person(user);
				//person.JabberId = user.Uri;
				PersonManager.AddPerson(person);
			}
		}

		public void ProviderUserRemoved (string uri)
		{
			PersonManager.RemovePerson(uri);
		}
		#endregion
	
	}
}
