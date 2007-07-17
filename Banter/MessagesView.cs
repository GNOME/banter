//***********************************************************************
// *  $RCSfile$ - MessagesView.cs
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
using Gdk;

namespace Banter
{
	public class MessagesView : Gtk.TextView
	{
		#region Public Constructors
		public MessagesView() : base ()
		{
			this.Editable = false;
			this.WrapMode = Gtk.WrapMode.Word;
			SetupTextBufferTags();
		}
		#endregion

		#region Private Methods
		private void SetupTextBufferTags()
		{
			Gtk.TextTag tag = new Gtk.TextTag("time");
			tag.Foreground = "darkgrey";
			//tag.Justification = Justification.Right;
			this.Buffer.TagTable.Add(tag);

			tag = new Gtk.TextTag("incoming");
			tag.Foreground = "darkgreen";
			tag.Weight = Pango.Weight.Bold;
			this.Buffer.TagTable.Add(tag);

			tag = new Gtk.TextTag("outgoing");
			tag.Foreground = "darkblue";
			tag.Weight = Pango.Weight.Bold;
			this.Buffer.TagTable.Add(tag);

			tag = new Gtk.TextTag("system");
			tag.Foreground = "darkgrey";
			tag.Style = Pango.Style.Italic;
			this.Buffer.TagTable.Add(tag);
		}

		private void AddTaggedString(string tag, string date)
		{
			TextIter insertIter, beginIter, endIter;
			int begin, end;

			begin = this.Buffer.CharCount;
			insertIter = this.Buffer.GetIterAtMark(
					this.Buffer.InsertMark);
			this.Buffer.Insert (insertIter, date);
			end = this.Buffer.CharCount;
			endIter = this.Buffer.GetIterAtOffset(end);
			beginIter = this.Buffer.GetIterAtOffset(begin);
			this.Buffer.ApplyTag (tag, beginIter, endIter);
		}

		private void AddPicture(Pixbuf pixbuf)
		{
			TextIter insertIter;

			if(pixbuf != null)
			{
				insertIter = this.Buffer.GetIterAtMark( 
						this.Buffer.InsertMark);
				this.Buffer.InsertPixbuf (ref insertIter, pixbuf);
			}
			else
				Console.WriteLine("THE pixbuf was null!");
		}


		#endregion

		#region Public Methods
		public void AddMessage (Message message, bool incoming, bool contentIsSimilar, string avatarPath)
		{
			AddTaggedString("time", String.Format("({0:t}) ", message.Creation));

			if (message is TextMessage) {
				string formatter;
				if(incoming)
					formatter = "incoming";
				else
					formatter = "outgoing";
				if(message.Sender != null)
					AddTaggedString(formatter, message.Sender.Alias);
				else
					AddTaggedString(formatter, "Unknown");
				this.Buffer.Insert(this.Buffer.EndIter, " ");
				this.Buffer.Insert(this.Buffer.EndIter, message.Text);
				this.Buffer.Insert(this.Buffer.EndIter, "\r");
			} else if (message is SystemMessage) {
//				this.Buffer.Insert(this.Buffer.EndIter, "\r");
				AddTaggedString("system", "System Message ");
				AddTaggedString("system", message.Text);
				this.Buffer.Insert(this.Buffer.EndIter, "\r");
			} else {
				Console.WriteLine ("FIXME: Deal with unknown message type"); 
			}
		}
		#endregion

		#region Public Properties
		#endregion
	}
}
/*
	public class MessagesView : Gecko.WebControl
	{
		MessageStyle messageStyle;
		string messageStyleVariant;
		CommandManager jsCall;
		
#region Public Constructors
		public MessagesView() : base ()
		{
			string variant = Preferences.Get (Preferences.SelectedMessageStyleVariant) as string;
			Init (ThemeManager.MessageStyle, variant); 
		}
		
		public MessagesView (MessageStyle messageStyle, string messageStyleVariant) : base ()
		{
			Init (messageStyle, messageStyleVariant);
		}
#endregion
		
#region Private Methods
		private void Init (MessageStyle messageStyle, string messageStyleVariant)
		{
			this.messageStyle = messageStyle;
			this.messageStyleVariant = messageStyleVariant;
			
			OpenUri += OnLinkClicked;
			Realized += OnWebControlRealized;
		}
		
		private string SubstituteKeywords (string html, Dictionary<string, string> keywords)
		{
			foreach (string key in keywords.Keys) {
				string value = keywords [key];
				
				html = Utilities.ReplaceString (html, key, value, true);
			}
			
			return html;
		}
		
		private void AppendHtmlMessage (string html)
		{
			if (html == null || jsCall == null)
				return;
			string javascript = string.Format (
					"appendMessage('{0}');",
					html);
			jsCall.JSEval (javascript);
		}
		
		private void AppendNextHtmlMessage (string html)
		{
			if (html == null || jsCall == null)
				return;
			string javascript = string.Format (
				"appendNextMessage('{0}');",
				html);
			jsCall.JSEval (javascript);
		}
#endregion

#region EventHandlers
		private void OnLinkClicked (object sender, OpenUriArgs args)
		{
			Console.WriteLine ("URL Clicked: {0}", args.AURI);
		}
		
		private void OnWebControlRealized (object sender, EventArgs args)
		{
			LoadUrl ("file://" + messageStyle.GetTemplateHtmlPath (messageStyleVariant));
			jsCall = new CommandManager (this);
		}
#endregion

#region Public Methods
		public void AddMessage (Message message, bool incoming, bool contentIsSimilar, string avatarPath)
		{
			Dictionary<string, string> keywords = new Dictionary<string,string> ();
			
			// Escape the apostrophes in the message
			string escapedMessage = Utilities.EscapeForJavaScript (message.Text);
			keywords [MessageStyle.MESSAGE_KEYWORD] = escapedMessage;
			Console.WriteLine ("FIXME: Message should have a DateTime stamp.  Using DateTime.Now instead");
			keywords [MessageStyle.TIME_KEYWORD] = message.Creation.ToString ();
			keywords [MessageStyle.SERVICE_KEYWORD] = String.Empty;
			Console.WriteLine ("FIXME: Message should include either Member/Person object to get a display name from");
			keywords [MessageStyle.SENDER_KEYWORD] = message.From;
			Console.WriteLine ("FIXME: Get the path to the buddy icon");
			if(avatarPath != null) {
				keywords [MessageStyle.USER_ICON_PATH_KEYWORD] = avatarPath;
			}
			
			string rawHtml = null;
			if (message is TextMessage) {
				
				if (incoming) {
					// Incoming message
					if (contentIsSimilar) {
						Console.WriteLine ("AddMessage : Incoming, Similar");
						rawHtml = messageStyle.NextContentInHtml;
					} else {
						Console.WriteLine ("AddMessage : Incoming, New");
						rawHtml = messageStyle.ContentInHtml;
					}
				} else {
					// Outgoing message
					if (contentIsSimilar) {
						Console.WriteLine ("AddMessage : Outgoing, Similar");
						rawHtml = messageStyle.NextContentOutHtml;
					} else {
						Console.WriteLine ("AddMessage : Outgoing, New");
						rawHtml = messageStyle.ContentOutHtml;
					}
				}
			} else if (message is SystemMessage) {
				rawHtml = messageStyle.StatusHtml;
				contentIsSimilar = false;
			} else {
				Console.WriteLine ("FIXME: Deal with unknown message type"); 
			}
			
			string html = null;
			try {
				html = SubstituteKeywords (rawHtml, keywords);
			} catch (Exception e) {
				Console.WriteLine ("Error during HTML keyword substitution: {0}\n{1}",
						e.Message,
						e.StackTrace);
			}
			
			if (html != null) {
				if (contentIsSimilar)
					AppendNextHtmlMessage (html);
				else
					AppendHtmlMessage (html);
			}
		}
		
		public void SetMessageStyle (MessageStyle style, string variant)
		{
			messageStyle = style;
			messageStyleVariant = variant;
			
			// Reload
			LoadUrl ("file://" + messageStyle.GetTemplateHtmlPath (messageStyleVariant));
		}
#endregion

#region Public Properties
#endregion
	}
}

*/