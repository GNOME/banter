
CONTACT_STYLE_FILES = \
	$(srcdir)/ContactStyles/basic.tar.gz \
	$(srcdir)/ContactStyles/basic-black.tar.gz

MESSAGE_STYLE_FILES = \
	$(srcdir)/MessageStyles/PurePlastics.AdiumMessageStyle.tar.gz

THEMESDIR = $(DESTDIR)$(datadir)/banter/Themes

install-exec-local: all
	$(mkinstalldirs) $(THEMESDIR)/ContactStyles
	$(mkinstalldirs) $(THEMESDIR)/MessageStyles
	$(INSTALL_PROGRAM) $(CONTACT_STYLE_FILES) $(THEMESDIR)/ContactStyles
	cd $(THEMESDIR)/ContactStyles; tar xzvf basic.tar.gz
	cd $(THEMESDIR)/ContactStyles; tar xzvf basic-black.tar.gz
	$(INSTALL_PROGRAM) $(MESSAGE_STYLE_FILES) $(THEMESDIR)/MessageStyles
	cd $(THEMESDIR)/MessageStyles; tar xzvf PurePlastics.AdiumMessageStyle.tar.gz

uninstall-local:
	rm -rf $(THEMESDIR)

EXTRA_DIST = \
	$(CONTACT_STYLE_FILES) \
	$(MESSAGE_STYLE_FILES) \
	$(srcdir)/MessageStyles/Template.html

#dist-hook:
#	mkdir -p $(distdir)
#	cp -pr $(srcdir)/ContactStyles $(distdir)
#	cp -pr $(srcdir)/MessageStyles $(distdir)
