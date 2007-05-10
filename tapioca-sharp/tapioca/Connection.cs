/***************************************************************************
 *  Connection.cs
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
using System.Collections;
using System.Collections.Generic;
using NDesk.DBus;
using ObjectPath = NDesk.DBus.ObjectPath;
using org.freedesktop.DBus;
using org.freedesktop.Telepathy;

namespace Tapioca
{
	namespace Exceptions
	{
		class ConnectionOpened : System.Exception { }
	}

	public delegate void ConnectionStatusChangedHandler (Connection sender, ConnectionStatus status, ConnectionStatusReason reason);
	public delegate void ConnectionChannelCreatedHandler (Connection sender, Tapioca.Channel channel);

	public class Connection : DBusProxyObject, IDisposable
	{
		public event ConnectionStatusChangedHandler StatusChanged;
		public event ConnectionChannelCreatedHandler ChannelCreated;

		//Implemented interfaces
		bool implements_aliasing = false;
		bool implements_avatars = false;
		bool implements_capabilities = false;
		bool implements_contactinfo = false;
		bool implements_forwarding = false;
		bool implements_renaming = false;
		bool implements_presence = false;
		bool implements_privacy = false;


		IConnection tlp_connection;

		ContactList contact_list;
		System.Collections.ArrayList channel_list;
		string protocol;
		ConnectionStatus status;
		UserContact connection_info;

//public methods:
		public string Protocol
		{
			get {
				return protocol;
			}
		}
		
		public UserContact Info
		{
			get {
				return connection_info;
			}
		}

		public string Name
		{
			get {
				uint[] ids = {tlp_connection.SelfHandle};
				string[] names = tlp_connection.InspectHandles (HandleType.Contact, ids);
				if (names.Length > 0)
					return names[0];
				return "";
			}
		}

		public ContactList ContactList
		{
			get {
				return contact_list;
			}
		}

		public Channel[] OpenedChannels
		{
			get {
				if (channel_list.Count > 0)
					return (Channel[]) channel_list.ToArray (typeof (Channel));
				else
					return new Channel[0];
			}
		}

		public ConnectionStatus Status
		{
			get {
				return status;
			}
		}

		public void Connect (ContactPresence initial_presence)
		{
			//TODO: implement initial presence
			TlpConnection.Connect ();
		}

		public void Disconnect ()
		{
			TlpConnection.Disconnect ();
		}

		public Channel CreateChannel (ChannelType type, ChannelTarget target)
		{
			if (status != ConnectionStatus.Connected) return null;

			switch (type)
			{
				case ChannelType.Text:
				{
					ObjectPath obj_path = TlpConnection.RequestChannel (org.freedesktop.Telepathy.ChannelType.Text, target.Handle.Type, target.Handle.Id, true);
					IChannelText channel = Bus.Session.GetObject<IChannelText> (ServiceName, obj_path);
					TextChannel text_channel = new TextChannel (this, channel, (Contact) target, ServiceName, obj_path);
					

					if (text_channel != null)
						channel_list.Add (text_channel);

					if (ChannelCreated != null)
						ChannelCreated (this, (Channel) text_channel);

					return (Channel) text_channel;
				}
				case ChannelType.StreamedMedia:
				{
					ObjectPath obj_path = TlpConnection.RequestChannel (org.freedesktop.Telepathy.ChannelType.StreamedMedia, target.Handle.Type, target.Handle.Id, true);
					IChannelStreamedMedia channel = Bus.Session.GetObject<IChannelStreamedMedia> (ServiceName, obj_path);
					
					StreamChannel stream_channel = new StreamChannel (this, channel, (Contact) target, ServiceName, obj_path);
					if (stream_channel != null)
						channel_list.Add (stream_channel);

					if (ChannelCreated != null)
						ChannelCreated (this, (Channel) stream_channel);

					return (Channel) stream_channel;
				}
				default:
					throw new NotImplementedException("Still needs to be designed");
			}
		}

		public bool SupportAliasing
		{
			get { return implements_aliasing; }
		}

		public bool SupportAvatars
		{
			get { /*return true; */ return implements_avatars; }
		}

		public bool SupportCapabilities
		{
			get { return implements_capabilities; }
		}

		public bool SupportContactInfo
		{
			get { return implements_contactinfo; }
		}

		public bool SupportForwarding
		{
			get { return implements_forwarding; }
		}

		public bool SupportRenaming
		{
			get { return implements_renaming; }
		}

		public bool SupportPresence
		{
			get {return implements_presence; }
		}

		public bool SupportPrivacy
		{
			get {return implements_privacy; }
		}

		public void Dispose ()
		{
			if (status == ConnectionStatus.Connected) 
				throw new Exceptions.ConnectionOpened();

			contact_list = null;
			channel_list = null;
			GC.SuppressFinalize (this);
		}


//internal methods:

		// ctor
		internal Connection (string protocol, IConnection connection, string bus_name, ObjectPath object_path)
			: base (bus_name, object_path)
		{
			this.protocol = protocol;
			this.tlp_connection = connection;
			this.TlpConnection.StatusChanged += OnConnectionStateChanged;
			this.TlpConnection.NewChannel += OnNewChannel;
			
			contact_list = new ContactList (this);
			channel_list = new System.Collections.ArrayList ();			
		}
		
		internal static Connection Load (IConnection connection, string bus_name, ObjectPath object_path)
		{
			Tapioca.Connection conn = new Connection (connection.Protocol, connection, bus_name, object_path);
			conn.ContactList.LoadContacts ();
			return conn;
		}

		internal IConnection TlpConnection
		{
			get { return this.tlp_connection; }
		}


//private methods:
		private void OnConnectionStateChanged (org.freedesktop.Telepathy.ConnectionStatus status,
			org.freedesktop.Telepathy.ConnectionStatusReason reason)
		{
			this.status = (ConnectionStatus) status;
			switch (status) {
				case org.freedesktop.Telepathy.ConnectionStatus.Connected:
					SetupConnection ();
					contact_list.LoadContacts ();
					break;
				case org.freedesktop.Telepathy.ConnectionStatus.Disconnected:
					Close ();
					break;
				default:
					break;
			}

			if (StatusChanged != null) {
				StatusChanged (this, (ConnectionStatus) status, (ConnectionStatusReason) reason);
			}			
		}

		private void Close ()
		{
			channel_list.Clear ();
			contact_list.Clear ();
			connection_info.Presence = ContactPresence.Offline;
			
			implements_aliasing = false;
			implements_avatars = false;
			implements_capabilities = false;
			implements_contactinfo = false;
			implements_forwarding = false;
			implements_renaming = false;
			implements_presence = false;
			implements_privacy = false;
		}
		
		private void OnNewChannel (ObjectPath channel, string channel_type, HandleType handle_type, uint handle, bool suppress_handler)
		{	
			Console.WriteLine ("New Channel {0}", channel_type);
			switch (channel_type)
			{
				case org.freedesktop.Telepathy.ChannelType.Text:
				{
					IChannelText ichannel = Bus.Session.GetObject<IChannelText> (ServiceName, channel);
					
					if (handle == 0)
						return;
					uint[] ids = {handle};
					Contact contact = contact_list.ContactLookup (ids[0]);
					if (contact == null)
						return;
					TextChannel text_channel = new TextChannel (this, ichannel, contact, ServiceName, channel);	
					if ((text_channel != null) && (!channel_list.Contains (text_channel)))
						channel_list.Add (text_channel);

					if (ChannelCreated != null)
						ChannelCreated (this, (Channel) text_channel);						
					break;
				}
				case org.freedesktop.Telepathy.ChannelType.StreamedMedia:
				{
					IChannelStreamedMedia ichannel = Bus.Session.GetObject<IChannelStreamedMedia> (ServiceName, channel);
					
					if (handle == 0) {
						if (ichannel.RemotePendingMembers.Length > 0)
							handle = ichannel.RemotePendingMembers[0];
						else if (ichannel.Members.Length > 0)
							handle = ichannel.Members[0];
						else
							return;
					}
						
					uint[] ids = {handle};
					Contact contact = contact_list.ContactLookup (ids[0]);
					if (contact == null)
						return;
						
					StreamChannel stream_channel = new StreamChannel (this, ichannel, contact, ServiceName, channel);	
					if ((stream_channel != null) && (!channel_list.Contains (stream_channel)))
						channel_list.Add (stream_channel);

					if (ChannelCreated != null)
						ChannelCreated (this, (Channel) stream_channel);
					break;
				}	
				default:
					break;
			}
		}

		private void SetupConnection ()
		{
			foreach (string interface_name in TlpConnection.Interfaces)
			{
				Console.WriteLine ("Implement interface {0}", interface_name);

				switch (interface_name)
				{
					case "org.freedesktop.Telepathy.Connection.Interface.Aliasing":
						implements_aliasing = true;
						break;
					case "org.freedesktop.Telepathy.Connection.Interface.Avatars":
						implements_avatars = true;
						break;
					case "org.freedesktop.Telepathy.Connection.Interface.Capabilities":
						implements_capabilities = true;
						break;
					case "org.freedesktop.Telepathy.Connection.Interface.ContactInfo":
						implements_contactinfo = true;
						break;
					case "org.freedesktop.Telepathy.Connection.Interface.Forwarding":
						implements_forwarding = true;
						break;
					case "org.freedesktop.Telepathy.Connection.Interface.Renaming":
						implements_renaming = true;
						break;
					case "org.freedesktop.Telepathy.Connection.Interface.Presence":
						implements_presence = true;
						break;
					case "org.freedesktop.Telepathy.Connection.Interface.Privacy":
						implements_privacy = true;
						break;
					default:
						Console.WriteLine ("Interface {0} is unknown", interface_name);
						break;
				}
				
				//TODO: init with real presence
				Handle self = new Handle (tlp_connection, HandleType.Contact, tlp_connection.SelfHandle);
				connection_info = new UserContact (this, self, ContactPresence.Available, "");
			}
		}
	}
}
