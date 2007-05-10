//***********************************************************************
// *  $RCSfile$ - Person.cs
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
using System.IO;
using System.Net;
using System.Text;
using Evolution;

namespace Banter
{
	///<summary>
	///	Person Class
	/// In memory representation of a person including data from online services
	/// EDS, and anything else that is needed to make this person functional
	///</summary>
	public class Person
	{
		#region Private Types
		//private Member member;
		private Tapioca.Contact tapiocaContact;
		private bool isSelf;
		private string photoFile = String.Empty;
		private Evolution.Contact edsContact;
		#endregion


		#region Properties
		/// <summary>
		/// Current Presence Status
		/// </summary>
		public Tapioca.ContactPresence TapiocaPresence
		{
			get
			{
				Logger.Debug("FIXME: Presence should be done using a presence engine");
				return Tapioca.ContactPresence.Available;
			}
			set
			{
				if (this.isSelf == false)
					throw new ApplicationException ("Invalid to set presence on friends");
			}
		}


		/// <summary>
		/// Online status message
		/// </summary>
		public string TapiocaPresenceMessage
		{
			get 
			{
				Logger.Debug("FIXME: Presence messages should be done using a presence engine");			
				return tapiocaContact.PresenceMessage;
			}
		}


		/// <summary>
		/// Online capabilities
		/// </summary>
		public Tapioca.ContactCapabilities TapiocaCapabilities
		{
			get
			{
				Logger.Debug("FIXME: Capabilities should be done using a presence engine");			
				return tapiocaContact.Capabilities;
			}
		}

		
		/// <summary>
		/// Online status message
		/// </summary>
		public string PresenceMessage
		{
			get
			{
				Logger.Debug("FIXME: Presence messages should be done using a presence engine");
				string message = String.Empty;
			
				/*
				if (member != null)
					message = member.PresenceMessage;
				else if (tapiocaContact != null)
					message = tapiocaContact.PresenceMessage;
				*/
				if (tapiocaContact != null)
					message = tapiocaContact.PresenceMessage;
				
				return message;
			}
		}

		
		/// <summary>
		/// Person's name to be displayed
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

				if (tapiocaContact != null) {
					if ((tapiocaContact.Alias != null) && (tapiocaContact.Alias.Length > 0))
						displayName = tapiocaContact.Alias;
					else
						displayName = tapiocaContact.Uri;
				}
				
				return displayName;
			}
		}


		/// <summary>
		/// Sets or gets the person's Jabber ID
		/// </summary>
		public string JabberId
		{
			get 
			{		
				string jabberId = String.Empty;
				
				if( (edsContact.ImJabber != null) && (edsContact.ImJabber.Length > 0) )
					jabberId = edsContact.ImJabber[0];
					
				return jabberId;
			}
			set
			{
				string[] jabberId = new string[1];
				jabberId[0] = value;
				edsContact.ImJabber = jabberId;
			}
		}

		
		/// <summary>
		/// Path to person's photo
		/// </summary>
		public string PhotoFile
		{
			get {return photoFile;}
		}

		
		/// <summary>
		/// Indicates self awareness
		/// </summary>
		public bool IsSelf
		{
			get{ return isSelf;}
			set
			{
				isSelf = value;
			}
		}

		
		/// <summary>
		/// The internal Member class
		/// </summary>
		public Tapioca.Contact Contact
		{
			get{ return tapiocaContact; }
			set{ tapiocaContact = value; }
		}

		
		/// <summary>
		/// The internal Evolution contact
		/// </summary>
		public Evolution.Contact EDSContact
		{
			get{ return edsContact; }
			set{ edsContact = value; }
		}

		
		/// <summary>
		/// The Id of this Person
		/// </summary>
		public string Id
		{
			get{ return edsContact.Id;}
		}		
		#endregion

		
		#region Constructors
		/// <summary>
		/// Constructs a person from a tapioca contact
		/// </summary>
		internal Person(Evolution.Contact edsContact)
		{
			this.isSelf = false;
			this.edsContact = edsContact;
		}

		
		/// <summary>
		/// Constructs a ME person from an Evolution contact
		/// </summary>
		internal Person(Evolution.Contact edsContact, bool self)
		{
			this.isSelf = false;
			this.edsContact = edsContact;
			this.isSelf = self;
		}

		
		/// <summary>
		/// Constructs a person with a displayName
		/// </summary>
		public Person(string displayName)
		{
			this.isSelf = false;
			this.edsContact = new Contact();
			edsContact.FileAs = displayName;
		}
		#endregion
	}	
}