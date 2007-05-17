//***********************************************************************
// *  $RCSfile$ - PersonGroup.cs
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
using Evolution;

namespace Banter
{
	///<summary>
	///	PersonGroup Class
	/// In memory representation of a group including data from online services
	/// EDS, and anything else that is needed to make this group functional
	///</summary>	
	public class PersonGroup
	{
		#region Private Types
		private Evolution.Contact edsContact;
		private Gtk.TreeStore personTreeStore;
		private System.Object locker;
		#endregion	

		#region Properties
		/// <summary>
		/// PersonGroups's name to be displayed
		/// </summary>
		public string DisplayName
		{
			get
			{
				string displayName = String.Empty;

				if (edsContact != null) {
					if ((edsContact.FileAs != null) && (edsContact.FileAs.Length > 0) ) {
						displayName = edsContact.FileAs;
						return displayName;
					}
				} 

				return displayName;
			}
		}

		/// <summary>
		/// Model of people in this group
		/// </summary>
		public Gtk.TreeModel People
		{
			get{ return personTreeStore;}
		}	
		
		/// <summary>
		/// The internal Evolution contact
		/// </summary>
		public Evolution.Contact EDSContact
		{
			get{ return edsContact;}
			set
			{
				edsContact = value;
				UpdateModel();
			}
		}
		
		/// <summary>
		/// The Id of this PersonGroup
		/// </summary>
		public string Id
		{
			get{ return edsContact.Id;}
		}			
		#endregion
		

		#region Constructors
		/// <summary>
		/// Constructor to create a new group
		/// call PersonStore.AddGroup() to add this group to the store
		/// </summary>		
		public PersonGroup(string groupName)
		{
			this.edsContact = new Contact();
			locker = new System.Object();
			personTreeStore = new Gtk.TreeStore(typeof(Person));
			edsContact.FileAs = groupName;
			edsContact.List = true;
		}
		
		/// <summary>
		/// Internal constructor to create a new group based on an edsContact
		/// </summary>			
		internal PersonGroup(Evolution.Contact edsContact)
		{
			this.edsContact = edsContact;
			locker = new System.Object();
			personTreeStore = new Gtk.TreeStore(typeof(Person));
			UpdateModel();
		}
		#endregion
		
		
		#region Interal Methods
		/// <summary>
		/// Created to handle property changes on an eds Contact, but it's not working
		/// </summary>			
		internal void PropertyChangedNotifyHandler (object o, GLib.NotifyArgs args)
		{
			Logger.Debug("A Property has changed on Group: {0}", this.DisplayName);
		}

		/// <summary>
		/// Reads the EMAIL attribute on the current EDS Contact and constructs the model
		/// of persons that are members of this group
		/// </summary>			
		internal void UpdateModel()
		{
			Logger.Debug ("FIXME: PersonGroup.UpdateModel() is brutal and needs work done!");		
			// FIXME: This is brutal to remove everyone and start over
			// change this to only remove and add people that aren't there any longer
			personTreeStore.Clear();

			if(edsContact == null)
				return;
				
			GLib.List attributeList = edsContact.GetAttributes(ContactField.Email);
			GLib.List attrList = new GLib.List (attributeList.Handle, typeof (VCardAttribute));
			foreach(VCardAttribute attr in attrList) {
				GLib.List paramList = new GLib.List (attr.Params.Handle, typeof (VCardAttributeParam));
				foreach(Evolution.VCardAttributeParam param in paramList)
				{
					if(param.Name.CompareTo("X-EVOLUTION-DEST-CONTACT-UID") == 0) {
						GLib.List valueList = new GLib.List (param.Values.Handle, typeof (string));					

						foreach(String valStr in valueList) {
							//Logger.Debug("  Value: {0}", valStr);

							Person person = PersonStore.GetPerson(valStr);
							if(person != null) {
								Gtk.TreeIter iter = personTreeStore.AppendValues(person);
								//Logger.Debug("  Contact: {0}", person.DisplayName);
							}										
						}
					}
				}
			}
		}
		

		/// <summary>
		/// Adds a person to a group and stores it in EDS
		/// </summary>			
		public bool IsPersonInGroup(Person person)
		{
			Gtk.TreeIter iter;
			
			if(personTreeStore.GetIterFirst(out iter)) {
				do {
					Person iterPerson = (Person) personTreeStore.GetValue(iter, 0);
					if(person.Id.CompareTo(iterPerson.Id) == 0)
						return true;
				} while(personTreeStore.IterNext(ref iter));
			}
			return false;
		}
		#endregion


		#region Public Methods
		/// <summary>
		/// Adds a person to a group.  You must call PersonStore CommitGroup with this group to save it to EDS
		/// </summary>			
		public void AddPerson(Person person)
		{
			if( (person.Id == null) || (person.Id.Length == 0) )
				throw new ApplicationException("Invalid Person object.  Person must be added to the PersonStore before adding them to a group");
				
			if(IsPersonInGroup(person))
				return;
			
			VCardAttribute attr = new VCardAttribute("", "EMAIL");
			VCardAttributeParam param = new VCardAttributeParam("X-EVOLUTION-DEST-CONTACT-UID");
			param.AddValue(person.Id);
			attr.AddParam(param);
			param = new VCardAttributeParam("X-EVOLUTION-DEST-NAME");
			param.AddValue(person.DisplayName);
			attr.AddParam(param);
//			param = new VCardAttributeParam("X-EVOLUTION-DEST-EMAIL");
//			param.AddValue(person..DisplayName);
//			attr.AddParam(param);
			param = new VCardAttributeParam("X-EVOLUTION-DEST-HTML-MAIL");
			param.AddValue("FALSE");
			attr.AddParam(param);

			attr.AddValue(person.DisplayName);

			edsContact.AddAttribute(attr);
		}
		

		/// <summary>
		/// Removes a person from a group.  You must call PersonStore CommitGroup with this group to save it to EDS
		/// </summary>			
		public void RemovePerson(Person person)
		{
			if( (person.Id == null) || (person.Id.Length == 0) )
				throw new ApplicationException("Invalid Person object.  Person must be added to the PersonStore before adding them to a group");

			if(edsContact == null)
				return;
				
			GLib.List attributeList = edsContact.GetAttributes(ContactField.Email);
			GLib.List attrList = new GLib.List (attributeList.Handle, typeof (VCardAttribute));
			foreach(VCardAttribute attr in attrList) {
				GLib.List paramList = new GLib.List (attr.Params.Handle, typeof (VCardAttributeParam));
				foreach(Evolution.VCardAttributeParam param in paramList)
				{
				
					if(param.Name.CompareTo("X-EVOLUTION-DEST-CONTACT-UID") == 0) {
						GLib.List valueList = new GLib.List (param.Values.Handle, typeof (string));					

						foreach(String valStr in valueList) {
							//Logger.Debug("  Value: {0}", valStr);
							if(person.Id.CompareTo(valStr) == 0) {
								edsContact.RemoveAttribute(attr);
								return;
							}
						}
					}
				}
			}
		}		
		

		#endregion
	}
}
