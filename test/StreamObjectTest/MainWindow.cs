using System;
using Gtk;
using Novell.Rtc;
using Tapioca;

public partial class MainWindow: Gtk.Window
{	
	private Connection connection;

	private StreamChannel stream_channel;

	//media obj
	private StreamAudio stream_audio;
	private StreamVideo stream_video;

	public MainWindow (): base (Gtk.WindowType.Toplevel)
	{
		Build ();
	}
	
	protected void OnDeleteEvent (object sender, DeleteEventArgs a)
	{
		Application.Quit ();
		a.RetVal = true;
	}

	protected virtual void OnButton2Clicked(object sender, System.EventArgs e)
	{
	
	
	//this.stream_channel = (StreamChannel) connection.CreateChannel (Tapioca.ChannelType.StreamedMedia, buddy.Contact);				
	
		VideoWindow vw = new VideoWindow();
		vw.Show();
	}
}