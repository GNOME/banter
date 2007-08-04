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
		//private Button showPersonDetailsButton;
		private VBox videoVBox;
		private VBox audioVBox;
		private VPaned messagesVPaned;
		private MessagesView messagesView;
		private VideoView videoView;
		private AudioView audioView;
		private VBox typingVBox;
		private Toolbar typingToolbar;
		private ScrolledWindow typingScrolledWindow;
		private TextView typingTextView;
		private ChatType chatType;
		private Person peerPerson;
		private ProviderUser peerProviderUser;
		private bool hasBeenShown;
		private bool notifyUser;
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

			/*
			switch(chatType) {
				case ChatType.Text:
					conv.AddTextChannel();
					break;
				case ChatType.Audio:
					conv.AddAudioChannel();
					//conv.AddTextChannel();
					break;
				case ChatType.Video:
					conv.AddAudioVideoChannels();
					//conv.AddTextChannel();
					break;
			}
			*/

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
			conv.MediaChannelClosed += OnMediaChannelClosed;
			
			//conv.VideoStreamUp += OnVideoStreamUp;
			
			conv.TextChannelOpened += OnTextChannelOpened;
			conv.MediaChannelOpened += OnMediaChannelOpened;
			
			conv.IncomingAudioCall += OnIncomingAudioCall;
			conv.IncomingVideoCall += OnIncomingVideoCall;
			conv.CallHangup += OnCallHangup;
		}


		///<summary>
		///	TearDownConversationEvents
		/// Tear down all conversationEvents
		///</summary>			
		void TearDownConversationEvents()
		{
			conv.MessageReceived -= OnTextMessageReceived;
			conv.MessageSent -= OnTextMessageSent;
			conv.MediaChannelClosed -= OnMediaChannelClosed;
			//conv.VideoStreamUp += OnVideoStreamUp;
			
			conv.TextChannelOpened -= OnTextChannelOpened;
			conv.MediaChannelOpened -= OnMediaChannelOpened;
			conv.IncomingAudioCall -= OnIncomingAudioCall;
			conv.IncomingVideoCall -= OnIncomingVideoCall;
			conv.CallHangup -= OnCallHangup;
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
			notifyUser = false;
			
			// Update the window title
			Title = string.Format ("Chat with {0}", peerPerson.DisplayName);
			Icon = Utilities.GetIcon ("banter-22", 22);	
			
			this.DefaultSize = new Gdk.Size (400, 700); 			
		
			hpaned = new HPaned ();
			hpaned.CanFocus = true;
			hpaned.Position = 300;
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
			
			//showPersonDetailsButton = new Button ();
			//showPersonDetailsButton.CanFocus = true;
			//showPersonDetailsButton.Label = Catalog.GetString ("Show Contact _Details");
			//showPersonDetailsButton.UseUnderline = true;
			//showPersonDetailsButton.Image = new Image (Stock.GoBack, IconSize.Menu);
			//showPersonDetailsButton.Show ();
			//hbox.PackStart (showPersonDetailsButton);
			
			videoVBox = new VBox (false, 0);
			if(this.chatType == ChatType.Video) {
				ShowVideoControl(true);
			} else {
				videoVBox.Visible = false;
			}

			rightPaneVBox.PackStart (videoVBox, false, true, 0);
			
			audioVBox = new VBox (false, 0);

			if(this.chatType == ChatType.Audio) {
				ShowAudioControl(true);
			} else {
				audioVBox.Visible = false;
			}

			rightPaneVBox.PackStart (audioVBox, false, false, 0);
			
			messagesVPaned = new VPaned ();
			messagesVPaned.CanFocus = true;
			// This is lame, fix the way this is all calculated
			if(videoView != null)
				messagesVPaned.Position = 100;
			else
				messagesVPaned.Position = 700;
			messagesVPaned.Show ();
			rightPaneVBox.PackStart (messagesVPaned, true, true, 0);
			
			Gtk.ScrolledWindow sw = new ScrolledWindow();
			sw.VscrollbarPolicy = PolicyType.Automatic;
			sw.HscrollbarPolicy = PolicyType.Never;
			//scrolledWindow.ShadowType = ShadowType.None;
			sw.BorderWidth = 0;
			sw.CanFocus = true;
			sw.Show ();
			
			messagesView = new MessagesView ();
			messagesView.Show ();
			sw.Add(messagesView);
			messagesVPaned.Pack1(sw, true, true);
			
			typingVBox = new VBox (false, 0);
			typingVBox.Show ();
			messagesVPaned.Pack2(typingVBox, false, false);
			
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
			this.FocusInEvent += FocusInEventHandler;
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

		public void FocusInEventHandler (object o, FocusInEventArgs args)
		{
			// remove the Urgency Hint if it was set
			this.UrgencyHint = false;
		}

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
			
			AddMessage (message, true, conversation.CurrentMessageSameAsLast, null);

			// if the window doesn't have focus, notify the user
			if( (notifyUser) &&(message is TextMessage) ) {
				if(hasBeenShown && (!HasToplevelFocus)) {
					this.UrgencyHint = true;
					NotificationManager.NotifyMessage(person, message);
				} else {
					Gnome.Sound.Play(System.IO.Path.Combine(Banter.Defines.SoundDir, "receive.wav"));
				}
			}
		}

		
		///<summary>
		///	ShowVideoControl
		/// Shows the Video Controls
		///</summary>
		private void ShowVideoControl(bool show)
		{
			if(show) {
				if(videoView != null) {
					videoView.Show();
				} else {
					videoView = new VideoView();
					videoView.EndVideoChat += OnEndVideo;				
					videoView.Show();				
					videoVBox.PackStart(videoView, false, false, 5);
				}
				videoVBox.Show();			
			} else {
				videoVBox.Hide();
				// we need to destroy this due to a preview issue
				if(videoView != null) {
					videoVBox.Remove(videoView);
					videoView = null;
				}
			}
		}


		///<summary>
		///	ShowAudioControl
		/// Shows the Audio controls
		///</summary>
		private void ShowAudioControl(bool show)
		{
			if(show) {
				if(audioView != null) {
					audioView.Show();
				} else {
					audioView = new AudioView();
					audioView.EndAudioChat += OnEndAudio;				
					audioView.Show();				
					audioVBox.PackStart(audioView, true, true, 5);
				}
				audioVBox.Show();
			} else {
				audioVBox.Hide();
			}
		}

		
		///<summary>
		///	OnEndVideo
		/// Ends the current video
		///</summary>
		private void OnEndVideo()
		{
			Logger.Debug("End the video conversation");

			conv.RemoveMediaChannel();
			ShowVideoControl(false);
		}
		
		
		///<summary>
		///	OnEndAudio
		/// Ends the current audio
		///</summary>
		private void OnEndAudio()
		{
			Logger.Debug("End the audio conversation");
			conv.RemoveMediaChannel();
			ShowAudioControl(false);
		}
		
		///<summary>
		///	OnCallHangup
		/// Ends the current Internet call
		///</summary>
		private void OnCallHangup (Conversation conversation, CallType calltype)
		{
			if (calltype == CallType.Audio)
				ShowAudioControl (false);
			else if (calltype == CallType.Video)
				ShowVideoControl (false);
		}
		
		///<summary>
		///	OnTextMessageSent
		/// Deals with all TextMessages sent
		///</summary>
		private void OnTextMessageSent (Conversation conversation, Message message)
		{
			Logger.Debug ("OnMessageSent called: {0}", message.Text);

			AddMessage (message, false, conversation.CurrentMessageSameAsLast, null);
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
				Logger.Debug("Window was destroyed, calling ConversationManager.Destroy on conversation");
				TearDownConversationEvents();		
				ConversationManager.Destroy(conv);
				conv = null;
			}
		}


		///<summary>
		///	OnMediaChannelOpened
		/// Handles a new Media Channel opening on the Conversation
		///</summary>	
		private void OnMediaChannelOpened (Conversation conversation)
		{
			Logger.Debug("OnMediaChannelOpened was called");
			Logger.Debug("FIXME: Prompt the idiot to accept right!");

			/*
			if (conversation.ActiveVideoStream == true ) {
				// If for some reason we already have a video view, get rid of it and start over
				if(this.videoView != null) ShowVideoControl(false);

				// Show the Video Control
				ShowVideoControl(true);
				conv.StartAudioVideoStreams (videoView.PreviewWindowId, videoView.WindowId);
			
			} else if (conversation.ActiveAudioStream == true) {
				conv.StartAudioStream ();		
			}
			*/
		}
		
		///<summary>
		///	OnIncomingAudioCall
		/// Called when an incoming audio call is detected
		///</summary>	
		private void OnIncomingAudioCall (Conversation conversation)
		{
			Logger.Debug ("OnIncomingAudio - called");
			
			// Remove then show the audio control
			ShowAudioControl (false);
			ShowAudioControl (true);
			conv.StartAudioStream ();		
		}


		///<summary>
		///	OnIncomingVideoCall
		/// Called when an incoming audio call is detected
		///</summary>	
		private void OnIncomingVideoCall (Conversation conversation)
		{
			Logger.Debug ("OnIncomingVideo - called");
			// If for some reason we already have a video view, get rid of it and start over
			if(this.videoView != null) ShowVideoControl(false);

			// Show the Video Control
			ShowVideoControl(true);
			conv.StartAudioVideoStreams (videoView.PreviewWindowId, videoView.WindowId);
		}

		///<summary>
		///	OnMediaChannelClosed
		/// Handles a new Media Channel closing on the Conversation
		///</summary>	
		private void OnMediaChannelClosed (Conversation conversation)
		{
			// A Media Channel was opened
			Logger.Debug("The Media Channel was close so we are going to close the view");

			ShowVideoControl(false);
			ShowAudioControl(false);			
		}
		

		///<summary>
		///	OnVideoStreamDown
		/// Called when the Video Stream goes down
		///</summary>
		private void OnVideoStreamDown(Conversation conversation)
		{
			// A Media Channel was opened
			Logger.Debug("The Video Stream went down, time to end.");

			ShowVideoControl(false);
				
			if(!conv.ActiveAudioStream)
				conv.RemoveMediaChannel();
		}


		///<summary>
		///	OnAudioStreamDown
		/// Called when the Audio Stream goes down
		///</summary>
		private void OnAudioStreamDown(Conversation conversation)
		{
			// A Media Channel was opened
			Logger.Debug("The Audio Stream went down, time to end.");

			ShowAudioControl(false);			
				
			if(!conv.ActiveAudioStream)
				conv.RemoveMediaChannel();
		}		
		
		
		///<summary>
		///	OnTextChannelOpened
		/// Called when a new text channel is opened
		///</summary>
		private void OnTextChannelOpened (Conversation conversation)
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
			
			TextMessage msg = new TextMessage (text, PersonManager.Me.ProviderUser);
			conv.SendMessage (msg);
			Gnome.Sound.Play(System.IO.Path.Combine(Banter.Defines.SoundDir, "send.wav"));
		}
		

		///<summary>
		///	OnVideoViewRealized
		/// Handles all setup of the window after it's been realized on the screen
		///</summary>			
		private void OnWindowShown (object sender, EventArgs args)
		{
			if(hasBeenShown)
				return;
			
			SetupConversationEvents();

			switch(chatType) {
				default:
				case ChatType.Text:
					// do nothing, text doesn't need to setup streams
					break;
				case ChatType.Audio:
					Logger.Debug("ChatWindow setting up video windows and calling StartAudioStream");
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

			notifyUser = false;		
			// Check for any existing messages including previous chat log
			Message[] messages = conv.GetReceivedMessages();
			foreach (Message msg in messages)
				OnTextMessageReceived (conv, msg);
			notifyUser = true;

			hasBeenShown = true;
				
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


		///<summary>
		///	Present
		/// Presents the window
		///</summary>			
		public void UpdateChatType (ChatType type)
		{
			this.chatType = type;
			
			switch(chatType) {
				default:
				case ChatType.Text:
					// do nothing, text doesn't need to setup streams
					break;
				case ChatType.Audio:
					if(!conv.ActiveAudioStream) {
						Logger.Debug("No active Audio Stream, adding video stream");					
						ShowAudioControl(true);
						conv.AddAudioChannel();
						conv.StartAudioStream();
					}
					break;
				case ChatType.Video:
					if(!conv.ActiveVideoStream) {
						Logger.Debug("No active Video Stream, adding video stream");
						ShowVideoControl(true);
						conv.AddAudioVideoChannels();
						conv.StartAudioVideoStreams(videoView.PreviewWindowId, videoView.WindowId);						
					}
					break;
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
