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
//using Evolution;

namespace Banter
{

	public delegate void PersonNotifyUpdated (Person person);
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
		//private Evolution.Contact edsContact;
		private string cachePath;
		private Gdk.Pixbuf avatar;
		private List<ProviderUser> providerUsers;
		private Presence presence;
		private string displayName;
		private int textNotifyCount;
		private int audioNotifyCount;
		private int videoNotifyCount;
		#endregion

		#region Public Events
		public event PersonNotifyUpdated NotifyUpdated;
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
		/// Updates the number of Text Notifies for this person
		/// </summary>
		public int TextNotifyCount
		{
			get { return textNotifyCount; }
			set {
				textNotifyCount = value;
				if(NotifyUpdated != null) {
					NotifyUpdated(this);
				}
				Logger.Debug("Text Notify Count for {0} is {1}", this.DisplayName, this.textNotifyCount);
			}
		}


		/// <summary>
		/// Updates the number of Audio Notifies for this person
		/// </summary>
		public int AudioNotifyCount
		{
			get { return audioNotifyCount; }
			set {
				audioNotifyCount = value;
				if(NotifyUpdated != null) {
					NotifyUpdated(this);
				}
				Logger.Debug("Audio Notify Count for {0} is {1}", this.DisplayName, this.audioNotifyCount);
			}
		}


		/// <summary>
		/// Updates the number of Text Notifies for this person
		/// </summary>
		public int VideoNotifyCount
		{
			get { return videoNotifyCount; }
			set {
				videoNotifyCount = value;
				if(NotifyUpdated != null) {
					NotifyUpdated(this);
				}
				Logger.Debug("Video Notify Count for {0} is {1}", this.DisplayName, this.videoNotifyCount);
			}
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
			get { return displayName; }
		}


		/// <summary>
		/// Sets or gets the person's Jabber ID
		/// </summary>
		public string JabberId
		{
			get 
			{		
				string jabberId = String.Empty;


				if(providerUsers.Count > 0) {
					jabberId = providerUsers[0].Uri;
				}
				
				/*
				if( (edsContact.ImJabber != null) && (edsContact.ImJabber.Length > 0) )
					jabberId = edsContact.ImJabber[0];
				*/	
				return jabberId;
			}
/*			set
			{
				string[] jabberId = new string[1];
				jabberId[0] = value;
				edsContact.ImJabber = jabberId;
			}
*/
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
		/*
		public Evolution.Contact EDSContact
		{
			get{ return edsContact; }
			set{ edsContact = value; }
		}
		*/

		/// <summary>
		/// The Id of this Person
		/// </summary>
		public string Id
		{
			get{ return JabberId;}
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
		/*internal Person(Evolution.Contact edsContact)
		{
			this.edsContact = edsContact;
			Init();
		}
		*/
		
		/// <summary>
		/// Constructs a person with a displayName
		/// </summary>
		public Person(ProviderUser user)
		{
			providerUsers = new List<ProviderUser> ();

			textNotifyCount = 0;
			audioNotifyCount = 0;
			videoNotifyCount = 0;

			if(user != null) {
				providerUsers.Add(user);
				if(user.Presence != null)
					presence = user.Presence;
				user.PresenceUpdated += ProviderUserPresenceUpdated;
				user.AvatarTokenUpdated += this.ProviderUserAvatarTokenUpdated;
				user.AvatarReceived += this.ProviderUserAvatarReceived;
				if(user.Alias.Length > 0)
					displayName = user.Alias;
				else
					displayName = user.Uri;
			} 

			if(presence == null)
				presence = new Presence(PresenceType.Offline);
		}
		#endregion
		

		#region Private Methods		
		/// <summary>
		/// Updates the persons presence to be the most present of his ProviderUsers
		/// </summary>
		private void UpdatePresence()
		{
			//Logger.Debug("FIXME: Person.UpdatePresence should use a policy to get the right presence");
			if(providerUsers.Count > 0) {
				presence = providerUsers[0].Presence;

				// check the display name while we are here
				if( (providerUsers[0].Alias != null) && (providerUsers[0].Alias.Length > 0) ) {
					displayName = providerUsers[0].Alias;
 				}

				// Call the event on the GUI thread		
				if(PresenceUpdated != null)
				{
					Gtk.Application.Invoke (delegate {
						PresenceUpdated(this);
					});			
				}
			}

		}

		private void ProviderUserPresenceUpdated (ProviderUser user)
		{
			Logger.Debug("Person:ProviderUserPresenceUpdated for ProviderUser: {0}", user.Alias);			
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
		/// Sets the status for the person if the person IsMe
		/// </summary>
		public void ResetNotifications()
		{
			textNotifyCount = 0;
			audioNotifyCount = 0;
			videoNotifyCount = 0;
			if(NotifyUpdated != null) {
				NotifyUpdated(this);
			}
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