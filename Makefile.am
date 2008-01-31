SUBDIRS = src example lib doc

rpm:
	tgz=$(DIST_ARCHIVES); \
	if [ ! -e $(DIST_ARCHIVES) ] ; then tgz=archive/$(DIST_ARCHIVES) ; fi ; \
	cp $$tgz /usr/src/packages/SOURCES/$(DIST_ARCHIVES) && \
		rpmbuild -bb mono-fuse.spec && \
		cp /usr/src/packages/RPMS/*/$(distdir)*.rpm archive

prepare-release:
	$(MAKE) distcheck
	mv $(DIST_ARCHIVES) archive

