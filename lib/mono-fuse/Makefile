
all: make-map.exe Mono.Fuse.dll ../map.c ../map.h HelloFS.exe

make-map.exe: make-map.cs
	gmcs -out:$@ -r:./Mono.Posix.dll $^

Mono.Fuse.dll: FileSystem.cs
	gmcs -out:$@ -t:library -r:./Mono.Posix.dll $^

../map.c ../map.h: Mono.Fuse.dll make-map.exe
	mono make-map.exe Mono.Fuse.dll ../map

HelloFS.exe : HelloFS.cs
	gmcs -out:$@ -r:Mono.Posix.dll -r:./Mono.Fuse.dll $^

