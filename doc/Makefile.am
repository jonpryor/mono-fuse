ASSEMBLIES = 				\
	Mono.Fuse.dll

UPDATE_ASSEMBLIES = $(addprefix ../lib/mono-fuse/, $(ASSEMBLIES))

UPDATER = $(MDOC) update -o en

if ENABLE_DOCS
SOURCESDIR=$(prefix)/lib/monodoc/sources
TARGETS=mono-fuse-docs.zip mono-fuse-docs.tree mono-fuse-docs.source
else
SOURCESDIR=$(datadir)
TARGETS=
endif

monodocdir=$(SOURCESDIR)
monodoc_DATA=$(TARGETS)

assemble: mono-fuse-docs.zip mono-fuse-docs.tree

mono-fuse-docs.zip mono-fuse-docs.tree: $(srcdir)/en/*/*.xml $(srcdir)/en/*.xml
	$(MDOC) assemble -o mono-fuse-docs $(srcdir)/en

update: $(UPDATE_ASSEMBLIES)
	$(UPDATER) $^

update-delete: $(UPDATE_ASSEMBLIES)
	$(UPDATER) --delete $^

CLEANFILES = mono-fuse-docs.zip mono-fuse-docs.tree

EXTRA_DIST =			\
	mono-fuse-docs.source

NAMESPACES=Mono.Fuse

dist-hook:
	mkdir -p $(distdir)/en
	cp $(srcdir)/en/*.xml $(distdir)/en/
	for i in $(NAMESPACES); do				\
		mkdir -p $(distdir)/en/$$i;			\
		cp $(srcdir)/en/$$i/*.xml $(distdir)/en/$$i;	\
	done

edit:
	monodoc --edit $(srcdir)/en

