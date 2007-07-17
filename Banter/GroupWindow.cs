//***********************************************************************
// *  $RCSfile$ - GroupWindow.cs
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
	// <summary>
	// The GroupWindow can be considered the main window of Banter.  It displays
	// a group of people and allows you to initiate communication with them, see
	// their online status, etc.
	// </summary>
	public class GroupWindow : Window
	{
		static object stateModificationLocker = new object ();
		
		#region const strings
		// bool
		const string ConfStateVisible = "/visible";
		
		// int
		const string ConfStateWidth = "/width";
		
		// int
		const string ConfStateHeight = "/height";
		
		// int
		const string ConfStateX = "/x";
		
		// int
		const string ConfStateY = "/y";
		
		// bool
		const string ConfStateShowSidebar = "/show_sidebar";
		
		// string: Valid values are "Small", "Medium", and "Large"
		const string ConfStatePersonCardSize = "/person_card_size";
		
		// string: An ID of 0 represents the Everyone group (i.e., no group selected)
		const string ConfStatePersonGroupID = "/person_group_id";
		#endregion // const strings
		
		#region fields
		private Guid guid;
		private PersonGroup selectedGroup;
		
		//
		// Sidebar Pane
		//
		private Widget sidebar;
		private AvatarSelector avatarSelector;
		private Label myDisplayName;
		private StatusEntry statusEntry;
		
		private Entry searchEntry;
		private Button cancelSearchButton;
		private uint searchTimeoutID;
		
		private TreeModel groupTreeModel;
		private Dictionary<int, GroupButton> groupButtonMap;
		private Button everyoneGroupButton;
		private VBox groupButtonsVBox;
		private Button newGroupButton;
		
		private Button settingsButton;
		private Button addPersonButton;
		
		//
		// PersonView Pane
		//
		private PersonView personView;
		
		//
		// Bottom Bar
		//
		private Button toggleSidebarButton;
		private HScale personHScale;
		
		private InterruptableTimeout saveStateTimeout;
		private bool saveNeeded;
		
		private PersonCardSize initialPersonCardSize;
		private bool initiallyShowSidebar;
		private string initialGroupId;
		#endregion
		
		#region public constructors
		public GroupWindow () : base (Catalog.GetString ("Banter"))
		{
			Init (Guid.NewGuid (),
				true, // visible
				600, // width
				480, // height
				0, // xPos
				0, // yPos,
				true, // show sidebar
				PersonCardSize.Small,
				"0" // show everyone
			);
			saveNeeded = true;
		}
		
		public GroupWindow (string guidStr) : base (Catalog.GetString ("Banter"))
		{
			string keyPrefix = Preferences.GroupWindowPrefix + "/" + guidStr;

			bool visible = (bool) Preferences.Get (keyPrefix + ConfStateVisible);
			int width = (int) Preferences.Get (keyPrefix + ConfStateWidth);
			int height = (int) Preferences.Get (keyPrefix + ConfStateHeight);
			int x = (int) Preferences.Get (keyPrefix + ConfStateX);
			int y = (int) Preferences.Get (keyPrefix + ConfStateY);
			bool showSidebar = (bool) Preferences.Get (keyPrefix + ConfStateShowSidebar);
			string cardSizeStr = (string) Preferences.Get (keyPrefix + ConfStatePersonCardSize);

			PersonCardSize personCardSize;
			switch (cardSizeStr) {
			case "Large":
				personCardSize = PersonCardSize.Large;
				break;
			case "Medium":
				personCardSize = PersonCardSize.Medium;
				break;
			default:
				personCardSize = PersonCardSize.Small;
				break;
			}

			string personGroupId = (string) Preferences.Get (keyPrefix + ConfStatePersonGroupID);

			Init (new Guid (guidStr), visible, width, height, x, y, showSidebar,
					personCardSize, personGroupId);
			saveNeeded = false;
		}
		#endregion
		
		#region public properties
		public PersonGroup SelectedGroup
		{
			get { return selectedGroup; }
			set {
				if (value == null) {
					// Select the "Everyone" group
				} else {
					foreach (GroupButton button in groupButtonMap.Values) {
						if (value == button.PersonGroup) {
							OnGroupButtonClicked (button, button.PersonGroup);
							return;
						}
					}
					
					Logger.Warn ("GroupWindow.SelectedGroup [set] called with an unknown PersonGroup: {0}", value.DisplayName);
				}
			}
		}
		#endregion

		#region public methods
		// <summary>
		// Save the state of the group window (i.e., position, size, selected group,
		// person card size, etc.
		// </summary>
		public void SaveState ()
		{
			// Do nothing if we don't need to save.  Avoid unnecessary saves
			// e.g. on forced quit when we call save for every group window.
			if (!saveNeeded)
				return;
			
			lock (stateModificationLocker) {
			
			bool visible;
			string guidStr;
			int width, height;
			int x, y;
			bool showSidebar;
			PersonCardSize personCardSize;
			PersonGroup personGroup;
			
			visible = Visible;
			guidStr = guid.ToString ();
			GetSize (out width, out height);
			GetPosition (out x, out y);
			showSidebar = sidebar.Visible;
			personCardSize = personView.PersonCardSize;
			personGroup = selectedGroup;
			
/*			Logger.Debug (
					"Window State:\n" +
					"\tVisible: {0}\n" +
					"\tGUID: {1}\n" +
					"\tWidth: {2}, Height: {3}\n" +
					"\tX: {4}, Y: {5}\n" +
					"\tShow Sidebar: {6}\n" +
					"\tPerson Card Size: {7}\n" +
					"\tSelected Group: {8}",
					visible,
					guidStr,
					width, height,
					x, y,
					showSidebar,
					personCardSize,
					personGroup == null ?
						"Everyone" : personGroup.DisplayName);
*/			
			string keyPrefix = Preferences.GroupWindowPrefix + "/" + guidStr;
			string key;
			
//			Logger.Info ("Saving window state: '{0}'...", Title);
			try {
				key = keyPrefix + ConfStateVisible;
				Preferences.Set (key, visible);
				
				key = keyPrefix + ConfStateWidth;
				Preferences.Set (key, width);
				
				key = keyPrefix + ConfStateHeight;
				Preferences.Set (key, height);
				
				key = keyPrefix + ConfStateX;
				Preferences.Set (key, x);
				
				key = keyPrefix + ConfStateY;
				Preferences.Set (key, y);
				
				key = keyPrefix + ConfStateShowSidebar;
				Preferences.Set (key, showSidebar);
				
				key = keyPrefix + ConfStatePersonCardSize;
				Preferences.Set (key, personCardSize.ToString ());
				
				key = keyPrefix + ConfStatePersonGroupID;
				Preferences.Set (key, personGroup == null ? "0" : personGroup.Id);
				
				// Update the saved group window lists
				string[] groupWindowIds =
					Preferences.Get (Preferences.GroupWindows) as string[];
					
				List<string> groupWindows;
				if (groupWindowIds == null) {
					groupWindows = new List<string> ();
				} else {
					groupWindows = new List<string> (groupWindowIds);
				}
				
				bool found = false;
				foreach (string groupWindowId in groupWindows) {
					if (string.Compare (groupWindowId, guidStr) == 0) {
						found = true;
						break;
					}
				}
				
				if (!found) {
					groupWindows.Add (guidStr);
					
					Preferences.Set (
						Preferences.GroupWindows, groupWindows.ToArray ());
				}
			} catch (Exception e) {
				Logger.Warn ("Error saving Group Window state: {0}, {1}",
						Title, e.Message);
			}
			}
		}
		
		// <summary>
		// Remove the saved state of the window from any configuration.
		// </summary>
		public void DeleteSaveState ()
		{
			lock (stateModificationLocker) {
				saveNeeded = false;
				saveStateTimeout.Cancel ();
				
				string guidStr = guid.ToString ();
				
				string[] groupWindowIds =
					Preferences.Get (Preferences.GroupWindows) as string[];
				if (groupWindowIds == null)
					return;
				
				Logger.Debug ("Array: # of groupWindows: {0}", groupWindowIds.Length);
				List<string> groupWindows = new List<string> (groupWindowIds);
				
				foreach (string id in groupWindows) {
					if (string.Compare (id, guidStr) == 0) {
						groupWindows.Remove (id);
						break;
					}
				}
				
				Logger.Debug ("List: # of groupWindows: {0}", groupWindows.Count);
				if (groupWindows.Count == 0) {
					Preferences.Unset (Preferences.GroupWindows);
				} else {
					Preferences.Set (Preferences.GroupWindows, groupWindows.ToArray ());
				}
				
				string key = Preferences.GroupWindowPrefix + "/" + guidStr;
				Preferences.RecursiveUnset (key);
			}
		}
		#endregion

		#region public events
		#endregion

		#region private methods
		~GroupWindow ()
		{
		}
		
		private void Init (System.Guid guid, bool visible, int width, int height,
				int xPos, int yPos, bool showSidebar, PersonCardSize personCardSize,
				string personGroupId)
		{
			this.guid = guid;
			searchTimeoutID = 0;
			
			CreateWidgets ();
			
			SetDefaultSize (width, height);
			Icon = Utilities.GetIcon ("banter-22", 22);
			AllowShrink = true;
			
			initialPersonCardSize = personCardSize;
			initiallyShowSidebar = showSidebar;
			initialGroupId = personGroupId;
			
			Move (xPos, yPos);

			groupTreeModel = PersonManager.Groups;
			groupButtonMap = new Dictionary<int, GroupButton> ();
			
			saveStateTimeout = new InterruptableTimeout ();
			saveStateTimeout.Timeout += SaveStateTimeout;
			
			Realized += OnRealizeWidget;
		}
		
		private void CreateWidgets ()
		{
			VBox vbox = new VBox (false, 0);
			
			// Menubar?  TODO
			// vbox.PackStart (CreateMenuBar (), false, false, 0);
			
			// Content Area (Sidebar and PersonView)
			vbox.PackStart (CreateContentArea (), true, true, 0);
			
			// Bottom Bar
			vbox.PackStart (CreateBottomBar (), false, false, 0);

			vbox.ShowAll ();
			Add (vbox);
		}
		
		private Widget CreateContentArea ()
		{
			EventBox eb = new EventBox ();
			eb.ModifyBg (StateType.Normal, this.Style.Background (StateType.Active));
			
			HBox hbox = new HBox (false, 0);
			hbox.Show ();
			eb.Add (hbox);
			
			sidebar = CreateSidebar ();
			hbox.PackStart (sidebar, false, false, 0);
			hbox.PackStart (CreatePersonView (), true, true, 0);
			
			return eb;
		}
		
		private Widget CreateSidebar ()
		{
			VBox vbox = new VBox (false, 0);
			vbox.BorderWidth = 4;
			vbox.WidthRequest = 175;
			
			// Avatar & Status
			vbox.PackStart (CreateAvatarBox (), false, false, 0);
			
			// spacer
			Label l = new Label ("<span size=\"small\"></span>");
			l.UseMarkup = true;
			l.Show ();
			vbox.PackStart (l, false, false, 0);
			
			// Search Entry
//			vbox.PackStart (CreateSidebarSearchEntry (), false, false, 0);
			
			// Groups
			vbox.PackStart (CreateGroupButtons (), false, false, 0);
			
			// Actions
			vbox.PackStart (CreateActionButtons (), false, false, 0);
			
			return vbox;
		}
		
		private Widget CreateAvatarBox ()
		{
			HBox hbox = new HBox (false, 4);
			
			avatarSelector = new AvatarSelector();
			
			if( (PersonManager.Me != null) && (PersonManager.Me.Photo != null) )
				avatarSelector.Pixbuf = PersonManager.Me.Photo;

			avatarSelector.Show();

			hbox.PackStart (avatarSelector, false, false, 0);
			
			VBox vbox = new VBox (false, 0);
			vbox.Show ();
			hbox.PackStart (vbox, true, true, 0);
			
			myDisplayName = new Label ();
			myDisplayName.Xalign = 0;
			myDisplayName.UseUnderline = false;
			myDisplayName.UseMarkup = true;
			myDisplayName.Show ();
			vbox.PackStart (myDisplayName, true, true, 0);
			
			statusEntry = new StatusEntry ();
			statusEntry.PresenceChanged += OnStatusEntryChanged;
			statusEntry.Show ();
			vbox.PackStart (statusEntry, false, false, 0);
			
//			statusComboBoxEntry = ComboBoxEntry.NewText ();
//			statusComboBoxEntry.KeyPressEvent += OnStatusComboKeyPress;
//			statusComboBoxEntry.Changed += OnStatusComboChanged;
			
//			statusComboBoxEntry.Show ();
//			vbox.PackStart (statusComboBoxEntry, false, false, 0);
			
			hbox.Show ();
			return hbox;
		}
		
		private void OnStatusEntryChanged (Presence presence)
		{
			//Logger.Debug ("Setting presence on Me to {0}", presence.Message);
			if (PersonManager.Me != null) {
				PersonManager.Me.SetStatus (presence);
				
				Logger.Debug ("FIXME: Save off a custom presence message so when Banter restarts, they'll be available.");
			}
		}

/*		private void OnStatusComboKeyPress (object sender, KeyPressEventArgs args)
		{
			if (args.Event.Key == Gdk.Key.Return) {
				if (PersonManager.Me != null) {
					Logger.Debug ("FIXME: Set \"my\" status to: {0}",
							statusComboBoxEntry.ActiveText);
					PersonManager.Me.Presence.Message =
							statusComboBoxEntry.ActiveText;
				}
			}
		}
*/		
/*		private void OnStatusComboChanged (object sender, EventArgs args)
		{
			Logger.Debug ("OnStatusComboChanged");
		}
*/		
		private Widget CreateSidebarSearchEntry ()
		{
			VBox vbox = new VBox (false, 0);
			
			Label l = new Label (
					string.Format ("<span size=\"large\">{0}</span>",
						Catalog.GetString ("Filter")));
			l.UseMarkup = true;
			l.ModifyFg (StateType.Normal, this.Style.Base (StateType.Selected));
			l.Xalign = 0;
			l.Show ();
			vbox.PackStart (l, false, false, 0);
			
			searchEntry = new Entry ();
			searchEntry.SelectRegion (0, -1);
			searchEntry.CanFocus = true;
			searchEntry.Changed += OnSearchEntryChanged;
			searchEntry.Show ();
			
			Image stopImage = new Image (Stock.Stop, Gtk.IconSize.Menu);
			stopImage.SetAlignment (0.5F, 0.0F);
			
			cancelSearchButton = new Button (stopImage);
			cancelSearchButton.Relief = ReliefStyle.None;
			cancelSearchButton.Sensitive = false;
			cancelSearchButton.Clicked += OnCancelSearchButton;
			cancelSearchButton.Show ();
			
			HBox searchHBox = new HBox (false, 4);
			searchHBox.PackStart (searchEntry, true, true, 0);
			searchHBox.PackStart (cancelSearchButton, false, false, 0);
			
			searchHBox.Show ();
			vbox.PackStart (searchHBox, false, false, 0);
			
			return vbox;
		}
		
		private Widget CreateGroupButtons ()
		{
			VBox vbox = new VBox (false, 0);
			
			Label l = new Label (
					string.Format ("<span size=\"large\">{0}</span>",
						Catalog.GetString ("Groups")));
			l.UseMarkup = true;
			l.Xalign = 0;
			l.ModifyFg (StateType.Normal, this.Style.Base (StateType.Selected));
			l.Show ();
			vbox.PackStart (l, false, false, 0);
			
			// Spacer
			HBox spacerHBox = new HBox (false, 0);
			spacerHBox.PackStart (new Label (""), false, false, 4);
			spacerHBox.ShowAll ();
			vbox.PackStart (spacerHBox, false, false, 0);
			
			VBox buttonsVBox = new VBox (false, 0);

			// Everyone
			everyoneGroupButton = CreateSidebarTextButton (
					Catalog.GetString ("Everyone"));
			everyoneGroupButton.Clicked += OnEveryoneClicked;
			buttonsVBox.PackStart (everyoneGroupButton, false, false, 0);
			
			// Placeholder for other groups
			groupButtonsVBox = new VBox (false, 0);
			vbox.Show ();
			buttonsVBox.PackStart (groupButtonsVBox, false, false, 0);
			
			// New Group
			newGroupButton = CreateSidebarTextButton (
					Catalog.GetString ("New Group..."));
			newGroupButton.Clicked += OnNewGroupClicked;
			buttonsVBox.PackStart (newGroupButton, false, false, 0);
			
			buttonsVBox.Show ();
			spacerHBox.PackStart (buttonsVBox, true, true, 0);
			
			return vbox;
		}
		
		private Widget CreateActionButtons ()
		{
			VBox vbox = new VBox (false, 0);
			
			Label l = new Label (
					string.Format ("<span size=\"large\">{0}</span>",
						Catalog.GetString ("Actions")));
			l.UseMarkup = true;
			l.Xalign = 0;
			l.ModifyFg (StateType.Normal, this.Style.Base (StateType.Selected));
			l.Show ();
			vbox.PackStart (l, false, false, 0);
			
			// Spacer
			HBox spacerHBox = new HBox (false, 0);
			spacerHBox.PackStart (new Label (""), false, false, 4);
			spacerHBox.ShowAll ();
			vbox.PackStart (spacerHBox, false, false, 0);
			
			VBox buttonsVBox = new VBox (false, 0);

			// Settings
			settingsButton = CreateSidebarTextButton (
					Catalog.GetString ("Settings"));
			settingsButton.Clicked += OnSettingsClicked;
			buttonsVBox.PackStart (settingsButton, false, false, 0);
			
			// Add Person
			addPersonButton = CreateSidebarTextButton (
					Catalog.GetString ("Add Person..."));
			addPersonButton.Clicked += OnAddPersonClicked;
			buttonsVBox.PackStart (addPersonButton, false, false, 0);
			
			buttonsVBox.Show ();
			spacerHBox.PackStart (buttonsVBox, true, true, 0);
			
			return vbox;
		}
		
		private Widget CreatePersonView ()
		{
			ScrolledWindow sw = new ScrolledWindow ();
			personView = new PersonView (sw);
			
			sw.AddWithViewport (personView);
			
			return sw;
		}
		
		private Widget CreateBottomBar ()
		{
			HButtonBox buttonBox = new HButtonBox ();
			buttonBox.Layout = ButtonBoxStyle.Edge;
			
			toggleSidebarButton = new Button ();
			toggleSidebarButton.Label = Catalog.GetString ("Close Sidebar");
			toggleSidebarButton.Relief = ReliefStyle.None;
			toggleSidebarButton.Clicked += OnToggleSidebarButtonClicked; 
			toggleSidebarButton.Show ();
			buttonBox.PackStart (toggleSidebarButton, false, false, 0);

			personHScale = new HScale (1, 3, 1);
			personHScale.DrawValue = false;
			personHScale.ValueChanged += OnPersonHScaledValueChanged;
			personHScale.Show ();
			buttonBox.PackEnd (personHScale, false, false, 0);
			
			buttonBox.Show ();
			return buttonBox;
		}
		
		private Button CreateSidebarTextButton (string buttonText)
		{
			Button button;
			HBox hbox;
			Label buttonLabel;
			
			hbox = new HBox (false, 0);
			buttonLabel = new Label (buttonText);
			buttonLabel.Xalign = 0;
			buttonLabel.UseUnderline = false;
			buttonLabel.UseMarkup = true;
			buttonLabel.Show ();
			hbox.PackStart (buttonLabel, false, false, 4);
			button = new Button (hbox);
			button.Relief = ReliefStyle.None;
			button.Show ();
			
			return button;
		}

		private bool SearchCallback ()
		{
			SearchPeople ();
			return false;
		}
		
		private void SearchPeople ()
		{
			Logger.Debug ("FIXME: Implement GroupWindow.SearchPeople ()");
		}
		
		private void BuildGroupButtonsView ()
		{
			TreeIter iter;
			
Logger.Debug ("GroupWindow.BuildGroupButtonsView adding {0} groups",
				groupTreeModel.IterNChildren ());
			
			// Loop through the model, create buttons, and add them into the
			// groupButtonsVBox.
			if (groupTreeModel.GetIterFirst (out iter)) {
				do {
					PersonGroup group =
							groupTreeModel.GetValue (iter, 0) as PersonGroup;
					if (group == null)
						continue;
					
					AddGroupButton (new GroupButton (group),
							groupTreeModel.GetPath (iter));
				} while (groupTreeModel.IterNext (ref iter));
			}
		}
		
		private void AddGroupButton (GroupButton button, TreePath path)
		{
			Logger.Debug ("Adding group button: {0}", button.PersonGroup.DisplayName);
			button.Show ();
			button.Clicked += OnGroupButtonClicked;
			button.RightClicked += OnGroupButtonRightClicked;
			groupButtonsVBox.PackStart (button, false, false, 0);
			groupButtonMap [path.Indices [0]] = button;
		}
		
		private void QueueSaveState ()
		{
//			Logger.Debug ("GroupWindow.QueueSaveState");
			
			// Replace the existing save timeout.  Wait
			// four seconds before saving ...
			saveStateTimeout.Reset (4000);
			saveNeeded = true;
		}
		
		private void SaveStateTimeout (object sender, EventArgs args)
		{
			try {
				SaveState ();
				saveNeeded = false;
			} catch (Exception e) {
				Logger.Warn ("Error while saving the state of the GroupWindow: {0} - {1}", Title, e.Message);
			}
		}
		#endregion
		
		#region method overrides
		protected override bool OnDeleteEvent (Gdk.Event evnt)
		{
			if (PersonManager.Me != null)
				PersonManager.Me.PresenceUpdated -= OnMyPresenceUpdated;
			
			groupTreeModel.RowInserted -= OnGroupRowInserted;
			groupTreeModel.RowDeleted -= OnGroupRowDeleted;
			groupTreeModel.RowChanged -= OnGroupRowChanged;
			
			saveStateTimeout.Cancel ();
			saveStateTimeout.Timeout -= SaveStateTimeout;
			
			DeleteSaveState ();

			return base.OnDeleteEvent (evnt);
		}
		#endregion
		
		#region event handlers
		private void OnRealizeWidget (object sender, EventArgs args)
		{
			// Set "my" display name
			Person me = PersonManager.Me;
			if (me != null) {
				myDisplayName.Markup =
					string.Format (
						"<span weight=\"bold\" size=\"small\">{0}</span>",
						me.DisplayName);
				
				// Set "my" status
				statusEntry.Presence = me.Presence;
				
				Logger.Debug ("FIXME: Populate the StatusEntry widget with saved status messages.");
				
				me.PresenceUpdated += OnMyPresenceUpdated;
			}
			
			// Fill out the existing groups
			BuildGroupButtonsView ();
			
			groupTreeModel.RowInserted += OnGroupRowInserted;
			groupTreeModel.RowDeleted += OnGroupRowDeleted;
			groupTreeModel.RowChanged += OnGroupRowChanged;
			
			personHScale.Value = (double) initialPersonCardSize;
			
			if (!initiallyShowSidebar) {
				sidebar.Hide ();
				toggleSidebarButton.Label = Catalog.GetString ("Show Sidebar");
			}
			
			// Set the view to the proper group

			if (string.Compare (initialGroupId, "0") == 0) {
				SelectEveryoneGroup();
			} else {
				foreach (GroupButton groupButton in groupButtonMap.Values) {
					PersonGroup group = groupButton.PersonGroup;
					if (string.Compare (group.Id, initialGroupId) == 0) {
						OnGroupButtonClicked (groupButton, group);
						break;
					}
				}
			}
			
			ConfigureEvent += OnConfigureEvent;
			
			// Cause brand new windows to be saved.  Previously existing windows
			// will be ignored unless the user moves them.
			SaveState (); 
			
			//Gnome.Sound.Play(System.IO.Path.Combine(Banter.Defines.SoundDir, "banter.wav"));				
		}
		
		private void OnSearchEntryChanged (object sender, EventArgs args)
		{
			if (searchTimeoutID != 0) {
				GLib.Source.Remove (searchTimeoutID);
				searchTimeoutID = 0;
			}
			
			cancelSearchButton.Sensitive = searchEntry.Text.Length > 0;
			
			searchTimeoutID = GLib.Timeout.Add (
					500, new GLib.TimeoutHandler (SearchCallback));
		}
		
		private void OnCancelSearchButton (object sender, EventArgs args)
		{
			searchEntry.Text = "";
			searchEntry.GrabFocus ();
		}
		
		private void OnEveryoneClicked (object sender, EventArgs args)
		{
			if (selectedGroup != null) { // Everyone isn't already selected
				SelectEveryoneGroup();
			}
		}
		
		private void SelectEveryoneGroup()
		{
			Title = Catalog.GetString ("Everyone - Banter");
			selectedGroup = null;
			
			personView.Model = PersonManager.People;
			
			QueueSaveState ();
		}
		
		private void OnNewGroupClicked (object sender, EventArgs args)
		{
			HIGMessageDialog dialog =
				new HIGMessageDialog (this, Gtk.DialogFlags.DestroyWithParent,
					Gtk.MessageType.Question,
					Gtk.ButtonsType.OkCancel,
					Catalog.GetString ("New group window"),
					Catalog.GetString ("Enter the name of the new group you'd like to create."));

			Gtk.Entry groupNameEntry = new Entry ();
            groupNameEntry.ActivatesDefault = true;
			dialog.ExtraWidget = groupNameEntry;
			
			int returnCode = dialog.Run ();
			
			if (returnCode == (int) Gtk.ResponseType.Ok) {
				string groupName = groupNameEntry.Text.Trim ();
				if (groupName.Length > 0) {
					try {
						PersonGroup group = new PersonGroup (groupName);
						PersonManager.AddGroup (group);
					} catch (Exception e) {
						Logger.Debug ("Couldn't create a group: {0}\n{1}\n{2}",
								groupName, e.Message, e.StackTrace);
					}
				}
			}
			
			dialog.Destroy ();
		}
		
		private void OnSettingsClicked (object sender, EventArgs args)
		{
			Application.ActionManager ["ShowPreferencesAction"].Activate ();
		}
		
		private void OnAddPersonClicked (object sender, EventArgs args)
		{
			Logger.Debug ("FIXME: Implement GroupWindow.OnAddPersonClicked ()");
		}
		
		private void OnToggleSidebarButtonClicked (object sender, EventArgs args)
		{
			// FIXME: This is REALLY choppy and not smooth.  Can anyone make it better?
			int width;
			int height;
			int xPos;
			int yPos;
			
			this.GetSize (out width, out height);
			this.GetPosition (out xPos, out yPos);
			
			if (sidebar.Visible == true) {
				sidebar.Hide ();
				this.Resize (width - 175, height);
				this.Move (xPos + 175, yPos);
				toggleSidebarButton.Label = Catalog.GetString ("Show Sidebar");
			} else {
				sidebar.Show ();
				this.Move (xPos - 175, yPos);
				this.Resize (width + 175, height);
				toggleSidebarButton.Label = Catalog.GetString ("Hide Sidebar");
			}
			
			QueueSaveState ();
		}
		
		private void OnPersonHScaledValueChanged (object sender, EventArgs args)
		{
			PersonCardSize newValue;
			
			if (personHScale.Value < 1.5)
				newValue = PersonCardSize.Small;
			else if (personHScale.Value < 2.5)
				newValue = PersonCardSize.Medium;
			else
				newValue = PersonCardSize.Large;
			
			// Update the personHScale so that the slider appears to "lock" at
			// the stops
			personHScale.ValueChanged -= OnPersonHScaledValueChanged;
			personHScale.Value = (double) newValue;
			personHScale.ValueChanged += OnPersonHScaledValueChanged;
			
			// Only update the personView if the value actually changed
			if (newValue != personView.PersonCardSize) {
				personView.PersonCardSize = newValue;
				QueueSaveState ();
			}
		}
		
		private void OnMyPresenceUpdated (Person me)
		{
			statusEntry.PresenceChanged -= OnStatusEntryChanged;
			statusEntry.Presence = me.Presence;
			statusEntry.PresenceChanged += OnStatusEntryChanged;
		}
		
		private void OnGroupRowInserted (object sender, RowInsertedArgs args)
		{
			PersonGroup group =
					groupTreeModel.GetValue (args.Iter, 0) as PersonGroup;
			if (group == null)
				return;
			
			AddGroupButton (new GroupButton (group),
					groupTreeModel.GetPath (args.Iter));
		}
		
		private void OnGroupRowDeleted (object sender, RowDeletedArgs args)
		{
			if (groupButtonMap.ContainsKey (args.Path.Indices [0]) == false) {
				Logger.Debug ("GroupWindow.OnGroupRowDeleted () called on a path we don't know about.");
				return;
			}
			
			GroupButton groupButton = groupButtonMap [args.Path.Indices [0]];
			
			Logger.Debug ("GroupWindow.OnGroupRowDeleted removing group: {0}", groupButton.PersonGroup.DisplayName);
			
			groupButtonMap.Remove (args.Path.Indices [0]);

			groupButtonsVBox.Remove (groupButton);
			
			// If this is the currently selected group, switch the view
			// back to the Everyone group so that there aren't weirdnesses.
			if (selectedGroup == groupButton.PersonGroup) {
				OnEveryoneClicked (this, EventArgs.Empty);
			}
			
			// FIXME: Determine whether we should be calling groupButton.Destroy () here.
			groupButton.Destroy ();
		}
		
		private void OnGroupRowChanged (object sender, RowChangedArgs args)
		{
			PersonGroup group =
					groupTreeModel.GetValue (args.Iter, 0) as PersonGroup;
			if (group == null)
				return;

			if (groupButtonMap.ContainsKey (args.Path.Indices [0]) == false) {
				Logger.Debug ("GroupWindow.OnGroupRowChanged () called on a path we don't know about, adding it now.");
				AddGroupButton (new GroupButton (group), args.Path);
				return;
			}
			
			GroupButton groupButton = groupButtonMap [args.Path.Indices [0]];
			
			Logger.Debug ("GroupWindow.OnGroupRowChanged updating group: {0} -> {1}",
					groupButton.PersonGroup.DisplayName,
					group.DisplayName);
			
			// Update the button's PersonGroup.  It'll do the work of updating the label.
			groupButton.PersonGroup = group;
		}
		
		private void OnGroupButtonClicked (GroupButton sender, PersonGroup group)
		{
			// Update the window title
			Title = string.Format (
					Catalog.GetString ("{0} - Banter"),
					group.DisplayName);
			
			// Update the PersonView
			personView.Model = group.People;
			
			selectedGroup = group;
			
			QueueSaveState ();
		}
		
		private void OnGroupButtonRightClicked (GroupButton sender, PersonGroup group)
		{
			HIGMessageDialog dialog = new HIGMessageDialog (
				this, DialogFlags.DestroyWithParent,
				MessageType.Warning, ButtonsType.YesNo,
				string.Format (
					Catalog.GetString ("Delete \"{0}\"?"),
					group.DisplayName),
				Catalog.GetString ("This will delete the group and not the contacts inside of the group."));
			int responseType = dialog.Run ();
			dialog.Destroy ();
			
			if (responseType == (int) ResponseType.Yes) {
				try {
					PersonManager.RemoveGroup (group.Id);
				} catch (Exception e) {
					Logger.Warn ("Error removing the group: {0}\n{1}\n{2}",
							group.DisplayName,
							e.Message,
							e.StackTrace);
				}
			}
		}
		
		// <summary>
		// Save the state of the window if its moved or resized.
		// </summary> 
		[GLib.ConnectBefore]
		private void OnConfigureEvent (object sender, ConfigureEventArgs args)
		{
			QueueSaveState ();
		}		
		#endregion
	}

	public delegate void GroupButtonClickedHandler (GroupButton sender, PersonGroup group); 

	public class GroupButton : Button
	{
		private PersonGroup group;
		private Label label;
		
		public GroupButton (PersonGroup personGroup)
		{
			group = personGroup;

			HBox hbox;
		
			hbox = new HBox (false, 0);
			label = new Label (group.DisplayName);
			label.Xalign = 0;
			label.UseUnderline = false;
			label.UseMarkup = true;
			label.Show ();
			hbox.PackStart (label, false, false, 4);
			this.Relief = ReliefStyle.None;
			Add (hbox);
		}
		
		public new void Show ()
		{
			ShowAll ();
		}
		
		public new event GroupButtonClickedHandler Clicked;
		public event GroupButtonClickedHandler RightClicked;
		
		public PersonGroup PersonGroup
		{
			get { return group; }
			set {
				if (value == null)
					return;
				
				// Update the group and the display name
				group = value;
				label.Text = group.DisplayName;
			}
		}
		
		protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
		{
			switch (evnt.Button) {
			case 1: // left-click
				if (Clicked != null)
					Clicked (this, group);
				break;
			case 3: // right-click
				if (RightClicked != null)
					RightClicked (this, group);
				break;
			}
			
			return true;
		}
	}
}
