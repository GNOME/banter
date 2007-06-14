//***********************************************************************
// *  $RCSfile$ - StatusEntry.cs
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
using Gtk;
using Mono.Unix;

namespace Banter
{
	public delegate void StatusEntryChangedHandler (Presence presence);
	
	public class StatusEntry : Gtk.EventBox 
	{
		Notebook notebook; 

		Label statusLabel;
		Button statusDownButton;
		Entry statusEntry;
		
		Dictionary<string, string> customAvailableMessages;
		Dictionary<string, string> customBusyMessages;
		Dictionary<string, string> customAwayMessages;
		
		Presence presence;
		PresenceType potentialPresenceType;
		
		public StatusEntry()
		{
			ModifyBg (StateType.Normal, Style.Background (StateType.Active));
			BorderWidth = 0;

			customAvailableMessages = new Dictionary<string,string> ();
			customBusyMessages = new Dictionary<string,string> ();
			customAwayMessages = new Dictionary<string,string> ();
			
			presence = null;
			potentialPresenceType = PresenceType.Offline;
			
			notebook = new Notebook ();
			notebook.ModifyBg (StateType.Normal, Style.Background (StateType.Active));
			notebook.ShowTabs = false;
			notebook.ShowBorder = false;
			
			notebook.AppendPage (CreateViewWidget (), new Label ());
			notebook.AppendPage (CreateEditWidget (), new Label());
			
			notebook.Show ();
			
			Add (notebook);
		}
		
#region Private Methods
		private Widget CreateViewWidget ()
		{
			HBox hbox = new HBox (false, 2);
			
			statusLabel = new Label ();
			statusLabel.ModifyBg (StateType.Normal, Style.Background (StateType.Active));
			statusLabel.LineWrap = false;
			statusLabel.UseMarkup = true;
			statusLabel.UseUnderline = false;
			statusLabel.Show ();
			hbox.PackStart (statusLabel, true, true, 0);
			
			Image downImage = new Image (Stock.GoDown, IconSize.Menu);
			statusDownButton = new Button (downImage);
			statusDownButton.ModifyBg (StateType.Normal, Style.Background (StateType.Active));
			statusDownButton.Relief = ReliefStyle.None;
			statusDownButton.Clicked += OnStatusDownButtonClicked;
			statusDownButton.Show ();
			hbox.PackStart (statusDownButton, false, false, 0);
			
			hbox.Show ();
			
			return hbox;
		}
		
		private Widget CreateEditWidget ()
		{
			statusEntry = new Entry ();
			statusEntry.ModifyBg (StateType.Normal, Style.Background (StateType.Active));
			statusEntry.Activated += OnStatusEntryActivated;
			statusEntry.KeyPressEvent += OnStatusEntryKeyPressEvent;
			statusEntry.Show ();
			
			return statusEntry;
		}
		
		private void SwitchToViewMode ()
		{
			potentialPresenceType = PresenceType.Offline;
			statusEntry.Text = string.Empty;
			notebook.Page = 0;
		}
		
		private void SwitchToEditMode ()
		{
			notebook.Page = 1;
		}
		
		private void SwitchToEditMode (PresenceType type)
		{
			this.potentialPresenceType = type;
			SwitchToEditMode ();
		}
		
#endregion
		
#region Overrides
		protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
		{
			SwitchToEditMode ();
			return false;
		}
#endregion

#region Event Handlers
		private void OnStatusDownButtonClicked (object sender, EventArgs args)
		{
			Menu popupMenu = new Menu ();
			StatusMenuItem item;
			
			// Available messages
			string availMsg = Catalog.GetString ("Available");
			AddCustomAvailableMessage (availMsg);
			
			// Make sure the Available message is shown first
			item = new StatusMenuItem (new Presence (PresenceType.Available, availMsg));
			item.Activated += OnAvailableMessageSelected;
			popupMenu.Add (item);
			
			foreach (string customMessage in customAvailableMessages.Keys) {
				if (string.Compare (customMessage, availMsg) == 0)
					continue; // skip the available item since it's already at the top of the list
				item = new StatusMenuItem (new Presence (PresenceType.Available, customMessage));
				item.Activated += OnAvailableMessageSelected;
				popupMenu.Add (item);
			}
			
			item = new StatusMenuItem (
					new Presence (PresenceType.Available,
							Catalog.GetString ("Custom message...")));
			item.Activated += OnCustomAvailableMessageSelected;
			popupMenu.Add (item);
			
			popupMenu.Add (new SeparatorMenuItem ());
			
			// Busy messages
			string busyMsg = Catalog.GetString ("Busy");
			AddCustomBusyMessage (busyMsg);
			
			// Make sure the Busy message is shown first
			item = new StatusMenuItem (new Presence (PresenceType.Busy, busyMsg));
			item.Activated += OnBusyMessageSelected;
			popupMenu.Add (item);
			
			foreach (string customMessage in customBusyMessages.Keys) {
				if (string.Compare (customMessage, busyMsg) == 0)
					continue; // skip the busy item since it's already at the top of the list
				item = new StatusMenuItem (new Presence (PresenceType.Busy, customMessage));
				item.Activated += OnBusyMessageSelected;
				popupMenu.Add (item);
			}
			
			item = new StatusMenuItem (
					new Presence (
						PresenceType.Busy,
						Catalog.GetString ("Custom message...")));
			item.Activated += OnCustomBusyMessageSelected;
			popupMenu.Add (item);

			popupMenu.Add (new SeparatorMenuItem ());

			// Away messages
			string awayMsg = Catalog.GetString ("Away");
			AddCustomAwayMessage (awayMsg);
			
			// Make sure the Away message is shown first
			item = new StatusMenuItem (new Presence (PresenceType.Away, awayMsg));
			item.Activated += OnAwayMessageSelected;
			popupMenu.Add (item);
			
			foreach (string customMessage in customAwayMessages.Keys) {
				if (string.Compare (customMessage, awayMsg) == 0)
					continue; // skip the away item since it's already at the top of the list
				item = new StatusMenuItem (new Presence (PresenceType.Away, customMessage));
				item.Activated += OnAwayMessageSelected;
				popupMenu.Add (item);
			}
			
			item = new StatusMenuItem (
					new Presence (
						PresenceType.Away,
						Catalog.GetString ("Custom message...")));
			item.Activated += OnCustomAwayMessageSelected;
			popupMenu.Add (item);

			popupMenu.Add (new SeparatorMenuItem ());
			
			// Sign on/off of chat
//			Logger.Debug ("FIXME: Make this changed based on online/offline status");
//			item = new StatusMenuItem (new Presence (PresenceType.Offline, Catalog.GetString ("Sign out of chat")));
//			item.Activated += OnSignOnOffChatSelected;
//			popupMenu.Add (item);

			// Clear custom messages
			item = new StatusMenuItem (
					new Presence (
						PresenceType.Offline,
						Catalog.GetString ("Clear custom messages")));
			item.Activated += OnClearCustomMessagesSelected;
			popupMenu.Add (item);
			
			popupMenu.ShowAll();
			popupMenu.Popup ();
		}
		
		private void OnAvailableMessageSelected (object sender, EventArgs args)
		{
			StatusMenuItem item = sender as StatusMenuItem;
			Presence = new Presence (PresenceType.Available, item.Message);
		}

		private void OnCustomAvailableMessageSelected (object sender, EventArgs args)
		{
			SwitchToEditMode (PresenceType.Available);
		}

		private void OnBusyMessageSelected (object sender, EventArgs args)
		{
			StatusMenuItem item = sender as StatusMenuItem;
			Presence = new Presence(PresenceType.Busy, item.Message);
		}

		private void OnCustomBusyMessageSelected (object sender, EventArgs args)
		{
			SwitchToEditMode (PresenceType.Busy);
		}

		private void OnAwayMessageSelected (object sender, EventArgs args)
		{
			StatusMenuItem item = sender as StatusMenuItem;
			Presence = new Presence(PresenceType.Away, item.Message);
		}

		private void OnCustomAwayMessageSelected (object sender, EventArgs args)
		{
			SwitchToEditMode (PresenceType.Away);
		}

		private void OnSignOnOffChatSelected (object sender, EventArgs args)
		{
			Logger.Debug ("FIXME: Implement OnCustomAvailableMessageSelected");
		}

		private void OnClearCustomMessagesSelected (object sender, EventArgs args)
		{
			ClearCustomMessages ();
		}
		
		private void OnStatusEntryActivated (object sender, EventArgs args)
		{
			string potentialMessage = statusEntry.Text.Trim ();
			if (potentialMessage.Length > 0) {
				if (potentialPresenceType != PresenceType.Offline)
					Presence = new Presence (potentialPresenceType, potentialMessage);
				else
					Presence = new Presence (presence.Type, potentialMessage);
			}

			SwitchToViewMode ();
		}
		
		private void OnStatusEntryKeyPressEvent (object sender, KeyPressEventArgs args)
		{
			// If the user presses the Escape key, clear out the statusEntry
			// and switch back to view mode without doing anything else.
			if (args.Event.Key == Gdk.Key.Escape) {
				SwitchToViewMode ();
			}
		}

#endregion

#region Public Events
		public event StatusEntryChangedHandler PresenceChanged;
		public event EventHandler CustomMessagesCleared;
#endregion

#region Public Methods
		public void AddCustomAvailableMessage (string message)
		{
			if (message == null)
				return;
			
			message = message.Trim ();
			if (message.Length == 0)
				return;
			
			if (customAvailableMessages.ContainsKey (message) == false)
				customAvailableMessages [message] = message;
		}
		
		public void AddCustomBusyMessage (string message)
		{
			if (message == null)
				return;
			
			message = message.Trim ();
			if (message.Length == 0)
				return;
			
			if (customBusyMessages.ContainsKey (message) == false)
				customBusyMessages [message] = message;
		}

		public void AddCustomAwayMessage (string message)
		{
			if (message == null)
				return;
			
			message = message.Trim ();
			if (message.Length == 0)
				return;
			
			if (customAwayMessages.ContainsKey (message) == false)
				customAwayMessages [message] = message;
		}
		
		public void ClearCustomMessages ()
		{
			customAvailableMessages.Clear ();
			customBusyMessages.Clear ();
			customAwayMessages.Clear ();
			
			if (CustomMessagesCleared != null)
				CustomMessagesCleared (this, EventArgs.Empty);
		}
		
#endregion

#region Public Properties
		public Presence Presence
		{
			get { return Presence; }
			set {
				if (value == null)
					return;
				
				this.presence = value;
				statusLabel.Markup = string.Format (
					"<span style=\"italic\" size=\"small\">{0}</span>",
					presence.Message);
				
				switch (presence.Type) {
				case PresenceType.Available:
					AddCustomAvailableMessage (presence.Message);
					break;
				case PresenceType.Busy:
					AddCustomBusyMessage (presence.Message);
					break;
				case PresenceType.Away:
					AddCustomAwayMessage (presence.Message);
					break;
				default:
					Logger.Debug ("StatusEntry.Presence [set] called with an unimplemented PresenceType: {0}", presence.Type);
					// Default to use The PresenceType.Available so the rest of the code doesn't misbehave.
					presence.Type = PresenceType.Available;
					break;
				}
				
				if (PresenceChanged != null)
					PresenceChanged (presence);
			}
		}
#endregion

#region StatusMenuItem Class
		
		class StatusMenuItem : ImageMenuItem
		{
			Presence presence;
			
			public StatusMenuItem (Presence presence) :
				base (presence.Message)
			{
				this.presence = presence;
			}
			
			public string Message
			{
				get { return presence.Message; }
			}
			
			public PresenceType PresenceType
			{
				get { return presence.Type; }
			}
		}
#endregion
	}
}
