SUBDIRS = tapioca-sharp Banter data po

pkglib_DATA =  $(DLL_REFERENCES)
DLL_REFERENCES =   $(top_srcdir)/external/db4o/6.1/Db4objects.Db4o.dll
EXTRA_DIST = \
	$(DLL_REFERENCES) \
	\
        intltool-extract.in     \
        intltool-merge.in       \
        intltool-update.in      \
	\
	MAINTAINERS

DISTCLEANFILES =                        \
        \
        intltool-extract                \
        intltool-merge                  \
        intltool-update                 \
        po/.intltool-merge-cache

DISTCHECK_CONFIGURE_FLAGS = --disable-schemas-install --disable-scrollkeeper

# Ignore scrollkeeper issues for now.  @#*$& scrollkeeper (from Evince)
distuninstallcheck_listfiles = find . -type f -print | grep -v scrollkeeper | grep -v /share/gnome/help/ | grep -v \.omf

run: $(PROGRAM)
	cd Banter && ./banter
