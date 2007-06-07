//***********************************************************************
// *  $RCSfile$ - PersonCard.cs
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
using Gecko;
using Gdk;
using Gtk;

namespace Banter
{
	///<summary>
	///	PersonCardSize enum
	/// PersonCards can render various sizes of the widget.  This enumerates them.
	///</summary>
	public enum PersonCardSize : uint
	{
		Small = 1,
		Medium,
		Large
	}
	
	///<summary>
	///	PersonCard
	/// A Gui Widget that renders a Person.
	///</summary>	
	public class PersonCard : Bin
	{
		#region Private Types	
		private Widget child;
		private WebControl webControl;
		private bool updateNeeded;
		private bool widgetRendered;
		private string widgetHtml;
		private Person person;
		private PersonCardSize cardSize;
		#endregion


		#region Constructors
		///<summary>
		///	Constructs a PersonCard from a Person object
		///</summary>			
		public PersonCard(Person person)
		{
			this.person = person;
			this.cardSize = PersonCardSize.Small;
			webControl = new WebControl();
			webControl.Realized += WebControlRealizedHandler;
			webControl.OpenUri += WebControlOpenUriHandler;
			this.Add(webControl);
			updateNeeded = true;			
			widgetRendered = false;

			person.PresenceUpdated += OnPersonPresenceUpdated;
			person.AvatarUpdated += OnPersonAvatarUpdated;
		}
		#endregion


		#region Public Properties
		///<summary>
		///	The Person object the PersonCard is rendering
		///</summary>			
		public Person Person
		{
			get { return person; }
			set {
				Logger.Debug ("FIXME: Implement PersonCard.Person [set] - Do we need to do anything here?");
				// person = value;
			}
		}
		
		
		///<summary>
		///	The size of the card to be rendered
		///</summary>			
		public PersonCardSize Size
		{
			get { return this.cardSize; }
			set { this.cardSize = value; }
		}
		
		#endregion


		#region Protected Methods
		///<summary>
		///	Overrides the OnDestroyed method for a Gtk.Widget
		///</summary>
		protected override void OnDestroyed ()
		{
			person.PresenceUpdated -= OnPersonPresenceUpdated;
			person.AvatarUpdated -= OnPersonAvatarUpdated;			
	
			Logger.Debug("FIXME: the base OnDestroy for the PersonCard is not being called");
			// this is not being called because gtkmozembed blows the next time you try to use
			// it if you destroy it
//			base.OnDestroyed ();
		}


		///<summary>
		///	Overrides the OnAdded method for a Gtk.Widget
		///</summary>		
		protected override void OnAdded (Widget widget)
		{
			base.OnAdded(widget);
			child = widget;
		}


		///<summary>
		///	Overrides the SizeRequested method for a Gtk.Widget
		///</summary>			
		protected override void OnSizeRequested (ref Requisition requisition)
		{
//			Console.WriteLine("OnSizeRequested being called");
			base.OnSizeRequested(ref requisition);
			switch(cardSize) {
				default:
				case PersonCardSize.Small:
					requisition.Height = 26;
					requisition.Width = 128;
					break;
				case PersonCardSize.Medium:
					requisition.Height = 50;
					requisition.Width = 75;
					break;
				case PersonCardSize.Large:
					requisition.Height = 300;
					requisition.Width = 75;
					break;					
			}
		}		


		///<summary>
		///	Overrides the SizeAllocated method for a Gtk.Widget
		///</summary>
		protected override void OnSizeAllocated (Rectangle allocation)
		{
			if(child != null)
			{
				child.SizeAllocate(allocation);
			}
			
			RenderWidget();
		}
		#endregion
		

		#region Private Methods
		///<summary>
		///	Handles the Realized event on the web control
		///</summary>			
		private void WebControlRealizedHandler(object obj, EventArgs args)
		{
			widgetRendered = true;
		}


		///<summary>
		///	Handles the OpenUri event on the web control
		///</summary>			
		private void WebControlOpenUriHandler(object o, OpenUriArgs args)
		{
			if(args.AURI.StartsWith("rtc://TEXT_CHAT")) {
				ChatWindowManager.InitiateChat(person, ChatType.Text);
			}
			else if(args.AURI.StartsWith("rtc://VIDEO_CHAT")) {
				ChatWindowManager.InitiateChat(person, ChatType.Video);
			}

			// set return to true so the web control doesn't attempt to handle the URI
			args.RetVal = true;
		}


		///<summary>
		///	Handles Presence Events on a Person
		///</summary>
		private void OnPersonPresenceUpdated (Person person)
		{
			// Logger.Debug("Updating presence on {0}", person.DisplayName);
			updateNeeded = true;
			RenderWidget();
		}
		

		///<summary>
		///	Handles Avatar Events on a Person
		///</summary>
		private void OnPersonAvatarUpdated (Person person)
		{
			// Logger.Debug("Updating presence on {0}", person.DisplayName);
			updateNeeded = true;
			RenderWidget();
		}


		///<summary>
		///	Handles Avatar Events on a Person
		///</summary>
		private void RenderWidget()
		{		
			if(widgetRendered && updateNeeded) {
				if(ThemeManager.ContactStyle != null) {
					string widgetHtml = ThemeManager.ContactStyle.RenderWidgetHtml(person, cardSize);
					webControl.RenderData(widgetHtml, "file://" + ThemeManager.ContactStyle.Path, "text/html");
					updateNeeded = false;
				}
				else {
					Logger.Debug("PersonCard: ThemeManager.ContactStyle is null, unable to render widget");
				}
			}
		}
		#endregion

	}
}




