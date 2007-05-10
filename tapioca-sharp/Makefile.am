
# Warning: This is an automatically generated file, do not edit!

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -debug -d:DEBUG
ASSEMBLY = ./bin/Debug/tapioca-sharp.dll
COMPILE_TARGET = library
PC_FILES_IN = 
PROJECT_REFERENCES = 
BUILD_DIR = ./bin/Debug
endif
if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4
ASSEMBLY = ./bin/Release/tapioca-sharp.dll
COMPILE_TARGET = library
PC_FILES_IN = 
PROJECT_REFERENCES = 
BUILD_DIR = ./bin/Release
endif

	
all: $(ASSEMBLY)

FILES = \
	./tapioca/TextChannel.cs \
	./tapioca/Connection.cs \
	./tapioca/ConnectionManager.cs \
	./tapioca/ConnectionManagerFactory.cs \
	./tapioca/Contact.cs \
	./tapioca/ContactBase.cs \
	./tapioca/ContactList.cs \
	./tapioca/DBusProxyObject.cs \
	./tapioca/Enum.cs \
	./tapioca/Handle.cs \
	./tapioca/TextChannelMessage.cs \
	./tapioca/UserContact.cs \
	./tapioca/Channel.cs \
	./tapioca/Util/ManagerFileReader.cs \
	./tapioca/Util/ConfigFileReader.cs \
	./tapioca/ConnectionManagerParameter.cs \
	./tapioca/PrivContactList.cs \
	./tapioca/ConnectionGroup.cs \
	./tapioca/Avatar.cs \
	./tapioca/ChannelTarget.cs \
	./tapioca/GroupManager.cs \
	./tapioca/ContactListControl.cs \
	./tapioca/StreamChannel.cs \
	./tapioca/StreamObject.cs \
	./tapioca/StreamAudio.cs \
	./tapioca/StreamVideo.cs \
	./tapioca/ContactGroup.cs 

DATA_FILES = 

RESOURCES = 

EXTRAS = 

REFERENCES =  \
	$(NDESK_DBUS_10_LIBS) \
	$(NDESK_DBUS_GLIB_10_LIBS) \
	$(GTK_DOTNET_20_LIBS) \
	$(TELEPATHY_SHARP_LIBS)

DLL_REFERENCES = 

$(ASSEMBLY) $(ASSEMBLY).mdb: $(build_sources) $(build_resources) $(build_datafiles) 
	mkdir -p $(dir $(ASSEMBLY))		

	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$@ -target:$(COMPILE_TARGET) $(build_sources) $(build_resources_embed) $(build_references_ref)

include $(top_srcdir)/Makefile.include
