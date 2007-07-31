//***********************************************************************
// *  $RCSfile$ - ChatWindowManager.cs
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
//using Evolution;
using GLib;
using System.Collections;
using System.Collections.Generic;
using Gtk;


namespace Banter
{
	///<summary>
	///	ChatWindowManager Class
	/// ChatWindowManager is a singleton that manages all ChatWindows.
	///</summary>
	public class ChatWindowManager
	{

		#region Private Static Types
		private static Banter.ChatWindowManager manager = null;
		private static System.Object locker = new System.Object();
		#endregion

	
		#region Private Types
		private Dictionary <uint, ChatWindow> chatWindows;
		#endregion


		#region Public Static Properties
		/// <summary>
		/// Obtain the singleton for PersonManager
		/// </summary>		
		public static ChatWindowManager Instance
		{
			get
			{
				lock(locker) {
					if(manager == null) {
						lock(locker) {
							manager = new ChatWindowManager();
						}
					}
					return manager;
				}
			}
		}
		#endregion


		#region Constructors
		/// <summary>
		/// A private constructor used when obtaining the Singleton by using the static property Instance.
		/// </summary>			
		private ChatWindowManager ()
		{
			chatWindows = new Dictionary<uint,ChatWindow> ();
			// ConversationManager.NewIncomingConversation += OnNewIncomingConversation;
		}
		#endregion	

		
		#region Private Methods
		private void OnChatWindowDeleted (object sender, DeleteEventArgs args)
		{
			ChatWindow cw = (ChatWindow) sender;
			
			if(chatWindows.ContainsKey(cw.PeerProviderUserID))
				chatWindows.Remove(cw.PeerProviderUserID);
		}
		#endregion
		
		#region Internal Methods
		/// <summary>
		/// OnNewIncomingConversation
		/// Handles new conversations initiated by a peer
		/// </summary>			
		static internal void HandleAcceptedConversation (Conversation conversation, ChatType chatType)
		{			
			Logger.Debug("ChatWindowManager.NewAcceptedConversation was called");
			
			if(conversation.PeerUser == null) {
				Logger.Error("NewIncomingConversation event had a conversation with null PeerUser");
				return;
			}
			
			// If we have a ChatWindow for this conversation, don't do anything... the ChatWindow
			// will handle the change
			if(ChatWindowManager.ChatWindowExists(conversation.PeerUser.ID))
				return;
				
			ChatWindowManager cwm = ChatWindowManager.Instance;				

			Logger.Debug("**************Creating chat window");
			ChatWindow cw = new ChatWindow(conversation, chatType);
			Logger.Debug("**************Creating chat window 2");
			cwm.chatWindows[conversation.PeerUser.ID] = cw;
			cw.DeleteEvent += cwm.OnChatWindowDeleted;
			cw.ShowAll();
		}
		#endregion
		

		#region Public Static Methods
		/// <summary>
		/// Closes all chat windows
		/// </summary>
		static public void CloseAllChatWindows()
		{
			foreach(ChatWindow cw in ChatWindowManager.Instance.chatWindows.Values)
			{
				cw.Hide();
			}
		}		
		
		/// <summary>
		/// Checks to see if we already have a chat windows for a given providerUserID
		/// </summary>			
		static public bool ChatWindowExists (uint providerUserID)
		{
			if (ChatWindowManager.Instance.chatWindows.ContainsKey (providerUserID)) {
				Logger.Debug("ChatWindow exists for ProviderUserID: {0}", providerUserID);
				return true;
			}

			Logger.Debug("ChatWindow doesn't exist for ProviderUserID {0}", providerUserID);
			return false;
		}

		/// <summary>
		/// Present the chat window if it exists for the given providerUserID
		/// </summary>	
		static public bool PresentChatWindow (uint providerUserID)
		{
			if (ChatWindowManager.Instance.chatWindows.ContainsKey (providerUserID)) {
				ChatWindow cw = ChatWindowManager.Instance.chatWindows[providerUserID];
				cw.Present();
				return true;
			}
			
			return false;
		}	
		
		/// <summary>
		/// Creates a chat of type with person
		/// </summary>	
		static public void InitiateChat(Person person, ChatType type)
		{
			Logger.Debug ("ChatWindowManager.InitiateChat with {0}", person.DisplayName);
			
			if(person.ProviderUser == null) {
				throw new ApplicationException("Person contained a null ProviderUser");
			}
			
			if (ChatWindowManager.ChatWindowExists (person.ProviderUser.ID) == true) {
				ChatWindow cw = ChatWindowManager.Instance.chatWindows[person.ProviderUser.ID];
				cw.UpdateChatType(type);
				cw.Present();			
			} else {
				// Create a new ChatWindow
				ChatWindow cw = new ChatWindow(person, person.ProviderUser, type);
				ChatWindowManager.Instance.chatWindows[person.ProviderUser.ID] = cw;
				cw.DeleteEvent += ChatWindowManager.Instance.OnChatWindowDeleted;
				Logger.Debug("About to present the window to chat with: {0}", person.DisplayName);
				cw.Present();
			}
		}		
		#endregion
		
		
		#region Public Methods	
		/// <summary>
		/// Initializes the Manager
		/// </summary>	
		public void Init()
		{
			// This does nothing but will create the static class to call it
		}
		#endregion
	}
}


