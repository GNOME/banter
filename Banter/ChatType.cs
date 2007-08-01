//***********************************************************************
// *  $RCSfile$ - ChatType.cs
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

namespace Banter
{
	///<summary>
	///	ChatType enum
	/// ChatType represents the type of chat with a person
	///</summary>
	public enum ChatType : uint
	{
		Text = 1,
		Audio,
		Video
	}

		
	///<summary>
	///	CallType enum
	/// CallType represents the type of incoming or outgoing
	/// internet call.
	/// FIXME::Move to own file
	///</summary>
	public enum CallType : uint
	{
		None,
		Audio,
		Video
	}
}
