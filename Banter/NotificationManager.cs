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
using Evolution;
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
		private Dictionary <Notification, NotificationData> notifications;
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
			notifications = new Dictionary<Notification, NotificationData> ();
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

			Person peer = PersonManager.GetPerson(conversation.PeerUser);
			String messageTitle = Catalog.GetString("Incoming Chat Request");
			String messageBody = String.Format(Catalog.GetString("{0} has initiated a chat"), peer.DisplayName);
			Notification notification;
			
			if(peer.Photo != null) {
				notification = new Notification(messageTitle,
												messageBody,
												peer.Photo);
			} else {
				notification = new Notification(messageTitle,
												messageBody,
												Application.AppIcon);
			}
			
			notification.AddAction("Accept", Catalog.GetString("Accept"), AcceptNotificationHandler);
			notification.AddAction("Decline", Catalog.GetString("Decline"), DeclineNotificationHandler);
			notification.Closed += ClosedNotificationHandler;
			//notification.Timeout = 10000;
			NotificationData data = new NotificationData(conversation, chatType, peer);
			notification.AddHint("PeerID", conversation.PeerUser.ID);
			notifications[notification] = data;
			
			Banter.Application.ShowAppNotification(notification);
		}
		
		/// <summary>
		/// AcceptNotificationHandler
		/// Handles notifications
		/// </summary>	
		private void AcceptNotificationHandler (object o, ActionArgs args)
		{
			Logger.Debug("The notification was accepted");
			Notification notification = (Notification)o;

			if(notifications.ContainsKey(notification)) {
				NotificationData data = notifications[notification];
				notifications.Remove(notification);
				ChatWindowManager.HandleAcceptedConversation(data.Conversation, data.ChatType);
			}
		}	

		/// <summary>
		/// DeclineNotificationHandler
		/// Handles notifications
		/// </summary>	
		private void DeclineNotificationHandler (object o, ActionArgs args)
		{
			Logger.Debug("The notification declined");
			Notification notification = (Notification)o;

			if(notifications.ContainsKey(notification)) {
				NotificationData data = notifications[notification];
				notifications.Remove(notification);				
				if (data.Conversation != null) {
					Logger.Debug("Notification was ignored, calling ConversationManager.Destroy on conversation");		
					ConversationManager.Destroy(data.Conversation);
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

			Notification notification = (Notification)o;

			if(notifications.ContainsKey(notification)) {
				NotificationData data = notifications[notification];
				notifications.Remove(notification);				
				if (data.Conversation != null) {
					Logger.Debug("Notification was ignored, calling ConversationManager.Destroy on conversation");		
					ConversationManager.Destroy(data.Conversation);
				}
			}
		}			
		#endregion
		

		#region Public Static Methods
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
