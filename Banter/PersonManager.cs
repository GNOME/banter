//***********************************************************************
// *  $RCSfile$ - PersonManager.cs
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
using Evolution;
using GLib;
using System.Collections;
using System.Collections.Generic;


namespace Banter
{
	///<summary>
	///	PersonManager Class
	/// PersonManager is a singleton that is the main interface into EDS.  It provides models for the groups
	/// which can be used to create a UI.  It also provides methods to manipulate
	/// Person and PersonGroup objects in the EDS.
	///</summary>
	public class PersonManager
	{

		#region Private Static Types
		private static Banter.PersonManager store = null;
		private static System.Object locker = new System.Object();
		#endregion

	
		#region Private Types
		private Book systemBook;
		private Gtk.TreeStore groupTreeStore;
		private Gtk.TreeStore personTreeStore;
		private BookView bookView;
		private Dictionary<string, Gtk.TreeIter> groupIters;
		private Dictionary<string, Gtk.TreeIter> personIters;
		private Person me;
		#endregion

		#region Internal Properties
		/// <summary>
		/// The internal Evolution.Book that is connected to the system address book
		/// </summary>
		internal Evolution.Book Book
		{
			get
			{
				return systemBook;
			}
		}
		#endregion


		#region Public Static Properties
		/// <summary>
		/// Obtain the singleton for PersonManager
		/// </summary>		
		public static PersonManager Instance
		{
			get
			{
				lock(locker) {
					if(store == null) {
						lock(locker) {
							store = new PersonManager();
						}
					}
					return store;
				}
			}
		}


		/// <summary>
		/// A TreeModel of the Groups created from List attributed contacts in EDS
		/// </summary>			
		public static Gtk.TreeModel Groups
		{
			get
			{
				return PersonManager.Instance.groupTreeStore;
			}
		}
		

		/// <summary>
		/// A TreeModel of the People created from contacts in EDS
		/// </summary>			
		public static Gtk.TreeModel People
		{
			get
			{
				return PersonManager.Instance.personTreeStore;
			}
		}
		
		
		/// <summary>
		/// The Person representing the current user
		/// </summary>
		public static Person Me
		{
			get
			{
				if(PersonManager.Instance.me == null) {
					Gtk.TreeIter iter;
					
					if(PersonManager.Instance.personTreeStore.GetIterFirst(out iter))
					{
						do {
							Person person = (Person) PersonManager.Instance.personTreeStore.GetValue(iter, 0);
							if(person.IsMe) {
								PersonManager.Instance.me = person;
								return person;
							}
						}
						while(PersonManager.Instance.personTreeStore.IterNext(ref iter));
					}
				}
				return PersonManager.Instance.me;
			}
		}			
		#endregion


		#region Constructors
		/// <summary>
		/// A private constructor used when obtaining the Singleton by using the static property Instance.
		/// </summary>			
		private PersonManager ()
		{
			systemBook = Book.NewSystemAddressbook();
			systemBook.Open(true);
			groupIters = new Dictionary<string, Gtk.TreeIter> ();
			personIters = new Dictionary<string, Gtk.TreeIter> ();
			InitStores();
		}
		#endregion	

		
		#region Private Methods
		/// <summary>
		/// Initializes the Group and People Stores
		/// </summary>			
		private void InitStores()
		{
			// Initialize the stores
			groupTreeStore = new Gtk.TreeStore(typeof(PersonGroup));
			personTreeStore =  new Gtk.TreeStore(typeof(Person));
			
			// Query Evolution for all contact objects (Contacts and Lists)
			// to populate the stores
			Evolution.BookQuery q = Evolution.BookQuery.AnyFieldContains ("");

			ArrayList fieldsList = new ArrayList ();
			bookView = systemBook.GetBookView (q, fieldsList, -1);

			bookView.ContactsAdded += OnContactsAdded;
			bookView.ContactsChanged += OnContactsChanged;
			bookView.ContactsRemoved += OnContactsRemoved;

			// starting the view will cause the above events to fire
			// and begin populating the stores
			bookView.Start();
		}

		
		/// <summary>
		/// Handles BookView event ContactsAdded
		/// </summary>			
		private void OnContactsAdded (object o, Evolution.ContactsAddedArgs args)
		{
			foreach (Contact contact in args.Contacts) {
				// test to make sure this is a list
				if(contact.List == true) { 
					if(groupIters.ContainsKey(contact.Id)) {
						Gtk.TreeIter iter = groupIters[contact.Id];
						PersonGroup group = (PersonGroup) groupTreeStore.GetValue(iter, 0);	
						group.EDSContact = contact;
						// because we just changed the internal object, we need to emit the event
						groupTreeStore.EmitRowChanged(groupTreeStore.GetPath(iter), iter);
						Logger.Debug("PersonManager.OnContactsAdded - Updated PersonGroup: {0}", group.DisplayName);
					}
					else {
						PersonGroup group = new PersonGroup(contact);
						Gtk.TreeIter iter = groupTreeStore.AppendValues(group);
						groupIters[contact.Id] = iter;
						Logger.Debug ("PersonManager.OnContactsAdded - Added PersonGroup: {0}", contact.FileAs);
					}
				}
				else {
					if(personIters.ContainsKey(contact.Id)) {
						Gtk.TreeIter iter = personIters[contact.Id];
						Person person = (Person) personTreeStore.GetValue(iter, 0);
						person.EDSContact = contact;
						// because we just changed the internal object, we need to emit the event
						personTreeStore.EmitRowChanged(personTreeStore.GetPath(iter), iter);
						Logger.Debug("PersonManager.OnContactsAdded - Updated Person: {0}", contact.FileAs);
					}
					else {
						Person person = new Person(contact);
						Gtk.TreeIter iter = personTreeStore.AppendValues(person);
						personIters[contact.Id] = iter;
						person.UpdateProviderUsers();
						Logger.Debug ("PersonManager.OnContactsAdded - Added Person: {0}", person.DisplayName);
					}
				}
			}
		}


		/// <summary>
		/// Handles BookView event ContactsChanged
		/// </summary>	
		private void OnContactsChanged (object o, Evolution.ContactsChangedArgs args)
		{
			foreach (Contact contact in args.Contacts) {
				if(contact.List == true) {
					if(groupIters.ContainsKey(contact.Id)) {
						Gtk.TreeIter iter = groupIters[contact.Id];
						PersonGroup group = (PersonGroup) groupTreeStore.GetValue(iter, 0);	
						group.EDSContact = contact;
						// because we just changed the internal object, we need to emit the event
						groupTreeStore.EmitRowChanged(groupTreeStore.GetPath(iter), iter);
						Logger.Debug("PersonManager.OnContactsChanged - Updated PersonGroup: {0}", group.DisplayName);
					}
					else {
						PersonGroup group = new PersonGroup(contact);
						Gtk.TreeIter iter = groupTreeStore.AppendValues(group);
						groupIters[contact.Id] = iter;
						Logger.Debug ("PersonManager.OnContactsChanged - Added PersonGroup: {0}", contact.FileAs);
					}
				}
				else {
					if(personIters.ContainsKey(contact.Id)) {
						Gtk.TreeIter iter = personIters[contact.Id];
						Person person = (Person) personTreeStore.GetValue(iter, 0);
						person.EDSContact = contact;
						// because we just changed the internal object, we need to emit the event
						personTreeStore.EmitRowChanged(personTreeStore.GetPath(iter), iter);
						Logger.Debug("PersonManager.OnContactsChanged - Updated Person: {0}", contact.FileAs);
					}
					else {
						Person person = new Person(contact);
						Gtk.TreeIter iter = personTreeStore.AppendValues(person);
						personIters[contact.Id] = iter;
						person.UpdateProviderUsers();						
						Logger.Debug ("PersonManager.OnContactsChanged - Added Person: {0}", person.DisplayName);
					}
				}
			}
		}


		/// <summary>
		/// Handles BookView event ContactsRemoved
		/// </summary>	
		private void OnContactsRemoved (object o, Evolution.ContactsRemovedArgs args)
		{
			// FIXME: This is a temporary workaround for the
			// fact that the evolution bindings return a
			// GLib.List with an object type, but there are
			// really strings in there.

			GLib.List id_list = new GLib.List (args.Ids.Handle,
							   typeof (string));

			foreach (string contactId in id_list) {
				if(groupIters.ContainsKey(contactId)) {
					Gtk.TreeIter iter = groupIters[contactId];
					PersonGroup group = (PersonGroup) groupTreeStore.GetValue(iter, 0);
					groupTreeStore.Remove(ref iter);
					Logger.Debug("PersonManager.OnContactsRemoved - Removed PersonGroup: {0}", group.DisplayName);
				}
				else if(personIters.ContainsKey(contactId)) {
					Gtk.TreeIter iter = personIters[contactId];
					Person person = (Person) personTreeStore.GetValue(iter, 0);
					personTreeStore.Remove(ref iter);
					Logger.Debug("PersonManager.OnContactsRemoved - Removed Person: {0}", person.DisplayName);
				}
			}
		}
		

		/// <summary>
		/// Finds a person based on an EDS Query.  It will run the query and return the first contact
		/// from the results.
		/// </summary>	
		private Person FindPerson(Evolution.BookQuery q)
		{
			Person person = null;
			
			Contact[] contactList = systemBook.GetContacts(q);
			if( (contactList != null) && (contactList.Length > 0) ) {
				Contact contact = contactList[0];
				if(personIters.ContainsKey(contact.Id)) {
					Gtk.TreeIter iter = personIters[contact.Id];
					person = (Person) personTreeStore.GetValue(iter, 0);
				}
				else
					Logger.Debug("EDS Contact was found, but wasn't in the list: {0}", contact.FileAs);
			}
			return person;
		}
		#endregion
		

		#region Public Static Methods
		/// <summary>
		/// Gets the Person object for a given Jabber ID
		/// </summary>	
		public static Person GetPersonByJabberId(string jabberId)
		{
			Evolution.BookQuery q = Evolution.BookQuery.FieldTest(	ContactField.ImJabber, 
																	BookQueryTest.Is, 
																	jabberId);
			return PersonManager.Instance.FindPerson(q);
		}


		/// <summary>
		/// Gets the Person object for a given Jabber ID
		/// </summary>	
		public static Person GetPerson(ProviderUser user)
		{
			if(user.Protocol.CompareTo(ProtocolName.Jabber) == 0) {
				Evolution.BookQuery q = Evolution.BookQuery.FieldTest(	ContactField.ImJabber, 
																		BookQueryTest.Is, 
																		user.Uri );
				return PersonManager.Instance.FindPerson(q);
			}
			else
				throw new ApplicationException("Can't do that");
		}

		
		/// <summary>
		/// Gets the Person object for a given Id
		/// </summary>	
		public static Person GetPerson(string contactId)
		{
			Person person = null;
			if(PersonManager.Instance.personIters.ContainsKey(contactId)) {
				Gtk.TreeIter iter = PersonManager.Instance.personIters[contactId];
				person = (Person) PersonManager.Instance.personTreeStore.GetValue(iter, 0);
			}
			return person;
		}

		
		/// <summary>
		/// Add a Person to the Store
		/// </summary>	
		public static bool AddPerson(Person person)
		{
			if(PersonManager.Instance.systemBook.AddContact(person.EDSContact)) {
				// if they added, then add the person to our tables to find them
				Gtk.TreeIter iter = PersonManager.Instance.personTreeStore.AppendValues(person);
				PersonManager.Instance.personIters[person.Id] = iter;
				return true;
			}
			return false;
		}


		/// <summary>
		/// Store the changes to a Person to the Store
		/// </summary>	
		public static bool CommitPerson(Person person)
		{
			return PersonManager.Instance.systemBook.CommitContact(person.EDSContact);
		}
		
		
		/// <summary>
		/// Remove the Person from the store by Id
		/// </summary>	
		public static bool RemovePerson(string Id)
		{
			return PersonManager.Instance.systemBook.RemoveContact(Id);
		}				
		

		/// <summary>
		/// Gets the PersonGroup object for a given Id
		/// </summary>	
		public static PersonGroup GetGroup(string groupId)
		{
			PersonGroup group = null;
			if(PersonManager.Instance.groupIters.ContainsKey(groupId)) {
				Gtk.TreeIter iter = PersonManager.Instance.groupIters[groupId];
				group = (PersonGroup) PersonManager.Instance.groupTreeStore.GetValue(iter, 0);
			}
			return group;
		}
		
		
		/// <summary>
		/// Add a PersonGroup to the Store
		/// </summary>	
		public static bool AddGroup(PersonGroup group)
		{
			if(PersonManager.Instance.systemBook.AddContact(group.EDSContact)) {
				// if they added, then add the person to our tables to find them
				Gtk.TreeIter iter = PersonManager.Instance.groupTreeStore.AppendValues(group);
				PersonManager.Instance.groupIters[group.Id] = iter;
				return true;
			}
			return false;
		}


		/// <summary>
		/// Store the changes to a Group to the Store
		/// </summary>	
		public static bool CommitGroup(PersonGroup group)
		{
			return PersonManager.Instance.systemBook.CommitContact(group.EDSContact);
		}		


		/// <summary>
		/// Remove the group from the store by Id
		/// </summary>	
		public static bool RemoveGroup(string Id)
		{
			return PersonManager.Instance.systemBook.RemoveContact(Id);
		}		
		#endregion
		
		
		#region Public Methods	
		/// <summary>
		/// Initializes the Store
		/// </summary>	
		public void Init()
		{
			// This does nothing but will create the static class to call it
		}
		#endregion
	}
}


