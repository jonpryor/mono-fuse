
ASSEMBLY = HelloFS.exe

FILES = \
	HelloFS.cs

build_sources = $(addprefix $(srcdir)/, $(FILES))

EXTRA_DIST = $(build_sources) $(srcdir)/hellofs.in

all: $(ASSEMBLY) hellofs

REFS = -r:Mono.Fuse.dll -r:Mono.Posix.dll

$(ASSEMBLY): $(build_sources) Mono.Fuse.dll Mono.Fuse.dll.mdb
	$(CSC) -debug+ $(CSC_FLAGS) $(build_sources) $(REFS) /out:$(ASSEMBLY)

Mono.Fuse.dll : $(top_builddir)/lib/mono-fuse/Mono.Fuse.dll
	-rm $@
	ln -s $< $@

Mono.Fuse.dll.mdb : $(top_builddir)/lib/mono-fuse/Mono.Fuse.dll.mdb
	-rm $@
	ln -s $< $@

noinst_SCRIPTS = hellofs

builddir = @builddir@

abs_topbuilddir = $(shell cd $(top_builddir) && pwd)
abs_builddir    = $(shell cd $(builddir) && pwd)

hellofs: hellofs.in
	cat $(srcdir)/hellofs.in | sed 's,@TOP_BUILDDIR@,$(abs_topbuilddir),' | \
		sed 's,@BUILDDIR@,$(abs_builddir),' > $@ && \
	chmod +x hellofs

CLEANFILES = hellofs Mono.Fuse.dll HelloFS.exe Mono.Fuse.dll.mdb HelloFS.exe.mdb

