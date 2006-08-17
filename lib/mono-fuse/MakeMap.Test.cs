// Test file for make-map.cs
using System;
using System.Runtime.InteropServices;
using Mono.Unix.Native;

[assembly:MapHeader (PublicIncludeFile="<stdlib.h>")]
[assembly:MapHeader (PublicMacro="DefineMe=42")]
[assembly:MapHeader (PublicDeclaration="struct foo {int foo;};")]
[assembly:MapHeader (ImplementationMacro="FOO=\"foo\"")]
[assembly:MapHeader (ImplementationIncludeFile="<stdio.h>")]

namespace MakeMap.Test {
	[Map ("struct foo")]
	struct Foo {
		public int foo;
	}

	delegate void DelFoo (int i, Foo f);
	delegate void DelRefFoo (int i, ref Foo f);
	delegate void DelArrayFoo (int i, Foo[] f);
	delegate void DelRefArrayFoo (int i, ref Foo[] f);
	delegate void DelBaz (int i, Baz b);
	delegate void DelRefBaz (int i, ref Baz b);
	delegate void DelArrayBaz (int i, Baz[] b);
	delegate void DelRefArrayBaz (int i, ref Baz[] b);

	[StructLayout (LayoutKind.Sequential)]
	class Baz {
		public DelFoo b1;
		public DelRefFoo b2;
		public DelArrayFoo b3;
		public DelRefArrayFoo b4;
		public DelBaz b5;
		public DelRefBaz b6;
		public DelArrayBaz b7;
		public DelRefArrayBaz b8;
	}

	[StructLayout (LayoutKind.Sequential)]
	class Qux {
		public int i;
		public Baz b;
	}

	class NativeMethods {
		[DllImport ("MonoFuseHelper")]
		private static extern void UseQux (DelFoo b, ref Qux q);
	}
}

