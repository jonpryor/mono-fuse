
FILES = \
	MapAttribute.cs \
	AssemblyInfo.cs \
	Mono.Fuse/FileSystem.cs \
	Mono.Fuse/FileNameMarshaler.cs

build_sources = $(addprefix $(srcdir)/, $(FILES))

EXTRA_DIST = $(build_sources) create-native-map.exe

DLL = $(top_builddir)/lib/mono-fuse/Mono.Fuse.dll

REFS = -r:Mono.Posix.dll

map_p = $(top_builddir)/lib/map

map_files = $(map_p).c $(map_p).h $(map_p).cs $(map_p).xml

all: $(DLL) $(map_files)

$(DLL) : $(build_sources)
	$(CSC) $(CSC_FLAGS) -d:TRACE -debug+ -out:$@ -target:library $(REFS) $(build_sources)

$(map_files) : $(DLL)
	mono $(srcdir)/create-native-map.exe  \
		--impl-macro:FUSE_USE_VERSION=26    \
		--impl-header:"<fuse.h>"            \
		--library=MonoFuseHelper            \
		--rename-member:st_atime=st_atime_  \
		--rename-member:st_mtime=st_mtime_  \
		--rename-member:st_ctime=st_ctime_  \
		$(DLL) $(map_p)

assemblydir = $(MF_DIR)
assembly_DATA = $(DLL) $(DLL).mdb $(DLL).config

update-map-source:
	cp `pkg-config --variable=Programs create-native-map` .
	cp `pkg-config --variable=Sources create-native-map` .

CLEANFILES = $(DLL) $(DLL).mdb $(map_files)

