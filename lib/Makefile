
libMonoFuseHelper.so: map.o mfh.o
	gcc -shared -o $@ $< `pkg-config --libs fuse glib-2.0`

.c.o:
	gcc -c -I. -o $@ $< `pkg-config --cflags fuse glib-2.0`

