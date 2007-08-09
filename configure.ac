dnl Warning: This is an automatically generated file, do not edit!
dnl Process this file with autoconf to produce a configure script.
AC_PREREQ([2.54])
AC_INIT([banter], [0.1.10])
AM_INIT_AUTOMAKE([foreign tar-ustar])
AM_MAINTAINER_MODE

GNOME_COMMON_INIT

AC_PROG_INSTALL
IT_PROG_INTLTOOL([0.35])
AC_PROG_LIBTOOL

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

PKG_CHECK_MODULES([NDESK_DBUS_GLIB_10], [ndesk-dbus-glib-1.0])
PKG_CHECK_MODULES([GCONF_SHARP_20], [gconf-sharp-2.0])
PKG_CHECK_MODULES([NDESK_DBUS_10], [ndesk-dbus-1.0])
PKG_CHECK_MODULES([GLIB_SHARP_20], [glib-sharp-2.0])
PKG_CHECK_MODULES([GNOME_SHARP_20], [gnome-sharp-2.0])
PKG_CHECK_MODULES([GNOME_KEYRING_SHARP], [gnome-keyring-sharp])
PKG_CHECK_MODULES([GTK_DOTNET_20], [gtk-dotnet-2.0])
PKG_CHECK_MODULES([TELEPATHY_SHARP], [telepathy-sharp])
PKG_CHECK_MODULES([NOTIFY_SHARP], [notify-sharp])
PKG_CHECK_MODULES([MONO_CAIRO], [mono-cairo])
#PKG_CHECK_MODULES([EVOLUTION_SHARP], [evolution-sharp])
#PKG_CHECK_MODULES([WEBKITGLUE], [WebKitGdk])
#PKG_CHECK_MODULES([GTK2], [gtk+-2.0])


AC_ARG_ENABLE(debug,
	AC_HELP_STRING([--enable-debug],
		[Use 'Debug' Configuration [default=yes]]),
		enable_debug=yes, enable_debug=no)
AM_CONDITIONAL(ENABLE_DEBUG, test "x$enable_debug" = "xyes")

AC_ARG_ENABLE(release,
	AC_HELP_STRING([--enable-release],
		[Use 'Release' Configuration [default=no]]),
		enable_release=yes, enable_release=no)
AM_CONDITIONAL(ENABLE_RELEASE, test "x$enable_release" = "xyes")

### Begin GAC tool ###

AC_PATH_PROG(GACUTIL, gacutil, no)
if test "x$GACUTIL" = "xno" ; then
        AC_MSG_ERROR([No gacutil tool found])
fi

AC_SUBST(GACUTIL)

GACUTIL_FLAGS='/gacdir $(DESTDIR)$(prefix)'
AC_SUBST(GACUTIL_FLAGS)

### End GAC tool ###

AC_CONFIG_FILES([
./Makefile
./Banter/Makefile
./data/Makefile
./data/images/Makefile
./data/sounds/Makefile
./po/Makefile.in
])

AC_OUTPUT
