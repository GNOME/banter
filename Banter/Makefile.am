
# Warning: This is an automatically generated file, do not edit!

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -debug -d:DEBUG
ASSEMBLY = ./bin/Debug/Banter.exe
COMPILE_TARGET = exe
ASSEMBLY_WRAPPER = banter
ASSEMBLY_WRAPPER_IN = banter.in
PROJECT_REFERENCES =  \
	$(top_builddir)/tapioca-sharp/bin/Debug/tapioca-sharp.dll
BUILD_DIR = ./bin/Debug
endif
if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4
ASSEMBLY = ./bin/Release/Banter.exe
COMPILE_TARGET = exe
ASSEMBLY_WRAPPER = banter
ASSEMBLY_WRAPPER_IN = banter.in
PROJECT_REFERENCES =  \
	$(top_builddir)/tapioca-sharp/bin/Release/tapioca-sharp.dll
BUILD_DIR = ./bin/Release
endif

	
all: $(ASSEMBLY)

FILES = \
	$(srcdir)/Application.cs \
	$(srcdir)/AppStyle.cs \
	$(srcdir)/AssemblyInfo.cs \
	$(srcdir)/Defines.cs \
	$(srcdir)/NotificationArea.cs \
	$(srcdir)/TrayLib.cs \
	$(srcdir)/MessageStore.cs \
	$(srcdir)/Message.cs \
	$(srcdir)/AccountManagement.cs \
	$(srcdir)/Account.cs \
	$(srcdir)/Utilities.cs \
	$(srcdir)/Person.cs \
	$(srcdir)/PersonGroup.cs \
	$(srcdir)/PersonStore.cs \
	$(srcdir)/PersonSync.cs \
	$(srcdir)/PersonView.cs \
	$(srcdir)/PersonCard.cs \
	$(srcdir)/Presence.cs \
	$(srcdir)/ProviderUser.cs \
	$(srcdir)/ProviderUserManager.cs \
	$(srcdir)/Conversation.cs \
	$(srcdir)/ConversationManager.cs \
	$(srcdir)/ChatWindow.cs \
	$(srcdir)/ContactStyle.cs \
	$(srcdir)/MessagesView.cs \
	$(srcdir)/MessageStyle.cs \
	$(srcdir)/MessageStyleManager.cs \
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
	$(srcdir)/InterruptableTimeout.cs

DATA_FILES = 

RESOURCES = \
	$(top_srcdir)/data/images/banter-16.png \
	$(top_srcdir)/data/images/banter-22.png \
	$(top_srcdir)/data/images/banter-24.png \
	$(top_srcdir)/data/images/banter-32.png \
	$(top_srcdir)/data/images/banter-44.png \
	$(top_srcdir)/data/images/blank-photo-128.png \
	$(top_srcdir)/data/images/blank-photo.svg \
	$(top_srcdir)/data/Themes/MessageStyles/Template.html \
	$(srcdir)/UIManagerLayout.xml 

EXTRAS = 

REFERENCES =  \
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
	$(EVOLUTION_SHARP_LIBS)

DLL_REFERENCES =  \
	$(BUILD_DIR)/Db4objects.Db4o.dll

$(ASSEMBLY) $(ASSEMBLY).mdb: $(build_sources) $(build_resources) $(build_datafiles) 
	mkdir -p $(dir $(ASSEMBLY))		
	cp -f $(top_srcdir)/external/db4o/6.1/Db4objects.Db4o.dll $(BUILD_DIR)/Db4objects.Db4o.dll

	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$@ -target:$(COMPILE_TARGET) $(build_sources) $(build_resources_embed) $(build_references_ref)

bin_SCRIPTS = banter

banter: $(srcdir)/banter.in Makefile
	chmod +x banter

include $(top_srcdir)/Makefile.include
