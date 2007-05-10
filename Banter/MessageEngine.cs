//***********************************************************************
// *  $RCSfile$ - MessageEngine.cs
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
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

using NDesk.DBus;
using org.freedesktop.DBus;
using org.freedesktop.Telepathy;

namespace Novell.Rtc
{
	internal class MessageContext
	{
		Conversation conversation;
		Message message;
		
		internal Message Message
		{
			get {return message;}
		}
			
		internal Conversation Conversation
		{
			get {return conversation;}
		}
		
		internal MessageContext (Conversation conversation, Message message)
		{
			this.conversation = conversation;
			this.message = message;
		}
	}
	
	/// <summary>
	///	Message engine is responsible for dispatching messages to
	/// the appropriate providers such as a telepathy based jabber
	/// provider and/or the log store.
	/// </summary>
	internal class MessageEngine
	{
		static private string locker = "lckr";
		static private bool started = false;
		static private bool stop = false;
		static private Thread engineThread = null;
		static private AutoResetEvent stopEvent;
		static private int maxWaitTime = ( 300 * 1000 );
		static private int waitTime = ( 5 * 1000 );
		static private Queue messages;
	
	
		/// <summary>
		/// Internal method to startup the message store database
		/// This method should only be called by the rtc monitor application
		/// </summary>
        static internal void Start()
        {
        	Console.WriteLine ("Messaging engine starting up");

			try
			{
				lock (locker)
				{
					if (started == false)
					{
						stopEvent = new AutoResetEvent (false);
						messages = new Queue();
						MessageEngine.engineThread = new Thread (new ThreadStart (MessageEngine.MessageDispatchThread));
						engineThread.IsBackground = true;
						engineThread.Priority = ThreadPriority.Normal;
						engineThread.Start();
					}
				}
			}
			catch (Exception e)
			{
				Console.WriteLine (e.Message);
				throw e;
			}
        }

		/// <summary>
		/// Internal method to shutdown and close the database
		/// This method should only be called the rtc monitor application
		/// </summary>
        static internal void Stop()
        {
        	Console.WriteLine ("Messaging engine shutting down");
			try
			{
				lock (MessageEngine.locker)
				{
					// Set state and then signal the event
					MessageEngine.stop = true;
					MessageEngine.stopEvent.Set();
					Thread.Sleep( 0 );
				}
			}
			catch ( Exception e ) {
				Console.WriteLine ("Exception shutting down the message engine");
				Console.WriteLine (e.Message);
				throw e;
			}
        }
        
		/// <summary>
		/// Messaging Engine Thread.
		/// </summary>
		static private void MessageDispatchThread()
		{
			Console.WriteLine ("MessageDispatchThread started");
			
			// Let the caller know we're good to go
			MessageEngine.started = true;
			MessageContext ctx;

			do
			{
				if (MessageEngine.stop == false)
				{
					ctx = null;
					MessageEngine.stopEvent.WaitOne (waitTime, false);
					if (MessageEngine.stop == true)
						continue;

					// Empty the queue before going back to sleep
					while (MessageEngine.messages.Count > 0) {
						lock (MessageEngine.locker) {
							ctx = MessageEngine.messages.Dequeue () as MessageContext;
						}
					
						if (ctx != null) {
							TextMessage txtMessage = (TextMessage) ctx.Message;
							ctx.Conversation.TextChannel.Send (
								org.freedesktop.Telepathy.MessageType.Normal,
								txtMessage.Text);
						}
					}

					waitTime = ( waitTime * 2 < maxWaitTime ) ? waitTime * 2 : maxWaitTime;
				}

			} while (MessageEngine.stop == false);

			MessageEngine.started = false;
			MessageEngine.stopEvent.Close();
			MessageEngine.stopEvent = null;
		}
		
		/// <summary>
		/// static method to submit a message to the messaging engine
		/// TODO: Add a delegate to notify the caller when the message has been sent
		/// </summary>
		static internal void DispatchMessage (Conversation conversation, Message message)
		{
			Console.WriteLine ("MessageEngine::DispatchMessage - called");
			MessageContext ctx = new MessageContext (conversation, message);
			lock (MessageEngine.locker) {
				MessageEngine.messages.Enqueue( ctx );
			}
			
			MessageEngine.stopEvent.Set();
		}

		/*
		#region IDisposable Members
		/// <summary>
		/// Dispose
		/// </summary>
		public void Dispose()
		{
			Stop();
		}
		#endregion
		*/
	}
}