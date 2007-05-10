/* This class was originally part of Tomboy's Tomboy/Utils.cs */

using System;

namespace Banter
{
	public class InterruptableTimeout
	{
		uint timeoutID;
		EventArgs args;

		public InterruptableTimeout ()
		{
		}

		public void Reset (uint timeoutMillis)
		{
			Reset (timeoutMillis, null);
		}

		public void Reset (uint timeoutMillis, EventArgs args)
		{
			Cancel ();
			this.args = args;
			timeoutID = GLib.Timeout.Add (timeoutMillis, 
						       new GLib.TimeoutHandler (TimeoutExpired));
		}

		public void Cancel ()
		{
			if (timeoutID != 0) {
				GLib.Source.Remove (timeoutID);
				timeoutID = 0;
				args = null;
			}
		}

		bool TimeoutExpired ()
		{
			if (Timeout != null)
				Timeout (this, args);

			timeoutID = 0;
			return false;
		}

		public event EventHandler Timeout;
	}
}
