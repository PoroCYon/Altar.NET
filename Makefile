
default: release

TARGET := Release # makes no sense for debug
TARGET_ := release

MONO ?= mono

OUTPUT := altar.exe
INPUTS := Altar.NET/bin/$(TARGET)/altar.exe \
    Altar.NET.Util/bin/$(TARGET)/Altar.NET.Util.dll \
    References/CommandLine.dll

ILREPACK := ILRepack/ILRepack.exe
ILRFLAGS := /union /target:exe

Altar.NET/bin/$(TARGET)/altar.exe: Altar.NET/ Altar.NET/bin/$(TARGET)/
	$(MAKE) -C "$<" "$(TARGET_)"
Altar.NET.Util/bin/$(TARGET)/Altar.NET.Util.dll: Altar.NET.Util/ Altar.NET.Util/bin/$(TARGET)/
	$(MAKE) -C "$<" "$(TARGET_)"

%/:
	mkdir -p "$@"

bin/%/$(OUTPUT): bin/%/ $(INPUTS)
	$(MONO) $(ILREPACK) $(ILRFLAGS) "/out:$@" $(INPUTS)

all: bin/$(TARGET)/$(OUTPUT)

release: all

clean:
	@rm -vf bin/$(TARGET)/*.{exe,dll,pdb,mdb}
	$(MAKE) -C Altar.NET clean
	$(MAKE) -C Altar.NET.Util clean

.PHONY: all release clean debug Altar.NET/bin/$(TARGET)/altar.exe \
    Altar.NET.Util/bin/$(TARGET)/Altar.NET.Util.dll

