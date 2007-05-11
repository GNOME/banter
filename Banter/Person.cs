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
		private string cachePath;
		private Gdk.Pixbuf avatar;
		private ArrayList providerUsers;
		private Presence presence;
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
		/// Current Presence Status
		/// </summary>
		public Presence Presence
		{
			get
			{
				return null;
			}
		}


		/// <summary>
		/// Online status message
		/// </summary>
		public string PresenceMessage
		{
			get 
			{			
				return string.Empty;
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
		/// Returns the stored avatar
		/// </summary>
		public Gdk.Pixbuf Photo
		{
			get {return avatar;}
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


		/// <summary>
		/// The ProviderUsers for this Person
		/// </summary>
		public ProviderUser[] ProviderUsers
		{
			get{ return (ProviderUser[]) providerUsers.ToArray(typeof(ProviderUser)); }
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
			Init();
		}

		
		/// <summary>
		/// Constructs a ME person from an Evolution contact
		/// </summary>
		internal Person(Evolution.Contact edsContact, bool self)
		{
			this.isSelf = false;
			this.edsContact = edsContact;
			this.isSelf = self;			
			Init();
		}

		
		/// <summary>
		/// Constructs a person with a displayName
		/// </summary>
		public Person(string displayName)
		{
			this.isSelf = false;
			this.edsContact = new Contact();
			edsContact.FileAs = displayName;
			Init();
		}
		#endregion
		

		#region Private Methods		
		private void Init()
		{
		
			// init internal types
			providerUsers = new ArrayList();
			presence = new Presence(PresenceType.Offline);

			UpdateProviderUsers();

			// first check to see if this is a real edsContact
			if(edsContact.Id != null) {
				string homeDirectoryPath = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
				cachePath = System.IO.Path.Combine (homeDirectoryPath, ".banter/Cache");
				cachePath = System.IO.Path.Combine (cachePath, this.Id);

				try
				{
					Logger.Debug("FIXME: A Person's Cache needs to be cleaned up somewhere too");
					if(!System.IO.Directory.Exists(cachePath)) {
						System.IO.Directory.CreateDirectory(cachePath);
					}
				}
				catch(Exception e)
				{
					Logger.Debug(e.Message);
				}
				
				// Read the contact's Avatar from EDS if it's there
				ContactPhoto photo = edsContact.Photo;
				if(photo.PhotoType == ContactPhotoType.Inlined) {  // the photo is stored in EDS
					avatar = new Gdk.Pixbuf(photo.Data);
				}
				else if(photo.PhotoType == ContactPhotoType.Uri) {				
					// not sure how to handle a Uri based photo
					Logger.Debug("FIXME: handle Uri based photo");
				}
			}
		}

		
		/// <summary>
		/// Ensures that we have ProviderUsers for eds Provider values
		/// </summary>
		private void UpdateProviderUsers()
		{
			Logger.Debug("FIXME: Person.UpdateProviderUsers should use a policy for the order");
			providerUsers.Clear();
		
			// Jabber values
			foreach(string uri in edsContact.ImJabber) {
				string key = ProviderUserManager.CreateKey(uri, ProtocolName.Jabber);
				ProviderUser providerUser = ProviderUserManager.GetProviderUser(key);
				if(providerUser == null) {
					providerUser = ProviderUserManager.CreateProviderUser(uri, ProtocolName.Jabber);
				}
				
				if(providerUser != null) {
					providerUsers.Add(providerUser);
				}
			}
			UpdatePresence();
		}
		

		/// <summary>
		/// Updates the persons presence to be the most present of his ProviderUsers
		/// </summary>
		private void UpdatePresence()
		{
			Logger.Debug("FIXME: Person.UpdatePresence should use a policy to get the right presence");
			if(providerUsers.Count > 0)
				presence = ((ProviderUser)providerUsers[0]).Presence;
		}		
		#endregion
		
		
		#region Public Methods
		/// <summary>
		/// Return the path to the photo size requested
		/// </summary>
		public string GetScaledAvatar(int size)
		{
			if(avatar == null)
				return null;
				
			string	avatarPath = null;
			bool	scalePhoto = false;
			
			switch(size) {
				case 16:
					avatarPath = Path.Combine(cachePath, "16x16");
					scalePhoto = true;
					break;
				case 24:
					avatarPath = Path.Combine(cachePath, "24x24");
					scalePhoto = true;
					break;
				case 36:
					avatarPath = Path.Combine(cachePath, "36x36");
					scalePhoto = true;
					break;
				case 48:
					avatarPath = Path.Combine(cachePath, "48x48");
					scalePhoto = true;
					break;
				default:
					avatarPath = Path.Combine(cachePath, "actual");
					break;
			}

			try
			{
				if(!System.IO.File.Exists(avatarPath)) {
					byte[] data;
					
					System.IO.FileStream fStream = new FileStream(avatarPath, FileMode.Create);
					if(scalePhoto) {
						Gdk.Pixbuf scaled = avatar.ScaleSimple(size, size, Gdk.InterpType.Bilinear);
						data = scaled.SaveToBuffer("png");
					}
					else {
						data = avatar.SaveToBuffer("png");
					}
					
					fStream.Write(data, 0, data.Length);
					fStream.Close();
				}
			}
			catch(Exception e)
			{
				Logger.Debug(e.Message);
			}

			return avatarPath;
		}
		#endregion
		
	}	
}