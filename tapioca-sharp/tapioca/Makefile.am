CSC_FLAGS=-debug

ASSEMBLY_NAME = INdT.Tapioca
ASSEMBLY = $(ASSEMBLY_NAME).dll
INdT_Tapiocadir = $(pkglibdir)
INdT_Tapioca_SCRIPTS = $(ASSEMBLY)

TARGET = $(ASSEMBLY)

UTIL_CSFILES = \
	Util/ConfigFileReader.cs \
	Util/ManagerFileReader.cs

SOURCES = \
	Avatar.cs \
	Channel.cs \
	ChannelTarget.cs \
	ConnectionGroup.cs \
	ConnectionManager.cs \
	ConnectionManagerFactory.cs \
	ConnectionManagerParameter.cs \
	ContactBase.cs \
	Connection.cs \
	Contact.cs \
	ContactListControl.cs \
	ContactList.cs \
	ContactGroup.cs \
	ConfigureDefines.cs \
	DBusProxyObject.cs \
	Enum.cs \
	Handle.cs \
	PrivContactList.cs \
	StreamAudio.cs \
	StreamChannel.cs \
	StreamObject.cs \
	StreamVideo.cs \
	TextChannel.cs \
	TextChannelMessage.cs \
	UserContact.cs

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb $(ASSEMBLY).config

PKG_REFERENCES = \
	$(TELEPATHY_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	$(NDESK_DBUS_GLIB_10_LIBS)


$(ASSEMBLY): $(SOURCES) $(UTIL_CSFILES)
	$(CSC) -target:library -out:$@ $(CSC_FLAGS) $(SOURCES) $(UTIL_CSFILES) $(PKG_REFERENCES)

all: $(ASSEMBLY)

