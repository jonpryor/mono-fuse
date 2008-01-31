lib_LTLIBRARIES = libMonoFuseHelper.la

INCLUDES = $(GLIB_CFLAGS) $(FUSE_CFLAGS) -I$(top_srcdir) -I$(top_builddir)/lib
AM_CFLAGS = $(MFH_CFLAGS) -DHAVE_STRUCT_FUSE_ARGS

MFH_SOURCE = \
	$(srcdir)/mfh.c \
	$(srcdir)/mph.c

EXTRA_DIST = $(MFH_SOURCE)

MFH_SOURCES = \
	$(MFH_SOURCE) \
	$(top_builddir)/lib/map.c

MFH_LIBS = $(GLIB_LIBS) $(FUSE_LIBS)

libMonoFuseHelper_la_SOURCES = $(MFH_SOURCES)
libMonoFuseHelper_la_LIBADD  = $(MFH_LIBS)
libMonoFuseHelper_la_LDFLAGS = -no-undefined -avoid-version

buildlibdir = $(shell cd $(top_builddir)/lib && pwd)

all: libMonoFuseHelper.la $(buildlibdir)/libMonoFuseHelper.so

$(buildlibdir)/libMonoFuseHelper.so: $(MFH_SOURCES)
	$(LIBTOOL) --mode=install install -c libMonoFuseHelper.la $(buildlibdir)

CLEANFILES = \
	$(top_builddir)/lib/libMonoFuseHelper.so \
	$(top_builddir)/lib/libMonoFuseHelper.la \
	$(top_builddir)/lib/libMonoFuseHelper.a

