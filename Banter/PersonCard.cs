//***********************************************************************
// *  $RCSfile$ - PersonCard.cs
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
using System.Collections;
using System.IO;
using Gtk;
using Mono.Unix;
using Gdk;

namespace Banter
{
	
	///<summary>
	///	PersonCardSize enum
	/// PersonCards can render various sizes of the widget.  This enumerates them.
	///</summary>
	public enum PersonCardSize : uint
	{
		Small = 1,
		Medium,
		Large
	}
	
	
	///<summary>
	///	TargetWindow
	/// Window holding all drop targets for giver
	///</summary>
	public enum DragTargetType
	{
		UriList,
		TomBoyNote
	};


	///<summary>
	///	PersonCard
	/// A Gui Widget that renders a Person.
	///</summary>	
	public class PersonCard : Gtk.EventBox
	{
		#region Private Types	
		private Person person;
		private PersonCardSize cardSize;
		private Gtk.Image image;
		private ProgressBar progressBar;
		private Label progressLabel;
		#endregion


		#region Public Properties
		///<summary>
		///	The Person object the PersonCard is rendering
		///</summary>			
		public Person Person
		{
			get { return person; }
			set {
				Logger.Debug ("FIXME: Implement PersonCard.Person [set] - Do we need to do anything here?");
				// person = value;
			}
		}
		
		
		///<summary>
		///	The size of the card to be rendered
		///</summary>			
		public PersonCardSize Size
		{
			get { return this.cardSize; }
			set { this.cardSize = value; }
		}
		#endregion


		#region Constructors
		///<summary>
		///	Constructs a PersonCard from a Person object
		///</summary>			
		public PersonCard(Person person)
		{
			this.person = person;
			this.cardSize = PersonCardSize.Small;

			person.PresenceUpdated += OnPersonPresenceUpdated;
			person.AvatarUpdated += OnPersonAvatarUpdated;
			Init();
		}
		#endregion		
		
		
		#region Private Methods		
		///<summary>
		///	Setup the gtk# structure
		///</summary>		
		private void Init()
		{
			this.BorderWidth = 0;
			//this.Relief = Gtk.ReliefStyle.None;
			this.CanFocus = false;

            this.ModifyBg(StateType.Normal, new Gdk.Color(255,255,255));
            this.ModifyBase(StateType.Normal, new Gdk.Color(255,255,255));
			this.BorderWidth = 0;
			
			VBox widgetVBox = new VBox (false, 4);
	        HBox widgetColumns = new HBox(false, 10);
			
			if(person.Photo != null)
				image = new Gtk.Image(person.Photo.ScaleSimple(32, 32, InterpType.Bilinear));
			else
				image = new Gtk.Image(Utilities.GetIcon("blank-photo-128", 32));			
			
			widgetColumns.PackStart(image, false, false, 0);

			// Set up the name and status labels
			VBox nameVBox = new VBox();
			widgetColumns.PackStart(nameVBox, true, true, 0);
			Label label = new Label();
			label.Justify = Gtk.Justification.Left;
            label.SetAlignment (0.0f, 0.5f);
			label.LineWrap = false;
			label.UseMarkup = true;
			label.UseUnderline = false;
			label.Markup = string.Format ("<span weight=\"bold\" size=\"medium\">{0}</span>",
											person.DisplayName);
			nameVBox.PackStart(label, true, true, 0);

			label = new Label();
			label.Justify = Gtk.Justification.Left;
            label.SetAlignment (0.0f, 0.5f);
			label.UseMarkup = true;
			label.UseUnderline = false;

			label.LineWrap = false;
			
			if (person.PresenceMessage.Length > 0) {
				label.Markup = String.Format("<span style=\"italic\" size=\"small\">{0}</span>", person.PresenceMessage);
			}
			else {
				label.Markup = String.Format("<span style=\"italic\" size=\"small\">{0}</span>", Presence.GetStatusString(person.Presence.Type));
			}
			nameVBox.PackStart(label, true, true, 0);


			// Add capabilities icons if they have any capabilities
			// change this later to show their capabilities when we actually have them
			if(person.Presence.Type != PresenceType.Offline) {
				HBox actionBox = new HBox(false, 0);

				widgetColumns.PackStart(actionBox, false, false, 0);

				Gtk.Image actionImage = new Gtk.Image(Utilities.GetIcon("webcam", 24));

				Gtk.Button button = new Gtk.Button();
				button.BorderWidth = 0;
				button.Relief = Gtk.ReliefStyle.None;
				button.CanFocus = false;
				button.Image = actionImage;
				button.Clicked += OnVideoChatClicked;
				actionBox.PackEnd(button, false, false, 0);


				actionImage = new Gtk.Image(Utilities.GetIcon("text", 24));
				button = new Gtk.Button();
				button.BorderWidth = 0;
				button.Relief = Gtk.ReliefStyle.None;
				button.CanFocus = false;
				button.Clicked += OnTextChatClicked;
				button.Image = actionImage;

				actionBox.PackEnd(button, false, false, 0);
			}

	        widgetColumns.ShowAll();
	        widgetVBox.PackStart (widgetColumns, true, true, 0);
	        

			// Add a progress bar for file transfer progress but hide it
	        progressBar = new ProgressBar ();
	        progressBar.Orientation = ProgressBarOrientation.LeftToRight;
	        progressBar.BarStyle = ProgressBarStyle.Continuous;
	        progressBar.NoShowAll = true;
	        widgetVBox.PackStart (progressBar, true, false, 0);
			progressLabel = new Label ();
			progressLabel.UseMarkup = true;
			progressLabel.Xalign = 0;
			progressLabel.UseUnderline = false;
			progressLabel.LineWrap = true;
			progressLabel.Wrap = true;
			progressLabel.NoShowAll = true;
			progressLabel.Ellipsize = Pango.EllipsizeMode.End;
			widgetVBox.PackStart (progressLabel, false, false, 0);

	        
	        widgetVBox.ShowAll ();
	        Add(widgetVBox);

			TargetEntry[] targets = new TargetEntry[] {
	                		new TargetEntry ("text/uri-list", 0, (uint) DragTargetType.UriList) };

			this.DragDataReceived += DragDataReceivedHandler;

			Gtk.Drag.DestSet(this,
						 DestDefaults.All | DestDefaults.Highlight,
						 targets,
						 Gdk.DragAction.Copy );
		}


		///<summary>
		///	Handles Avatar Events on a Person
		///</summary>
		private void OnPersonAvatarUpdated (Person person)
		{
			// Logger.Debug("Updating presence on {0}", person.DisplayName);
			//RenderWidget();
		}	

		///<summary>
		///	Handles Presence Events on a Person
		///</summary>
		private void OnPersonPresenceUpdated (Person person)
		{
			// Logger.Debug("Updating presence on {0}", person.DisplayName);
//			updateNeeded = true;
			//RenderWidget();
		}

	
		
		
		private void DragDataReceivedHandler (object o, DragDataReceivedArgs args)
		{
/*			//args.Context.
			switch(args.Info) {
				case (uint) DragTargetType.UriList:
				{
                    UriList uriList = new UriList(args.SelectionData);
					string[] paths = uriList.ToLocalPaths();

					if(paths.Length > 0)
					{
						if(!isManual) {
							Application.EnqueueFileSend(serviceInfo, uriList.ToLocalPaths());
						} else {
							// Prompt for the info to send here
						}
					} else {
						// check for a tomboy notes
						foreach(Uri uri in uriList) {
							if( (uri.Scheme.CompareTo("note") == 0) &&
								(uri.Host.CompareTo("tomboy") == 0) ) {
								string[] files = new string[1];
								string tomboyID = uri.AbsolutePath.Substring(1);

								string homeFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
								string path = System.IO.Path.Combine(homeFolder, ".tomboy");
								path = System.IO.Path.Combine(path, tomboyID + ".note");
								files[0] = path;
								Logger.Debug("Go and get the tomboy note {0}", path);
								Application.EnqueueFileSend(serviceInfo, files);
							}
						}
					}
					break;
				}
				default:
					break;
			}
*/
			//Logger.Debug("DragDataReceivedHandler called");
            Gtk.Drag.Finish (args.Context, true, false, args.Time);
		}

		#endregion

		#region Overrides
/*		protected override void OnClicked ()
		{
			Menu popupMenu = new Menu ();
			ImageMenuItem item;
			
      		item = new ImageMenuItem ("Give a File...");
			item.Image = new Gtk.Image(Gtk.Stock.File, IconSize.Button);
			item.Activated += OnSendFile;
			popupMenu.Add (item);
			
			item = new ImageMenuItem("Give a Folder...");
			item.Image = new Gtk.Image(Gtk.Stock.Directory, IconSize.Button);
			item.Activated += OnSendFolder;
			popupMenu.Add (item);
			
			popupMenu.ShowAll();
			popupMenu.Popup ();
		}
*/
		#endregion

		#region Event Handlers
/*
		private void FileTransferStartedHandler (TransferStatusArgs args)
		{
			Gtk.Application.Invoke ( delegate {
				ProgressText = string.Format (
					Catalog.GetString ("Giving: {0}"),
					args.Name);
				progressBar.Text = string.Format (
					Catalog.GetString ("{0} of {1}"),
					args.CurrentCount,
					args.TotalCount);
			});
		}
		
		private void TransferProgressHandler (TransferStatusArgs args)
		{
			double fraction = ((double)args.TotalBytesTransferred) / ((double)args.TotalBytes);

			Gtk.Application.Invoke (delegate {
				progressBar.Fraction = fraction;
			});
		}
		
		private void TransferEndedHandler (TransferStatusArgs args)
		{
			Giver.Application.Instance.FileTransferStarted -= FileTransferStartedHandler;
			Giver.Application.Instance.TransferProgress -= TransferProgressHandler;
			Giver.Application.Instance.TransferEnded -= TransferEndedHandler;

			Gtk.Application.Invoke (delegate {
				progressBar.Hide ();
				ProgressText = string.Empty;
				progressLabel.Hide ();
			});
		}
*/

		private void OnTextChatClicked (object o, EventArgs args)
		{
			ChatWindowManager.InitiateChat(person, ChatType.Text);
		}

		private void OnVideoChatClicked (object o, EventArgs args)
		{
			ChatWindowManager.InitiateChat(person, ChatType.Video);
		}

		#endregion

		#region Public Properties
		public string ProgressText
		{
			set {
				progressLabel.Markup =
					string.Format ("<span size=\"small\" style=\"italic\">{0}</span>",
						value);
			}
		}
		#endregion
		
		#region Public Methods		
		public void UpdateImage (Gdk.Pixbuf newImage)
		{
			Logger.Debug ("TargetService::UpdateImage called");
			this.image.FromPixbuf = newImage;
		}

		public void SetupTransferEventHandlers ()
		{
			Gtk.Application.Invoke ( delegate {
				progressBar.Show ();
				progressLabel.Show ();
			});
/*			Giver.Application.Instance.FileTransferStarted += FileTransferStartedHandler;
			Giver.Application.Instance.TransferProgress += TransferProgressHandler;
			Giver.Application.Instance.TransferEnded += TransferEndedHandler;
*/
		}

		#endregion
		
	}
}



















/*


using System;
using Gecko;
using Gdk;
using Gtk;

namespace Banter
{
	///<summary>
	///	PersonCardSize enum
	/// PersonCards can render various sizes of the widget.  This enumerates them.
	///</summary>
	public enum PersonCardSize : uint
	{
		Small = 1,
		Medium,
		Large
	}
	
	///<summary>
	///	PersonCard
	/// A Gui Widget that renders a Person.
	///</summary>	
	public class PersonCard : Bin
	{
		#region Private Types	
		private Widget child;
		private WebControl webControl;
		private bool updateNeeded;
		private bool widgetRendered;
		private string widgetHtml;
		private Person person;
		private PersonCardSize cardSize;
		#endregion


		#region Constructors
		///<summary>
		///	Constructs a PersonCard from a Person object
		///</summary>			
		public PersonCard(Person person)
		{
			this.person = person;
			this.cardSize = PersonCardSize.Small;
			webControl = new WebControl();
			webControl.Realized += WebControlRealizedHandler;
			webControl.OpenUri += WebControlOpenUriHandler;
			this.Add(webControl);
			updateNeeded = true;			
			widgetRendered = false;

			person.PresenceUpdated += OnPersonPresenceUpdated;
			person.AvatarUpdated += OnPersonAvatarUpdated;
		}
		#endregion


		#region Public Properties
		///<summary>
		///	The Person object the PersonCard is rendering
		///</summary>			
		public Person Person
		{
			get { return person; }
			set {
				Logger.Debug ("FIXME: Implement PersonCard.Person [set] - Do we need to do anything here?");
				// person = value;
			}
		}
		
		
		///<summary>
		///	The size of the card to be rendered
		///</summary>			
		public PersonCardSize Size
		{
			get { return this.cardSize; }
			set { this.cardSize = value; }
		}
		
		#endregion


		#region Protected Methods
		///<summary>
		///	Overrides the OnDestroyed method for a Gtk.Widget
		///</summary>
		protected override void OnDestroyed ()
		{
			person.PresenceUpdated -= OnPersonPresenceUpdated;
			person.AvatarUpdated -= OnPersonAvatarUpdated;			
	
			Logger.Debug("FIXME: the base OnDestroy for the PersonCard is not being called");
			// this is not being called because gtkmozembed blows the next time you try to use
			// it if you destroy it
//			base.OnDestroyed ();
		}


		///<summary>
		///	Overrides the OnAdded method for a Gtk.Widget
		///</summary>		
		protected override void OnAdded (Widget widget)
		{
			base.OnAdded(widget);
			child = widget;
		}


		///<summary>
		///	Overrides the SizeRequested method for a Gtk.Widget
		///</summary>			
		protected override void OnSizeRequested (ref Requisition requisition)
		{
//			Console.WriteLine("OnSizeRequested being called");
			base.OnSizeRequested(ref requisition);
			switch(cardSize) {
				default:
				case PersonCardSize.Small:
					requisition.Height = 26;
					requisition.Width = 128;
					break;
				case PersonCardSize.Medium:
					requisition.Height = 50;
					requisition.Width = 75;
					break;
				case PersonCardSize.Large:
					requisition.Height = 300;
					requisition.Width = 75;
					break;					
			}
		}		


		///<summary>
		///	Overrides the SizeAllocated method for a Gtk.Widget
		///</summary>
		protected override void OnSizeAllocated (Rectangle allocation)
		{
			if(child != null)
			{
				child.SizeAllocate(allocation);
			}
			
			RenderWidget();
		}
		#endregion
		

		#region Private Methods
		///<summary>
		///	Handles the Realized event on the web control
		///</summary>			
		private void WebControlRealizedHandler(object obj, EventArgs args)
		{
			widgetRendered = true;
		}


		///<summary>
		///	Handles the OpenUri event on the web control
		///</summary>			
		private void WebControlOpenUriHandler(object o, OpenUriArgs args)
		{
			if(args.AURI.StartsWith("rtc://TEXT_CHAT")) {
				ChatWindowManager.InitiateChat(person, ChatType.Text);
			}
			else if(args.AURI.StartsWith("rtc://VIDEO_CHAT")) {
				ChatWindowManager.InitiateChat(person, ChatType.Video);
			}

			// set return to true so the web control doesn't attempt to handle the URI
			args.RetVal = true;
		}


		///<summary>
		///	Handles Presence Events on a Person
		///</summary>
		private void OnPersonPresenceUpdated (Person person)
		{
			// Logger.Debug("Updating presence on {0}", person.DisplayName);
			updateNeeded = true;
			RenderWidget();
		}
		

		///<summary>
		///	Handles Avatar Events on a Person
		///</summary>
		private void OnPersonAvatarUpdated (Person person)
		{
			// Logger.Debug("Updating presence on {0}", person.DisplayName);
			updateNeeded = true;
			RenderWidget();
		}


		///<summary>
		///	Handles Avatar Events on a Person
		///</summary>
		private void RenderWidget()
		{		
			if(widgetRendered && updateNeeded) {
				if(ThemeManager.ContactStyle != null) {
					string widgetHtml = ThemeManager.ContactStyle.RenderWidgetHtml(person, cardSize);
					webControl.RenderData(widgetHtml, "file://" + ThemeManager.ContactStyle.Path, "text/html");
					updateNeeded = false;
				}
				else {
					Logger.Debug("PersonCard: ThemeManager.ContactStyle is null, unable to render widget");
				}
			}
		}
		#endregion

	}
}


*/

