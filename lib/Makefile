
libMonoFuseHelper.so: map.o mfh.o mph.o
	gcc -g -shared -o $@ $^ `pkg-config --libs fuse glib-2.0` -lMonoPosixHelper

.c.o:
	gcc -Wall -g -c -I. -o $@ $^ `pkg-config --cflags fuse glib-2.0`

