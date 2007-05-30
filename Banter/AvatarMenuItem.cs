//***********************************************************************
// *  $RCSfile$ - AvatarMenuItem.cs
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

namespace Banter
{

	/// <summary>
	/// MenuItem used to display avatars in a menu for selecting them
	/// </summary>
	public class AvatarMenuItem : Gtk.ImageMenuItem
	{
//		Note note;
//		Gtk.Image pin_img;
//		bool pinned;
//		bool inhibit_activate;

//		static Gdk.Pixbuf note_icon;
//		static Gdk.Pixbuf pinup;
//		static Gdk.Pixbuf pinup_active;
//		static Gdk.Pixbuf pindown;

//		static AvatarMenuItem ()
//		{
			// Cache this since we use it a lot.
//			note_icon = GuiUtils.GetIcon ("tomboy-note", 16);
//			pinup = GuiUtils.GetIcon ("pinup", 16);
//			pinup_active = GuiUtils.GetIcon ("pinup-active", 16);
//			pindown = GuiUtils.GetIcon ("pindown", 16);
//		}

		public AvatarMenuItem () : base ("")
		{
//			this.note = note;
//			Image = new Gtk.Image (note_icon);

/*
			if (show_pin) {
				Gtk.HBox box = new Gtk.HBox (false, 0);
				Gtk.Widget child = Child;
				Remove (child);
				box.PackStart (child, true, true, 0);
				Add (box);
				box.Show();

				pinned = note.IsPinned;
				pin_img = new Gtk.Image(pinned ? pindown : pinup);
				pin_img.Show();
				box.PackStart (pin_img, false, false, 0);
			}
*/
		}

/*		static string FormatForLabel (string name)
		{
			// Replace underscores ("_") with double-underscores ("__")
			// so Note menuitems are not created with mnemonics.
			return name.Replace ("_", "__");
		}

		static string GetDisplayName (Note note)
		{
			string display_name = note.Title;
			if (note.IsNew)
				display_name += Catalog.GetString (" (new)");
			return FormatForLabel (display_name);
		}
*/
		protected override void OnActivated () 
		{
/*			if (!inhibit_activate) {
				if (note != null)
					note.Window.Present ();
			}
*/
		}

		protected override bool OnButtonPressEvent (Gdk.EventButton ev)
		{
/*			if (pin_img != null &&
			    ev.X >= pin_img.Allocation.X && 
			    ev.X < pin_img.Allocation.X + pin_img.Allocation.Width) {
				pinned = note.IsPinned = !pinned;
				pin_img.Pixbuf = pinned ? pindown : pinup;
				inhibit_activate = true;
				return true;
			}
*/
			return base.OnButtonPressEvent (ev);
		}

		protected override bool OnButtonReleaseEvent (Gdk.EventButton ev)
		{
/*			if (inhibit_activate) {
				inhibit_activate = false;
				return true;
			}
*/
			return base.OnButtonReleaseEvent (ev);
		}

		protected override bool OnMotionNotifyEvent (Gdk.EventMotion ev)
		{
/*
			if (!pinned && pin_img != null) {
				if (ev.X >= pin_img.Allocation.X && 
				    ev.X < pin_img.Allocation.X + pin_img.Allocation.Width) {
					if (pin_img.Pixbuf != pinup_active)
						pin_img.Pixbuf = pinup_active;
				} else if (pin_img.Pixbuf != pinup) {
					pin_img.Pixbuf = pinup;
				}
			}
*/
			return base.OnMotionNotifyEvent (ev);
		}

		protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing ev)
		{
/*
			if (!pinned && pin_img != null) {
				pin_img.Pixbuf = pinup;
			}
*/
			return base.OnLeaveNotifyEvent (ev);			
		}
	}
}
