//***********************************************************************
// *  $RCSfile$ - Logger.cs
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
using System.IO;

namespace Banter
{
	public enum LogLevel { Debug, Info, Warn, Error, Fatal };
	
	public interface ILogger
	{
		void Log (LogLevel lvl, string message, params object[] args);
	}
	
	class NullLogger : ILogger
	{
		public void Log (LogLevel lvl, string msg, params object[] args)
		{
		}
	}

	class ConsoleLogger : ILogger
	{
		public void Log (LogLevel lvl, string msg, params object[] args)
		{
			msg = string.Format ("[{0}]: {1}", Enum.GetName (typeof (LogLevel), lvl), msg);
			Console.WriteLine (msg, args);
		}
	}

	class FileLogger : ILogger
	{
		StreamWriter log;
		ConsoleLogger console;

		public FileLogger ()
		{
			try {
				log = File.CreateText (Path.Combine (
					Environment.GetEnvironmentVariable ("HOME"), 
					".banter.log"));
				log.Flush ();
			} catch (IOException) {
				// FIXME: Use temp file
			}

			console = new ConsoleLogger ();
		}

		~FileLogger ()
		{
			if (log != null)
				log.Flush ();
		}

		public void Log (LogLevel lvl, string msg, params object[] args)
		{
			console.Log (lvl, msg, args);

			if (log != null) {
				msg = string.Format ("{0} [{1}]: {2}", 
						     DateTime.Now.ToString(), 
						     Enum.GetName (typeof (LogLevel), lvl), 
						     msg);
				log.WriteLine (msg, args);
				log.Flush();
			}
		}
	}

	// <summary>
	// This class provides a generic logging facility. By default all
	// information is written to standard out and a log file, but other 
	// loggers are pluggable.
	// </summary>
	public static class Logger
	{
		private static LogLevel logLevel = LogLevel.Debug;

		static ILogger logDev = new FileLogger ();

		static bool muted = false;

		public static LogLevel LogLevel
		{
			get { return logLevel; }
			set { logLevel = value; }
		}

		public static ILogger LogDevice
		{
			get { return logDev; }
			set { logDev = value; }
		}

		public static void Debug (string msg, params object[] args)
		{
			Log (LogLevel.Debug, msg, args);
		}

		public static void Info (string msg, params object[] args)
		{
			Log (LogLevel.Info, msg, args);
		}

		public static void Warn (string msg, params object[] args)
		{
			Log (LogLevel.Warn, msg, args);
		}

		public static void Error (string msg, params object[] args)
		{
			Log (LogLevel.Error, msg, args);
		}

		public static void Fatal (string msg, params object[] args)
		{
			Log (LogLevel.Fatal, msg, args);
		}

		public static void Log (LogLevel lvl, string msg, params object[] args)
		{
			if (!muted && lvl >= logLevel)
				logDev.Log (lvl, msg, args);
		}

		public static void Mute ()
		{
			muted = true;
		}

		public static void Unmute ()
		{
			muted = false;
		}
	}
}
