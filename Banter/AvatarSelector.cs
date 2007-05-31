//***********************************************************************
// *  $RCSfile$ - AvatarSelector.cs
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
using System.Collections.Generic;
using Gtk;
using Mono.Unix;

namespace Banter
{
//	public delegate void AvatarChangedHandler (AvatarSelector selector);


	/// <summary>
	/// Widget used to show and change a person's avatar
	/// </summary>
	public class AvatarSelector : Gtk.Button
	{
		private Image avatarImage;
		private Gdk.Pixbuf pixbuf;
		
		public AvatarSelector()
		{
			pixbuf = Utilities.GetIcon("blank-photo-128", 32);
			avatarImage = new Image (pixbuf);
			avatarImage.Show ();
			
			this.BorderWidth = 0;
			this.Relief = Gtk.ReliefStyle.None;
			this.Add(avatarImage);
		}
		
#region Overrides
		protected override void OnClicked ()
		{
			Menu popupMenu = new Menu ();
			Menu recentMenu = new Menu ();
			MenuItem item;
			ImageMenuItem imageItem;
			
			AvatarMenuItem amItem = new AvatarMenuItem();
			amItem.AvatarSelected += OnAvatarSelected;
			popupMenu.Add(amItem);
			
			popupMenu.Add (new SeparatorMenuItem ());

			item = new MenuItem("Edit Picture...");
			item.Activated += OnEditPicture;
			popupMenu.Add (item);
			
			item = new MenuItem("Clear Recent Pictures");
			item.Activated += OnClearRecentPictures;
			popupMenu.Add (item);
			
			popupMenu.ShowAll();
			popupMenu.Popup ();
		}
#endregion

#region Event Handlers
		private void OnAvatarSelected(Gdk.Pixbuf avatar)
		{
			this.Pixbuf = avatar;
			Logger.Debug("FIXME: This should set the avatar but telepathy is broken!");
			// The following lines eventually call down into telepathy connection to set an avatar
			// and it crashes the app with an error that Avatars method doesn't exist
			//if(PersonManager.Me != null) {
			//	PersonManager.Me.SetAvatar(avatar);
			//}			
		}
		private void OnEditPicture (object sender, EventArgs args)
		{
			FileSelection fs = new FileSelection(Catalog.GetString("Select an Avatar"));
			int fsreturn = fs.Run();
			fs.Hide();
			
			if(fsreturn == -5) {
				Gdk.Pixbuf avatar;
				try {
					avatar = new Gdk.Pixbuf(fs.Filename);
					this.Pixbuf = avatar;
					AvatarManager.AddAvatar(avatar);
				} catch(Exception ex) {
					Logger.Debug("Exception loading image from file: {0}", fs.Filename);
					Logger.Debug(ex.Message);
					return;
				}
				
				Logger.Debug("FIXME: This should set the avatar but telepathy is broken!");
				// The following lines eventually call down into telepathy connection to set an avatar
				// and it crashes the app with an error that Avatars method doesn't exist
				//if(PersonManager.Me != null) {
				//	PersonManager.Me.SetAvatar(avatar);
				//}
			}
		}

		private void OnClearRecentPictures (object sender, EventArgs args)
		{
			AvatarManager.Clear();
		}
#endregion

#region Public Properties
		public Gdk.Pixbuf Pixbuf
		{
			get { return pixbuf; }
			set {
				if (value == null)
					return;
				
				this.pixbuf = value;
				avatarImage.Pixbuf = pixbuf.ScaleSimple(48,48,Gdk.InterpType.Bilinear);
			}
		}
#endregion
	}
}