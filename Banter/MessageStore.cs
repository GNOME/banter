//***********************************************************************
// *  $RCSfile$ - MessageStore.cs
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

using Db4objects;
using Db4objects.Db4o;
using Db4objects.Db4o.Config;

namespace Banter
{
	/// <summary>
	/// Client object interface into the message store
	/// The Message store must be started and running before
	/// client instances can be instantiated.
	/// </summary>
	public class MessageStoreClient
	{
		IObjectContainer oc = null;
		
		public MessageStoreClient ()
		{
       		try	{
       			oc =
       				Db4oFactory.OpenClient(
       					"localhost",
       					MessageStore.serverPort,
						Environment.UserName,
						"rtc" );
       		} catch (Exception msc) {
       			Console.WriteLine (msc.Message);
       			throw new ApplicationException ("Could not connect to the message store");
       		}
		}
		
        public void Commit ()
        {
        	oc.Commit ();
        }
        
		public void LogMessage (Message message)
		{
			oc.Set (message);
		}
		
		public void LogAndCommitMessage (Message message)
		{
			oc.Set (message);
			oc.Commit();
		}
	}
	
    /// <summary>
    ///  Singleton for controlling access to the database
    /// </summary>
    public class MessageStore
    {
    	//private Db4objects.Db4o.Db4oFactory.OpenServer
        //private IObjectServer observer = null;
        static private IObjectServer objectServer = null;
        static public int serverPort = 8737;
        static private string dbdir = "novell-rtc";
        static private string dbname = "rtc.yap";
        static private string locker = "lckr";
        //static private MessageStore instance = null;

		/// <summary>
		/// Return a client object container to the message store.
		/// </summary>
		
        public IObjectContainer ClientOC
        {
        	get
        	{
        		IObjectContainer oc = null;
        	
        		try	{
        			oc =
        				Db4oFactory.OpenClient(
        					"localhost",
        					MessageStore.serverPort,
							Environment.UserName,
							"rtc" );
        		}
        		catch{}
        		return oc;
        	}	
        }
        
		/*		
        static public MessageStore GetInstance()
        {
            if (MessageStore.instance == null) {
                lock (MessageStore.locker)
                    if (MessageStore.instance == null) {
                    	MessageStore.Start();
                    }
            }

            return MessageStore.instance;
        }
        */

		static internal string GetStorePath()
		{
			return		
                String.Format(
                    "{0}{1}{2}",
                    Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData ),
                    Path.DirectorySeparatorChar.ToString(),
                    MessageStore.dbdir );
		}

		/// <summary>
		/// Internal method to startup the message store database
		/// This method should only be called by the rtc monitor application
		/// </summary>
        static internal void Start()
        {
            if (MessageStore.objectServer == null) {
            	lock (MessageStore.locker) {
                	if (MessageStore.objectServer == null) {
			            string configpath = MessageStore.GetStorePath ();
			            if (Directory.Exists (configpath) == false)
			                Directory.CreateDirectory (configpath);

			            string fullpath =
			                String.Format (
			                    "{0}{1}{2}",
			                    configpath,
			                    Path.DirectorySeparatorChar.ToString(),
			                    MessageStore.dbname);
			                    
			            try {
			                Console.WriteLine( "Opening database: {0}", fullpath );
			                MessageStore.objectServer =
			                	Db4oFactory.OpenServer (fullpath, MessageStore.serverPort);
			                MessageStore.objectServer.GrantAccess (Environment.UserName, "rtc");
			            } catch (Exception os) {
			                Console.WriteLine ("Exception occurred opening the database");
			                Console.WriteLine (os.Message);

			                if (MessageStore.objectServer != null)
			                    MessageStore.objectServer.Close ();
			            }
                    }
                }
            }
        }

		/// <summary>
		/// Internal method to shutdown and close the database
		/// This method should only be called the rtc monitor application
		/// </summary>
        static internal void Stop()
        {
        	Console.WriteLine ("Shutting down the message store");
        	lock (MessageStore.locker)
	       		if (MessageStore.objectServer != null)
        			MessageStore.objectServer.Close ();
        }
    }
}
