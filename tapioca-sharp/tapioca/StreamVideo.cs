/***************************************************************************
 *  StreamVideo.cs
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
	public class StreamVideo : StreamObject
	{
		uint window_id;
		uint window_preview_id;
		
		public StreamVideo(StreamChannel channel, uint stream_id, Contact contact)
			: base (channel, stream_id, contact)
		{
			stream_engine.Receiving += OnReceiving;
		}

		public override void Pause ()
		{
		}
		
		public override StreamType Type
		{ 
			get {
				return StreamType.Video;
			}
		}		
		
		public uint WindowID
		{
			get {
				return window_id;
			}
			set {
				stream_engine.SetOutputWindow (channel.ObjectPath, Id, value);
				window_id = value;
			}
		}
		
		public uint WindowPreviewID
		{
			get {
				return window_preview_id;
			}
			set {
				stream_engine.AddPreviewWindow (value);	
				window_preview_id = value;
			}
		}
		
		private void OnReceiving (ObjectPath channel_path, uint stream_id, bool state)
		{
		}
	}
}
