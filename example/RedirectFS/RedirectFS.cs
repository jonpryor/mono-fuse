//
// RedirectFS.cs: Port of
// http://fuse.cvs.sourceforge.net/fuse/fuse/example/fusexmp.c?view=log
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
	class RedirectFS : FileSystem {

		private string basedir;

		public RedirectFS ()
		{
		}

		protected override Errno OnGetPathStatus (string path, out Stat buf)
		{
			int r = Syscall.lstat (basedir+path, out buf);
			if (r == -1)
				return Stdlib.GetLastError ();
			return 0;
		}

		protected override Errno OnAccessPath (string path, AccessModes mask)
		{
			int r = Syscall.access (basedir+path, mask);
			if (r == -1)
				return Stdlib.GetLastError ();
			return 0;
		}

		protected override Errno OnReadSymbolicLink (string path, out string target)
		{
			target = null;
			StringBuilder buf = new StringBuilder (256);
			do {
				int r = Syscall.readlink (basedir+path, buf);
				if (r < 0) {
					return Stdlib.GetLastError ();
				}
				else if (r == buf.Capacity) {
					buf.Capacity *= 2;
				}
				else {
					target = buf.ToString (0, r);
					return 0;
				}
			} while (true);
		}

		protected override Errno OnReadDirectory (string path, OpenedPathInfo fi,
				out IEnumerable<DirectoryEntry> paths)
		{
			IntPtr dp = Syscall.opendir (basedir+path);
			if (dp == IntPtr.Zero) {
				paths = null;
				return Stdlib.GetLastError ();
			}

			Dirent de;
			List<DirectoryEntry> entries = new List<DirectoryEntry> ();
			while ((de = Syscall.readdir (dp)) != null) {
				DirectoryEntry e = new DirectoryEntry (de.d_name);
				e.Stat.st_ino  = de.d_ino;
				e.Stat.st_mode = (FilePermissions) (de.d_type << 12);
				entries.Add (e);
			}
			Syscall.closedir (dp);

			paths = entries;
			return 0;
		}

		protected override Errno OnCreateSpecialFile (string path, FilePermissions mode, ulong rdev)
		{
			int r;

			// On Linux, this could just be `mknod(basedir+path, mode, rdev)' but this is
			// more portable.
			if ((mode & FilePermissions.S_IFMT) == FilePermissions.S_IFREG) {
				r = Syscall.open (basedir+path, OpenFlags.O_CREAT | OpenFlags.O_EXCL |
						OpenFlags.O_WRONLY, mode);
				if (r >= 0)
					r = Syscall.close (r);
			}
			else if ((mode & FilePermissions.S_IFMT) == FilePermissions.S_IFIFO) {
				r = Syscall.mkfifo (basedir+path, mode);
			}
			else {
				r = Syscall.mknod (basedir+path, mode, rdev);
			}

			if (r == -1)
				return Stdlib.GetLastError ();

			return 0;
		}

		protected override Errno OnCreateDirectory (string path, FilePermissions mode)
		{
			int r = Syscall.mkdir (basedir+path, mode);
			if (r == -1)
				return Stdlib.GetLastError ();
			return 0;
		}

		protected override Errno OnRemoveFile (string path)
		{
			int r = Syscall.unlink (basedir+path);
			if (r == -1)
				return Stdlib.GetLastError ();
			return 0;
		}

		protected override Errno OnRemoveDirectory (string path)
		{
			int r = Syscall.rmdir (basedir+path);
			if (r == -1)
				return Stdlib.GetLastError ();
			return 0;
		}

		protected override Errno OnCreateSymbolicLink (string from, string to)
		{
			int r = Syscall.symlink (from, basedir+to);
			if (r == -1)
				return Stdlib.GetLastError ();
			return 0;
		}

		protected override Errno OnRenamePath (string from, string to)
		{
			int r = Syscall.rename (basedir+from, basedir+to);
			if (r == -1)
				return Stdlib.GetLastError ();
			return 0;
		}

		protected override Errno OnCreateHardLink (string from, string to)
		{
			int r = Syscall.link (basedir+from, basedir+to);
			if (r == -1)
				return Stdlib.GetLastError ();
			return 0;
		}

		protected override Errno OnChangePathPermissions (string path, FilePermissions mode)
		{
			int r = Syscall.chmod (basedir+path, mode);
			if (r == -1)
				return Stdlib.GetLastError ();
			return 0;
		}

		protected override Errno OnChangePathOwner (string path, long uid, long gid)
		{
			int r = Syscall.lchown (basedir+path, (uint) uid, (uint) gid);
			if (r == -1)
				return Stdlib.GetLastError ();
			return 0;
		}

		protected override Errno OnTruncateFile (string path, long size)
		{
			int r = Syscall.truncate (basedir+path, size);
			if (r == -1)
				return Stdlib.GetLastError ();
			return 0;
		}

		protected override Errno OnChangePathTimes (string path, ref Utimbuf buf)
		{
			int r = Syscall.utime (basedir+path, ref buf);
			if (r == -1)
				return Stdlib.GetLastError ();
			return 0;
		}

		protected override Errno OnOpenHandle (string path, OpenedPathInfo info)
		{
			return ProcessFile (basedir+path, info.OpenFlags, delegate (int fd) {return 0;});
		}

		private delegate int FdCb (int fd);
		private static Errno ProcessFile (string path, OpenFlags flags, FdCb cb)
		{
			int fd = Syscall.open (path, flags);
			if (fd == -1)
				return Stdlib.GetLastError ();
			int r = cb (fd);
			Errno res = 0;
			if (r == -1)
				res = Stdlib.GetLastError ();
			Syscall.close (fd);
			return res;
		}

		protected override unsafe Errno OnReadHandle (string path, OpenedPathInfo info, byte[] buf, 
				long offset, out int bytesRead)
		{
			int br = 0;
			Errno e = ProcessFile (basedir+path, OpenFlags.O_RDONLY, delegate (int fd) {
				fixed (byte *pb = buf) {
					return br = (int) Syscall.pread (fd, pb, (ulong) buf.Length, offset);
				}
			});
			bytesRead = br;
			return e;
		}

		protected override unsafe Errno OnWriteHandle (string path, OpenedPathInfo info,
				byte[] buf, long offset, out int bytesWritten)
		{
			int bw = 0;
			Errno e = ProcessFile (basedir+path, OpenFlags.O_WRONLY, delegate (int fd) {
				fixed (byte *pb = buf) {
					return bw = (int) Syscall.pwrite (fd, pb, (ulong) buf.Length, offset);
				}
			});
			bytesWritten = bw;
			return e;
		}

		protected override Errno OnGetFileSystemStatus (string path, out Statvfs stbuf)
		{
			int r = Syscall.statvfs (basedir+path, out stbuf);
			if (r == -1)
				return Stdlib.GetLastError ();
			return 0;
		}

		protected override Errno OnReleaseHandle (string path, OpenedPathInfo info)
		{
			return 0;
		}

		protected override Errno OnSynchronizeHandle (string path, OpenedPathInfo info, bool onlyUserData)
		{
			return 0;
		}

		protected override Errno OnSetPathExtendedAttribute (string path, string name, byte[] value, XattrFlags flags)
		{
			int r = Syscall.lsetxattr (basedir+path, name, value, (ulong) value.Length, flags);
			if (r == -1)
				return Stdlib.GetLastError ();
			return 0;
		}

		protected override Errno OnGetPathExtendedAttribute (string path, string name, byte[] value, out int bytesWritten)
		{
			int r = bytesWritten = (int) Syscall.lgetxattr (basedir+path, name, value, (ulong) value.Length);
			if (r == -1)
				return Stdlib.GetLastError ();
			return 0;
		}

		protected override Errno OnListPathExtendedAttributes (string path, out string[] names)
		{
			int r = (int) Syscall.llistxattr (basedir+path, out names);
			if (r == -1)
				return Stdlib.GetLastError ();
			return 0;
		}

		protected override Errno OnRemovePathExtendedAttribute (string path, string name)
		{
			int r = Syscall.lremovexattr (basedir+path, name);
			if (r == -1)
				return Stdlib.GetLastError ();
			return 0;
		}

		protected override Errno OnLockHandle (string file, OpenedPathInfo info, FcntlCommand cmd, ref Flock @lock)
		{
			Flock _lock = @lock;
			Errno e = ProcessFile (basedir+file, info.OpenFlags, fd => Syscall.fcntl (fd, cmd, ref _lock));
			@lock = _lock;
			return e;
		}

		private bool ParseArguments (string[] args)
		{
			for (int i = 0; i < args.Length; ++i) {
				switch (args [i]) {
					case "-h":
					case "--help":
						ShowHelp ();
						return false;
					default:
						if (base.MountPoint == null)
							base.MountPoint = args [i];
						else
							basedir = args [i];
						break;
				}
			}
			if (base.MountPoint == null) {
				return Error ("missing mountpoint");
			}
			if (basedir == null) {
				return Error ("missing basedir");
			}
			return true;
		}

		private static void ShowHelp ()
		{
			Console.Error.WriteLine ("usage: redirectfs [options] mountpoint basedir:");
			FileSystem.ShowFuseHelp ("redirectfs");
			Console.Error.WriteLine ();
			Console.Error.WriteLine ("redirectfs options:");
			Console.Error.WriteLine ("    basedir                Directory to mirror");
		}

		private static bool Error (string message)
		{
			Console.Error.WriteLine ("redirectfs: error: {0}", message);
			return false;
		}

		public static void Main (string[] args)
		{
			using (RedirectFS fs = new RedirectFS ()) {
				string[] unhandled = fs.ParseFuseArguments (args);
				if (!fs.ParseArguments (unhandled))
					return;
				fs.Start ();
			}
		}
	}
}

