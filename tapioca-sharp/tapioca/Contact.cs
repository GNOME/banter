/***************************************************************************
 *  Contact.cs
 *
 *  Copyright (C) 2006 INdT
 *  Written by
 *      Andre Moreira Magalhaes <andre.magalhaes@indt.org.br>
 *      Kenneth Christiansen <kenneth.christiansen@gmail.com>
 *      Renato Araujo Oliveira Filho <renato.filho@indt.org>
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW:
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the Software),
 *  to deal in the Software without restriction, including without limitation
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,
 *  and/or sell copies of the Software, and to permit persons to whom the
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
 *  DEALINGS IN THE SOFTWARE.
 */

using System;
using org.freedesktop;

namespace Tapioca
{
	public delegate void ContactSStatusChangedHandler (Contact sender, ContactSubscriptionStatus status);
	public delegate void ContactAStatusChangedHandler (Contact sender, ContactAuthorizationStatus status);

	public class Contact : ContactBase
	{
		public event ContactSStatusChangedHandler SubscriptionStatusChanged;
		public event ContactAStatusChangedHandler AuthorizationStatusChanged;
		
		bool blocked = false;
		bool hide = false;
		
		ContactSubscriptionStatus status;
		ContactAuthorizationStatus authorization_status;
		ContactListControl control;

//public methods:		
		public bool IsBlocked
		{
			get { return blocked; }
		}
		
		public bool IsHiddenFrom 
		{
			get { return hide; }
		}
		
		public ContactSubscriptionStatus SubscriptionStatus
		{
			get { return status; }
		}
		
		public ContactAuthorizationStatus AuthorizationStatus
		{
			get { return authorization_status; }
		}
		
		public void Subscribe (bool flag)
		{
			control.Subscribe (Handle, flag);
		}
		
		public void Authorize (bool flag)
		{			
			control.Authorize (Handle, flag);
		}
		
		public void HideFrom (bool flag)
		{
			control.HideFrom (Handle, flag);
		}
		
		public void Block (bool flag)
		{
			control.Block (Handle, flag);
		}		
		
//internal methods:
		internal Contact (ContactListControl control, 
				Connection connection, 
				Handle handle, 
				ContactSubscriptionStatus status,
				ContactAuthorizationStatus authorization_status,
				ContactPresence presence, 
				string presence_msg)
				
			: base (connection, handle, presence, presence_msg)
		{ 
			this.status = status;
			this.authorization_status = authorization_status;
			this.control = control;
		}						
		
		internal void Release ()
		{
			this.Handle.Dispose ();
		}
		
		internal void UpdateStatus (ContactSubscriptionStatus status)
		{
			this.status = status;
			if (SubscriptionStatusChanged != null)
				SubscriptionStatusChanged (this, status);
		}
		
		internal void UpdateAuthorize (ContactAuthorizationStatus status)
		{
			this.authorization_status = status;
			if (AuthorizationStatusChanged != null)
				AuthorizationStatusChanged (this, status);			
		}
		
		internal bool Blocked
		{
			set { blocked = value; }
		}
		
		internal bool HiddenFrom
		{
			set { hide = value; }
		}
	}
}
