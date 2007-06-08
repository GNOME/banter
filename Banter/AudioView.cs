//***********************************************************************
// *  $RCSfile$ - AudioView.cs
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
	public delegate void EndAudioChatHandler();

	public class AudioView : Gtk.EventBox
	{
		public event EndAudioChatHandler EndAudioChat;	

		public AudioView()
		{
//			this.WidthRequest = 500; //250;
//			this.HeightRequest = 375; //187;
			
			Frame frame = new Frame();
			frame.BorderWidth = 5;
			frame.Show();
			
			VBox vbox = new VBox(false, 0);
			frame.Add(vbox);
			vbox.Show();
			
			Label label = new Label(Catalog.GetString("Audio Chat in progress..."));

			label.Show();
			vbox.PackStart(label, false, true, 5);

			Button button = new Button(Catalog.GetString("End Call"));
			button.Clicked += OnCloseAudioClicked;
			button.Show();
			vbox.PackStart(button, false, true, 5);
			
			this.Add(frame);
		}
		
		private void OnCloseAudioClicked (object sender, EventArgs args)
		{
			Logger.Debug("Close Audio was clicked");
			if(EndAudioChat != null)
				EndAudioChat();
		}
/*
#region Overrides
		protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
		{
			Logger.Debug("User clicked somewhere on video");
			return false;
		}
#endregion
*/
	}
}
