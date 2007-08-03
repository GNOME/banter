//***********************************************************************
// *  $RCSfile$ - PersonView.cs
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
	public class PersonView : EventBox
	{
		#region fields
		private VBox vbox;
//		private Widget parentWidget;
//		private bool alreadyDisposed;
		private Dictionary<TreeIter, PersonCard> personCardMap;
		private TreeModel model;
		private PersonCardSize personCardSize;
		#endregion

		
		#region Public Constructors
		public PersonView (Widget parentWidget)
		{
			Init (parentWidget, null);
		}
		
		public PersonView (Widget parentWidget, TreeModel personModel)
		{
			Init (parentWidget, personModel);
		}
		#endregion

		
		#region Public Properties
		public TreeModel Model
		{
			get { return model; }
			set {
				if (model != null) {
					model.RowInserted -= OnPersonRowInserted;
					model.RowDeleted -= OnPersonRowDeleted;
					model.RowChanged -= OnPersonRowChanged;
				}
				model = value;
				
				if (model != null) {
					model.RowInserted += OnPersonRowInserted;
					model.RowDeleted += OnPersonRowDeleted;
					model.RowChanged += OnPersonRowChanged;
				}

				PopulatePersonView ();
			}
		}
		
		public PersonCardSize PersonCardSize
		{
			get { return personCardSize; }
			set {
				Logger.Debug ("FIXME: PersonView should handle card sizing better: {0}", value);
				personCardSize = value;
				// PopulatePersonView();
			}
		}
		#endregion


		#region Public Methods
		public void EnableRemoveButtons(bool enable)
		{
			foreach (Widget child in vbox.Children) {
				PersonCard card = (PersonCard) child;
				card.ShowRemoveButton = enable;
			}
		}

		#endregion


		#region Private Methods
		private void Init (Widget parentWidget, TreeModel personModel)
		{
			this.ModifyBg (StateType.Normal, this.Style.Base (StateType.Normal));
			this.ModifyBase (StateType.Normal, this.Style.Base (StateType.Normal));
			this.CanFocus = true;
			
			vbox = new VBox (false, 0);
			this.Add (vbox);
			
			personCardMap = new Dictionary<TreeIter, PersonCard> ();
			
			personCardSize = PersonCardSize.Small;
			
			Model = personModel;
		}
		
		private void PopulatePersonView ()
		{
			List<Widget> children = new List<Widget> (vbox.Children);
			foreach (Widget child in children) {
				vbox.Remove (child);
				try {
					child.Destroy ();
				} catch {}
			}
			
			// personCardMap.Clear ();
			
			if (model == null) {
				Logger.Debug ("PersonView.PopulatePersonView returning since the model is null.");
				return;
			}
			
			TreeIter iter;
						
			// Loop through the model, create the PersonCard objects and add
			// them into the vbox.
			if (model.GetIterFirst (out iter)) {
				do {
					Person person = model.GetValue (iter, 0) as Person;
					if (person == null)
						continue;

					// don't put yourself in the view
					if (person.IsMe)
						continue;
					
					TreePath path = model.GetPath (iter);
					PersonCard card = new PersonCard(person);
					card.Size = personCardSize;
					card.ShowAll ();
					vbox.PackStart (card, false, false, 0);
					vbox.ReorderChild(card, path.Indices [0]);
					// personCardMap[iter] = card;
				} while (model.IterNext (ref iter));
			}
		}
		#endregion

		
		#region Method Overrides
		protected override bool OnDeleteEvent (Gdk.Event evnt)
		{
			model.RowInserted -= OnPersonRowInserted;
			model.RowDeleted -= OnPersonRowDeleted;
			model.RowChanged -= OnPersonRowChanged;

			return base.OnDeleteEvent (evnt);
		}
		#endregion

		
		#region Event Handlers
		private void OnPersonRowInserted (object sender, RowInsertedArgs args)
		{
//			Logger.Debug("PersonView:OnPersonRowInserted Called");
			TreePath path = model.GetPath (args.Iter);
			PersonCard card = new PersonCard();

			Person person = model.GetValue (args.Iter, 0) as Person;
			if (person != null) {
				// don't put yourself in the view
				if (person.IsMe) {
					return;					
				}
				card.Person = person;
			}

			card.Size = personCardSize;
			card.ShowAll ();
			vbox.PackStart (card, false, false, 0);
			vbox.ReorderChild(card, path.Indices [0]);
			personCardMap[args.Iter] = card;

		}
		
		private void OnPersonRowDeleted (object sender, RowDeletedArgs args)
		{
//			Logger.Debug("PersonView:OnPersonRowDeleted Called");
			PersonCard card = (PersonCard) vbox.Children[args.Path.Indices [0]];
			vbox.Remove(card);
			foreach(TreeIter iter in personCardMap.Keys) {
				if(card == personCardMap[iter]) {
					personCardMap.Remove(iter);

					return;
				}
			}

		}
		
		private void OnPersonRowChanged (object sender, RowChangedArgs args)
		{
//			Logger.Debug("PersonView:OnPersonRowChanged Called");
			PersonCard card = personCardMap[args.Iter];
			if(card != null) {
				if(card.Person == null) {
					Person person = model.GetValue (args.Iter, 0) as Person;
					if (person != null) {
						card.Person = person;
						if(person.IsMe) {
							vbox.Remove(card);
							foreach(TreeIter iter in personCardMap.Keys) {
								if(card == personCardMap[iter]) {
									personCardMap.Remove(iter);
									return;
								}
							}
							return;
						}
					}
				}

				vbox.ReorderChild(card, args.Path.Indices [0]);
			}
		}
		#endregion
	}
}
