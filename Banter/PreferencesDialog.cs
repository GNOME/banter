//***********************************************************************
// *  $RCSfile$ - PreferencesDialog.cs
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
using Gtk;
using Mono.Unix;
using GConf.PropertyEditors;

namespace Banter
{
	public class PreferencesDialog : Gtk.Dialog
	{
		// Widgets used for the General Tab
		
		// Widgets used for the Messages Tab
//		MessagesView messagesView;
//		ComboBox messageStyleComboBox;
//		ComboBox variantComboBox;
		
		// Widgets used for the Accounts Tab
		Entry serverAddressEntry;
		Entry serverPortEntry;
		Entry usernameEntry;
		Entry passwordEntry;
		
//		Entry sipServerAddressEntry;
//		Entry sipUsernameEntry;
//		Entry sipPasswordEntry;
		
		public PreferencesDialog() : base ()
		{
			SetDefaultSize (600, 600);
			WindowPosition = WindowPosition.Center;
			IconName = "rtc";
			HasSeparator = false;
			BorderWidth = 5;
			Resizable = true;
			Title = Catalog.GetString ("Banter Preferences");
			
			VBox.Spacing = 5;
			ActionArea.Layout = ButtonBoxStyle.End;
			
			// Notebook Tabs (General, Messages)...
			Gtk.Notebook notebook = new Notebook ();
			notebook.TabPos = PositionType.Top;
			notebook.BorderWidth = 5;
			notebook.Show ();
			
//			notebook.AppendPage (MakeGeneralPage (),
//									new Label (Catalog.GetString ("General")));
			notebook.AppendPage (MakeAccountsPage (),
									new Label (Catalog.GetString ("Accounts")));
			notebook.AppendPage (MakeMessagesPage (),
									new Label (Catalog.GetString ("Messages")));
			
			VBox.PackStart (notebook, true, true, 0);
			
			// Close button...
			Button button = new Button (Stock.Close);
			button.CanDefault = true;
			button.Show ();
			
			AccelGroup accelGroup = new AccelGroup ();
			AddAccelGroup (accelGroup);
			
			button.AddAccelerator ("activate",
									accelGroup,
									(uint) Gdk.Key.Escape,
									0,
									0);
			
			AddActionWidget (button, ResponseType.Close);
			DefaultResponse = ResponseType.Close;
			
			Realized += DialogRealized;
			
			Preferences.PreferenceChanged += PreferenceChanged;
			
			ShowAll ();
		}
		
#region Private Methods
//		private Widget MakeGeneralPage ()
//		{
//			return new Label ("FIXME: Implement the General Page in the Preferences Dialog");
//		}
		
		private Widget MakeMessagesPage ()
		{
			//PropertyEditorEntry peditorEntry;
			//PropertyEditorBool peditorBool;

			VBox vbox = new VBox (false, 0);
			return vbox;
		}
		
		private Widget MakeAccountsPage ()
		{
			VBox vbox = new VBox (false, 0);
			vbox.BorderWidth = 8;
			
			vbox.PackStart (MakeGoogleTalkPreferences (), false, false, 0);
			//vbox.PackStart (MakeSipPreferences (), false, false, 0);
			
			vbox.Show ();
			return vbox;
		}
		
		private Widget MakeGoogleTalkPreferences ()
		{
			PropertyEditor peditor;
			
			VBox vbox = new VBox (false, 4);
			
			Label label = MakeLabel (
					string.Format (
						"<span size=\"larger\" weight=\"bold\">{0}</span>",
						Catalog.GetString ("GoogleTalk Account Settings")));
			label.Xalign = 0;
			vbox.PackStart (label, false, false, 0);
			label = MakeLabel (
					string.Format (
						"<span size=\"smaller\">{0}</span>",
						Catalog.GetString (
							"In this alpha-phase of the project, this is the " +
							"only IM account type we support (so stop your worryin'!).")));
			label.Xalign = 0;
			label.Wrap = true;
			vbox.PackStart (label, false, true, 0);
			
			Table table = new Table (4, 2, false);
			table.BorderWidth = 8;
			table.RowSpacing = 4;
			table.ColumnSpacing = 8;
			vbox.PackStart (table, true, true, 0);
			
			// Server address
			label = MakeLabel (Catalog.GetString ("Server Address:"));
			label.Xalign = 1;
			label.Yalign = 0;
			table.Attach (label, 0, 1, 0, 1, AttachOptions.Fill, 0, 0, 0);
			
			serverAddressEntry = new Entry ();
			label.MnemonicWidget = serverAddressEntry;
			serverAddressEntry.Show ();
			peditor = new PropertyEditorEntry (
					Preferences.GoogleTalkServer, serverAddressEntry);
			SetupPropertyEditor (peditor);
			table.Attach (serverAddressEntry, 1, 2, 0, 1, AttachOptions.Expand | AttachOptions.Fill, 0, 0, 0);

			// Server port
			label = MakeLabel (Catalog.GetString ("Server Port:"));
			label.Xalign = 1;
			label.Yalign = 0;
			table.Attach (label, 0, 1, 1, 2, AttachOptions.Fill, 0, 0, 0);
			
			serverPortEntry = new Entry ();
			label.MnemonicWidget = serverPortEntry;
			serverPortEntry.Show ();
			peditor = new PropertyEditorEntry (
					Preferences.GoogleTalkPort, serverPortEntry);
			SetupPropertyEditor (peditor);
			table.Attach (serverPortEntry, 1, 2, 1, 2, AttachOptions.Expand | AttachOptions.Fill, 0, 0, 0);
			
			// Username
			label = MakeLabel (Catalog.GetString ("Username:"));
			label.Xalign = 1;
			label.Yalign = 0;
			table.Attach (label, 0, 1, 2, 3, AttachOptions.Fill, 0, 0, 0);

			usernameEntry = new Entry ();
			label.MnemonicWidget = usernameEntry;
			usernameEntry.Show ();
			table.Attach (usernameEntry, 1, 2, 2, 3, AttachOptions.Expand | AttachOptions.Fill, 0, 0, 0);

			// Password
			label = MakeLabel (Catalog.GetString ("Password:"));
			label.Xalign = 1;
			label.Yalign = 0;
			table.Attach (label, 0, 1, 3, 4, AttachOptions.Fill, 0, 0, 0);

			passwordEntry = new Entry ();
			label.MnemonicWidget = passwordEntry;
			passwordEntry.Visibility = false; // password field
			passwordEntry.Show ();
			table.Attach (passwordEntry, 1, 2, 3, 4, AttachOptions.Expand | AttachOptions.Fill, 0, 0, 0);
			
			vbox.Show ();
			
			return vbox;
		}

/*		
		private Widget MakeSipPreferences ()
		{
			PropertyEditor peditor;
			
			VBox vbox = new VBox (false, 4);
			
			Label label = MakeLabel (
					string.Format (
						"<span size=\"larger\" weight=\"bold\">{0}</span>",
						Catalog.GetString ("SIP Account Settings")));
			label.Xalign = 0;
			vbox.PackStart (label, false, false, 0);
			label = MakeLabel (
					string.Format (
						"<span size=\"smaller\">{0}</span>",
						Catalog.GetString (
							"In this alpha-phase of the project, we only support " +
							"one SIP account.")));
			label.Xalign = 0;
			label.Wrap = true;
			vbox.PackStart (label, false, true, 0);
			
			Table table = new Table (3, 2, false);
			table.BorderWidth = 8;
			table.RowSpacing = 4;
			table.ColumnSpacing = 8;
			vbox.PackStart (table, true, true, 0);
			
			// Server address
			label = MakeLabel (Catalog.GetString ("Server Address:"));
			label.Xalign = 1;
			label.Yalign = 0;
			table.Attach (label, 0, 1, 0, 1, AttachOptions.Fill, 0, 0, 0);
			
			sipServerAddressEntry = new Entry ();
			label.MnemonicWidget = sipServerAddressEntry;
			sipServerAddressEntry.Show ();
			peditor = new PropertyEditorEntry (
					Preferences.SipServer, sipServerAddressEntry);
			SetupPropertyEditor (peditor);
			table.Attach (sipServerAddressEntry, 1, 2, 0, 1, AttachOptions.Expand | AttachOptions.Fill, 0, 0, 0);

			// Username
			label = MakeLabel (Catalog.GetString ("Username:"));
			label.Xalign = 1;
			label.Yalign = 0;
			table.Attach (label, 0, 1, 1, 2, AttachOptions.Fill, 0, 0, 0);

			sipUsernameEntry = new Entry ();
			label.MnemonicWidget = sipUsernameEntry;
			sipUsernameEntry.Show ();
			table.Attach (sipUsernameEntry, 1, 2, 1, 2, AttachOptions.Expand | AttachOptions.Fill, 0, 0, 0);

			// Password
			label = MakeLabel (Catalog.GetString ("Password:"));
			label.Xalign = 1;
			label.Yalign = 0;
			table.Attach (label, 0, 1, 2, 3, AttachOptions.Fill, 0, 0, 0);

			sipPasswordEntry = new Entry ();
			label.MnemonicWidget = passwordEntry;
			sipPasswordEntry.Visibility = false; // password field
			sipPasswordEntry.Show ();
			table.Attach (sipPasswordEntry, 1, 2, 2, 3, AttachOptions.Expand | AttachOptions.Fill, 0, 0, 0);
			
			vbox.Show ();
			
			return vbox;
		}
*/		
		Label MakeLabel (string label_text)
		{
			Gtk.Label label = new Gtk.Label (label_text);
			label.UseMarkup = true;
			label.UseUnderline = true;
			label.Justify = Gtk.Justification.Left;
			label.SetAlignment (0.0f, 0.5f);
			label.Show ();

			return label;
		}
		
		/*
		CheckButton MakeCheckButton (string label_text)
		{
			Gtk.Label label = MakeLabel (label_text);

			Gtk.CheckButton check = new Gtk.CheckButton ();
			check.Add (label);
			check.Show ();

			return check;
		}
		*/

/*
		Gtk.Label MakeTipLabel (string label_text)
		{
			Gtk.Label label =  MakeLabel (String.Format ("<small>{0}</small>", 
								     label_text));
			label.LineWrap = true;
			label.Xpad = 20;
			return label;
		}
		
		Gtk.RadioButton MakeRadioButton (string label_text)
		{
			RadioButton radio = new RadioButton (label_text);
			radio.Show ();
			
			return radio;
		}
		
		Gtk.RadioButton MakeRadioButton (RadioButton group, string label_text)
		{
			RadioButton radio = new RadioButton (group, label_text);
			radio.Show ();
			
			return radio;
		}
*/

		void SetupPropertyEditor (PropertyEditor peditor)
		{
			// Ensure the key exists
			Preferences.Get (peditor.Key);
			peditor.Setup ();
		}

/*		
		void VariantComboBoxDataFunc (
					CellLayout cellLayout, CellRenderer cell,
					TreeModel treeModel, TreeIter iter)
		{
			MessageStyleVariant variant = treeModel.GetValue (iter, 0) as MessageStyleVariant;
			if (variant != null)
				(cell as CellRendererText).Text = variant.Name;
			else
				(cell as CellRendererText).Text = Catalog.GetString ("<Unknown Variant>");
		}
*/		
		// <summary>
		// Simulate a conversation so the user can see a preview of what the
		// selected message style looks like.
		// </summary>
		bool SimulateConversation ()
		{
			
			return false;
		}
#endregion

#region Event Handlers
		void PreferenceChanged (object sender, PreferenceChangedEventArgs args)
		{
			Logger.Debug ("FIXME: Implement PreferencesDialog.PreferencesChanged");
		}
		
		void DialogRealized (object sender, EventArgs args)
		{
			// Load the stored preferences
			// Set the username and password if one exists
			string username;
			string password;
			
			if (AccountManagement.GetGoogleTalkCredentialsHack (out username, out password)) {
				usernameEntry.Text = username;
				passwordEntry.Text = password;
			} else {
				usernameEntry.Text = Catalog.GetString ("your.username@gmail.com");
			}
			
/*			if (AccountManagement.GetSipCredentialsHack (out username, out password)) {
				sipUsernameEntry.Text = username;
				sipPasswordEntry.Text = password;
			} else {
				sipUsernameEntry.Text = Catalog.GetString ("your.username@ekiga.net");
			}
*/		}
		
#endregion

#region Public Properties
		public string GoogleTalkUsername
		{
			get {
				// Check to see if the username is zero-length and
				// trim off any extra whitespace before or after the
				// text of the username.
				string text = usernameEntry.Text;
				if (text.Trim ().Length == 0)
					return null;
				else
					return text.Trim ();
			}
		}
		
		public string GoogleTalkPassword
		{
			get {
				// Check to see if the password is zero-length, but
				// don't return a trimmed value if the password happens
				// to have [pre/a]ppended whitespace.
				string text = passwordEntry.Text;
				if (text.Trim ().Length == 0)
					return null;
				else
					return text;
			}
		}

/*		public string SipUsername
		{
			get {
				string text = sipUsernameEntry.Text;
				if (text.Trim ().Length == 0)
					return null;
				else
					return text.Trim ();
			}
		}
		
		public string SipPassword
		{
			get {
				string text = sipPasswordEntry.Text;
				if (text.Trim ().Length == 0)
					return null;
				else
					return text;
			}
		}
*/
#endregion
	}
}
