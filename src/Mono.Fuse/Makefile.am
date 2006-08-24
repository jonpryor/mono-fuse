
FILES = \
	Mono.Fuse/FileSystem.cs

build_sources = $(addprefix $(srcdir)/, $(FILES))

DLL = $(top_builddir)/lib/mono-fuse/Mono.Fuse.dll

REFS = -r:Mono.Posix.dll

map_p = $(top_builddir)/lib/map

map_files = $(map_p).c $(map_p).h $(map_p).cs $(map_p).xml

all: $(DLL) $(map_files)

$(DLL) : $(build_sources)
	$(CSC) $(CSC_FLAGS) -debug+ -out:$@ -target:library $(REFS) $(build_sources)

$(map_files) : $(DLL)
	create-native-map --library=MonoFuseHelper $(DLL) $(map_p)

assemblydir = $(MF_DIR)
assembly_DATA = $(DLL)

CLEANFILES = $(DLL) $(DLL).mdb $(map_files)

