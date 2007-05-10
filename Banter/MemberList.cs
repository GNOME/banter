//***********************************************************************
// *  $RCSfile$ - MemberList.cs
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

using NDesk.DBus;
using org.freedesktop.DBus;
using org.freedesktop.Telepathy;

namespace Novell.Rtc
{
	///<summary>
	///	MemberList Class
	///</summary>
	public class MemberList : IDisposable
	{
		#region Private Types
		private bool online = false;
		private bool presenceConnected = false;
		private bool aliasConnected = false;
		private Connection connection;
		private Account account;
		private System.Collections.Hashtable members;
		private uint selfID;
		
		// Private types that must be changed when we 
		// support more than one account
		private string username;
		ObjectPath memberListPath;
		IChannelGroup memberListGroup;
		#endregion

		#region Constructors	
		/// <summary>
		///
		/// </summary>
		internal MemberList(Connection connection)
		{
			Console.WriteLine ("MemberList Constructor - called");
			this.connection = connection;
			this.account = connection.Account;
			this.members = new System.Collections.Hashtable ();
			this.selfID = connection.TlpConnection.SelfHandle;
			
			// Connect up the presence, alias and avatar handlers
			this.ConnectHandlers();
			this.LoadMembers();			
		}
		#endregion
		
		#region Private Methods
		private void LoadMembers ()
		{
			Console.WriteLine ("Load members");
			try
			{
				IConnection conn = connection.TlpConnection;
				
				// Now retrieve all the members or buddies
				string[] args = {"subscribe"};
				uint[] handles = conn.RequestHandles (HandleType.List, args);
				ObjectPath op = conn.RequestChannel (ChannelType.ContactList, HandleType.List, handles[0], true);
				IChannelGroup cl = 
					Bus.Session.GetObject<IChannelGroup> (account.TelepathyBusName, op);
					
				// FIX - need to verify this code
				// what happens when a user has a screen name and no alias
				uint[] memberHandles = cl.Members;
				string[] memberNames = conn.InspectHandles (HandleType.Contact, memberHandles);
				string[] aliasNames;
				
				if (connection.SupportAliasing == true) {
					aliasNames = conn.RequestAliases (memberHandles);
					for (int i = 0; i < memberNames.Length; i++) {
						members.Add( 
							memberHandles[i], 
							new Member(connection, memberHandles[i], memberNames[i], aliasNames[i], MemberPresence.Available, String.Empty));
					}
				} else {
					for (int i = 0; i < memberNames.Length; i++) {
						members.Add( 
							memberHandles[i], 
							new Member(connection, memberHandles[i], memberNames[i], String.Empty, MemberPresence.Available, String.Empty));
					}
				}
				
				// Now request presence for all discovered members
				conn.RequestPresence (memberHandles);
				
			} catch (Exception gmf) {
				Console.WriteLine ("Exception getting subscribed members - message: {0}", gmf.Message);
				throw gmf;
			}
		}
		
		private void CleanupMembers ()
		{
			DisconnectHandlers ();
			
			if (this.members == null)
				return;
				
			/*
			foreach (Member member in members.Values) {
				member.Dispose();
			}
			*/
			
			members.Clear ();
			members = null;
		}
		
		private void ConnectHandlers ()
		{
			Console.WriteLine ("MemberList::ConnectHandlers");
			if (this.connection.SupportAliasing == true) {
				this.connection.TlpConnection.AliasesChanged += OnAliasesChanged;
				aliasConnected = true;
			}
			
			if (this.connection.SupportPresence == true) {
				this.connection.TlpConnection.PresenceUpdate += OnPresenceUpdate;
				presenceConnected = true;
			}
		}
		
		private void DisconnectHandlers ()
		{
			Console.WriteLine ("MemberList::DisconnectHandlers");
			if (presenceConnected == true) {
				this.connection.TlpConnection.PresenceUpdate -= OnPresenceUpdate;
				presenceConnected = false;
			}
			
			if (aliasConnected) {
				this.connection.TlpConnection.AliasesChanged -= OnAliasesChanged;
				aliasConnected = false;
			}
		}
		
		private void OnAliasesChanged (AliasInfo[] aliases)
		{
			foreach (AliasInfo info in aliases) {
				if (members.ContainsKey (info.ContactHandle)) {
					Member member = members[info.ContactHandle] as Member;
					member.UpdateAlias (info.NewAlias);
				}
			}
		}
		
		private void OnPresenceUpdate (IDictionary<uint, PresenceUpdateInfo> infos)
		{
			Console.WriteLine ("OnPresenceUpdate - called");
			foreach (KeyValuePair<uint, PresenceUpdateInfo> entry in infos)
			{
				Console.WriteLine ("  key: {0}", entry.Key);
				Member member = MemberLookup (entry.Key);
				if (member == null ) return;
				
				foreach (KeyValuePair<string, IDictionary<string, object>> info in entry.Value.info)
				{
					string message = String.Empty;
					foreach (KeyValuePair<string, object> val in info.Value)
					{
						if (val.Key == "message")
							message = val.Value as String;
					}
					
					member.UpdatePresence (info.Key, message);
				}
			}
		}
		#endregion
		
		#region Internal Methods
		internal void Clear ()
		{
			CleanupMembers ();
		}
		
		/// <summary>
		/// Look up a member in the internal 
		/// hashtable based on their id or handle
		/// </summary>
		internal Member MemberLookup (uint id)
		{
			if (members.ContainsKey (id) == true)
				return members[id] as Member;
			return null;
		}
		#endregion
		
		#region Public Methods
		
		public void Dispose ()
		{
			Console.WriteLine ("MemberList::Disposed - called");
			//CleanupMembers ();
		}
		
		// TODO add check for subscribed members
		public Member[] GetMembers ()
		{
			System.Collections.ArrayList subscribedMembers = 
				new System.Collections.ArrayList();
			
			foreach (Member member in members.Values) {
				Console.WriteLine ("adding {0} to the return list", member.ScreenName);
				subscribedMembers.Add (member);
			}
			
			if (subscribedMembers.Count > 0) 
				return (Member[]) subscribedMembers.ToArray (typeof (Member));
			else
				return new Member[0];
		}
		
		/// <summary>
		/// Method to lookup up a member by screen name
		/// </summary>
		public Member LookupMemberByName (string name)
		{
			Member member = null;
			string nameLower = name.ToLower();
			
			foreach (Member m in members.Values) {
				if (m.ScreenName.ToLower() == nameLower) {
					member = m;
					break;
				}
			}
			
			if (member == null)
				throw new ApplicationException (String.Format ("{0} not found", name));
				
			return member;
		}
		#endregion
	}
}	
