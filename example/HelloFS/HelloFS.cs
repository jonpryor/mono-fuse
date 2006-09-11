//
// HelloFS.cs
//
// Authors:
//   Jonathan Pryor (jonpryor@vt.edu)
//
// (C) 2006 Jonathan Pryor
//
// Mono.Fuse example program
//

//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Mono.Fuse;
using Mono.Unix.Native;

namespace Mono.Fuse.Samples {
	class HelloFS : FileSystem {
		static readonly byte[] hello_str = Encoding.UTF8.GetBytes ("Hello World!\n");
		const string hello_path = "/hello";
		const string data_path  = "/data";
		const string data_im_path  = "/data.im";

		const int data_size = 100000000;

		byte[] data_im_str;
		bool have_data_im = false;
		object data_im_str_lock = new object ();

		public HelloFS ()
		{
			Trace.WriteLine ("(HelloFS creating)");
		}

		protected override Errno OnGetPathStatus (string path, ref Stat stbuf)
		{
			Trace.WriteLine ("(OnGetPathStatus {0})", path);

			stbuf = new Stat ();
			switch (path) {
				case "/":
					stbuf.st_mode = FilePermissions.S_IFDIR | 
						NativeConvert.FromOctalPermissionString ("0755");
					stbuf.st_nlink = 2;
					return 0;
				case hello_path:
				case data_path:
				case data_im_path:
					stbuf.st_mode = FilePermissions.S_IFREG |
						NativeConvert.FromOctalPermissionString ("0444");
					stbuf.st_nlink = 1;
					int size = 0;
					switch (path) {
						case hello_path:   size = hello_str.Length; break;
						case data_path:
						case data_im_path: size = data_size; break;
					}
					stbuf.st_size = size;
					return 0;
				default:
					return Errno.ENOENT;
			}
		}

		protected override Errno OnReadDirectory (string path, OpenedPathInfo fi,
				out IEnumerable<FileSystemEntry> paths)
		{
			Trace.WriteLine ("(OnReadDirectory {0})", path);
			paths = null;
			if (path != "/")
				return Errno.ENOENT;

			paths = GetEntries ();
			return 0;
		}

		private IEnumerable<FileSystemEntry> GetEntries ()
		{
			yield return ".";
			yield return "..";
			yield return "hello";
			yield return "data";
			if (have_data_im)
				yield return "data.im";
		}

		protected override Errno OnOpenHandle (string path, OpenedPathInfo fi)
		{
			Trace.WriteLine (string.Format ("(OnOpen {0} Flags={1})", path, fi.OpenFlags));
			if (path != hello_path && path != data_path && path != data_im_path)
				return Errno.ENOENT;
			if (path == data_im_path && !have_data_im)
				return Errno.ENOENT;
			if (!fi.OpenReadOnly)
				return Errno.EACCES;
			return 0;
		}

		protected override Errno OnReadHandle (string path, OpenedPathInfo fi, byte[] buf, long offset, out int bytesWritten)
		{
			Trace.WriteLine ("(OnRead {0})", path);
			bytesWritten = 0;
			int size = buf.Length;
			if (path == data_im_path)
				FillData ();
			if (path == hello_path || path == data_im_path) {
				byte[] source = path == hello_path ? hello_str : data_im_str;
				if (offset < (long) source.Length) {
					if (offset + (long) size > (long) source.Length)
						size = (int) ((long) source.Length - offset);
					Buffer.BlockCopy (source, (int) offset, buf, 0, size);
				}
				else
					size = 0;
			}
			else if (path == data_path) {
				int max = System.Math.Min ((int) data_size, (int) (offset + buf.Length));
				for (int i = 0, j = (int) offset; j < max; ++i, ++j) {
					if ((j % 27) == 0)
						buf [i] = (byte) '\n';
					else
						buf [i] = (byte) ((j % 26) + 'a');
				}
			}
			else
				return Errno.ENOENT;

			bytesWritten = size;
			return 0;
		}

		private bool ParseArguments (string[] args)
		{
			for (int i = 0; i < args.Length; ++i) {
				switch (args [i]) {
					case "--data.im-in-memory":
						have_data_im = true;
						break;
					case "-h":
					case "--help":
						FileSystem.ShowFuseHelp ("hellofs");
						Console.Error.WriteLine ("hellofs options:");
						Console.Error.WriteLine ("    --data.im-in-memory    Add data.im file");
						return false;
					default:
						base.MountPoint = args [i];
						break;
				}
			}
			return true;
		}

		private void FillData ()
		{
			lock (data_im_str_lock) {
				if (data_im_str != null)
					return;
				data_im_str = new byte [data_size];
				for (int i = 0; i < data_im_str.Length; ++i) {
					if ((i % 27) == 0)
						data_im_str [i] = (byte) '\n';
					else
						data_im_str [i] = (byte) ((i % 26) + 'a');
				}
			}
		}

		public static void Main (string[] args)
		{
			using (HelloFS fs = new HelloFS ()) {
				string[] unhandled = fs.ParseFuseArguments (args);
				foreach (string key in fs.FuseOptions.Keys) {
					Console.WriteLine ("Option: {0}={1}", key, fs.FuseOptions [key]);
				}
				if (!fs.ParseArguments (unhandled))
					return;
				// fs.MountAt ("path" /* , args? */);
				fs.Start ();
			}
		}
	}
}

#if false
int main(int argc, char *argv[])
{
    // return fuse_main(argc, argv, &hello_oper);
    struct fuse_args args = FUSE_ARGS_INIT(argc, argv);
    struct fuse *fuse;
	char *mountpoint;
	int multithreaded;
    int foreground;
    int res;
	int fd;

    res = fuse_parse_cmdline(&args, &mountpoint, &multithreaded, &foreground);
    if (res == -1) {
	  	fprintf (stderr, "hwfs2: unable to parse command line\n");
		return 1;
	}

    fd = fuse_mount(mountpoint, &args);
    if (fd == -1) {
        fuse_opt_free_args(&args);
	  	fprintf (stderr, "hwfs2: unable to mount %s\n", mountpoint);
		free (mountpoint);
		return 1;
    }

	fuse = fuse_new (fd, &args, &hello_oper, sizeof(*(&hello_oper)));
    fuse_opt_free_args(&args);
    if (fuse == NULL) {
	  	fprintf (stderr, "hwfs2: Unable to create new fuse instance\n");
	  	fuse_unmount (mountpoint);
		free (mountpoint);
		return 2;
	}


#if false
    res = fuse_set_signal_handlers(fuse_get_session(fuse));
    if (res == -1) {
        fuse_destroy (fuse);
        fuse_unmount (mountpoint);
        free (mountpoint);
        fprintf (stderr, "hwfs2: Unable to set signal handlers\n");
        return 3;
	}
#endif

    if (multithreaded)
        res = fuse_loop_mt(fuse);
    else
        res = fuse_loop(fuse);

    fuse_teardown(fuse, fd, mountpoint);

    return 0;
}

#endif
