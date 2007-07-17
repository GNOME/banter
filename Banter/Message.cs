//***********************************************************************
// *  $RCSfile$ - Message.cs
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
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace Banter
{
	/// <summary>
	/// Message priority when processed in the messaging engine
	/// </summary>
	public enum MessagePriority
	{
		Low = 1,
		Normal,
		High
	}
	
	///<summary>
	///	Abstract Message Class
	/// Messages are submitted to the message engine where they are forwarded
	/// and sent to the appropriate protocol provider and then persisted
	/// in the long term message store.
	///
	/// Note: the current database is db4o which can't persist public properties
	/// so the data to persist are defined as public types instead.
	///</summary>
	public abstract class Message
	{
		public string ID;
		public ProviderUser Sender;
		public DateTime Creation;
		public MessagePriority Priority;
		public string Protocol;
		public string Text;
		
		protected Message ()
		{
			ID = Guid.NewGuid().ToString();
			Creation = DateTime.Now;
			Priority = MessagePriority.Normal;
		}
		
		protected Message (string text) : base()
		{
			ID = Guid.NewGuid().ToString();
			Creation = DateTime.Now;
			Priority = MessagePriority.Normal;
			Text = text;
		}
	}
	
	/// <summary>
	/// Text Message
	/// Normally used for an instant message
	/// </summary>
	public class TextMessage : Message
	{
		public TextMessage (string message, ProviderUser sender) : base(message)
		{
			this.Sender = sender;
		}
	}
	
	/// <summary>
	/// System Message
	/// Messages setup and set by components of the real time collaboration
	/// application.  ex. log when a voice call starts
	/// </summary>
	public class SystemMessage : Message
	{
		public string Component;
		public string Metadata;
		
		public SystemMessage (string message) : base(message)
		{
		}
		
		public SystemMessage (string message, string component) : this(message)
		{
			Component = component;
		}
	}
	
	/// <summary>
	/// EmailMessage
	/// Messages to represent received email.
	/// </summary>
	public class EmailMessage : Message
	{
		public EmailMessage (string subject) : base (subject)
		{
		}
	}
	
	/// <summary>
	/// StatusMessage
	/// Used to show a change of status.
	/// </summary>
	public class StatusMessage : Message
	{
		public StatusMessage (string statusMessage) : base (statusMessage)
		{
		}
	}
	
	/// <summary>
	/// ActivityMessage
	/// Used to indicate me or a peer has started typing
	/// a text message
	/// </summary>
	public class ActivityMessage : Message
	{
		public ActivityMessage () : base ()
		{
		}
	}
}