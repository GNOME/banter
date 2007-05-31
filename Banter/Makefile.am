
CSC = gmcs

TARGET = Banter.exe
WRAPPER = banter

DB4O_DLL = \
	-r:$(top_srcdir)/external/db4o/6.1/Db4objects.Db4o.dll

if ENABLE_DEBUG
CSFLAGS =  -noconfig -codepage:utf8 -warn:4 -debug -d:DEBUG
TAPIOCA_DLL =  \
	-r:$(top_builddir)/tapioca-sharp/bin/Debug/tapioca-sharp.dll
endif
if ENABLE_RELEASE
CSFLAGS =  -noconfig -codepage:utf8 -warn:4
TAPIOCA_DLL =  \
	-r:$(top_builddir)/tapioca-sharp/bin/Release/tapioca-sharp.dll
endif

CSFILES = \
	$(srcdir)/Application.cs \
	$(srcdir)/AppStyle.cs \
	$(srcdir)/AppStyleInfo.cs \
	$(srcdir)/AssemblyInfo.cs \
	$(srcdir)/AvatarButton.cs \
	$(srcdir)/AvatarManager.cs \
	$(srcdir)/AvatarMenuItem.cs \
	$(srcdir)/AvatarSelector.cs \
	$(srcdir)/NotificationArea.cs \
	$(srcdir)/TrayLib.cs \
	$(srcdir)/MessageStore.cs \
	$(srcdir)/Message.cs \
	$(srcdir)/AccountManagement.cs \
	$(srcdir)/Account.cs \
	$(srcdir)/Utilities.cs \
	$(srcdir)/Person.cs \
	$(srcdir)/PersonGroup.cs \
	$(srcdir)/PersonManager.cs \
	$(srcdir)/PersonSync.cs \
	$(srcdir)/PersonView.cs \
	$(srcdir)/PersonCard.cs \
	$(srcdir)/Presence.cs \
	$(srcdir)/ProviderUser.cs \
	$(srcdir)/ProviderUserManager.cs \
	$(srcdir)/Conversation.cs \
	$(srcdir)/ConversationManager.cs \
	$(srcdir)/ComplexMenuItem.cs \
	$(srcdir)/ChatWindow.cs \
	$(srcdir)/ContactStyle.cs \
	$(srcdir)/ContactStyleInfo.cs \
	$(srcdir)/MessagesView.cs \
	$(srcdir)/MessageStyle.cs \
	$(srcdir)/MessageStyleInfo.cs \
	$(srcdir)/ThemeManager.cs \
	$(srcdir)/Theme.cs \
	$(srcdir)/ThemeInfo.cs \
	$(srcdir)/GConfPreferencesProvider.cs \
	$(srcdir)/Preferences.cs \
	$(srcdir)/PreferencesDialog.cs \
	$(srcdir)/XmlFilePreferencesProvider.cs \
	$(srcdir)/Logger.cs \
	$(srcdir)/ActionManager.cs \
	$(srcdir)/VideoWindow.cs \
	$(srcdir)/Avatar.cs \
	$(srcdir)/AvatarRequirements.cs \
	$(srcdir)/GroupWindow.cs \
	$(srcdir)/WrapBox.cs \
	$(srcdir)/InterruptableTimeout.cs \
	$(srcdir)/HIGMessageDialog.cs \
	$(srcdir)/StatusEntry.cs

RESOURCES = \
	-resource:$(top_srcdir)/data/images/banter-16.png \
	-resource:$(top_srcdir)/data/images/banter-22.png \
	-resource:$(top_srcdir)/data/images/banter-24.png \
	-resource:$(top_srcdir)/data/images/banter-32.png \
	-resource:$(top_srcdir)/data/images/banter-44.png \
	-resource:$(top_srcdir)/data/images/blank-photo-128.png \
	-resource:$(top_srcdir)/data/images/blank-photo.svg \
	-resource:$(top_srcdir)/data/Themes/MessageStyles/Template.html \
	-resource:$(srcdir)/UIManagerLayout.xml 

ASSEMBLIES =  \
	-r:System \
	-r:Mono.Posix \
	-r:System.Xml \
	$(JSCALL_SHARP_LIBS) \
	$(NDESK_DBUS_GLIB_10_LIBS) \
	$(NDESK_DBUS_10_LIBS) \
	$(GNOME_KEYRING_SHARP_LIBS) \
	$(GCONF_SHARP_20_LIBS) \
	$(GECKO_SHARP_20_LIBS) \
	$(GLIB_SHARP_20_LIBS) \
	$(GNOME_SHARP_20_LIBS) \
	$(GTK_DOTNET_20_LIBS) \
	$(EVOLUTION_SHARP_LIBS) \
	$(TELEPATHY_SHARP_LIBS) \
	$(TAPIOCA_DLL) \
	$(DB4O_DLL)

$(TARGET): $(CSFILES) Defines.cs
	$(CSC) -out:$@ $(CSFLAGS) $^ $(ASSEMBLIES) $(RESOURCES)

banterlibdir = $(pkglibdir)
banterlib_DATA = $(TARGET)	

bin_SCRIPTS = $(WRAPPER)

$(WRAPPER): $(srcdir)/$(WRAPPER).in Makefile
	sed -e "s|\@prefix\@|$(prefix)|g"               \
		-e "s|\@exec_prefix\@|$(bindir)|g"			\
		-e "s|\@libdir\@|$(libdir)|g"				\
		-e "s|\@pkglibdir\@|$(pkglibdir)|g"         \
		-e "s|\@bindir\@|$(bindir)|g"               \
		-e "s|\@target\@|$(TARGET)|g"               \
		-e "s|\@wrapper\@|$(WRAPPER)|g"             \
		-e "s|\@srcdir\@|$(PWD)|g"  \
		< $< > $@
	chmod +x $(WRAPPER)

Defines.cs: $(srcdir)/Defines.cs.in Makefile
	sed -e "s|\@version\@|$(VERSION)|"     \
		-e "s|\@datadir\@|$(datadir)|"     \
		-e "s|\@pkglibdir\@|$(pkglibdir)|" \
		< $< > $@

EXTRA_DIST = \
	$(CSFILES)					\
	$(WRAPPER).in				\
	$(srcdir)/Defines.cs.in		\
	$(srcdir)/UIManagerLayout.xml 

CLEANFILES = \
	$(TARGET)					\
	$(TARGET).mdb				\
	$(WRAPPER)					\
	Defines.cs

DISTCLEANFILES =                        \
        $(WRAPPER)			\
		$(TARGET)			\
		$(TARGET).mdb		\
		Defines.cs			
