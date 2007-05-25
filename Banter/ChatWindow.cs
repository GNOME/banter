//***********************************************************************
// *  $RCSfile$ - ChatWindow.cs
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
	public class ChatWindow : Gtk.Window 
	{
		//Application app;
		
		Conversation conv;
		bool everShown;
		//Person lastSender;
		bool shiftKeyPressed;
		
		// Internal Widgets
		HPaned hpaned;
		VBox leftPaneVBox;
		VBox rightPaneVBox;
		VBox personControlVBox;
		Button showPersonDetailsButton;
		VBox videoVBox;
		VBox audioVBox;
		VPaned messagesVPaned;
		MessagesView messagesView;
		VBox typingVBox;
		Toolbar typingToolbar;
		ScrolledWindow typingScrolledWindow;
		TextView typingTextView;
		
		static private Dictionary <string, ChatWindow> chatWindows;
		
		static ChatWindow()
		{
			Logger.Debug ("ChatWindow::ChatWindow - static constructor called");
			chatWindows = new Dictionary<string,ChatWindow> ();
		}
		
//		public ChatWindow(Conversation conversation) :
		public ChatWindow (Conversation conversation) :
			base (WindowType.Toplevel)
		{
			//app = Application.Instance;
			
			conv = conversation;
			conv.MessageReceived += OnTapiocaMessageReceived;
			conv.MessageSent += OnTapiocaMessageSent;
			
			everShown = false;
			//lastSender = null;
			shiftKeyPressed = false;

			Person peer = PersonStore.GetPersonByJabberId (conv.PeerUser.Uri);
			chatWindows[peer.Id] = this;
			
			// Update the window title
			if (peer.DisplayName != null)
				Title = string.Format ("Chat with {0}", peer.DisplayName);
			
			this.DefaultSize = new Gdk.Size (400, 800); 
			
			SetUpWidgets (peer);
			Realized += WindowRealized;
			DeleteEvent += WindowDeleted;
			
		}
		
#region Private Methods
		void SetUpWidgets (Person peer)
		{
			hpaned = new HPaned ();
			hpaned.CanFocus = true;
			hpaned.Position = 220;
			hpaned.Show ();
			
			this.Add (hpaned);
			
			leftPaneVBox = new VBox ();
			leftPaneVBox.NoShowAll = true;
			leftPaneVBox.Visible = false;
			hpaned.Add1 (leftPaneVBox);
			
			rightPaneVBox = new VBox ();
			rightPaneVBox.BorderWidth = 8;
			rightPaneVBox.Show ();
			hpaned.Add2 (rightPaneVBox);
			
			personControlVBox = new VBox (false, 4);
			personControlVBox.Show ();
			rightPaneVBox.PackStart (personControlVBox, false, false, 0);
			
			PersonCard card = new PersonCard(peer);
			card.Size = PersonCardSize.Medium;
			// Not sure why but we need to call ShowAll on the PersonCard for it to display
			card.ShowAll();
			personControlVBox.PackStart (card, true, true, 0);
			
			HBox hbox = new HBox (false, 0);
			hbox.Show ();
			personControlVBox.PackStart (hbox, false, false, 0);
			
			showPersonDetailsButton = new Button ();
			showPersonDetailsButton.CanFocus = true;
			showPersonDetailsButton.Label = Catalog.GetString ("Show Contact _Details");
			showPersonDetailsButton.UseUnderline = true;
			showPersonDetailsButton.Image = new Image (Stock.GoBack, IconSize.Menu);
			showPersonDetailsButton.Show ();
			hbox.PackStart (showPersonDetailsButton);
			
			videoVBox = new VBox (false, 0);
			videoVBox.Visible = false;
			rightPaneVBox.PackStart (videoVBox, false, false, 0);
			
			audioVBox = new VBox (false, 0);
			audioVBox.Visible = false;
			rightPaneVBox.PackStart (audioVBox, false, false, 0);
			
			
			messagesVPaned = new VPaned ();
			messagesVPaned.CanFocus = true;
			messagesVPaned.Position = 600;
			messagesVPaned.Show ();
			rightPaneVBox.PackStart (messagesVPaned, true, true, 0);
			
			messagesView = new MessagesView ();
			messagesView.Show ();
			messagesVPaned.Add1 (messagesView);
			
			typingVBox = new VBox (false, 0);
			typingVBox.Show ();
			messagesVPaned.Add2 (typingVBox);
			
			typingToolbar = new Toolbar ();
			typingToolbar.ShowArrow = false;
			typingToolbar.ToolbarStyle = ToolbarStyle.Icons;
			typingToolbar.IconSize = IconSize.SmallToolbar;
			typingToolbar.Show ();
			typingVBox.PackStart (typingToolbar, false, false, 0);
			
			typingScrolledWindow = new ScrolledWindow ();
			typingScrolledWindow.VscrollbarPolicy = PolicyType.Automatic;
			typingScrolledWindow.HscrollbarPolicy = PolicyType.Automatic;
			typingScrolledWindow.ShadowType = ShadowType.EtchedIn;
			typingScrolledWindow.CanFocus = true;
			typingScrolledWindow.Show ();
			typingVBox.PackStart (typingScrolledWindow, true, true, 0);
			
			typingTextView = new TextView ();
			typingTextView.CanFocus = true;
			typingTextView.WrapMode = WrapMode.Word;
			typingTextView.LeftMargin = 4;
			typingTextView.RightMargin = 4;
			typingTextView.KeyPressEvent += OnTypingTextViewKeyPressEvent;
			typingTextView.KeyReleaseEvent += OnTypingTextViewKeyReleaseEvent;
			typingTextView.Show ();
			typingScrolledWindow.Add (typingTextView);
		}
#endregion

#region EventHandlers

		/*
		// When this event is received, the message should be added to the GUI.
		// Additionally, depending on the user preferences, the status of the
		// window should changes so that it blinks/etc. as desired.
		void OnMessageReceived (Conversation conversation, Message message)
		{
Logger.Debug ("OnMessageReceived called: {0}", message.Text);
			// FIXME: This should eventually pull the from off of the Message
			Person sender = null;
			try {
				sender = app.GetPerson (conversation.Peer);
			} catch (Exception e) {
				Console.WriteLine ("Application.GetPerson () threw an exception: {0}\n{1}", e.Message, e.StackTrace);
			}
			
			Logger.Debug ("Peer Handle: {0}", conversation.Peer.Handle);
			Logger.Debug ("Peer Screenname: {0}", conversation.Peer.ScreenName);
			Logger.Debug ("Sender: {0}", sender.DisplayName);
			Logger.Debug ("lastSender: {0}", lastSender == null ? "null" : lastSender.DisplayName);
			Logger.Debug ("Sender is Self? {0}", sender.IsSelf);
			
			bool contentIsSimilar =
				(lastSender == null || lastSender != sender || message is TextMessage == false) ?
					false : true;
			AddMessage (message, true, contentIsSimilar);
			lastSender = sender;
		}
		*/
		
		// When this event is received, the message should be added to the GUI.
		// Additionally, depending on the user preferences, the status of the
		// window should changes so that it blinks/etc. as desired.
		void OnTapiocaMessageReceived (Conversation conversation, Message message)
		{
			string avatarPath = null;
			Logger.Debug ("OnMessageReceived called: {0}", message.Text);
			Logger.Debug ("Peer Handle: {0}", conversation.PeerUser.ID);
			Logger.Debug ("Peer Screenname: {0}", conversation.PeerUser.Uri);
			Logger.Debug ("Sender: {0}", conversation.PeerUser.Alias);
			
			Person person = null;
			try {
				person = PersonStore.GetPerson (conversation.PeerUser);
			} catch{}
			
			if(person != null)
				avatarPath = person.GetScaledAvatar(36);
			
			AddMessage (message, true, conversation.CurrentMessageSameAsLast, avatarPath);
		}
		


		void OnTapiocaMessageSent (Conversation conversation, Message message)
		{
			string avatarPath = null;
Logger.Debug ("OnMessageSent called: {0}", message.Text);

			/*
			Person person = PersonStore.GetPersonByJabberId(conversation.MeContact.Uri);
			if(person != null)
				avatarPath = person.GetScaledAvatar(36);
			*/
			AddMessage (message, false, conversation.CurrentMessageSameAsLast, avatarPath);
		}
		
		void AddMessage (Message message, bool incoming, bool contentIsSimilar, string avatarPath)
		{
			messagesView.AddMessage (message, incoming, contentIsSimilar, avatarPath);
		}

		[GLib.ConnectBeforeAttribute]
		protected virtual void OnTypingTextViewKeyPressEvent(object sender, KeyPressEventArgs args)
		{
			// Keep track of when the SHIFT key is down
			// If the user presses ENTER, send the message inside the textView
			// If the user presses SHIFT+ENTER, insert a newline
			
			// Allow the event to pass through to the TextView
			bool retVal = false;
			
			switch (args.Event.Key) {
			case Gdk.Key.Shift_L:
			case Gdk.Key.Shift_R:
				shiftKeyPressed = true;
				break;
			case Gdk.Key.Return:
				if (!shiftKeyPressed) {
					SendMessage ();
					// Prevent the event from passing on to the TextView
					retVal = true;
				}
				
				break;
			}
			
			args.RetVal = retVal;
		}

		[GLib.ConnectBeforeAttribute]
		protected virtual void OnTypingTextViewKeyReleaseEvent(object sender, KeyReleaseEventArgs args)
		{
			// Allow the event to pass through to the TextView
			bool retVal = false;
			
			switch (args.Event.Key) {
			case Gdk.Key.Shift_L:
			case Gdk.Key.Shift_R:
				shiftKeyPressed = false;
				break;
			}

			args.RetVal = retVal;
		}
		
		private void WindowDeleted (object sender, DeleteEventArgs args)
		{
			if (conv != null) {
				/*
				conv.MessageReceived -= OnMessageReceived;
				conv.MessageSent -= OnMessageSent;
				*/
			}
		}
		
		private void SendMessage ()
		{
			/*
			if (conv == null) {
				Logger.Info ("This window does not have a valid Conversation and cannot be used to chat with yet.");
				return;
			}
			*/
			
			// Grab the text from the TextBuffer and clear it out
			string text = typingTextView.Buffer.Text;
			typingTextView.Buffer.Clear ();
			
			TextMessage msg = new TextMessage (text);
			conv.SendMessage (msg);
		}
		
		private void WindowRealized (object sender, EventArgs args)
		{
			// Check for any existing messages
			Message[] messages = conv.GetReceivedMessages();
			foreach (Message msg in messages)
				OnTapiocaMessageReceived (conv, msg);
				
			// Set the default focus to the TextView where users should type
			typingTextView.GrabFocus ();
		}
#endregion

#region Public Methods
		public new void Present ()
		{
			if (everShown == false) {
				Show ();
				everShown = true;
			} else {
				base.Present ();
			}
		}
#endregion

#region Public Properties
		public Conversation Conversation
		{
			get { return conv; }
			
			/*
			set {
				if (conv != null) {
					// Unregister event handlers on the old conversation
					conv.MessageReceived -= OnTapiocaMessageReceived;
					conv.MessageSent -= OnTapiocaMessageSent;
				}
				
				if (value != null) {
					conv = value;
					conv.MessageReceived += OnTapiocaMessageReceived;
					conv.MessageSent += OnTapiocaMessageSent;
					
					// Update the window title
					try {
						Person peer = 
							Application.Instance.GetPersonFromContact (conv.PeerContact);
						Title = string.Format ("Chat with {0}", peer.DisplayName);
					} catch {}
				}
			}
			*/
		}
#endregion

#region Static Public Properties

		static public bool AlreadyExist (string peerId)
		{
			Logger.Debug ("ChatWindow::AlreadyExists - called");
			if (ChatWindow.chatWindows.ContainsKey (peerId)) return true;
			return false;
		}

		static public bool PresentWindow (string peerId)
		{
			if (chatWindows.ContainsKey (peerId)) {
				chatWindows[peerId].Present ();
				return true;
			}
			
			return false;
		}
		
#endregion
	}
}
