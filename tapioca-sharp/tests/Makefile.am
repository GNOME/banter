MCS=gmcs

MCS_FLAGS=-debug

ASSEMBLY_NAME=Test
ASSEMBLY=$(ASSEMBLY_NAME).exe

TARGET=$(ASSEMBLY)

ASSEMBLY_CSFILES=\
	ITest.cs \
	ConnectionTest.cs \
	ConnectionManagerTest.cs \
	Main.cs

CLEANFILES = *.exe*

PKG_REFERENCES = \
	-r:../tapioca/INdT.Tapioca.dll \
	-r:../dbus-sharp-glib/NDesk.DBus.GLib.dll \
	$(GTK_SHARP_LIBS) \
	$(TELEPATHY_SHARP_LIBS)

all: $(TARGET)

$(TARGET): $(ASSEMBLY_CSFILES)
	$(MCS) $(MCS_FLAGS) $(PKG_REFERENCES) $(ASSEMBLY_CSFILES) \
	-target:exe -out:"$@"

