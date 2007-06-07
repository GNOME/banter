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

	public delegate void PersonPresenceUpdatedHandler (Person person);
	public delegate void PersonAvatarUpdatedHandler (Person person);

	///<summary>
	///	Person Class
	/// In memory representation of a person including data from online services
	/// EDS, and anything else that is needed to make this person functional
	///</summary>
	public class Person
	{
		#region Private Types
		private string photoFile = String.Empty;
		private Evolution.Contact edsContact;
		private string cachePath;
		private Gdk.Pixbuf avatar;
		private List<ProviderUser> providerUsers;
		private Presence presence;
		#endregion

		#region Public Events
		public event PersonPresenceUpdatedHandler PresenceUpdated;
		public event PersonAvatarUpdatedHandler AvatarUpdated;
		#endregion


		#region Properties
		/// <summary>
		/// Current Presence Status
		/// </summary>
		public Presence Presence
		{
			get { return presence; }
		}


		/// <summary>
		/// Online status message
		/// </summary>
		public string PresenceMessage
		{
			get { return presence.Message; }
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
			get
			{
				if(avatar == null) {
					// if the new presence is not offline, request the avatar data
					// FIXME: we should not just flat out request this every time
					if(providerUsers.Count > 0) {
						providerUsers[0].RequestAvatarData();
					}
				}
			
				return avatar;
			}
		}

		
		/// <summary>
		/// Indicates self awareness
		/// </summary>
		public bool IsMe
		{
			get
			{
				foreach(ProviderUser user in providerUsers) {
					if(user.IsMe)
						return true;
				}
				return false;
			}
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
			get{ return providerUsers.ToArray(); }
		}
		

		/// <summary>
		/// The Default (by policy) ProviderUser for this Person
		/// </summary>
		public ProviderUser ProviderUser
		{
			get
			{ 
				if(providerUsers.Count > 0) {
					return providerUsers[0];
				} else {
					return null;
				}
			}
		}
		
		#endregion

		
		#region Constructors
		/// <summary>
		/// Constructs a person from a tapioca contact
		/// </summary>
		internal Person(Evolution.Contact edsContact)
		{
			this.edsContact = edsContact;
			Init();
		}

		
		/// <summary>
		/// Constructs a person with a displayName
		/// </summary>
		public Person(string displayName)
		{
			this.edsContact = new Contact();
			edsContact.FileAs = displayName;
			Init();
		}
		#endregion
		

		#region Private Methods		
		private void Init()
		{
		
			// init internal types
			providerUsers = new List<ProviderUser> ();
			presence = new Presence(PresenceType.Offline);

			// first check to see if this is a real edsContact
			if(edsContact.Id != null) {
				string homeDirectoryPath = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
				cachePath = System.IO.Path.Combine (homeDirectoryPath, ".banter/Cache");
				cachePath = System.IO.Path.Combine (cachePath, this.Id);

				try
				{
					//Logger.Debug("FIXME: A Person's Cache needs to be cleaned up somewhere too");
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
		/// Updates the persons presence to be the most present of his ProviderUsers
		/// </summary>
		private void UpdatePresence()
		{
			// Logger.Debug("FIXME: Person.UpdatePresence should use a policy to get the right presence");
			if(providerUsers.Count > 0) {
				presence = providerUsers[0].Presence;
			}

			// Call the event on the GUI thread				
			if(PresenceUpdated != null)
			{
				Gtk.Application.Invoke (delegate {
					PresenceUpdated(this);
				});			
			}
		}
		
		
		private void ProviderUserPresenceUpdated (ProviderUser user)
		{
			//Logger.Debug("Presence updated for ProviderUser: {0}", user.Alias);			
			UpdatePresence();
		}
		
		
		/// <summary>
		/// Handles notification from a ProviderUser that the Avatar Token has been updated
		/// AvatarTokenUpdated handler
		/// AvatarReceived
		/// </summary>
		private void ProviderUserAvatarTokenUpdated (ProviderUser user, string newToken)
		{
			// FIXME: we need to test to see if we really need to request the data
			// FIXME: Save off the newToken for later
			user.RequestAvatarData();
		}


		/// <summary>
		/// Handles notification from a ProviderUser that the Avatar has been received
		/// AvatarTokenUpdated handler
		/// AvatarReceived
		/// </summary>
		private void ProviderUserAvatarReceived (ProviderUser user, string token, string mimeType, byte[] avatarData)
		{
			// FIXME: This needs to determine if the AvatarUpdated needs to be called depending
			// on the avatar stored in EDS etc.
			avatar = new Gdk.Pixbuf(avatarData);

			// Call the event on the GUI thread
			if(AvatarUpdated != null)
			{
				Gtk.Application.Invoke (delegate {
					AvatarUpdated(this);
				});			
			}
		}
        
		#endregion
		
		
		#region Public Methods
		/// <summary>
		/// Return the path to the photo size requested
		/// </summary>
		public string GetScaledAvatar(int size)
		{
			if(this.Photo == null)
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


		/// <summary>
		/// Ensures that we have ProviderUsers for eds Provider values
		/// </summary>
		public void UpdateProviderUsers()
		{
			//Logger.Debug("FIXME: Person.UpdateProviderUsers should use a policy for the order");
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
					providerUser.PresenceUpdated += ProviderUserPresenceUpdated;
					providerUser.AvatarTokenUpdated += this.ProviderUserAvatarTokenUpdated;
					providerUser.AvatarReceived += this.ProviderUserAvatarReceived;
				}
			}
			UpdatePresence();
		}
		
		/// <summary>
		/// Sets the status for the person if the person IsMe
		/// </summary>
		public void SetStatus(Presence presence)
		{
			if(!this.IsMe)
				return;
			
			if(providerUsers.Count > 0) {
				foreach(ProviderUser user in providerUsers) {
					user.SetStatus(presence);
				}
			}
		}
		

		/// <summary>
		/// Sets the avatar for the person if the person IsMe
		/// </summary>
		public void SetAvatar(Gdk.Pixbuf newAvatar)
		{
			if(!this.IsMe)
				return;
			
			if(providerUsers.Count > 0) {
				byte[] data;
				
				data = newAvatar.SaveToBuffer("png");

				foreach(ProviderUser user in providerUsers) {
					user.SetAvatar("png", data);
				}
			}
		}

		#endregion
		
	}	
}