SOUND_FILES = \
	$(srcdir)/banter.wav \
	$(srcdir)/notify.wav \
	$(srcdir)/send.wav \
	$(srcdir)/receive.wav 

SOUNDSDIR = $(DESTDIR)$(datadir)/banter/sounds

install-exec-local: all
	$(mkinstalldirs) $(SOUNDSDIR)
	$(INSTALL_PROGRAM) $(SOUND_FILES) $(SOUNDSDIR)

uninstall-local:
	rm -rf $(SOUNDSDIR)

EXTRA_DIST = \
	$(SOUND_FILES) 

