//***********************************************************************
// *  $RCSfile$ - VideoView.cs
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
using System.Runtime.InteropServices;
using Gtk;
using Gdk;

namespace Banter
{
	public class VideoView : Gtk.EventBox
	{
		public enum PreviewPos 
		{
			TopLeft,
			TopRight,
			ButtonLeft,
			ButtonRight
		}	
	
		Gtk.DrawingArea preview;
		Gtk.Fixed fix;
		bool moving= false;
		PreviewPos preview_pos;
		
		const int space = 5;
		
		public uint PreviewWindowId
		{
			get { return gdk_x11_drawable_get_xid (preview.GdkWindow.Handle); }
		}
		
		public uint WindowId
		{
			get { return gdk_x11_drawable_get_xid (this.GdkWindow.Handle); }
		}	
		
		public VideoView()
		{
			preview_pos = PreviewPos.ButtonRight;
			
			this.WidthRequest = 500; //250;
			this.HeightRequest = 375; //187;
			
			preview = new Gtk.DrawingArea ();
			preview.WidthRequest = 150;
			preview.HeightRequest = 112;
			preview.ModifyBg (Gtk.StateType.Normal, new Gdk.Color (0,0,0));
			preview.ModifyBg (Gtk.StateType.Active, new Gdk.Color (0,0,0));
			preview.Show();
			
			fix = new Gtk.Fixed ();
			fix.Put (preview, space, space);
			fix.Show();
			this.Add (fix);	
			
			this.SizeRequested += OnsizeRequested;
			this.QueueResize ();
			MovePreview ();
		}
		
		private bool MovePreview ()
		{	
			int w, h;
			
			this.GetSizeRequest(out w, out h);
			switch (preview_pos)
			{
				case PreviewPos.TopLeft:
					fix.Move (preview, space, space);
					break;
				case PreviewPos.TopRight:
					fix.Move (preview, w - preview.WidthRequest - space, space);
					break;
				case PreviewPos.ButtonLeft:
					fix.Move (preview, space, h - preview.HeightRequest - space);
					break;
				case PreviewPos.ButtonRight:
					fix.Move (preview, w - preview.WidthRequest - space, h - preview.HeightRequest - space);
					break;
				default:
					break;
			
			}

			preview.Show ();
			return false;
		}
		
		protected void OnsizeRequested(object o, SizeRequestedArgs args)
		{
			if (!moving) {
				GLib.Idle.Add (MovePreview);
				moving = true;
			}
			else
				moving = false;
		}
		
/*		protected override bool OnKeyReleaseEvent (Gdk.EventKey args)
		{
			if (args.Key == Gdk.Key.F) {				
				this.Fullscreen ();
			}
			return true;
		}
*/
#region Overrides
		protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
		{
			Logger.Debug("User clicked somewhere on video");
			return false;
		}
#endregion


		[DllImport ("gdk-x11-2.0")]
		private static extern uint gdk_x11_drawable_get_xid (System.IntPtr window);		
	}
}
