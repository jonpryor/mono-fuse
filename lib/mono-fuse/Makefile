
all: make-map.exe \
	MakeMap.Test.dll test.c test.h \
	Mono.Fuse.dll ../map.c ../map.h HelloFS.exe

make-map.exe: make-map.cs
	gmcs -debug+ -out:$@ -r:Mono.Posix.dll $^

Mono.Fuse.dll: FileSystem.cs
	gmcs -debug+ -out:$@ -t:library -r:Mono.Posix.dll $^

../map.c ../map.h: Mono.Fuse.dll make-map.exe
	mono --debug make-map.exe Mono.Fuse.dll ../map

HelloFS.exe: HelloFS.cs Mono.Fuse.dll
	gmcs -debug+ -out:$@ -r:Mono.Posix.dll -r:./Mono.Fuse.dll $<

MakeMap.Test.dll: MakeMap.Test.cs make-map.exe
	mcs -debug+ -out:$@ -t:library -r:Mono.Posix.dll $<

test.c test.h: MakeMap.Test.dll make-map.exe
	mono --debug make-map.exe \
		--autoconf-member autoconf_me \
		--exclude-native-symbol=exclude_native_symbol \
		--library=MonoFuseHelper \
		--rename-member=st_atime=st_atime_ \
		--rename-namespace=NSFrom=NSTo \
		MakeMap.Test.dll test

libtest.so: test.c
	gcc -shared -o $@ $^ -I.. `pkg-config --libs --cflags glib-2.0`

check:
	for f in test.c test.h test.cs test.xml ; do \
		diff -rup $$f.ref $$f ; \
	done

check-update:
	for f in test.c test.h test.cs test.xml ; do \
		mv $$f $$f.ref ; \
	done

hellofs:
	LD_LIBRARY_PATH=`pwd`/.. mono --debug HelloFS.exe -odebug t

