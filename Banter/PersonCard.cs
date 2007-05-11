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
		private int widgetHeight;
		private int webControlHeight;
		private bool widgetRendered;
		private string widgetHtml;
		private Person person;
		private string listStylesPath;
		#endregion


		#region Constructors
		///<summary>
		///	Constructs a PersonCard from a Person object
		///</summary>			
		public PersonCard(Person person)
		{
			this.person = person;
			webControl = new WebControl();
			webControl.Realized += WebControlRealizedHandler;
			webControl.OpenUri += WebControlOpenUriHandler;
			this.Add(webControl);
			widgetHeight = 0;
			widgetRendered = false;

		    string homeDirectoryPath = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
		    listStylesPath = System.IO.Path.Combine (homeDirectoryPath, ".banter/Themes/ListStyles/Current");
		    
		    if (person.Contact != null)
		    	person.Contact.PresenceUpdated += OnTapiocaPresenceUpdated;
		    /*
		    else
		    	person.Member.PresenceUpdated += OnPresenceUpdated;
		    */

			ReadSmallWidgetHtml();
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
		#endregion


		#region Protected Methods
		///<summary>
		///	Overrides the OnDestroyed method for a Gtk.Widget
		///</summary>
		protected override void OnDestroyed ()
		{
			Logger.Debug ("PersonCard.OnDestroyed");
			try {
				person.Contact.PresenceUpdated -= OnTapiocaPresenceUpdated;
			} catch {}
	
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
			requisition.Height = 26;
			requisition.Width = 128;
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
			SizeHeaderIfNeeded(allocation);
		}


		///<summary>
		///	Sizes the header of widget if the size is large enough
		///</summary>
		protected void SizeHeaderIfNeeded(Rectangle allocation)
		{
			if((widgetRendered) && (widgetHeight != allocation.Height))
			{
//				Console.WriteLine("Size is: " + allocation.Width + "x" + allocation.Height);
				if(allocation.Height < 90)
				{
					if(webControlHeight != 16)
					{
						webControlHeight = 16;
						ReadSmallWidgetHtml();
						webControl.RenderData(widgetHtml, "file://" + listStylesPath, "text/html");
					}
				}
				else
				{
					if(webControlHeight != 90)
					{
						ReadWidgetHtml();
						webControlHeight = 90;
						webControl.RenderData(widgetHtml, "file://" + listStylesPath, "text/html");
					}
				}
			}
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
				Application.Instance.InitiateChat(person);
			}
			else if(args.AURI.StartsWith("rtc://VIDEO_CHAT")) {
				Application.Instance.InitiateVideoChat(person);
			}

			// set return to true so the web control doesn't attempt to handle the URI
			args.RetVal = true;
		}


		///<summary>
		///	Builds the Html for a normal sized Widget
		///</summary>	
		private void ReadWidgetHtml()
		{
			string readHtml;
			string tmpHtml;
			string actionHtml;
			System.IO.StreamReader htmlReader;
			
			htmlReader = new System.IO.StreamReader(System.IO.Path.Combine(listStylesPath, "person.html"));
			readHtml = htmlReader.ReadToEnd();

			string avatar = person.GetScaledAvatar(48);
			
			if(avatar != null)
				tmpHtml = readHtml.Replace("%PERSON_PHOTO%", "file://" + avatar);
			else
				tmpHtml = readHtml.Replace("%PERSON_PHOTO%", "file://" + System.IO.Path.Combine(listStylesPath, "blankhead.png"));
			
			tmpHtml = tmpHtml.Replace("%PERSON_DISPLAY_NAME%", person.DisplayName);
			tmpHtml = tmpHtml.Replace("%PERSON_STATUS_TEXT%", person.PresenceMessage);
			widgetHtml = tmpHtml;

			htmlReader = new System.IO.StreamReader(System.IO.Path.Combine(listStylesPath, "action.html"));
			readHtml = htmlReader.ReadToEnd();
			tmpHtml = readHtml.Replace("%ACTION_HREF%", "rtc://TEXT_CHAT");
			tmpHtml = tmpHtml.Replace("%ACTION_IMAGE%", "file://" + System.IO.Path.Combine(listStylesPath, "text-chat.png"));
			actionHtml = tmpHtml;
			
			htmlReader = new System.IO.StreamReader(System.IO.Path.Combine(listStylesPath, "action.html"));
			readHtml = htmlReader.ReadToEnd();
			tmpHtml = readHtml.Replace("%ACTION_HREF%", "rtc://VIDEO_CHAT");
			tmpHtml = tmpHtml.Replace("%ACTION_IMAGE%", "file://" + System.IO.Path.Combine(listStylesPath, "text-chat.png"));
			actionHtml += tmpHtml;

			widgetHtml = widgetHtml.Replace("<!--ACTION_DATA-->", actionHtml);
		}


		///<summary>
		///	Builds the Html for a small sized Widget
		///</summary>
		private void ReadSmallWidgetHtml()
		{
			string readHtml;
			string tmpHtml;
			string actionHtml;
			System.IO.StreamReader htmlReader;
			
			htmlReader = new System.IO.StreamReader(System.IO.Path.Combine(listStylesPath, "person-small.html"));
			readHtml = htmlReader.ReadToEnd();
			
			string avatar = person.GetScaledAvatar(16);
			
			if(avatar != null) {
				tmpHtml = readHtml.Replace("%PERSON_PHOTO%", "file://" + avatar);
			}
			else {
				tmpHtml = readHtml.Replace("%PERSON_PHOTO%", "file://" + System.IO.Path.Combine(listStylesPath, "blankhead.png"));
			}
			
			tmpHtml = tmpHtml.Replace("%PERSON_DISPLAY_NAME%",  person.DisplayName);

			// NOTE! Tapiopca version
			if (person.Contact != null) {
				if(person.Contact.Presence == Tapioca.ContactPresence.Offline) {
					tmpHtml = tmpHtml.Replace("%PERSON_STATUS_TEXT%", "offline");
				}
				else if (person.PresenceMessage.Length > 0) {
					tmpHtml = tmpHtml.Replace("%PERSON_STATUS_TEXT%", person.PresenceMessage);
				}
				else if(person.Contact.Presence == Tapioca.ContactPresence.Busy) {
					tmpHtml = tmpHtml.Replace("%PERSON_STATUS_TEXT%", "busy");
				}
				else if(person.Contact.Presence == Tapioca.ContactPresence.Away) {
					tmpHtml = tmpHtml.Replace("%PERSON_STATUS_TEXT%", "away");
				}
				else {
					tmpHtml = tmpHtml.Replace("%PERSON_STATUS_TEXT%", "online");
				}
			}
			else
			{
				tmpHtml = tmpHtml.Replace("%PERSON_STATUS_TEXT%", "");
			}
			/*
			else
			{
				if(person.Member.Presence == MemberPresence.Offline)
				{
					tmpHtml = tmpHtml.Replace("%PERSON_STATUS_TEXT%", "offline");
				}
				else if(person.PresenceMessage.Length > 0)
				{
					tmpHtml = tmpHtml.Replace("%PERSON_STATUS_TEXT%", person.PresenceMessage);
				}
				else if(person.Member.Presence == MemberPresence.Busy)
				{
					tmpHtml = tmpHtml.Replace("%PERSON_STATUS_TEXT%", "busy");
				}
				else if(person.Member.Presence == MemberPresence.Away)
				{
					tmpHtml = tmpHtml.Replace("%PERSON_STATUS_TEXT%", "away");
				}
				else
				{
					tmpHtml = tmpHtml.Replace("%PERSON_STATUS_TEXT%", "online");
				}
			}
			*/
			
			widgetHtml = tmpHtml;

			// change this later to show their capabilities when we actually have them
			if (person.Contact != null) {
				if(person.Contact.Presence != Tapioca.ContactPresence.Offline) {
					htmlReader = new System.IO.StreamReader(System.IO.Path.Combine(listStylesPath, "action-small.html"));
					readHtml = htmlReader.ReadToEnd();
					tmpHtml = readHtml.Replace("%ACTION_HREF%", "rtc://TEXT_CHAT");
					tmpHtml = tmpHtml.Replace("%ACTION_IMAGE%", "file://" + System.IO.Path.Combine(listStylesPath, "text-chat-small.png"));
					actionHtml = tmpHtml;
					
					htmlReader = new System.IO.StreamReader(System.IO.Path.Combine(listStylesPath, "action.html"));
					readHtml = htmlReader.ReadToEnd();
					tmpHtml = readHtml.Replace("%ACTION_HREF%", "rtc://VIDEO_CHAT");
					tmpHtml = tmpHtml.Replace("%ACTION_IMAGE%", "file://" + System.IO.Path.Combine(listStylesPath, "video-chat-small.png"));
					actionHtml += tmpHtml;
	
					widgetHtml = widgetHtml.Replace("<!--ACTION_DATA-->", actionHtml);
				}
			}
			/*
			else
			{	
				// FIXME::Lame but ok for now
				if(person.Member.Presence != MemberPresence.Offline)
				{
					htmlReader = new System.IO.StreamReader(System.IO.Path.Combine(listStylesPath, "action-small.html"));
					readHtml = htmlReader.ReadToEnd();
					tmpHtml = readHtml.Replace("%ACTION_HREF%", "rtc://TEXT_CHAT");
					tmpHtml = tmpHtml.Replace("%ACTION_IMAGE%", "file://" + System.IO.Path.Combine(listStylesPath, "text-chat-small.png"));
					actionHtml = tmpHtml;
					
					htmlReader = new System.IO.StreamReader(System.IO.Path.Combine(listStylesPath, "action.html"));
					readHtml = htmlReader.ReadToEnd();
					tmpHtml = readHtml.Replace("%ACTION_HREF%", "rtc://VIDEO_CHAT");
					tmpHtml = tmpHtml.Replace("%ACTION_IMAGE%", "file://" + System.IO.Path.Combine(listStylesPath, "video-chat-small.png"));
					actionHtml += tmpHtml;
	
					widgetHtml = widgetHtml.Replace("<!--ACTION_DATA-->", actionHtml);
				}
			}
			*/
		}
		
		/*
		private void OnPresenceUpdated (Member sender, MemberPresence presence)
		{
			Logger.Debug("Updating presence on {0}", person.DisplayName);
			ReadSmallWidgetHtml();
			webControl.RenderData(widgetHtml, "file://" + listStylesPath, "text/html");
		}
		*/


		///<summary>
		///	Handles Presence Events in Tapioca
		///</summary>
		private void OnTapiocaPresenceUpdated (Tapioca.ContactBase sender, Tapioca.ContactPresence presence)
		{
			Logger.Debug("Updating presence on {0}", person.DisplayName);
			ReadSmallWidgetHtml();
			webControl.RenderData(widgetHtml, "file://" + listStylesPath, "text/html");
		}
		#endregion

	}
}




