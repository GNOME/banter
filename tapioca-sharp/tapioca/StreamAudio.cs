/***************************************************************************
 *  StreamAudio.cs
 *
 *  Copyright (C) 2006 INdT
 *  Written by
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
using org.freedesktop.Telepathy;
using ObjectPath = NDesk.DBus.ObjectPath;

namespace Tapioca
{	
	public class StreamAudio : StreamObject
	{	
		private uint volume;
		
		public StreamAudio(StreamChannel channel, uint stream_id, Contact contact)
			: base (channel, stream_id, contact)
		{
		}
		
		public override void Pause ()
		{
		}
		
		public override StreamType Type
		{ 
			get {
				return StreamType.Audio;
			}
		}
		
				
		public uint Volume 
		{
			get {
				return volume;
			}
			set {
				volume  = value;
				stream_engine.SetOutputVolume (channel.ObjectPath, this.stream_id, volume);	
			}
		}
		
		public void MuteIn (bool mute)
		{
			stream_engine.MuteInput (channel.ObjectPath, this.stream_id, mute);	
		}
		
		public void MuteOut (bool mute)
		{
			stream_engine.MuteOutput (channel.ObjectPath, this.stream_id, mute);
		}
	}
}
