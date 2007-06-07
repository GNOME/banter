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

namespace Banter
{
	//public delegate void MessageSentHandler (Conversation conversation, Message message);
	//public delegate void MessageReceivedHandler (Conversation conversation, Message message);
	public delegate void NewIncomingConversation (Conversation conversation ); //, channel
	
	public class ConversationManager
	{
		static private IList <Conversation> conversations = null;
		static private System.Object lckr = null;
		
		static public NewIncomingConversation OnNewIncomingConversation;
		
		static ConversationManager()
		{
			lckr = new System.Object ();
        	conversations = new List<Conversation> ();	
		}
		
		static public bool Exist (ProviderUser peer)
		{
			bool exists = false;
			foreach (Conversation conversation in ConversationManager.conversations)
			{
				if (conversation.PeerUser.Uri.CompareTo (peer.Uri) == 0)
				{
					exists = true;
				}
			}
			
			return exists;
		}
		
		static public Conversation Create (Account account, Person peer, bool initiate)
		{
			Conversation conversation = null;
			lock (lckr)
			{
				// Check if a conversation already exists
				foreach (Conversation c in ConversationManager.conversations)
				{
					foreach (ProviderUser pu in peer.ProviderUsers) {
						if (pu.Uri.CompareTo (c.PeerUser.Uri) == 0)
						{
							conversation = c;
							break;
						}
					}
				}

				if (conversation == null)
				{
					// FIXEME::ProviderUsers will be indexed off the person
					// object in priority
					
					Logger.Debug ("Conversation with {0} doesn't exist", peer.DisplayName);
					conversation = new Conversation (account, peer, peer.ProviderUsers[0], initiate);
					conversations.Add (conversation);
				}
			}
			
			return conversation;
		}
		
		static public Conversation Create (ProviderUser provideruser)
		{
			Conversation conversation = null;
			lock (lckr)
			{
				// Check if a conversation already exists
				foreach (Conversation c in ConversationManager.conversations)
				{
					if (provideruser.Uri.CompareTo (c.PeerUser.Uri) == 0) {
						conversation = c;
						break;
					}
				}

				if (conversation == null)
				{
					Logger.Debug ("Conversation with {0} doesn't exist", provideruser.Uri);
					conversation = new Conversation (provideruser);
					conversations.Add (conversation);
				}
			}
			
			return conversation;
		}
		
		static internal void AddConversation (Conversation conversation)
		{
			lock (lckr)
			{
				// Check if a conversation already exists
				foreach (Conversation c in ConversationManager.conversations)
				{
					if (conversation.PeerUser.Uri.CompareTo (c.PeerUser.Uri) == 0) {
						throw new ApplicationException ("Conversation with user already exists");
					}
				}

				Logger.Debug (
					"Adding an incoming conversation with {0} to the list", 
					conversation.PeerUser.Uri);
				conversations.Add (conversation);
			}
		}
	}
}	

