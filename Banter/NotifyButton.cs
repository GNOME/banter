//***********************************************************************
// *  $RCSfile$ - NotifyButton.cs
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
using Gdk;
using Cairo;

namespace Banter
{
	public class NotifyButton : Gtk.Button
	{
		private int notifyCount;
		private Gdk.Pixbuf originalPixbuf;
		private Gdk.Pixbuf newPixbuf;
		private bool imageNeedsUpdating;

		///<summary>
		///	The pixbuf used on the button
		///</summary>
		public int NotifyCount
		{
			get { return notifyCount; }
			set { 
				notifyCount = value;
				imageNeedsUpdating = true;
				UpdatePixbuf();
			}
		}


		public new Widget Image
		{
			get { return base.Image; }
			set {
				base.Image = value;
				originalPixbuf = ( (Gtk.Image)base.Image).Pixbuf;
				imageNeedsUpdating = true;
				UpdatePixbuf();
			}
		}


		public NotifyButton() : base()
		{
			notifyCount = 0;
			imageNeedsUpdating = false;
			this.ExposeEvent += ExposeHandler;
		}

		private void ExposeHandler(object source, ExposeEventArgs args)
		{
			if(imageNeedsUpdating)
				UpdatePixbuf();
		}


		private void UpdatePixbuf()
		{
			if(base.Image == null)
				return;

			if(notifyCount > 0) {
				if(base.Image.ParentWindow == null)
					return;

				Gdk.Window win = Image.ParentWindow;

				Pixmap image;

				image = new Gdk.Pixmap(win, 18, 18);	
				
				if (image == null)
					return;

				int wide, high;
				
				image.GetSize(out wide, out high);

				Cairo.Context cc = CairoHelper.CreateCairoDrawable(image);
				//Cairo.Context cc = new Cairo.Context(surface);

				cc.Save();
//				cc.PaintWithAlpha(1.0);
				cc.Rectangle(0, 0, wide, high);
				cc.SetSourceRGBA(1.0, 1.0, 1.0, 1.0);
				//cc.FillPreserve();
				cc.Fill();
				cc.Restore();
				cc.Stroke();

				cc.Save();
//				cc.LineWidth = 0.01;
				cc.Arc( ((double)wide)/2.0, ((double)high)/2.0, (((double)wide) / 2.0), 0.0, 2.0 * 3.14);
				if(notifyCount < 10)  // yellow
					cc.SetSourceRGBA(0.99, 0.91, 0.31, 1.0);
				else if(notifyCount < 25) // orange
					cc.SetSourceRGBA(0.99, 0.69, 0.24, 1.0);
				else // red
					cc.SetSourceRGBA(0.94, 0.16, 0.16, 1.0);
//				cc.FillPreserve();
				cc.Fill();
				cc.Restore();
				cc.Stroke();

				string badgeString = String.Format("{0}", notifyCount);
				cc.Save();
				cc.SetSourceRGBA(0.0, 0.0, 0.0, 1.0);
				cc.FontFace("sans", FontSlant.Normal, FontWeight.Bold);
				cc.FontSize = 12;
				Cairo.TextExtents extents = cc.TextExtents(badgeString);
				double xpos, ypos;
				xpos = ((double)wide)/2.0 - extents.Width/2.0;
				ypos = (((double)high)/2.0) + (extents.Height/2.0) - 0.5;
				cc.MoveTo(xpos, ypos);
				cc.ShowText(badgeString);
				cc.Restore();
				cc.Stroke();

				Pixbuf dp = Gdk.Pixbuf.FromDrawable(image, image.Colormap, 0, 0, 0, 0, 18, 18);
		
				// Create the composite image
				Colorspace colorspace = originalPixbuf.Colorspace;
				bool hasAlpha           = originalPixbuf.HasAlpha;
				int bitsPerSample     = originalPixbuf.BitsPerSample;
				newPixbuf		      = new Pixbuf (colorspace,
													true,
													bitsPerSample,
													originalPixbuf.Width,
													originalPixbuf.Height);

				originalPixbuf.CopyArea(0, 0, originalPixbuf.Width, originalPixbuf.Height, newPixbuf, 0, 0);
				dp.CopyArea(0, 0, dp.Width, dp.Height, newPixbuf, originalPixbuf.Width - dp.Width, 0);

				( (Gtk.Image)base.Image).Pixbuf = newPixbuf;
			} else {
				if( ((Gtk.Image)base.Image).Pixbuf != originalPixbuf) {
					((Gtk.Image)base.Image).Pixbuf = originalPixbuf;
				}
			}
			imageNeedsUpdating = false;
		}
	}
}
