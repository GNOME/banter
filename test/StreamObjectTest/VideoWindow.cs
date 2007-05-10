using System;
using System.Runtime.InteropServices;
using Gtk;
using Gdk;

namespace Novell.Rtc
{
	public class VideoWindow : Gtk.Window
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
			get {
				return gdk_x11_drawable_get_xid (preview.GdkWindow.Handle);
			}
		}
		
		public uint WindowId
		{
			get {
				return gdk_x11_drawable_get_xid (this.GdkWindow.Handle);
			}
		}	
		
		public VideoWindow()
			: base ("Video")
		{
			preview_pos = PreviewPos.ButtonLeft;
			
			this.WidthRequest = 500;
			this.HeightRequest = 375;
			
			preview = new Gtk.DrawingArea ();
			preview.WidthRequest = 150;
			preview.HeightRequest = 112;
			preview.ModifyBg (Gtk.StateType.Normal, new Gdk.Color (0,0,0));
			preview.ModifyBg (Gtk.StateType.Active, new Gdk.Color (0,0,0));
			
			fix = new Gtk.Fixed ();
			fix.Put (preview, space, space);
			this.Add (fix);			
			
			this.SizeRequested += OnsizeRequested;
			this.QueueResize ();
			MovePreview ();
		}
		
		private bool MovePreview ()
		{	
			int w, h;
			
			this.GetSize (out w, out h);			
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
		
		protected override bool OnKeyReleaseEvent (Gdk.EventKey args)
		{
			if (args.Key == Gdk.Key.F) {				
				this.Fullscreen ();
			}
			return true;
		}


		[DllImport ("gdk-x11-2.0")]
		private static extern uint gdk_x11_drawable_get_xid (System.IntPtr window);		
	}
}
