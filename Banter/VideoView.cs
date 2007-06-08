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
using Mono.Unix;

namespace Banter
{
	public delegate void EndVideoChatHandler();
	
	public class VideoView : Gtk.EventBox
	{
		public event EndVideoChatHandler EndVideoChat;
		
		public enum PreviewPos 
		{
			TopLeft,
			TopRight,
			ButtonLeft,
			ButtonRight
		}	
	
		Gtk.DrawingArea preview;
		Gtk.EventBox mainView;
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
			get { return gdk_x11_drawable_get_xid (mainView.GdkWindow.Handle); }
		}	
		
		public VideoView()
		{
			preview_pos = PreviewPos.ButtonRight;

			VBox vbox = new VBox(false, 0);
			vbox.Show();
			
			Frame frame = new Frame();
			//frame.BorderWidth = 5;
			frame.Show();
			
			vbox.Add(frame);			
			
			mainView = new Gtk.EventBox();
			mainView.WidthRequest = 400;
			mainView.HeightRequest = 300;
			mainView.ModifyBg (Gtk.StateType.Normal, new Gdk.Color (255,255,255));
			mainView.ModifyBg (Gtk.StateType.Active, new Gdk.Color (255,255,255));			
			mainView.Show();

			//this.WidthRequest = 333; // 500; //250;
			//this.HeightRequest = 250; // 375; //187; 250
			preview = new Gtk.DrawingArea ();
			preview.WidthRequest = 120; // 75; //150;
			preview.HeightRequest = 90; // 56; //112;
			preview.ModifyBg (Gtk.StateType.Normal, new Gdk.Color (0,0,0));
			preview.ModifyBg (Gtk.StateType.Active, new Gdk.Color (0,0,0));
			preview.Show();
			
			fix = new Gtk.Fixed ();
			fix.Put (preview, space, space);
			fix.Show();
			mainView.Add(fix);
			
			frame.Add(mainView);

//			Label label = new Label(Catalog.GetString("Video Chat in progress..."));

//			label.Show();
//			vbox.PackStart(label, false, true, 0);

			Button button = new Button(Catalog.GetString("End Call"));
			button.Clicked += OnCloseVideoClicked;
			button.Show();
			vbox.PackStart(button, false, false, 5);
			
			this.Add(vbox);
			mainView.SizeAllocated += OnSizeAllocated;
//			this.SizeRequested += OnsizeRequested;
			this.QueueResize ();
			MovePreview ();
		}
		
		private bool MovePreview ()
		{	
			int w, h;
			if(mainView.GdkWindow == null)
				return true;
				
			Logger.Debug("About to Move Preview");	
			mainView.GdkWindow.GetSize(out w, out h);
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

		
		protected void OnSizeAllocated (object o, SizeAllocatedArgs args)
		{
			Logger.Debug("Size was allocated");
			if (!moving) {
				Logger.Debug("Setting up to resize");
				GLib.Idle.Add (MovePreview);
				moving = true;
			}
			else
				moving = false;			
		}
		

		private void OnCloseVideoClicked (object sender, EventArgs args)
		{
			Logger.Debug("Close Video was clicked");
			if(EndVideoChat != null)
				EndVideoChat();
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
