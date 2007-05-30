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
using Gtk;

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

			VBox vbox = new VBox(true, 2);
			Widget child = Child;
			Remove (child);

			HBox box = new HBox (true, 2);
			Gdk.Pixbuf pixbuf = Utilities.GetIcon("blank-photo-128", 32);
			Image image = new Image(pixbuf.ScaleSimple(24,24,Gdk.InterpType.Bilinear));		
			box.PackStart (image, true, true, 0);
			image = new Image(pixbuf.ScaleSimple(24,24,Gdk.InterpType.Bilinear));
			box.PackStart (image, true, true, 0);
			image = new Image(pixbuf.ScaleSimple(24,24,Gdk.InterpType.Bilinear));
			box.PackStart (image, true, true, 0);
			image = new Image(pixbuf.ScaleSimple(24,24,Gdk.InterpType.Bilinear));
			box.PackStart (image, true, true, 0);
			image = new Image(pixbuf.ScaleSimple(24,24,Gdk.InterpType.Bilinear));
			box.PackStart (image, true, true, 0);

			vbox.PackStart(box, true, true, 0);

			box = new HBox (true, 2);
			image = new Image(pixbuf.ScaleSimple(24,24,Gdk.InterpType.Bilinear));
			box.PackStart (image, true, true, 0);
			image = new Image(pixbuf.ScaleSimple(24,24,Gdk.InterpType.Bilinear));
			box.PackStart (image, true, true, 0);
			image = new Image(pixbuf.ScaleSimple(24,24,Gdk.InterpType.Bilinear));
			box.PackStart (image, true, true, 0);
			image = new Image(pixbuf.ScaleSimple(24,24,Gdk.InterpType.Bilinear));
			box.PackStart (image, true, true, 0);
			image = new Image(pixbuf.ScaleSimple(24,24,Gdk.InterpType.Bilinear));
			box.PackStart (image, true, true, 0);
			
			vbox.PackStart(box, true, true, 0);

			box = new HBox (true, 2);
			image = new Image(pixbuf.ScaleSimple(24,24,Gdk.InterpType.Bilinear));
			box.PackStart (image, true, true, 0);
			image = new Image(pixbuf.ScaleSimple(24,24,Gdk.InterpType.Bilinear));
			box.PackStart (image, true, true, 0);
			image = new Image(pixbuf.ScaleSimple(24,24,Gdk.InterpType.Bilinear));
			box.PackStart (image, true, true, 0);
			image = new Image(pixbuf.ScaleSimple(24,24,Gdk.InterpType.Bilinear));
			box.PackStart (image, true, true, 0);
			image = new Image(pixbuf.ScaleSimple(24,24,Gdk.InterpType.Bilinear));
			box.PackStart (image, true, true, 0);
			
			vbox.PackStart(box, true, true, 0);

			
			Add (vbox);
			box.Show();
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
			Logger.Debug("Mouse location {0}x{1}", ev.X, ev.Y);
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
		
		[GLib.ConnectBefore]
		protected override bool OnEnterNotifyEvent (Gdk.EventCrossing ev)
		{
			Logger.Debug("Entered the MenuItem");
			return false; //base.OnLeaveNotifyEvent(ev);
		}
		
		[GLib.ConnectBefore]
		protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing ev)
		{
			Logger.Debug("Left the MenuItem");
/*
			if (!pinned && pin_img != null) {
				pin_img.Pixbuf = pinup;
			}
*/
			return false; //base.OnLeaveNotifyEvent (ev);			
		}
	}
}
