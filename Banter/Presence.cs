//***********************************************************************
// *  $RCSfile$ - Presence.cs
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
using Mono.Unix;

namespace Banter
{

	/// <summary>
	/// PresenceType Enum
	/// enumeration of the types of presence that are possible
	/// </summary>	
	public enum PresenceType : uint
	{
		Offline = 1,
		Available = 2,
		Away = 3,
		XA = 4,
		Hidden = 5,
		Busy = 6
	}	
	
		
	///<summary>
	///	Presence Class
	/// Represents presence for a ProviderUser and Person
	///</summary>	
	public class Presence
	{
		#region Private Types
		private PresenceType presenceType;
		private string message;
		private int time;
		#endregion	
	

		#region Public Properties
		/// <summary>
		/// The type of this Presence
		/// </summary>		
		public PresenceType Type
		{
			get { return presenceType; }
			set
			{ 
				this.presenceType = value;
				this.message = GetStatusString(value);
			}
		}


		/// <summary>
		/// The type of this Presence
		/// </summary>		
		public string Message
		{
			get { return message; }
			set { this.message = value; }
		}
		

		/// <summary>
		/// The name of this Presence
		/// </summary>	
		public string Name
		{
			get
			{
	            switch (presenceType)
	            {
	                case PresenceType.Offline:
	                    return "offline";
	                case PresenceType.Available:
	                    return "available";
	                case PresenceType.Away:
	                    return "away";
	                case PresenceType.XA:
	                    return "xa";
	                case PresenceType.Hidden:
	                    return "hidden";
	                case PresenceType.Busy:
	                    return "dnd";
	            }
	            return "";
			}
        }

		
		#endregion


		#region Constructors
		/// <summary>
		/// Constructs a Presence Object
		/// </summary>	
		public Presence(PresenceType type)
		{
			this.presenceType = type;
			this.message = GetStatusString(type);
			this.time = 0;
		}
		
		/// <summary>
		/// Constructs a Presence Object with a specific message
		/// </summary>
		public Presence(PresenceType type, string message)
		{
			this.presenceType = type;
			this.message = message;
			this.time = 0;
		}
		#endregion
		
		#region Public Static Methods
		/// <summary>
		/// Gets a default translated string for status
		/// </summary>
		static public string GetStatusString(PresenceType type)
		{
			switch (type)
			{
				case PresenceType.Offline:
					return Catalog.GetString ("Offline");
				case PresenceType.Available:
					return Catalog.GetString ("Available");	           
				case PresenceType.Away:
					return Catalog.GetString ("Away");	           
				case PresenceType.XA:
					return Catalog.GetString ("XA");	           
				case PresenceType.Hidden:
					return Catalog.GetString ("Hidden");	           
				case PresenceType.Busy:
					return Catalog.GetString ("Busy");	           
			}
			return String.Empty;
		}
		#endregion

	}
}
