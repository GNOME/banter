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
		private string largePersonHtml;
		private string largeActionHtml;
		private string largeDataSectionHtml;
		private string largeSingleDataEntryHtml;
		private string largeTitleDataEntryHtml;
		private string largeTextGraphic;
		private string largeVideoGraphic;
		private string largeBlankHead;
		private string mediumPersonHtml;
		private string mediumActionHtml;
		private string mediumTextGraphic;
		private string mediumVideoGraphic;
		private string mediumBlankHead;
		private string smallPersonHtml;
		private string smallActionHtml;
		private string smallTextGraphic;
		private string smallVideoGraphic;
		private string smallBlankHead;		
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

 			ReadLargeHtml();
			ReadMediumHtml();
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
			string blankHeadPng;
			string personHtml;
			string actionHtml;
			string dataHtml;
			string titleDataHtml;
			string singleDataHtml;				
			int avatarSize = 0;
			string tmpHtml;

			switch(size) {
				default:
				case PersonCardSize.Small:
					textChatPng = smallTextGraphic;
					videoChatPng = smallVideoGraphic;
					blankHeadPng = smallBlankHead;
					personHtml = smallPersonHtml;
					actionHtml = smallActionHtml;
					dataHtml = null;
					titleDataHtml = null;
					singleDataHtml = null;						
					avatarSize = 16;
					break;
				case PersonCardSize.Medium:
					textChatPng = mediumTextGraphic;
					videoChatPng = mediumVideoGraphic;
					blankHeadPng = mediumBlankHead;
					personHtml = mediumPersonHtml;
					actionHtml = mediumActionHtml;
					dataHtml = null;
					titleDataHtml = null;
					singleDataHtml = null;					
					avatarSize = 36;
					break;
				// Haven't created any Large, just use Medium
				case PersonCardSize.Large:
					textChatPng = largeTextGraphic;
					videoChatPng = largeVideoGraphic;
					blankHeadPng = largeBlankHead;
					personHtml = largePersonHtml;
					actionHtml = largeActionHtml;
					dataHtml = largeDataSectionHtml;
					titleDataHtml = largeTitleDataEntryHtml;
					singleDataHtml = largeSingleDataEntryHtml;
					avatarSize = 48;					
					break;
			}
			
			string avatar = person.GetScaledAvatar(avatarSize);

			if(avatar != null) {
				tmpHtml = personHtml.Replace("%PERSON_PHOTO%", "file://" + avatar);
			}
			else {
				tmpHtml = personHtml.Replace("%PERSON_PHOTO%", "file://" + blankHeadPng);
			}
			
			tmpHtml = tmpHtml.Replace("%PERSON_DISPLAY_NAME%",  person.DisplayName);

			if (person.PresenceMessage.Length > 0) {
				tmpHtml = tmpHtml.Replace("%PERSON_STATUS_TEXT%", person.PresenceMessage);
			}
			else {
				tmpHtml = tmpHtml.Replace("%PERSON_STATUS_TEXT%", Presence.GetStatusString(person.Presence.Type));
			}
			
			widgetHtml = tmpHtml;

			// change this later to show their capabilities when we actually have them
			if(person.Presence.Type != PresenceType.Offline) {
				string tmpActionHtml;

				tmpHtml = actionHtml.Replace("%ACTION_HREF%", "rtc://TEXT_CHAT");
				tmpHtml = tmpHtml.Replace("%ACTION_IMAGE%", "file://" + textChatPng);
				tmpActionHtml = tmpHtml;
				
				tmpHtml = actionHtml.Replace("%ACTION_HREF%", "rtc://VIDEO_CHAT");
				tmpHtml = tmpHtml.Replace("%ACTION_IMAGE%", "file://" + videoChatPng);
				tmpActionHtml += tmpHtml;

				widgetHtml = widgetHtml.Replace("<!--ACTION_DATA-->", tmpActionHtml);
			}
			
			
			if( (dataHtml != null) &&
				(titleDataHtml != null) &&
				(singleDataHtml != null) ) {

				string allDataHtml = null;
				string dataSectionHtml;
				string dataEntryHtml;
				
				
				if( (person.JabberId != null) && (person.JabberId.Length > 0) ) {
					dataSectionHtml = dataHtml.Replace("%SECTION_TITLE%", "Instant Messaging");
					dataEntryHtml = titleDataHtml.Replace("%DATA_TEXT%", person.JabberId);
					dataEntryHtml = dataEntryHtml.Replace("%DATA_TITLE%", "jabber");
					tmpHtml = dataEntryHtml;
/*					dataEntryHtml = titleDataHtml.Replace("%DATA_TEXT%", "cgaisford.novell");
					dataEntryHtml = dataEntryHtml.Replace("%DATA_TITLE%", "gwim");
					tmpHtml += dataEntryHtml;
*/
					dataSectionHtml = dataSectionHtml.Replace("<!--DATA_ENTRY-->", tmpHtml);
					if(allDataHtml == null)
						allDataHtml = dataSectionHtml;
					else
						allDataHtml += dataSectionHtml;
				}
				
				if( (person.EDSContact != null) &&	(person.EDSContact.Email1 != null) && (person.EDSContact.Email1.Length > 0) ) {
					dataSectionHtml = dataHtml.Replace("%SECTION_TITLE%", "Email");
					dataEntryHtml = titleDataHtml.Replace("%DATA_TEXT%", person.EDSContact.Email1);
					dataEntryHtml = dataEntryHtml.Replace("%DATA_TITLE%", "Work");
					tmpHtml = dataEntryHtml;
					if(	(person.EDSContact.Email2 != null) && (person.EDSContact.Email2.Length > 0) ) {
						dataEntryHtml = titleDataHtml.Replace("%DATA_TEXT%", person.EDSContact.Email2);
						dataEntryHtml = dataEntryHtml.Replace("%DATA_TITLE%", "Home");
						tmpHtml += dataEntryHtml;
					}
					if(	(person.EDSContact.Email3 != null) && (person.EDSContact.Email3.Length > 0) ) {
						dataEntryHtml = titleDataHtml.Replace("%DATA_TEXT%", person.EDSContact.Email3);
						dataEntryHtml = dataEntryHtml.Replace("%DATA_TITLE%", "Other");
						tmpHtml += dataEntryHtml;
					}

					dataSectionHtml = dataSectionHtml.Replace("<!--DATA_ENTRY-->", tmpHtml);
					
					if(allDataHtml == null)
						allDataHtml = dataSectionHtml;
					else
						allDataHtml += dataSectionHtml;
				}

				if( (person.EDSContact != null) &&	(person.EDSContact.PrimaryPhone != null) && (person.EDSContact.PrimaryPhone.Length > 0) ) {
					dataSectionHtml = dataHtml.Replace("%SECTION_TITLE%", "Phone");
					dataEntryHtml = titleDataHtml.Replace("%DATA_TEXT%", person.EDSContact.PrimaryPhone);
					dataEntryHtml = dataEntryHtml.Replace("%DATA_TITLE%", "Primary");
					tmpHtml = dataEntryHtml;
					if(	(person.EDSContact.BusinessPhone != null) && (person.EDSContact.BusinessPhone.Length > 0) && (person.EDSContact.BusinessPhone.CompareTo(person.EDSContact.PrimaryPhone) != 0) ) {
						dataEntryHtml = titleDataHtml.Replace("%DATA_TEXT%", person.EDSContact.BusinessPhone);
						dataEntryHtml = dataEntryHtml.Replace("%DATA_TITLE%", "Work");
						tmpHtml += dataEntryHtml;
					}
					if(	(person.EDSContact.HomePhone != null) && (person.EDSContact.HomePhone.Length > 0) && (person.EDSContact.HomePhone.CompareTo(person.EDSContact.PrimaryPhone) != 0) ) {
						dataEntryHtml = titleDataHtml.Replace("%DATA_TEXT%", person.EDSContact.HomePhone);
						dataEntryHtml = dataEntryHtml.Replace("%DATA_TITLE%", "Home");
						tmpHtml += dataEntryHtml;
					}
					if(	(person.EDSContact.MobilePhone != null) && (person.EDSContact.MobilePhone.Length > 0) && (person.EDSContact.MobilePhone.CompareTo(person.EDSContact.PrimaryPhone) != 0) ) {
						dataEntryHtml = titleDataHtml.Replace("%DATA_TEXT%", person.EDSContact.MobilePhone);
						dataEntryHtml = dataEntryHtml.Replace("%DATA_TITLE%", "Mobile");
						tmpHtml += dataEntryHtml;
					}

					dataSectionHtml = dataSectionHtml.Replace("<!--DATA_ENTRY-->", tmpHtml);
					
					if(allDataHtml == null)
						allDataHtml = dataSectionHtml;
					else
						allDataHtml += dataSectionHtml;
				}

				if( (person.EDSContact != null) &&	(person.EDSContact.AddressLabelWork != null) && (person.EDSContact.AddressLabelWork.Length > 0) ) {
					dataSectionHtml = dataHtml.Replace("%SECTION_TITLE%", "Work Address");
					dataEntryHtml = singleDataHtml.Replace("%DATA_TEXT%", person.EDSContact.AddressLabelWork);
					tmpHtml = dataEntryHtml;

					dataSectionHtml = dataSectionHtml.Replace("<!--DATA_ENTRY-->", tmpHtml);
					
					if(allDataHtml == null)
						allDataHtml = dataSectionHtml;
					else
						allDataHtml += dataSectionHtml;
				}

				if( (person.EDSContact != null) &&	(person.EDSContact.AddressLabelHome != null) && (person.EDSContact.AddressLabelHome.Length > 0) ) {
					dataSectionHtml = dataHtml.Replace("%SECTION_TITLE%", "Home Address");
					dataEntryHtml = singleDataHtml.Replace("%DATA_TEXT%", person.EDSContact.AddressLabelHome);
					tmpHtml = dataEntryHtml;

					dataSectionHtml = dataSectionHtml.Replace("<!--DATA_ENTRY-->", tmpHtml);
					
					if(allDataHtml == null)
						allDataHtml = dataSectionHtml;
					else
						allDataHtml += dataSectionHtml;
				}

				if(allDataHtml != null)				
					widgetHtml = widgetHtml.Replace("<!--DATA_SECTION-->", allDataHtml);
			}
			
			return widgetHtml;
		}
		#endregion

		
		#region Private Methods
		///<summary>
		///	Reads the LargeHtml from the Theme
		///</summary>	
		private void ReadLargeHtml()
		{
			System.IO.StreamReader htmlReader;
			
			try {
				htmlReader = new System.IO.StreamReader(System.IO.Path.Combine(stylePath, "large/contact.html"));
				largePersonHtml = htmlReader.ReadToEnd();
			}
			catch (Exception e) {
				// must be an invalid Theme
				throw new ApplicationException("Theme missing large/contact.html");
			}


			try {
				htmlReader = new System.IO.StreamReader(System.IO.Path.Combine(stylePath, "large/action.html"));
				largeActionHtml = htmlReader.ReadToEnd();
			}
			catch (Exception e) {
				// must be an invalid Theme
				throw new ApplicationException("Theme missing large/action.html");
			}

			try {
				htmlReader = new System.IO.StreamReader(System.IO.Path.Combine(stylePath, "large/datasection.html"));
				largeDataSectionHtml = htmlReader.ReadToEnd();
			}
			catch (Exception e) {
				// must be an invalid Theme
				throw new ApplicationException("Theme missing large/datasection.html");
			}

			try {
				htmlReader = new System.IO.StreamReader(System.IO.Path.Combine(stylePath, "large/singledataentry.html"));
				largeSingleDataEntryHtml = htmlReader.ReadToEnd();
			}
			catch (Exception e) {
				// must be an invalid Theme
				throw new ApplicationException("Theme missing large/singledataentry.html");
			}

			try {
				htmlReader = new System.IO.StreamReader(System.IO.Path.Combine(stylePath, "large/titledataentry.html"));
				largeTitleDataEntryHtml = htmlReader.ReadToEnd();
			}
			catch (Exception e) {
				// must be an invalid Theme
				throw new ApplicationException("Theme missing large/titledataentry.html");
			}			


			largeTextGraphic = System.IO.Path.Combine(stylePath, "large/text.png");
			if (!File.Exists (largeTextGraphic)) {
				largeTextGraphic = System.IO.Path.Combine(stylePath, "text.png");
				if (!File.Exists (largeTextGraphic)) {
					throw new ApplicationException("Theme missing large text.png");
				}
			}

			largeVideoGraphic = System.IO.Path.Combine(stylePath, "large/video.png");
			if (!File.Exists (largeVideoGraphic)) {
				largeVideoGraphic = System.IO.Path.Combine(stylePath, "video.png");
				if (!File.Exists (largeVideoGraphic)) {
					throw new ApplicationException("Theme missing large video.png");
				}
			}

			largeBlankHead = System.IO.Path.Combine(stylePath, "large/blankhead.png");
			if (!File.Exists (largeBlankHead)) {
				largeBlankHead = System.IO.Path.Combine(stylePath, "blankhead.png");
				if (!File.Exists (largeBlankHead)) {
					throw new ApplicationException("Theme missing large blankhead.png");
				}
			}
			
			
		}


		///<summary>
		///	Reads the MediumHtml from the Theme
		///</summary>	
		private void ReadMediumHtml()
		{
			System.IO.StreamReader htmlReader;
			
			try {
				htmlReader = new System.IO.StreamReader(System.IO.Path.Combine(stylePath, "medium/contact.html"));
				mediumPersonHtml = htmlReader.ReadToEnd();
			}
			catch (Exception e) {
				// must be an invalid Theme
				throw new ApplicationException("Theme missing medium/contact.html");
			}


			try {
				htmlReader = new System.IO.StreamReader(System.IO.Path.Combine(stylePath, "medium/action.html"));
				mediumActionHtml = htmlReader.ReadToEnd();
			}
			catch (Exception e) {
				// must be an invalid Theme
				throw new ApplicationException("Theme missing medium/action.html");
			}


			mediumTextGraphic = System.IO.Path.Combine(stylePath, "medium/text.png");
			if (!File.Exists (mediumTextGraphic)) {
				mediumTextGraphic = System.IO.Path.Combine(stylePath, "text.png");
				if (!File.Exists (mediumTextGraphic)) {
					throw new ApplicationException("Theme missing medium text.png");
				}
			}

			mediumVideoGraphic = System.IO.Path.Combine(stylePath, "medium/video.png");
			if (!File.Exists (mediumVideoGraphic)) {
				mediumVideoGraphic = System.IO.Path.Combine(stylePath, "video.png");
				if (!File.Exists (mediumVideoGraphic)) {
					throw new ApplicationException("Theme missing medium video.png");
				}
			}

			mediumBlankHead = System.IO.Path.Combine(stylePath, "medium/blankhead.png");
			if (!File.Exists (mediumBlankHead)) {
				mediumBlankHead = System.IO.Path.Combine(stylePath, "blankhead.png");
				if (!File.Exists (mediumBlankHead)) {
					throw new ApplicationException("Theme missing medium blankhead.png");
				}
			}			
		}


		///<summary>
		///	Reads the NormalHtml from the Theme
		///</summary>	
		private void ReadSmallHtml()
		{
			System.IO.StreamReader htmlReader;
			
			try {
				htmlReader = new System.IO.StreamReader(System.IO.Path.Combine(stylePath, "small/contact.html"));
				smallPersonHtml = htmlReader.ReadToEnd();
			}
			catch (Exception e) {
				// must be an invalid Theme
				throw new ApplicationException("Theme missing small/contact.html");
			}


			try {
				htmlReader = new System.IO.StreamReader(System.IO.Path.Combine(stylePath, "small/action.html"));
				smallActionHtml = htmlReader.ReadToEnd();
			}
			catch (Exception e) {
				// must be an invalid Theme
				throw new ApplicationException("Theme missing small/action.html");
			}
			smallTextGraphic = System.IO.Path.Combine(stylePath, "small/text.png");
			if (!File.Exists (smallTextGraphic)) {
				smallTextGraphic = System.IO.Path.Combine(stylePath, "text.png");
				if (!File.Exists (smallTextGraphic)) {
					throw new ApplicationException("Theme missing small text.png");
				}
			}

			smallVideoGraphic = System.IO.Path.Combine(stylePath, "small/video.png");
			if (!File.Exists (smallVideoGraphic)) {
				smallVideoGraphic = System.IO.Path.Combine(stylePath, "video.png");
				if (!File.Exists (smallVideoGraphic)) {
					throw new ApplicationException("Theme missing small video.png");
				}
			}

			smallBlankHead = System.IO.Path.Combine(stylePath, "small/blankhead.png");
			if (!File.Exists (smallBlankHead)) {
				smallBlankHead = System.IO.Path.Combine(stylePath, "blankhead.png");
				if (!File.Exists (smallBlankHead)) {
					throw new ApplicationException("Theme missing small blankhead.png");
				}
			}			
		}
		#endregion

	}
}