//***********************************************************************
// *  $RCSfile$ - NotificationManager.cs
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
using System.IO;
//using Evolution;
using GLib;
using System.Collections;
using System.Collections.Generic;
using Gtk;
using Mono.Unix;
using Notifications;

namespace Banter
{
	///<summary>
	///	NotificationManager Class
	/// NotificationManager is a singleton that manages all Notifications.
	///</summary>
	public class NotificationManager
	{

		#region Private Static Types
		private static Banter.NotificationManager manager = null;
		private static System.Object locker = new System.Object();
		#endregion

	
		#region Private Types
		private Dictionary <uint, NotificationData> pendingData;
		private System.Object notifyLock;
		private Notification currentNotification;
		private uint currentPeerID;
		#endregion


		#region Public Static Properties
		/// <summary>
		/// Obtain the singleton for PersonManager
		/// </summary>		
		public static NotificationManager Instance
		{
			get
			{
				lock(locker) {
					if(manager == null) {
						lock(locker) {
							manager = new NotificationManager();
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
		private NotificationManager ()
		{
			notifyLock = new System.Object();

			pendingData = new Dictionary <uint, NotificationData> ();
			ConversationManager.NewIncomingConversation += OnNewIncomingConversation;
		}
		#endregion	

		
		#region Private Methods

		/// <summary>
		/// OnNewIncomingConversation
		/// Handles new conversations initiated by a peer
		/// </summary>			
		private void OnNewIncomingConversation (Conversation conversation, ChatType chatType)
		{
			Logger.Debug("NotificationManager.OnNewIncomingConversation was called");
			
			if(conversation.PeerUser == null) {
				Logger.Error("NewIncomingConversation event had a conversation with null PeerUser");
				return;
			}			
			
			// If we have a ChatWindow for this conversation, don't do anything... the ChatWindow
			// will handle the change
			if(ChatWindowManager.ChatWindowExists(conversation.PeerUser.ID))
				return;

			if(chatType == ChatType.Text) {
				NotifyOfTextMessage(conversation);
			}
			else if(chatType == ChatType.Audio) {
				NotifyOfAudioRequest(conversation);
			}
			else if(chatType == ChatType.Video) {
				NotifyOfVideoRequest(conversation);
			}

			conversation.MessageReceived += OnTextAdditionalMessageReceived;
			conversation.MediaChannelOpened += OnMediaChannelOpened;
			conversation.MediaChannelClosed += OnMediaChannelClosed;
			conversation.TextChannelOpened += OnTextChannelOpened;
		}
		

		///<summary>
		///	OnTextAdditionalMessageReceived
		/// Handles additional Text messages on a pending conversation
		///</summary>
		private void OnTextAdditionalMessageReceived (Conversation conversation, Message message)
		{
			Logger.Debug("NotificationManager.OnTextAdditionalMessageReceived was called");		
			NotifyOfTextMessage(conversation);
		}

		private void OnTextChannelOpened (Conversation conversation)
		{
			Logger.Debug("NotificationManager.OnTextChannelOpened was called");			
			NotifyOfTextMessage(conversation);		
		}

		///<summary>
		///	OnMediaChannelOpened
		/// Handles a media channel opened on a conversation
		///</summary>
		private void OnMediaChannelOpened (Conversation conversation)
		{
			Logger.Debug("NotificationManager.OnMediaChannelOpened was called");			
			NotifyOfVideoRequest(conversation);
		}


		private void OnVideoStreamDown(Conversation conversation)
		{
			// close the current notification before adding another
			if(currentNotification != null) {
				Logger.Debug("Current notification != null");
				currentNotification.Close();
				currentNotification = null;
				currentPeerID = 0;
			}
			CleanUpConversation(conversation, true);
		}

		
		private void OnMediaChannelClosed (Conversation conversation)
		{
			// close the current notification before adding another
			if(currentNotification != null) {
				Logger.Debug("Current notification != null");
				currentNotification.Close();
				currentNotification = null;
				currentPeerID = 0;
			}
			CleanUpConversation(conversation, true);
		}
		
		
		
		/// <summary>
		/// NotifyOfTextMessage
		/// Notifies user of an incoming text message
		/// </summary>	
		private void NotifyOfTextMessage(Conversation conversation)
		{
			// close the current notification before adding another
			if(currentNotification != null) {
				Logger.Debug("Current notification != null");
				currentNotification.Close();
				currentNotification = null;
				currentPeerID = 0;
			}

			lock(notifyLock) {
				Person peer = PersonManager.GetPerson(conversation.PeerUser);
				if(peer == null)
					return;

				String messageTitle = String.Format(Catalog.GetString("Message from {0}"), peer.DisplayName);
				Message[] messages = conversation.GetReceivedMessages();
				String messageBody;
				
				if(messages.Length > 0) {
					messageBody = messages[messages.Length - 1].Text;
				}
				else
					messageBody = "";

				// Limit the size of the message that is sent					
				if(messageBody.Length > 200) {
					messageBody = messageBody.Substring(0, 200);
					messageBody = messageBody + " ...";
				}
					
				Notification notification;
				if(peer.Photo != null) {
					Gdk.Pixbuf sizedPhoto = peer.Photo.ScaleSimple(48, 48, Gdk.InterpType.Bilinear);
					notification = new Notification(messageTitle,
													messageBody,
													sizedPhoto);
				} else {
					Gdk.Pixbuf banterIcon = Application.GetIcon ("banter-44", 44);
					notification = new Notification(messageTitle,
													messageBody,
													banterIcon);
				}

				NotificationData data = new NotificationData(conversation, ChatType.Text, peer);
				pendingData[conversation.PeerUser.ID] = data;
				
				notification.AddAction("Accept", Catalog.GetString("Accept"), AcceptNotificationHandler);
				notification.AddAction("Decline", Catalog.GetString("Decline"), DeclineNotificationHandler);
				notification.Closed += ClosedNotificationHandler;
				currentNotification = notification;
				currentPeerID = conversation.PeerUser.ID;
				Banter.Application.ShowAppNotification(notification);
				Gnome.Sound.Play(Path.Combine(Banter.Defines.SoundDir, "notify.wav"));
			}
		}

		/// <summary>
		/// NotifyOfVideoRequest
		/// Notifies user of an incoming audio request
		/// </summary>	
		private void NotifyOfAudioRequest(Conversation conversation)
		{
			// close the current notification before adding another
			if(currentNotification != null) {
				Logger.Debug("Current notification != null");
				currentNotification.Close();
				currentNotification = null;
				currentPeerID = 0;
			}

			lock(notifyLock) {
				Person peer = PersonManager.GetPerson(conversation.PeerUser);
				if(peer == null)
					return;

				String messageTitle = Catalog.GetString("Incoming Audio Chat");
				String messageBody = String.Format(Catalog.GetString("{0} is requesting an audio chat"), peer.DisplayName);
				Message[] messages = conversation.GetReceivedMessages();
					
				Notification notification;
				if(peer.Photo != null) {
					Gdk.Pixbuf sizedPhoto = peer.Photo.ScaleSimple(48, 48, Gdk.InterpType.Bilinear);
					notification = new Notification(messageTitle,
													messageBody,
													sizedPhoto);
				} else {
					Gdk.Pixbuf banterIcon = Application.GetIcon ("banter-44", 44);
					notification = new Notification(messageTitle,
													messageBody,
													banterIcon);
				}

				NotificationData data = new NotificationData(conversation, ChatType.Audio, peer);
				pendingData[conversation.PeerUser.ID] = data;
				
				notification.AddAction("Accept", Catalog.GetString("Accept"), AcceptNotificationHandler);
				notification.AddAction("Decline", Catalog.GetString("Decline"), DeclineNotificationHandler);
				notification.Closed += ClosedNotificationHandler;
				notification.Timeout = 120000;
				currentNotification = notification;
				currentPeerID = conversation.PeerUser.ID;
				Banter.Application.ShowAppNotification(notification);
				Gnome.Sound.Play(Path.Combine(Banter.Defines.SoundDir, "notify.wav"));
			}
		}
		

		/// <summary>
		/// NotifyOfVideoRequest
		/// Notifies user of an incoming video request
		/// </summary>	
		private void NotifyOfVideoRequest(Conversation conversation)
		{
			// close the current notification before adding another
			if(currentNotification != null) {
				Logger.Debug("Current notification != null");
				currentNotification.Close();
				currentNotification = null;
				currentPeerID = 0;
			}

			lock(notifyLock) {
				Person peer = PersonManager.GetPerson(conversation.PeerUser);
				if(peer == null)
					return;

				String messageTitle = Catalog.GetString("Incoming Video Chat");
				String messageBody = String.Format(Catalog.GetString("{0} is requesting a video chat"), peer.DisplayName);
				Message[] messages = conversation.GetReceivedMessages();
					

				Notification notification;
				if(peer.Photo != null) {
					Gdk.Pixbuf sizedPhoto = peer.Photo.ScaleSimple(48, 48, Gdk.InterpType.Bilinear);
					notification = new Notification(messageTitle,
													messageBody,
													sizedPhoto);
				} else {
					Gdk.Pixbuf banterIcon = Application.GetIcon ("banter-44", 44);
					notification = new Notification(messageTitle,
													messageBody,
													banterIcon);
				}

				NotificationData data = new NotificationData(conversation, ChatType.Video, peer);
				pendingData[conversation.PeerUser.ID] = data;
				
				notification.AddAction("Accept", Catalog.GetString("Accept"), AcceptNotificationHandler);
				notification.AddAction("Decline", Catalog.GetString("Decline"), DeclineNotificationHandler);
				notification.Closed += ClosedNotificationHandler;
				notification.Timeout = 120000;
				currentNotification = notification;
				currentPeerID = conversation.PeerUser.ID;
				Banter.Application.ShowAppNotification(notification);
				Gnome.Sound.Play(Path.Combine(Banter.Defines.SoundDir, "notify.wav"));
			}
		}
		
		
		/// <summary>
		/// AcceptNotificationHandler
		/// Handles notifications
		/// </summary>	
		private void AcceptNotificationHandler (object o, ActionArgs args)
		{
			lock(notifyLock) {
				Logger.Debug("The notification was accepted");
				Notification notification = (Notification)o;

				if(currentNotification != null) {
					NotificationData data = pendingData[currentPeerID];
					if(data.Conversation != null)
						CleanUpConversation(data.Conversation, false);

					currentNotification = null;
					currentPeerID = 0;
					ChatWindowManager.HandleAcceptedConversation(data.Conversation, data.ChatType);
				}
			}
		}	

		/// <summary>
		/// DeclineNotificationHandler
		/// Handles notifications
		/// </summary>	
		private void DeclineNotificationHandler (object o, ActionArgs args)
		{
			lock(notifyLock) {
				Logger.Debug("The notification declined");
				Notification notification = (Notification)o;

				if(currentNotification != null) {
					NotificationData data = pendingData[currentPeerID];
					if(data.Conversation != null)
						CleanUpConversation(data.Conversation, true);
						
					currentNotification = null;
					currentPeerID = 0;
				}
			}
		}
		
		/// <summary>
		/// ClosedNotificationHandler
		/// Handles notifications
		/// </summary>	
		private void ClosedNotificationHandler (object o, EventArgs args)
		{
			Logger.Debug("The notification windows was closed");
			lock(notifyLock) {
				Notification notification = (Notification)o;

				if(currentNotification != null) {
					currentNotification = null;
					currentPeerID = 0;
				}
			}
		}

		/// <summary>
		/// CleanUpConversation
		/// Removes all event handlers and optionally destroys the conversation
		/// </summary>			
		private void CleanUpConversation(Conversation conversation, bool destroyit)
		{
			conversation.MessageReceived -= OnTextAdditionalMessageReceived;
			conversation.MediaChannelOpened -= OnMediaChannelOpened;
			conversation.MediaChannelClosed -= OnMediaChannelClosed;
			conversation.TextChannelOpened -= OnTextChannelOpened;		
			if(pendingData.ContainsKey(conversation.PeerUser.ID)) {
				pendingData.Remove(conversation.PeerUser.ID);
			}

			if(destroyit) {
				ConversationManager.Destroy(conversation);			
			}
		}
		#endregion
		

		#region Public Static Methods
		/// <summary>
		/// Pops up a notification for a chat, could be because the chat window doesn't have focus
		/// </summary>	
		public static void NotifyMessage(Person person, Message message)
		{
			String messageTitle = String.Format(Catalog.GetString("Message from {0}"), person.DisplayName);

			Notification notification;
			if(person.Photo != null) {
				Gdk.Pixbuf sizedPhoto = person.Photo.ScaleSimple(48, 48, Gdk.InterpType.Bilinear);
				notification = new Notification(messageTitle,
												message.Text,
												sizedPhoto);
			} else {
				Gdk.Pixbuf banterIcon = Application.GetIcon ("banter-44", 44);
				notification = new Notification(messageTitle,
												message.Text,
												banterIcon);
			}

//			notification.Timeout = 120000;
//			currentNotification = notification;
//			currentPeerID = conversation.PeerUser.ID;
			Banter.Application.ShowAppNotification(notification);
			Gnome.Sound.Play(Path.Combine(Banter.Defines.SoundDir, "notify.wav"));
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
