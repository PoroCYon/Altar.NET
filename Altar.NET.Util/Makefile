
default: release

ILASM ?= ilasm

OUTPUT := Altar.NET.Util.dll
ILFLAGS := /nologo /dll

%/:
	mkdir -p "$@"

release: TARGET := Release
release: bin/Release/$(OUTPUT)

debug: TARGET := Debug
debug: ILFLAGS += /debug
debug: bin/Debug/$(OUTPUT)


bin/%/$(OUTPUT): ILHacks.il bin/%/
	$(ILASM) $(ILFLAGS) /output:"$@" "$<"

clean:
	@rm -vf bin/{Debug,Release}/{A,a}ltar.NET.Util.*

.PHONY:
	all debug release default clean

