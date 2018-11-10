
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

Altar.NET/bin/Release/altar.exe: Altar.NET/ Altar.NET/bin/Release/ Altar.NET.Util/bin/Release/Altar.NET.Util.dll
	$(MAKE) -C "$<" "$(TARGET_)"
Altar.NET.Util/bin/Release/Altar.NET.Util.dll: Altar.NET.Util/ Altar.NET.Util/bin/Release/
	$(MAKE) -C "$<" "$(TARGET_)"

%/:
	mkdir -p "$@"
bin/$(TARGET):
	mkdir -p "$@"

bin/%/$(OUTPUT): bin/%/ $(INPUTS)
	$(MONO) $(ILREPACK) $(ILRFLAGS) "/out:$@" $(INPUTS)

utils/lsiff: utils/
	$(MAKE) -C lsiff

all: bin/$(TARGET)/ bin/$(TARGET)/$(OUTPUT) utils/lsiff

release: all

clean:
	@rm -vf bin/$(TARGET)/*.{exe,dll,pdb,mdb}
	$(MAKE) -C Altar.NET clean
	$(MAKE) -C Altar.NET.Util clean

.PHONY: all release clean debug Altar.NET/bin/$(TARGET)/altar.exe \
    Altar.NET.Util/bin/$(TARGET)/Altar.NET.Util.dll utils/lsiff

