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
	/// Widget used to avatars in a menu
	/// </summary>
	public class AvatarMenuItem : ComplexMenuItem
	{

    	public delegate void AvatarSelectedHandler(Gdk.Pixbuf pixbuf);
    	
    	public event AvatarSelectedHandler AvatarSelected;    	
	
	    private Label description_label;
	    
	    public AvatarMenuItem() : base(true)
	    {
	    	Gdk.Pixbuf[] avatars = AvatarManager.Avatars;
	    	
	    	uint cols = 4;
	    	uint rows = 5;
	    
	        Table table = new Table(rows, cols, false);
	        
			if(avatars.Length > 0) {
				// Create a label and add it to the table
		        Label label = new Label("Recent Avatars:");
		        label.Xalign = 0.0f;
		        Widget label_host = RegisterWidget(label);
		        table.Attach(label_host, 0, 6, 0, 1, AttachOptions.Fill | AttachOptions.Expand, AttachOptions.Shrink, 0, 0);
				uint curIndex = 0;
				
		        for(uint row = 1; row < rows; row++) {
		        	for(uint col = 0; col < cols; col++) {				
			            if( curIndex < avatars.Length ) {
							AvatarButton button = new AvatarButton(avatars[curIndex], curIndex);
							button.Clicked += OnAvatarButtonClicked;
				            Widget button_host = RegisterWidget(button);
				            table.Attach(button_host, col, col + 1, row, row + 1, AttachOptions.Shrink, AttachOptions.Shrink, 0, 0);
			            }
			            curIndex++;
		        	}
		        }
		    } else {
				// Create a label and add it to the table
		        Label label = new Label("No Recent Avatars");
		        label.Sensitive = false;
		        label.Xalign = 0.0f;
		        Widget label_host = RegisterWidget(label);
		        table.Attach(label_host, 0, 6, 0, 1, AttachOptions.Fill | AttachOptions.Expand, AttachOptions.Shrink, 0, 0);
		    }
	        
	        HBox shrinker = new HBox();
	        shrinker.PackStart(table, false, false, 0);
	        
	        shrinker.ShowAll();
	        Add(shrinker);
	    }
	    
	    private void OnAvatarButtonClicked(object o, EventArgs args)
	    {
	    	AvatarButton button = (AvatarButton) o;
	    	
	    	if(AvatarSelected != null) {
		    	Gdk.Pixbuf avatar = AvatarManager.PromoteAvatar(button.Index);
		    	if(avatar != null) {
					AvatarSelected(avatar);
		    	}
		    }
	    }
	}


}
