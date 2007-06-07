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
	///<summary>
	///	ChatWindow
	/// The window used to handle all text, audio, and video chatting in Banter
	///</summary>
	public class ChatWindow : Gtk.Window 
	{
		#region Private Types
		private Conversation conv;
		private bool everShown;
		private bool shiftKeyPressed;
		
		// Widgets
		private HPaned hpaned;
		private VBox leftPaneVBox;
		private VBox rightPaneVBox;
		private VBox personControlVBox;
		private Button showPersonDetailsButton;
		private VBox videoVBox;
		private VBox audioVBox;
		private VPaned messagesVPaned;
		private MessagesView messagesView;
		private VideoView videoView;
		private VBox typingVBox;
		private Toolbar typingToolbar;
		private ScrolledWindow typingScrolledWindow;
		private TextView typingTextView;
		private ChatType chatType;
		private Person peerPerson;
		private ProviderUser peerProviderUser;
		private bool hasBeenShown;
		#endregion
		

		#region Constructors
		///<summary>
		///	Constructor
		/// Creates a ChatWindow and a conversation according to the type requested
		///</summary>		
		public ChatWindow (Person person, ProviderUser providerUser, ChatType type) : 
			base (WindowType.Toplevel)
		{
			Logger.Debug("ChatWindow is being created with the ChatType: {0}", type.ToString());
			this.chatType = type;
			conv = ConversationManager.Create(providerUser);

			switch(chatType) {
				case ChatType.Text:
					conv.AddTextChannel();
					break;
				case ChatType.Audio:
					conv.AddAudioChannel();
					conv.AddTextChannel();
					break;
				case ChatType.Video:
					conv.AddAudioVideoChannels();
					conv.AddTextChannel();
					break;
			}

			peerPerson = person;
			peerProviderUser = providerUser;
			
			InitWindow();
		}
		
		
		///<summary>
		///	Constructor
		/// Creates a ChatWindow based on an existing conversation.  This mainly used on
		/// incoming conversations.
		///</summary>		
		public ChatWindow (Conversation conversation, ChatType type) :
			base (WindowType.Toplevel)
		{
			Logger.Debug("ChatWindow is being created with the ChatType: {0}", type.ToString());
			
			this.chatType = type;
			conv = conversation;

			// no need to Add any channels, they will be set up already

			peerProviderUser = conv.PeerUser;
			peerPerson = PersonManager.GetPerson(peerProviderUser);
			
			InitWindow();
		}		
		#endregion


		#region Private Methods
		///<summary>
		///	SetupConversationEvents
		/// Connects all conversation event handlers
		///</summary>			
		void SetupConversationEvents()
		{
			conv.MessageReceived += OnTextMessageReceived;
			conv.MessageSent += OnTextMessageSent;
			
			conv.NewAudioChannel += OnNewAudioChannel;
			conv.NewVideoChannel += OnNewVideoChannel;
			conv.NewTextChannel += OnNewTextChannel;			
		}
		
		
		///<summary>
		///	InitWindow
		/// Sets up the widgets and events in the chat window
		///</summary>	
		void InitWindow()
		{
			hasBeenShown = false;
			everShown = false;
			shiftKeyPressed = false;
			
			// Update the window title
			Title = string.Format ("Chat with {0}", peerPerson.DisplayName);	
			
			this.DefaultSize = new Gdk.Size (400, 600); 			
		
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
			
			PersonCard card = new PersonCard(peerPerson);
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
			if(this.chatType == ChatType.Video) {
				videoView = new VideoView();
				videoView.WidthRequest = 500; //250;
				videoView.HeightRequest = 375; //187;
				videoView.Show();
				videoVBox.PackStart(videoView, true, true, 0);
				videoVBox.Show();
			} else {
				videoVBox.Visible = false;
			}

			rightPaneVBox.PackStart (videoVBox, false, false, 0);
			
			audioVBox = new VBox (false, 0);
			audioVBox.Visible = false;
			rightPaneVBox.PackStart (audioVBox, false, false, 0);
			
			messagesVPaned = new VPaned ();
			messagesVPaned.CanFocus = true;
			// This is lame, fix the way this is all calculated
			if(videoView != null)
				messagesVPaned.Position = 100;
			else
				messagesVPaned.Position = 400;
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
			
			Shown += OnWindowShown;
			DeleteEvent += WindowDeleted;			
		}
	
	
		///<summary>
		///	AddMessage
		/// Addes messages to the messagesView
		///</summary>		
		private void AddMessage (Message message, bool incoming, bool contentIsSimilar, string avatarPath)
		{
			messagesView.AddMessage (message, incoming, contentIsSimilar, avatarPath);
		}	
		#endregion


		#region EventHandlers
		///<summary>
		///	OnTextMessageReceived
		/// Handles all incoming TextMessages and places them into the text chat area
		///</summary>
		private void OnTextMessageReceived (Conversation conversation, Message message)
		{
			string avatarPath = null;
			Logger.Debug ("OnMessageReceived called: {0}", message.Text);
			Logger.Debug ("Peer Handle: {0}", conversation.PeerUser.ID);
			Logger.Debug ("Peer Screenname: {0}", conversation.PeerUser.Uri);
			Logger.Debug ("Sender: {0}", conversation.PeerUser.Alias);
			
			Person person = null;
			try {
				person = PersonManager.GetPerson (conversation.PeerUser);
			} catch{}
			
			if(person != null)
				avatarPath = person.GetScaledAvatar(36);
			
			AddMessage (message, true, conversation.CurrentMessageSameAsLast, avatarPath);
		}
		

		///<summary>
		///	OnTextMessageSent
		/// Deals with all TextMessages sent
		///</summary>
		private void OnTextMessageSent (Conversation conversation, Message message)
		{
			string avatarPath = null;
			Logger.Debug ("OnMessageSent called: {0}", message.Text);

			/*
			Person person = PersonManager.GetPersonByJabberId(conversation.MeContact.Uri);
			if(person != null)
				avatarPath = person.GetScaledAvatar(36);
			*/
			AddMessage (message, false, conversation.CurrentMessageSameAsLast, avatarPath);
		}


		///<summary>
		///	OnTypingTextViewKeyPressEvent
		/// Handles typing events in the ChatWindow
		///</summary>	
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
					if(!HandleMessageTrigger())
						SendMessage ();
					// Prevent the event from passing on to the TextView
					retVal = true;
				}
				
				break;
			}
			
			args.RetVal = retVal;
		}


		///<summary>
		///	OnTypingTextViewKeyReleaseEvent
		/// Handles typing events in the ChatWindow
		///</summary>	
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
		
		
		///<summary>
		///	WindowDeleted
		/// Cleans up the conversation object with the ConversationManager
		///</summary>	
		private void WindowDeleted (object sender, DeleteEventArgs args)
		{
			if (conv != null) {
				Logger.Debug("FIXME: Call ConversationManager to clean up conversation");
				// ConversationManager.DestroyConversation(conv);
			}
		}
		

		///<summary>
		///	OnNewAudioChannel
		/// Handles the new Audio Channel event on a conversation
		///</summary>
		private void OnNewAudioChannel (Conversation conversation)
		{
			// code goes here
		}
		
		
		///<summary>
		///	OnNewVideoChannel
		/// Handles the new Video Channel event on a conversation
		///</summary>
		private void OnNewVideoChannel (Conversation conversation)
		{
			// code goes here
		}
		
		
		///<summary>
		///	OnNewTextChannel
		/// Handles the new Text Channel event on a conversation
		///</summary>
		private void OnNewTextChannel (Conversation conversation)
		{
			// code goes here
		}

		
		///<summary>
		///	HandleMessageTrigger
		/// Handles all message triggers in the Text Chat window
		///</summary>	
		private bool HandleMessageTrigger()
		{
			bool handled = false;
			
			string text = typingTextView.Buffer.Text;
			if(text.StartsWith("/status ")) {
				string statusText = text.Substring(8);
				if(statusText.Length > 0) {
					if(PersonManager.Me != null) {
						Presence presence = new Presence(PersonManager.Me.Presence.Type);
						presence.Message = statusText;
						PersonManager.Me.SetStatus(presence);
					}
				}
				typingTextView.Buffer.Clear ();
				handled = true;
			}
			
			return handled;
		}
		

		///<summary>
		///	SendMessage
		/// Sends Messages
		///</summary>			
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
		

		///<summary>
		///	OnVideoViewRealized
		/// Handles all setup of the window after it's been realized on the screen
		///</summary>			
		private void OnWindowShown (object sender, EventArgs args)
		{
			if(hasBeenShown)
				return;
			
			hasBeenShown = true;
			
			SetupConversationEvents();

			switch(chatType) {
				default:
				case ChatType.Text:
					// do nothing, text doesn't need to setup streams
					break;
				case ChatType.Audio:
					Logger.Debug("ChatWindow setting up video windows and calling StartAudioVideoStreams");
					conv.StartAudioStream();
					break;
				case ChatType.Video:
					if(this.videoView != null) {
						Logger.Debug("ChatWindow setting up video windows and calling StartAudioVideoStreams");
						conv.StartAudioVideoStreams(videoView.PreviewWindowId, videoView.WindowId);
					} else {
						Logger.Debug("ChatWindow didn't have a videoWindow created");
					}
					break;
			}		
		
			// Check for any existing messages including previous chat log
			Message[] messages = conv.GetReceivedMessages();
			foreach (Message msg in messages)
				OnTextMessageReceived (conv, msg);
				
			// Set the default focus to the TextView where users should type
			typingTextView.GrabFocus ();
		}
		#endregion


		#region Public Methods
		///<summary>
		///	Present
		/// Presents the window
		///</summary>			
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
		///<summary>
		///	PeerProviderUserID
		/// The Id of the peer that is part of this chat
		///</summary>		
		public uint PeerProviderUserID
		{
			get { return this.peerProviderUser.ID; }
		}


		///<summary>
		///	PreviewWindowId
		/// Obsolete: the Id of the PreviewWindow
		///</summary>			
		public uint PreviewWindowId
		{
			get 
			{
				Logger.Debug("FIXME: Take this property out, nobody needs and you shouldn't be calling it");
				return videoView.PreviewWindowId; 
			}
		}


		///<summary>
		///	VideoWindowId
		/// Obsolete: the Id of the VideoWindow
		///</summary>		
		public uint VideoWindowId
		{
			get
			{ 
				Logger.Debug("FIXME: Take this property out, nobody needs and you shouldn't be calling it");
				return videoView.WindowId; 
			}
		}	
		#endregion

	}
}
