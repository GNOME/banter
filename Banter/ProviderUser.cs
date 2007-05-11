//***********************************************************************
// *  $RCSfile$ - ProviderUser.cs
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
	///	ProviderUser Class
	/// ProviderUser represents buddies from providers in telepathy.
	///</summary>
	public class ProviderUser
	{
		#region Private Types
		private string uri;
		private string alias;
		private string protocol;
		private Presence presence;
		private string accountName;
		private bool isMe;
		#endregion		


		#region Public Properties
		/// <summary>
		/// The Uri of the ProviderUser from telepathy
		/// </summary>		
		public string Uri
		{
			get { return uri; }
			set { this.uri = value; }
		}


		/// <summary>
		/// The Alias of the ProviderUser from telepathy
		/// </summary>		
		public string Alias
		{
			get { return alias; }
			set { this.alias = value; }
		}


		/// <summary>
		/// The Protocol of the ProviderUser from telepathy
		/// </summary>		
		public string Protocol
		{
			get { return protocol; }
			set { this.protocol = value; }
		}


		/// <summary>
		/// The Presence of the ProviderUser from telepathy
		/// </summary>		
		public Presence Presence
		{
			get { return presence; }
		}		


		/// <summary>
		/// The Account Name of the ProviderUser from telepathy
		/// </summary>		
		public string AccountName
		{
			get { return accountName; }
			set { this.accountName = value; }
		}		
		

		/// <summary>
		/// true if this represents the ProviderUser logged in
		/// </summary>		
		public bool IsMe
		{
			get { return isMe; }
			set { this.isMe = value; }
		}	
		#endregion	
		
		
		#region Constructors
		/// <summary>
		/// Constructs a ProviderUser
		/// </summary>	
		public ProviderUser()
		{
			this.presence = new Presence(PresenceType.Offline);
			this.uri = String.Empty;
			this.accountName = String.Empty;
			this.alias = String.Empty;
			this.isMe = false;
			this.protocol = String.Empty;
		}
		#endregion
		
	}
}
