//***********************************************************************
// *  $RCSfile$ - AvatarManager.cs
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
//using Evolution;
using GLib;
using System.Collections;
using System.Collections.Generic;


namespace Banter
{
	///<summary>
	///	AvatarManager Class
	/// AvatarManager is a singleton that manages all cached avatars on the system
	///</summary>
	public class AvatarManager
	{

		#region Private Static Types
		private static Banter.AvatarManager manager = null;
		private static System.Object locker = new System.Object();
		#endregion

	
		#region Private Types
		private List<Gdk.Pixbuf> avatars;
		#endregion


		#region Public Static Properties
		/// <summary>
		/// The list of avatars in the system
		/// </summary>
		public static Gdk.Pixbuf[] Avatars
		{
			get
			{
				return AvatarManager.Instance.avatars.ToArray();
			}
		}

		/// <summary>
		/// Obtain the singleton for AvatarManager
		/// </summary>		
		public static AvatarManager Instance
		{
			get
			{
				lock(locker) {
					if(manager == null) {
						lock(locker) {
							manager = new AvatarManager();
						}
					}
					return manager;
				}
			}
		}
		#endregion


		#region Constructors
		/// <summary>
		/// A private constructor used when obtaining the Singleton by using the static property Instance.
		/// </summary>			
		private AvatarManager ()
		{
			avatars = new List<Gdk.Pixbuf> ();
		}
		#endregion	

		
		#region Private Methods
		#endregion
		

		#region Public Static Methods
		/// <summary>
		/// Adds the pixbuf to the recent avatars
		/// </summary>	
		public static void AddAvatar(Gdk.Pixbuf avatar)
		{
			AvatarManager manager = AvatarManager.Instance;
			
			// The Manager only holds 16 avatars
			if(manager.avatars.Count == 16) {
				manager.avatars.RemoveAt(15);
			}
			manager.avatars.Insert(0, avatar);
		}
		
		/// <summary>
		/// Retrieves the avatar at index and promotes it to the top
		/// </summary>	
		public static Gdk.Pixbuf PromoteAvatar(uint index)
		{
			AvatarManager manager = AvatarManager.Instance;
		
			if(index > manager.avatars.Count)
				return null;
			
			Gdk.Pixbuf avatar = manager.avatars[(int)index];
			manager.avatars.RemoveAt((int)index);
			manager.avatars.Insert(0, avatar);
			return avatar;
		}

		/// <summary>
		/// Clears the avatars from the manager
		/// </summary>	
		public static void Clear()
		{
			AvatarManager manager = AvatarManager.Instance;
		
			manager.avatars.Clear();
		}		
		
		#endregion
		
		
		#region Public Methods	
		#endregion
	}
}


