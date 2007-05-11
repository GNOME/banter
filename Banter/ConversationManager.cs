//***********************************************************************
// *  $RCSfile$ - ConversationManager.cs
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
using System.Collections.Generic;
using System.Net;
using System.Text;

using NDesk.DBus;
using org.freedesktop.DBus;
using Tapioca;

namespace Banter
{
	//public delegate void MessageSentHandler (Conversation conversation, Message message);
	//public delegate void MessageReceivedHandler (Conversation conversation, Message message);
	
	public class ConversationManager
	{
		
		static private IList <Conversation> conversations = null;
		static private System.Object lckr = null;
		
		static ConversationManager()
		{
			lckr = new System.Object ();
        	conversations = new List<Conversation> ();	
		}
		
		static public bool Exist (Tapioca.Contact peer)
		{
			bool exists = false;
			foreach (Conversation conversation in ConversationManager.conversations)
			{
				if (conversation.PeerContact.Uri.CompareTo (peer.Uri) == 0)
				{
					exists = true;
				}
			}
			
			return exists;
		}
		
		static public Conversation Create (Account account, Tapioca.Contact peer, bool initiate)
		{
			Conversation conversation = null;
			lock (lckr)
			{
				// Check if a conversation already exists
				foreach (Conversation c in ConversationManager.conversations)
				{
					if (c.PeerContact.Uri.CompareTo (peer.Uri) == 0)
					{
						conversation = c;
						break;
					}
				}

				if (conversation == null)
				{
					conversation = new Conversation (account, peer);
					conversations.Add (conversation);
				}
			}
			
			return conversation;
		}
	}
}	

