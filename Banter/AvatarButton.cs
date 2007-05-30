//***********************************************************************
// *  $RCSfile$ - AvatarButton.cs
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
	public class AvatarButton : Button
	{
	    private string avatarPath;
	    
	    public AvatarButton(string path) : base()
	    {
	    	Gdk.Pixbuf pixbuf;
	    	avatarPath = path;
	    	
	    	if(avatarPath != null) {
	    		 pixbuf = new Gdk.Pixbuf(avatarPath);
	    	} else {
				pixbuf = Utilities.GetIcon("blank-photo-128", 32);
	    	}
	        Image image = new Image(pixbuf.ScaleSimple(24,24,Gdk.InterpType.Bilinear));
	        Image = image;
	    }
	    
/*	    protected override void OnStateChanged(StateType previous_state)
	    {
	        if(State == StateType.Active && previous_state == StateType.Normal) {
	            if(Selected != null) {
	                Selected(this);
	            }
	        } else if(State == StateType.Normal && previous_state == StateType.Active) {
	            if(Unselected != null) {
	                Unselected(this);
	            }
	        }
	        
	        base.OnStateChanged(previous_state);
	    }
*/	    
		public string Path
		{
			get { return avatarPath; }
			set { this.avatarPath = value; }
		}
	}
}