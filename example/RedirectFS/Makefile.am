
REDIRECT_FS = RedirectFS.exe

REDIRECT_FS_FILES = \
	RedirectFS.cs

redirect_fs_sources = $(addprefix $(srcdir)/, $(REDIRECT_FS_FILES))

REDIRECT_FS_FH = RedirectFS-FH.exe

REDIRECT_FS_FH_FILES = \
	RedirectFS-FH.cs

redirect_fs_fh_sources = $(addprefix $(srcdir)/, $(REDIRECT_FS_FH_FILES))

EXTRA_DIST = \
	$(redirect_fs_sources) \
	$(srcdir)/redirectfs.in \
	$(redirect_fs_fh_sources) \
	$(srcdir)/redirectfs-fh.in

all: $(REDIRECT_FS) redirectfs $(REDIRECT_FS_FH) redirectfs-fh

REFS = -r:Mono.Fuse.dll -r:Mono.Posix.dll

$(REDIRECT_FS): $(redirect_fs_sources) Mono.Fuse.dll
	$(CSC) /unsafe -d:TRACE $(CSC_FLAGS) $(redirect_fs_sources) $(REFS) /out:$@

$(REDIRECT_FS_FH): $(redirect_fs_fh_sources) Mono.Fuse.dll
	$(CSC) /unsafe -d:TRACE $(CSC_FLAGS) $(redirect_fs_fh_sources) $(REFS) /out:$@

Mono.Fuse.dll : $(top_builddir)/lib/mono-fuse/Mono.Fuse.dll
	-rm $@
	ln -s $< $@

noinst_SCRIPTS = redirectfs redirectfs-fh

builddir = @builddir@

abs_topbuilddir = $(shell cd $(top_builddir) && pwd)
abs_builddir    = $(shell cd $(builddir) && pwd)

redirectfs : redirectfs.in
	cat $(srcdir)/redirectfs.in | sed 's,@TOP_BUILDDIR@,$(abs_topbuilddir),' | \
		sed 's,@BUILDDIR@,$(abs_builddir),' > $@ && \
	chmod +x redirectfs

redirectfs-fh : redirectfs-fh.in
	cat $(srcdir)/redirectfs-fh.in | sed 's,@TOP_BUILDDIR@,$(abs_topbuilddir),' | \
		sed 's,@BUILDDIR@,$(abs_builddir),' > $@ && \
	chmod +x redirectfs-fh

CLEANFILES = redirectfs redirectfs-fh Mono.Fuse.dll RedirectFS.exe RedirectFS-FH.exe

