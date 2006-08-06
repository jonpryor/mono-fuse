// Mono.Fuse example program
using System;
using System.Runtime.InteropServices;
using System.Text;
using Mono.Fuse;
using Mono.Unix.Native;

namespace Mono.Fuse.Samples {
	class HelloFS : FileSystem {
		static readonly byte[] hello_str = Encoding.UTF8.GetBytes ("Hello World!\n");
		const string hello_path = "/hello";

		public HelloFS (string[] args) : base (args)
		{
		}

		protected override Errno OnGetFileAttributes (string path, ref Stat stbuf)
		{
			Errno res = 0;

			stbuf = new Stat ();
			if (path.StartsWith ("/")) {
				stbuf.st_mode = FilePermissions.S_IFDIR | 
					NativeConvert.FromOctalPermissionString ("0755");
				stbuf.st_nlink = 2;
			}
			else if (path == hello_path) {
				stbuf.st_mode = FilePermissions.S_IFREG |
					NativeConvert.FromOctalPermissionString ("0444");
				stbuf.st_nlink = 1;
				stbuf.st_size = hello_str.Length;
			}
			else
				res = Errno.ENOENT;
			return res;
		}

		protected override Errno OnReadDirectory (string path, 
				[Out] out string[] paths, OpenedFileInfo fi)
		{
			paths = null;
			if (path != "/")
				return Errno.ENOENT;

			paths = new string[]{
				".",
				"..",
				"hello",
			};

			return 0;
		}

		protected override Errno OnOpen (string path, OpenedFileInfo fi)
		{
			if (path != hello_path)
				return Errno.ENOENT;
			// if ((fi.flags & 3) != OpenFlags.O_RDONLY)
			if (fi.Flags != OpenFlags.O_RDONLY)
				return Errno.EACCES;
			return 0;
		}

		protected override int OnRead (string path, byte[] buf, ulong size, long offset, OpenedFileInfo fi)
		{
			if (path != hello_path)
				return -(int) Errno.ENOENT;

			if (offset < (long) hello_str.Length) {
				if (offset + (long) size > (long) hello_str.Length)
					size = (ulong) ((long) hello_str.Length - offset);
				Buffer.BlockCopy (hello_str, (int) offset, buf, 0, (int) size);
			}
			else
				size = 0;

			return (int) size;
		}

		public static void Main (string[] args)
		{
			using (FileSystem fs = new HelloFS (args)) {
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
