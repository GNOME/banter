// SidebarTextButton.cs created with MonoDevelop
// User: calvin at 12:50 PM 8/3/2007
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using Gtk;

namespace Banter
{
	public class SidebarTextButton : Gtk.Button
	{
		private HBox hbox;
		private Label buttonLabel;

		public new string Label
		{
			get { return buttonLabel.Text; }
			set { buttonLabel.Text = value; }
		}

		public SidebarTextButton(string text) : base()
		{
			hbox = new HBox (false, 0);
			buttonLabel = new Label (text);
			buttonLabel.Xalign = 0;
			buttonLabel.UseUnderline = false;
			buttonLabel.UseMarkup = true;
			buttonLabel.Show ();
			hbox.PackStart (buttonLabel, false, false, 4);

			this.Child = hbox;
			//this(hbox);
			Relief = ReliefStyle.None;
		}
	}
}
