dnl Warning: This is an automatically generated file, do not edit!
dnl Process this file with autoconf to produce a configure script.
AC_PREREQ([2.54])
AC_INIT([banter], [0.1.5])
AM_INIT_AUTOMAKE([foreign tar-ustar])
AM_MAINTAINER_MODE

GNOME_COMMON_INIT

AC_PROG_INSTALL
IT_PROG_INTLTOOL([0.35])

dnl pkg-config
AC_PATH_PROG(PKG_CONFIG, pkg-config, no)
if test "x$PKG_CONFIG" = "xno"; then
        AC_MSG_ERROR([You need to install pkg-config])
fi

AC_PROG_INSTALL

AC_PATH_PROG(GMCS, gmcs, no)
if test "x$GMCS" = "xno"; then
        AC_MSG_ERROR([gmcs Not found])
fi

#
# Setup GETTEXT
#

GETTEXT_PACKAGE=banter
AC_SUBST(GETTEXT_PACKAGE)
AC_DEFINE_UNQUOTED(GETTEXT_PACKAGE, "$GETTEXT_PACKAGE", [Gettext package])
AM_GLIB_GNU_GETTEXT

#
# Find gconftool-2
#
AC_PATH_PROG(GCONFTOOL, gconftool-2)
AM_GCONF_SOURCE_2

PKG_CHECK_MODULES([JSCALL_SHARP], [jscall-sharp])
PKG_CHECK_MODULES([GECKO_SHARP_20], [gecko-sharp-2.0])
PKG_CHECK_MODULES([NDESK_DBUS_GLIB_10], [ndesk-dbus-glib-1.0])
PKG_CHECK_MODULES([GCONF_SHARP_20], [gconf-sharp-2.0])
PKG_CHECK_MODULES([NDESK_DBUS_10], [ndesk-dbus-1.0])
PKG_CHECK_MODULES([GLIB_SHARP_20], [glib-sharp-2.0])
PKG_CHECK_MODULES([GNOME_SHARP_20], [gnome-sharp-2.0])
PKG_CHECK_MODULES([GNOME_KEYRING_SHARP], [gnome-keyring-sharp])
PKG_CHECK_MODULES([GTK_DOTNET_20], [gtk-dotnet-2.0])
PKG_CHECK_MODULES([TELEPATHY_SHARP], [telepathy-sharp])
PKG_CHECK_MODULES([EVOLUTION_SHARP], [evolution-sharp])


AC_ARG_ENABLE(debug,
	AC_HELP_STRING([--enable-debug],
		[Use 'Debug' Configuration [default=yes]]),
		enable_debug=yes, enable_debug=no)
AM_CONDITIONAL(ENABLE_DEBUG, test "x$enable_debug" = "xyes")
if test "x$enable_debug" = "xyes" ; then
	TAPIOCA_SHARP_DEBUG_LIB='tapioca-sharp.dll'
	AC_SUBST(TAPIOCA_SHARP_DEBUG_LIB)
	DEBUG_CONFIG_LIBRARIES=' ${pkglibdir}/tapioca-sharp.dll ${pkglibdir}/Db4objects.Db4o.dll'
	DEBUG_CONFIG_LIBS=' -r:${pkglibdir}/tapioca-sharp.dll -r:${pkglibdir}/Db4objects.Db4o.dll'
	AC_SUBST(DEBUG_CONFIG_LIBRARIES)
	AC_SUBST(DEBUG_CONFIG_LIBS)
	CONFIG_REQUESTED="yes"
fi
AC_ARG_ENABLE(release,
	AC_HELP_STRING([--enable-release],
		[Use 'Release' Configuration [default=no]]),
		enable_release=yes, enable_release=no)
AM_CONDITIONAL(ENABLE_RELEASE, test "x$enable_release" = "xyes")
if test "x$enable_release" = "xyes" ; then
	TAPIOCA_SHARP_RELEASE_LIB='tapioca-sharp.dll'
	AC_SUBST(TAPIOCA_SHARP_RELEASE_LIB)
	RELEASE_CONFIG_LIBRARIES=' ${pkglibdir}/tapioca-sharp.dll ${pkglibdir}/Db4objects.Db4o.dll'
	RELEASE_CONFIG_LIBS=' -r:${pkglibdir}/tapioca-sharp.dll -r:${pkglibdir}/Db4objects.Db4o.dll'
	AC_SUBST(RELEASE_CONFIG_LIBRARIES)
	AC_SUBST(RELEASE_CONFIG_LIBS)
	CONFIG_REQUESTED="yes"
fi
if test -z "$CONFIG_REQUESTED" ; then
	TAPIOCA_SHARP_DEBUG_LIB='tapioca-sharp.dll'
	AC_SUBST(TAPIOCA_SHARP_DEBUG_LIB)
	DEBUG_CONFIG_LIBRARIES=' ${pkglibdir}/tapioca-sharp.dll ${pkglibdir}/Db4objects.Db4o.dll'
	DEBUG_CONFIG_LIBS=' -r:${pkglibdir}/tapioca-sharp.dll -r:${pkglibdir}/Db4objects.Db4o.dll'
	AC_SUBST(DEBUG_CONFIG_LIBRARIES)
	AC_SUBST(DEBUG_CONFIG_LIBS)
	AM_CONDITIONAL(ENABLE_DEBUG, true)
fi


AC_CONFIG_FILES([
./Makefile
./Banter/Makefile
./data/Makefile
./data/images/Makefile
./data/Themes/Makefile
./po/Makefile.in
./tapioca-sharp/Makefile
])

AC_OUTPUT
