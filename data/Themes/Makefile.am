
LIST_STYLE_FILES = \
	$(srcdir)/ListStyles/basic.tar.gz \
	$(srcdir)/ListStyles/basic-black.tar.gz

MESSAGE_STYLE_FILES = \
	$(srcdir)/MessageStyles/PurePlastics.AdiumMessageStyle.tar.gz

THEMESDIR = $(DESTDIR)$(datadir)/banter/Themes

install-exec-local: all
	$(mkinstalldirs) $(THEMESDIR)/ListStyles
	$(mkinstalldirs) $(THEMESDIR)/MessageStyles
	$(INSTALL_PROGRAM) $(LIST_STYLE_FILES) $(THEMESDIR)/ListStyles
	cd $(THEMESDIR)/ListStyles; tar xzvf basic.tar.gz
	cd $(THEMESDIR)/ListStyles; tar xzvf basic-black.tar.gz
	cd $(THEMESDIR)/ListStyles; rm Current; ln -s basic Current
	$(INSTALL_PROGRAM) $(MESSAGE_STYLE_FILES) $(THEMESDIR)/MessageStyles
	cd $(THEMESDIR)/MessageStyles; tar xzvf PurePlastics.AdiumMessageStyle.tar.gz

uninstall-local:
	rm -rf $(THEMESDIR)

EXTRA_DIST = \
	$(LIST_STYLE_FILES) \
	$(MESSAGE_STYLE_FILES)

#dist-hook:
#	mkdir -p $(distdir)
#	cp -pr $(srcdir)/ListStyles $(distdir)
#	cp -pr $(srcdir)/MessageStyles $(distdir)
