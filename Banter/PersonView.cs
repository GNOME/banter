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
		private Dictionary<int, PersonCard> personCardMap;
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
//		public override void Dispose ()
//		{
//			Dispose (false);
//		}
		#endregion


		#region Public Events
		#endregion


		#region Private Methods
		private void Init (Widget parentWidget, TreeModel personModel)
		{
			this.ModifyBg (StateType.Normal, this.Style.Base (StateType.Normal));
			this.ModifyBase (StateType.Normal, this.Style.Base (StateType.Normal));
			this.CanFocus = true;
			// this.parentWidget = parentWidget;
//			this.alreadyDisposed = false;
			
			vbox = new VBox (false, 0);
			this.Add (vbox);
			
			personCardMap = new Dictionary<int, PersonCard> ();
			
			personCardSize = PersonCardSize.Small;
			
			Model = personModel;

//			parentWidget.SizeAllocated +=
//				new SizeAllocatedHandler (OnSizeAllocated);
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
			
			personCardMap.Clear ();
			
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
					AddPersonCard (path.Indices [0], card);
				} while (model.IterNext (ref iter));
			}
		}
		
		private void AddPersonCard (int treePathIndex, PersonCard personCard)
		{
			if (personCardMap.ContainsKey (treePathIndex)) {
				Logger.Debug ("This person has already been added: {0}", personCard.Person.DisplayName);
				return;
			}

			if(personCard.Person.IsMe) {
				Logger.Debug("Someone tried to add a PersonCard with me in it");
				return;
			}
			
			// Logger.Debug ("Adding person card: {0}", personCard.Person.DisplayName);
			personCard.ShowAll ();
			vbox.PackStart (personCard, false, false, 0);
			personCardMap [treePathIndex] = personCard;
		}

//		~PersonView()
//		{
//			Dispose (true);
//		}
		
//		public void AddPerson (Person person)
//		{
//			PersonCard card = new PersonCard (person);
//			card.Show ();
//			vbox.PackStart (card, false, true, 0);
//			people [person] = card;
//		}

//		private void Dispose (bool calledFromFinalizer)
//		{
//			if (!alreadyDisposed)
//			{
//				alreadyDisposed = true;
//				parentWidget.SizeAllocated -=
//					new SizeAllocatedHandler (OnSizeAllocated);
//				
//				if (!calledFromFinalizer)
//					GC.SuppressFinalize (this);
//			}
//		}

//		private void OnSizeAllocated(object sender, SizeAllocatedArgs args)
//		{
//			foreach (PersonCard card in people.Values) {
//				card.OnSizeAllocated (sender, args);
//			}
//		}
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
			Person person =
					model.GetValue (args.Iter, 0) as Person;
			if (person == null)
				return;

			// don't put yourself in the view
			if (person.IsMe)
				return;
			
			TreePath path = model.GetPath (args.Iter);
			PersonCard card = new PersonCard(person);
			card.Size = personCardSize;
			AddPersonCard (path.Indices [0], card);
		}
		
		private void OnPersonRowDeleted (object sender, RowDeletedArgs args)
		{
			if (personCardMap.ContainsKey (args.Path.Indices [0]) == false) {
				Logger.Debug ("PersonView.OnPersonRowDeleted () called on a path we don't know about.");
				return;
			}
			
			PersonCard personCard = personCardMap [args.Path.Indices [0]];
			
			Logger.Debug ("PersonView.OnPersonRowDeleted removing person: {0}", personCard.Person.DisplayName);
			
			personCardMap.Remove (args.Path.Indices [0]);

			vbox.Remove (personCard);
			
			// FIXME: Determine whether we should be calling personCard.Destroy () here.
			personCard.Destroy ();
		}
		
		private void OnPersonRowChanged (object sender, RowChangedArgs args)
		{
			Person person =
					model.GetValue (args.Iter, 0) as Person;
			if (person == null)
				return;

			// don't put yourself in the view
			if (person.IsMe)
				return;

			if (personCardMap.ContainsKey (args.Path.Indices [0]) == false) {
				Logger.Debug ("PersonView.OnPersonRowChanged() called with unknown path, adding it now.");
				PersonCard card = new PersonCard(person);
				card.Size = personCardSize;
				AddPersonCard (args.Path.Indices [0], card);
				return;
			}
			
			PersonCard personCard = personCardMap [args.Path.Indices [0]];
			
			Logger.Debug ("PersonView.OnPersonRowChanged updating person: {0} -> {1}",
					personCard.Person.DisplayName,
					person.DisplayName);
			
			// Update the card's person
			personCard.Person = person;
		}
		#endregion
	}
}
