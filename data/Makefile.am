
SUBDIRS = images Themes

#@INTLTOOL_DESKTOP_RULE@

#desktop_in_files = banter.desktop.in
#desktop_files    = $(desktop_in_files:.desktop.in=.desktop)

#desktopdir   = $(datadir)/applications
#desktop_DATA = $(desktop_files)

schemasdir	 = $(GCONF_SCHEMA_FILE_DIR)
schemas_in_files = banter.schemas.in
schemas_DATA     = $(schemas_in_files:.schemas.in=.schemas)

@INTLTOOL_SCHEMAS_RULE@

if GCONF_SCHEMAS_INSTALL
install-data-local:
	GCONF_CONFIG_SOURCE=$(GCONF_SCHEMA_CONFIG_SOURCE) \
	$(GCONFTOOL) --makefile-install-rule $(schemas_DATA)
endif

noinst_DATA = 

EXTRA_DIST = 					\
	$(desktop_in_files)     		\
	$(schemas_in_files)     		\
	$(noinst_DATA)

DISTCLEANFILES = 				\
	$(desktop_files)       			\
	$(schemas_DATA)
