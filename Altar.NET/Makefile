
default: release

MCS ?= mcs

OUTPUT := altar.exe
CSFLAGS := -checked- -clscheck- -langversion:Experimental -unsafe+ -define:MCS
LIBS := ../References/CommandLine.dll System.Numerics

INFILES := $(shell find . -type f -name "*.cs")

%/:
	mkdir -p "$@"

release: TARGET := Release
release: CSFLAGS += -optimize+ -define:TRACE
release: UTILLIB := ../Altar.NET.Util/bin/Release/Altar.NET.Util.dll
release: LIBS += $(UTILLIB)
release: CSFLAGS += $(patsubst %,-r:%,$(LIBS))
release: bin/Release/$(OUTPUT)

debug: TARGET := Debug
debug: CSFLAGS += -debug+ -define:DEBUG -define:TRACE
debug: UTILLIB := ../Altar.NET.Util/bin/Debug/Altar.NET.Util.dll
debug: LIBS += $(UTILLIB)
debug: CSFLAGS += $(patsubst %,-r:%,$(LIBS))
debug: bin/Debug/$(OUTPUT)

$(UTILLIB): ../Altar.NET.Util
	$(MAKE) -C "$<" $(shell echo $(TARGET) | tr A-Z a-z)

bin/%/$(OUTPUT): bin/%/ $(INFILES) $(UTILLIB)
	$(MCS) $(CSFLAGS) -out:"$@" $(INFILES)
	@cp -v $(filter %.dll %.exe,$(LIBS)) "$<"

clean:
	@rm -vf bin/{Debug,Release}/{A,a}ltar.*
	@rm -vf bin/{Debug,Release}/CommandLine.*

.PHONY:
	all debug release default clean

