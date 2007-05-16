//***********************************************************************
// *  $RCSfile$ - ContactStyle.cs
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
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;

namespace Banter
{
	// <summary>
	// Style (or Theme) used for the contacts in Banter
	// </summary>
	public class ContactStyle
	{
		#region Private Types
		private ContactStyleInfo styleInfo;
		private string stylePath;
		private string normalPersonHtml;
		private string normalActionHtml;
		private string smallPersonHtml;
		private string smallActionHtml;	
		#endregion


		#region Public Properties
		///<summary>
		///	Returns the name of the Style
		///</summary>			
		public string Name
		{
			get { return styleInfo.Name; }
		}
		
		///<summary>
		///	Returns the path of the Style
		///</summary>			
		public string Path
		{
			get { return stylePath; }
		}
		
		public ContactStyleInfo ContactStyleInfo
		{
			get { return styleInfo; }
		}
		#endregion


		#region Constructors
		///<summary>
		///	Constructs a ContactStyle from a path
		///</summary>			
		public ContactStyle(ContactStyleInfo info)
		{
			if (info == null)
				throw new ArgumentException ("Exception creating a ContactStyle because a null ContactStyleInfo was passed into the constructor.");
			this.styleInfo = info;

			string tmpPath = info.Path;
			
			if (tmpPath.StartsWith (System.IO.Path.PathSeparator.ToString ()) == false)
				tmpPath = System.IO.Path.Combine (ThemeManager.SystemThemesPath, tmpPath); 
			
			if (!Directory.Exists (tmpPath))
				throw new System.IO.FileNotFoundException (
						"The Contact Style directory does not exist",
						tmpPath);
			
			this.stylePath = tmpPath;

			ReadNormalHtml();
			ReadSmallHtml();		
		}
		#endregion


		#region Public Static Methods
		/// <summary>
		/// Determines if the path passed in is a valid Contact Style
		/// </summary>	
//		public static bool IsValid(string path)
//		{
//			try {
//				ContactStyle cs = new ContactStyle(path);
//			} catch (Exception e) {
//				return false;
//			}
//			return true;
//		}
		#endregion


		#region Public Methods
		///<summary>
		///	Renders the HTML for the widget
		///</summary>	
		public string RenderWidgetHtml(Person person, PersonCardSize size)
		{
			string widgetHtml = string.Empty;
			string textChatPng;
			string videoChatPng;
			string personHtml;
			string actionHtml;
			int avatarSize = 0;
			string tmpHtml;
			
			switch(size) {
				default:
				case PersonCardSize.Small:
					textChatPng = "text-chat-small.png";
					videoChatPng = "video-chat-small.png";
					personHtml = smallPersonHtml;
					actionHtml = smallActionHtml;
					avatarSize = 16;
					break;
				case PersonCardSize.Medium:
					textChatPng = "text-chat.png";
					videoChatPng = "video-chat.png";
					personHtml = normalPersonHtml;
					actionHtml = normalActionHtml;
					avatarSize = 48;
					break;
				// Haven't created any Large, just use Medium
				case PersonCardSize.Large:
					textChatPng = "text-chat.png";
					videoChatPng = "video-chat.png";
					personHtml = normalPersonHtml;
					actionHtml = normalActionHtml;
					avatarSize = 48;					
					break;
			}
			
			string avatar = person.GetScaledAvatar(avatarSize);

			if(avatar != null) {
				tmpHtml = personHtml.Replace("%PERSON_PHOTO%", "file://" + avatar);
			}
			else {
				tmpHtml = personHtml.Replace("%PERSON_PHOTO%", "file://" + System.IO.Path.Combine(stylePath, "blankhead.png"));
			}
			
			tmpHtml = tmpHtml.Replace("%PERSON_DISPLAY_NAME%",  person.DisplayName);

			if(person.Presence.Type == PresenceType.Offline) {
				tmpHtml = tmpHtml.Replace("%PERSON_STATUS_TEXT%", "offline");
			}
			else if (person.PresenceMessage.Length > 0) {
				tmpHtml = tmpHtml.Replace("%PERSON_STATUS_TEXT%", person.PresenceMessage);
			}
			else if(person.Presence.Type == PresenceType.Busy) {
				tmpHtml = tmpHtml.Replace("%PERSON_STATUS_TEXT%", "busy");
			}
			else if(person.Presence.Type == PresenceType.Away) {
				tmpHtml = tmpHtml.Replace("%PERSON_STATUS_TEXT%", "away");
			}
			else {
				tmpHtml = tmpHtml.Replace("%PERSON_STATUS_TEXT%", "online");
			}
			
			widgetHtml = tmpHtml;

			// change this later to show their capabilities when we actually have them
			if(person.Presence.Type != PresenceType.Offline) {
				string tmpActionHtml;			

				tmpHtml = actionHtml.Replace("%ACTION_HREF%", "rtc://TEXT_CHAT");
				tmpHtml = tmpHtml.Replace("%ACTION_IMAGE%", "file://" + System.IO.Path.Combine(stylePath, textChatPng));
				tmpActionHtml = tmpHtml;
				
				tmpHtml = actionHtml.Replace("%ACTION_HREF%", "rtc://VIDEO_CHAT");
				tmpHtml = tmpHtml.Replace("%ACTION_IMAGE%", "file://" + System.IO.Path.Combine(stylePath, videoChatPng));
				tmpActionHtml += tmpHtml;

				widgetHtml = widgetHtml.Replace("<!--ACTION_DATA-->", tmpActionHtml);
			}
			
			return widgetHtml;
		}
		#endregion

		
		#region Private Methods
		///<summary>
		///	Reads the NormalHtml from the Theme
		///</summary>	
		private void ReadNormalHtml()
		{
			System.IO.StreamReader htmlReader;
			
			try {
				htmlReader = new System.IO.StreamReader(System.IO.Path.Combine(stylePath, "person.html"));
				normalPersonHtml = htmlReader.ReadToEnd();
			}
			catch (Exception e) {
				// must be an invalid Theme
				throw new ApplicationException("Theme missing person.html");
			}


			try {
				htmlReader = new System.IO.StreamReader(System.IO.Path.Combine(stylePath, "action.html"));
				normalActionHtml = htmlReader.ReadToEnd();
			}
			catch (Exception e) {
				// must be an invalid Theme
				throw new ApplicationException("Theme missing action.html");
			}
		}


		///<summary>
		///	Reads the NormalHtml from the Theme
		///</summary>	
		private void ReadSmallHtml()
		{
			System.IO.StreamReader htmlReader;
			
			try {
				htmlReader = new System.IO.StreamReader(System.IO.Path.Combine(stylePath, "person-small.html"));
				smallPersonHtml = htmlReader.ReadToEnd();
			}
			catch (Exception e) {
				// must be an invalid Theme
				throw new ApplicationException("Theme missing person-small.html");
			}


			try {
				htmlReader = new System.IO.StreamReader(System.IO.Path.Combine(stylePath, "action-small.html"));
				smallActionHtml = htmlReader.ReadToEnd();
			}
			catch (Exception e) {
				// must be an invalid Theme
				throw new ApplicationException("Theme missing action-small.html");
			}
		}
		#endregion

	}
}