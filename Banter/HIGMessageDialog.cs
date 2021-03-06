/* This file was originally part of the Tomboy project inside Tomboy/Utils.cs */

using System;
using Gtk;

namespace Banter
{
	public class HIGMessageDialog : Gtk.Dialog
	{
		Gtk.AccelGroup accel_group;
		Gtk.VBox label_vbox;
		Widget extraWidget;

		public HIGMessageDialog (Gtk.Window parent,
					 Gtk.DialogFlags flags,
					 Gtk.MessageType type,
					 Gtk.ButtonsType buttons,
					 string          title,
					 string          header,
					 string          msg)
			: base ()
		{
			HasSeparator = false;
			BorderWidth = 5;
			Resizable = false;
			Title = title;

			VBox.Spacing = 12;
			ActionArea.Layout = Gtk.ButtonBoxStyle.End;

			accel_group = new Gtk.AccelGroup ();
			AddAccelGroup (accel_group);

			Gtk.HBox hbox = new Gtk.HBox (false, 12);
			hbox.BorderWidth = 5;
			hbox.Show ();
			VBox.PackStart (hbox, false, false, 0);

			Gtk.Image image = null;

			switch (type) {
			case Gtk.MessageType.Error:
				image = new Gtk.Image (Gtk.Stock.DialogError, 
						       Gtk.IconSize.Dialog);
				break;
			case Gtk.MessageType.Question:
				image = new Gtk.Image (Gtk.Stock.DialogQuestion, 
						       Gtk.IconSize.Dialog);
				break;
			case Gtk.MessageType.Info:
				image = new Gtk.Image (Gtk.Stock.DialogInfo, 
						       Gtk.IconSize.Dialog);
				break;
			case Gtk.MessageType.Warning:
				image = new Gtk.Image (Gtk.Stock.DialogWarning, 
						       Gtk.IconSize.Dialog);
				break;
			}

			image.Show ();
			image.Yalign = 0;
			hbox.PackStart (image, false, false, 0);
			
			label_vbox = new Gtk.VBox (false, 8);
			label_vbox.Show ();
			hbox.PackStart (label_vbox, true, true, 0);

			string message = String.Format ("<span weight='bold' size='larger'>{0}" +
						      "</span>\n",
						      header);

			Gtk.Label label;

			label = new Gtk.Label (message);
			label.UseMarkup = true;
			label.Justify = Gtk.Justification.Left;
			label.LineWrap = true;
			label.SetAlignment (0.0f, 0.5f);
			label.Show ();
			label_vbox.PackStart (label, false, false, 0);

			label = new Gtk.Label (msg);
			label.UseMarkup = true;
			label.Justify = Gtk.Justification.Left;
			label.LineWrap = true;
			label.SetAlignment (0.0f, 0.5f);
			label.Show ();
			label_vbox.PackStart (label, false, false, 0);
			
			switch (buttons) {
			case Gtk.ButtonsType.None:
				break;
			case Gtk.ButtonsType.Ok:
				AddButton (Gtk.Stock.Ok, Gtk.ResponseType.Ok, true);
				break;
			case Gtk.ButtonsType.Close:
				AddButton (Gtk.Stock.Close, Gtk.ResponseType.Close, true);
				break;
			case Gtk.ButtonsType.Cancel:
				AddButton (Gtk.Stock.Cancel, Gtk.ResponseType.Cancel, true);
				break;
			case Gtk.ButtonsType.YesNo:
				AddButton (Gtk.Stock.No, Gtk.ResponseType.No, false);
				AddButton (Gtk.Stock.Yes, Gtk.ResponseType.Yes, true);
				break;
			case Gtk.ButtonsType.OkCancel:
				AddButton (Gtk.Stock.Cancel, Gtk.ResponseType.Cancel, false);
				AddButton (Gtk.Stock.Ok, Gtk.ResponseType.Ok, true);
				break;
			}

			if (parent != null)
				TransientFor = parent;

			if ((int) (flags & Gtk.DialogFlags.Modal) != 0)
				Modal = true;

			if ((int) (flags & Gtk.DialogFlags.DestroyWithParent) != 0)
				DestroyWithParent = true;
		}

		void AddButton (string stock_id, Gtk.ResponseType response, bool is_default)
		{
			Gtk.Button button = new Gtk.Button (stock_id);
			button.CanDefault = true;
			button.Show ();

			AddActionWidget (button, response);

			if (is_default) {
				DefaultResponse = response;
/*				button.AddAccelerator ("activate",
						       accel_group,
						       (uint) Gdk.Key.Escape, 
						       0,
						       Gtk.AccelFlags.Visible);
*/
			}
		}
		
		/// <summary>
		/// Allows the consumer of this dialog to add an extra widget.
		/// </summary>
		public Gtk.Widget ExtraWidget
		{
			get { return extraWidget; }
			set {
				if (extraWidget != null) {
					label_vbox.Remove (extraWidget);
					if (extraWidget != value)
						extraWidget.Destroy ();
				}
				
				extraWidget = value;
				
				if (extraWidget != null) {
					label_vbox.PackStart (extraWidget, true, true, 0);
					extraWidget.ShowAll ();
				}
			}
		}
	}
}
