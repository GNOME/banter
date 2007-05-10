//***********************************************************************
// *  $RCSfile$ - Defines.cs
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
	public class Defines
	{
		public const string Version          	= "@version@";
		public const string DataDir          	= "@datadir@";
		public const string GnomeLocaleDir		= "@datadir@/locale";
		public const string GnomeHelDir			= "@datadir@/gnome/help/rtc";
		
		static public string[] TelepathyManagerLocations = {"/usr/share/telepathy/managers", "/usr/local/share/telepathy/managera"};
	}
}
