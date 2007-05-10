
pixmapdir = $(datadir)/pixmaps
pixmap_DATA = 	banter-16.png \
				banter-22.png \
				banter-24.png \
				banter-32.png \
				banter-44.png \
	blank-photo-128.png \
	blank-photo.svg

hicolordir = $(datadir)/icons/hicolor

tango_icons = 				\
				banter-16.png \
				banter-22.png \
				banter-24.png \
				banter-32.png \
				banter-44.png

install-data-local:
	@-$(mkinstalldirs) $(DESTDIR)$(hicolordir)/16x16/apps $(DESTDIR)$(hicolordir)/22x22/apps $(DESTDIR)$(hicolordir)/24x24/apps $(DESTDIR)$(hicolordir)/32x32/apps $(DESTDIR)$(hicolordir)/44x44/apps
	$(INSTALL_DATA) $(srcdir)/banter-16.png $(DESTDIR)$(hicolordir)/16x16/apps/banter.png
	$(INSTALL_DATA) $(srcdir)/banter-22.png $(DESTDIR)$(hicolordir)/22x22/apps/banter.png
	$(INSTALL_DATA) $(srcdir)/banter-24.png $(DESTDIR)$(hicolordir)/24x24/apps/banter.png
	$(INSTALL_DATA) $(srcdir)/banter-32.png $(DESTDIR)$(hicolordir)/32x32/apps/banter.png
	$(INSTALL_DATA) $(srcdir)/banter-44.png $(DESTDIR)$(hicolordir)/44x44/apps/banter.png

gtk_update_icon_cache = gtk-update-icon-cache -f -t $(datadir)/icons/hicolor

install-data-hook:
	@-if test -z "$(DESTDIR)"; then \
		echo "Updating Gtk icon cache."; \
		$(gtk_update_icon_cache); \
	else \
		echo "*** Icon cache not updated.  After install, run this:"; \
		echo "***   $(gtk_update_icon_cache)"; \
	fi

uninstall-hook:
	rm -f $(DESTDIR)$(hicolordir)/16x16/apps/banter.png
	rm -f $(DESTDIR)$(hicolordir)/22x22/apps/banter.png
	rm -f $(DESTDIR)$(hicolordir)/24x24/apps/banter.png
	rm -f $(DESTDIR)$(hicolordir)/32x32/apps/banter.png
	rm -f $(DESTDIR)$(hicolordir)/44x44/apps/banter.png

noinst_DATA =

EXTRA_DIST = $(noinst_DATA) $(pixmap_DATA) $(tango_icons)
