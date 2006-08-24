
ASSEMBLY = HelloFS.exe

FILES = \
	HelloFS.cs

build_sources = $(addprefix $(srcdir)/, $(FILES))

all: $(ASSEMBLY) hellofs

REFS = -r:Mono.Fuse.dll -r:Mono.Posix.dll

$(ASSEMBLY): $(build_sources) Mono.Fuse.dll
	$(CSC) $(CSC_FLAGS) $(build_sources) $(REFS) /out:$(ASSEMBLY)

Mono.Fuse.dll : $(top_builddir)/lib/mono-fuse/Mono.Fuse.dll
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

CLEANFILES = hellofs Mono.Fuse.dll HelloFS.exe

