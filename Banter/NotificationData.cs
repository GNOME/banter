//***********************************************************************
// *  $RCSfile$ - NotificationData.cs
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
using System.Collections;
using System.Net;
using System.Text;

namespace Banter
{
	///<summary>
	///	NotificationData Class
	/// NotificationData holds data about notifications
	///</summary>
	public class NotificationData
	{
		#region Private Types
		private Conversation conversation;
		private ChatType chatType;
		private Person person;
		#endregion
		
		
		#region Public Properties
		/// <summary>
		/// The conversation for this notification
		/// </summary>		
		public Conversation Conversation
		{
			get { return conversation; }
		}

		/// <summary>
		/// The ChatType for this notification
		/// </summary>		
		public ChatType ChatType
		{
			get { return chatType; }
		}

		/// <summary>
		/// The Person for this notification
		/// </summary>		
		public Person Person
		{
			get { return person; }
		}		
		#endregion		
		
		public NotificationData(Conversation conversation, ChatType chatType, Person person)
		{
			this.conversation = conversation;
			this.chatType = chatType;
			this.person = person;
		}
	}
}
