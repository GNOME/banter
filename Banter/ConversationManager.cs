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
using org.freedesktop.Telepathy;

namespace Banter
{
	//public delegate void MessageSentHandler (Conversation conversation, Message message);
	//public delegate void MessageReceivedHandler (Conversation conversation, Message message);
	public delegate void NewIncomingConversationHandler (Conversation conversation, ChatType chatType); //, channel
	
	public class ConversationManager
	{
		static private IList <Conversation> conversations = null;
		static private System.Object lckr = null;
		
		static public NewIncomingConversationHandler NewIncomingConversation;
		
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

		/// <summary>
		///	Method called from Account when a new channel is created
		/// </summary>
		static internal void ProcessNewChannel (
						Account account,
						ObjectPath channelPath,
						string channelType,
						HandleType handleType,
						uint handle,
						bool suppressHandler)

		{
			Logger.Debug ("ConversationManager::ProcessNewChannel - called");
			Conversation conversation = null;
			ChatType chattype = ChatType.Text;
			ProviderUser peerUser = null;
			
			switch (channelType)
			{
				case org.freedesktop.Telepathy.ChannelType.Text:
				{
					IChannelText txtChannel = null;
					if (handle == 0) return;

					// Check if we have an existing conversation with the peer user
					try {
						peerUser = ProviderUserManager.GetProviderUser (handle);
						if (peerUser == null) return;
						
						txtChannel = 
							Bus.Session.GetObject<IChannelText> (
								account.BusName,
								channelPath);
					} catch{}
					
					if (ConversationManager.Exist (peerUser) == true) {
						// FIXME::Pump conversation to create the channel
						Logger.Debug (
							"An existing conversation with {0} already exists", 
							peerUser.Uri);
						return;
					}
					
					try
					{
						Logger.Debug ("creating conversation object");
						conversation = 
							new Conversation (account, peerUser, channelPath, txtChannel);
						conversations.Add (conversation);
						Logger.Debug ("created new conversation object");
					}
					catch (Exception es)
					{
						Logger.Debug (es.Message);
						Logger.Debug (es.StackTrace);
					}
					break;
				}
				
				case org.freedesktop.Telepathy.ChannelType.StreamedMedia:
				{
					// Check if we have an existing conversation with the peer user
					IChannelStreamedMedia mediaChannel = null;
					try {
						mediaChannel = 
							Bus.Session.GetObject<IChannelStreamedMedia> (
								account.BusName,
								channelPath);
						
						peerUser = ProviderUserManager.GetProviderUser (mediaChannel.Members[0]);
						if (peerUser == null) return;
						
						mediaChannel.AddMembers (mediaChannel.LocalPendingMembers, String.Empty);
						
					} catch{}
					
					if (peerUser == null) return;
					
					if (ConversationManager.Exist (peerUser) == true) {
						// FIXME::Pump conversation to create the channel
						Logger.Debug (
							"An existing conversation with {0} already exists", 
							peerUser.Uri);
						return;
					}
					
					try
					{
						Logger.Debug ("creating conversation object");
						conversation = 
							new Conversation (account, peerUser, channelPath, mediaChannel);
						conversations.Add (conversation);
						chattype = ChatType.Video;
						Logger.Debug ("created new conversation object");
					}
					catch (Exception es)
					{
						Logger.Debug (es.Message);
						Logger.Debug (es.StackTrace);
					}
					break;
					
					/*
					if(ichannel.Members.Length > 0) {
						foreach(uint ch in ichannel.Members) {
							Logger.Debug("Member in ichannel.Members {0}", ch);
						}

					}
					if(ichannel.Members.Length > 0) {
						peerHandle = ichannel.Members[0];
					}
					else
						return;
					*/
					
					/*
					if (handle == 0) {
					
						if (ichannel.LocalPendingMembers.Length > 0) {
							Logger.Debug ("Incoming media conversation");
							handle = ichannel.LocalPendingMembers[0];
						} else if (ichannel.RemotePendingMembers.Length > 0) {
							handle = ichannel.RemotePendingMembers[0];
							Logger.Debug ("Pulled the handle from ichannel.RemotePendingMembers");
							return;
						} else if (ichannel.Members.Length > 0) {
							handle = ichannel.Members[0];
							Logger.Debug ("Pulled the handle from ichannel.Members");
							return;
						} else {
							Logger.Debug ("Could not resolve the remote handle");
							return;
						}	
					} else {
						Logger.Debug ("Handle was non-zero {0} - returning", handle);
						return;
					}
					
					if (handle == this.tlpConnection.SelfHandle) {
						Logger.Debug ("Handle was me - yay");
						uint[] meHandles = {handle};
						
						uint[] ids = {ichannel.Members[0]};
							
						// Check if we have an existing conversation with the peer user
						ProviderUser puMe = null;
						ProviderUser puPeer = null;
						
						try {
							puMe = ProviderUserManager.GetProviderUser (handle);
							puPeer = ProviderUserManager.GetProviderUser(peerHandle);
						} catch{}
					
						if (puMe == null) return;
						if (puPeer == null) return;
					
					
						if (ConversationManager.Exist (puPeer) == true) {
							Logger.Debug ("An existing conversation with {0} already exists", puPeer.Uri);
							return;
						}

						ichannel.AddMembers(meHandles, String.Empty);
					
						Logger.Debug ("Peer: {0}", peer.Id);
						Logger.Debug ("Peer Name: {0}", peer.DisplayName);
					
						try
						{
							Logger.Debug ("creating conversation object");
							conversation = ConversationManager.Create (this, peer, false);
							IChannelText txtChannel = 
								Bus.Session.GetObject<IChannelText> (busName, channelPath);
						
							conversation.SetTextChannel (txtChannel);
							conversation.SetMediaChannel (ichannel, channelPath);
							Logger.Debug ("created new conversation object");
							
							conversation.SetPreviewWindow (cw.PreviewWindowId);
							conversation.SetPeerWindow (cw.VideoWindowId);
							conversation.StartVideo (false);
						}
						catch (Exception es)
						{
							Logger.Debug (es.Message);
							Logger.Debug (es.StackTrace);
						}
					}
					
					break;
					*/
				}
				
				default:
					break;
			}
			
			// If successfully created a conversation and have registered consumers
			// of the callback event - fire the rocket
			if (conversation != null & NewIncomingConversation != null)
				NewIncomingConversation (conversation, chattype);
		}
	}
}	

