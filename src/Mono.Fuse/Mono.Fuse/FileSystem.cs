//
// Mono.Fuse/FileSystem.cs
//
// Authors:
//   Jonathan Pryor (jonpryor@vt.edu)
//
// (C) 2006-2007 Jonathan Pryor
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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Mono.Unix;
using Mono.Unix.Native;

namespace Mono.Fuse {

	[StructLayout (LayoutKind.Sequential)]
	public sealed class FileSystemOperationContext {
		private IntPtr fuse;
		[Map ("uid_t")] private long userId;
		[Map ("gid_t")] private long groupId;
		[Map ("pid_t")] private int  processId;

		internal FileSystemOperationContext ()
		{
		}

		public long UserId {
			get {return userId;}
		}

		public long GroupId {
			get {return groupId;}
		}

		public int ProcessId {
			get {return processId;}
		}
	}

	public class DirectoryEntry {
		private string name;

		public string Name {
			get {return name;}
		}

		// This is used only if st_ino is non-zero and
		// FileSystem.SetsInodes is true
		public Stat Stat;

		private static char[] invalidPathChars = new char[]{'/'};

		public DirectoryEntry (string name)
		{
			if (name == null)
				throw new ArgumentNullException ("name");
			if (name.IndexOfAny (invalidPathChars) != -1)
				throw new ArgumentException (
					"name cannot contain directory separator char", "name");
			this.name = name;
		}
	}

	[Map]
	[StructLayout (LayoutKind.Sequential)]
	public sealed class OpenedPathInfo {
		internal OpenFlags flags;
		private int   write_page;
		private bool  direct_io;
		private bool  keep_cache;
		private ulong file_handle;

		internal OpenedPathInfo ()
		{
		}

		public OpenFlags OpenFlags {
			get {return flags;}
			set {flags = value;}
		}

		private const OpenFlags accessMask = 
			OpenFlags.O_RDONLY | OpenFlags.O_WRONLY | OpenFlags.O_RDWR;

		public OpenFlags OpenAccess {
			get {return flags & accessMask;}
		}

		public int WritePage {
			get {return write_page;}
			set {write_page = value;}
		}

		public bool DirectIO {
			get {return direct_io;}
			set {direct_io = value;}
		}

		public bool KeepCache {
			get {return keep_cache;}
			set {keep_cache = value;}
		}

		public IntPtr Handle {
			get {return (IntPtr) (long) file_handle;}
			set {file_handle = (ulong) (long) value;}
		}
	}

	delegate int GetPathStatusCb (
			[MarshalAs (UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(FileNameMarshaler))]
			string path, IntPtr stat);
	delegate int ReadSymbolicLinkCb (
			[MarshalAs (UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(FileNameMarshaler))]
			string path, IntPtr buf, ulong bufsize);
	delegate int CreateSpecialFileCb (
			[MarshalAs (UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(FileNameMarshaler))]
			string path, uint perms, ulong dev);
	delegate int CreateDirectoryCb (
			[MarshalAs (UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(FileNameMarshaler))]
			string path, uint mode);
	delegate int RemoveFileCb (
			[MarshalAs (UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(FileNameMarshaler))]
			string path);
	delegate int RemoveDirectoryCb (
			[MarshalAs (UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(FileNameMarshaler))]
			string path);
	delegate int CreateSymbolicLinkCb (
			[MarshalAs (UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(FileNameMarshaler))]
			string oldpath, 
			[MarshalAs (UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(FileNameMarshaler))]
			string newpath);
	delegate int RenamePathCb (
			[MarshalAs (UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(FileNameMarshaler))]
			string oldpath, 
			[MarshalAs (UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(FileNameMarshaler))]
			string newpath);
	delegate int CreateHardLinkCb (
			[MarshalAs (UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(FileNameMarshaler))]
			string oldpath, 
			[MarshalAs (UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(FileNameMarshaler))]
			string newpath);
	delegate int ChangePathPermissionsCb (
			[MarshalAs (UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(FileNameMarshaler))]
			string path, uint mode);
	delegate int ChangePathOwnerCb (
			[MarshalAs (UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(FileNameMarshaler))]
			string path, long owner, long group);
	delegate int TruncateFileb (
			[MarshalAs (UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(FileNameMarshaler))]
			string path, long length);
	delegate int ChangePathTimesCb (
			[MarshalAs (UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(FileNameMarshaler))]
			string path, IntPtr buf);
	delegate int OpenHandleCb (
			[MarshalAs (UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(FileNameMarshaler))]
			string path, IntPtr info); 
	delegate int ReadHandleCb (
			[MarshalAs (UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(FileNameMarshaler))]
			string path, 
			[Out, MarshalAs (UnmanagedType.LPArray, ArraySubType=UnmanagedType.U1, SizeParamIndex=2)]
			byte[] buf, ulong size, long offset, IntPtr info, out int bytesRead);
	delegate int WriteHandleCb (
			[MarshalAs (UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(FileNameMarshaler))]
			string path, 
			[In, MarshalAs (UnmanagedType.LPArray, ArraySubType=UnmanagedType.U1, SizeParamIndex=2)]
			byte[] buf, ulong size, long offset, IntPtr info, out int bytesRead);
	delegate int GetFileSystemStatusCb (
			[MarshalAs (UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(FileNameMarshaler))]
			string path, IntPtr buf);
	delegate int FlushHandleCb (
			[MarshalAs (UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(FileNameMarshaler))]
			string path, IntPtr info);
	delegate int ReleaseHandleCb (
			[MarshalAs (UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(FileNameMarshaler))]
			string path, IntPtr info);
	delegate int SynchronizeHandleCb (
			[MarshalAs (UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(FileNameMarshaler))]
			string path, bool onlyUserData, IntPtr info);
	delegate int SetPathExtendedAttributeCb (
			[MarshalAs (UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(FileNameMarshaler))]
			string path, 
			[MarshalAs (UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(FileNameMarshaler))]
			string name, 
			[In, MarshalAs (UnmanagedType.LPArray, ArraySubType=UnmanagedType.U1, SizeParamIndex=3)]
			byte[] value, ulong size, int flags);
	delegate int GetPathExtendedAttributeCb (
			[MarshalAs (UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(FileNameMarshaler))]
			string path, 
			[MarshalAs (UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(FileNameMarshaler))]
			string name, 
			[Out, MarshalAs (UnmanagedType.LPArray, ArraySubType=UnmanagedType.U1, SizeParamIndex=3)]
			byte[] value, ulong size, out int bytesWritten);
	delegate int ListPathExtendedAttributesCb (
			[MarshalAs (UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(FileNameMarshaler))]
			string path, 
			[Out, MarshalAs (UnmanagedType.LPArray, ArraySubType=UnmanagedType.U1, SizeParamIndex=2)]
			byte[] list, ulong size, out int bytesWritten);
	delegate int RemovePathExtendedAttributeCb (
			[MarshalAs (UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(FileNameMarshaler))]
			string path, 
			[MarshalAs (UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(FileNameMarshaler))]
			string name);
	delegate int OpenDirectoryCb (
			[MarshalAs (UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(FileNameMarshaler))]
			string path, IntPtr info);
	delegate int ReadDirectoryCb (
			[MarshalAs (UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(FileNameMarshaler))]
			string path, IntPtr buf, IntPtr filler, 
			long offset, IntPtr info, IntPtr stbuf);
	delegate int ReleaseDirectoryCb (
			[MarshalAs (UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(FileNameMarshaler))]
			string path, IntPtr info);
	delegate int SynchronizeDirectoryCb (
			[MarshalAs (UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(FileNameMarshaler))]
			string path, bool onlyUserData, IntPtr info);
	delegate IntPtr InitCb ();
	delegate int AccessPathCb (
			[MarshalAs (UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(FileNameMarshaler))]
			string path, int mode);
	delegate int CreateHandleCb (
			[MarshalAs (UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(FileNameMarshaler))]
			string path, uint mode, IntPtr info);
	delegate int TruncateHandleCb (
			[MarshalAs (UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(FileNameMarshaler))]
			string path, long length, IntPtr info);
	delegate int GetHandleStatusCb (
			[MarshalAs (UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(FileNameMarshaler))]
			string path, IntPtr buf, IntPtr info);

	[Map]
	[StructLayout (LayoutKind.Sequential)]
	class Operations {
		public GetPathStatusCb                getattr;
		public ReadSymbolicLinkCb             readlink;
		public CreateSpecialFileCb            mknod;
		public CreateDirectoryCb              mkdir;
		public RemoveFileCb                   unlink;
		public RemoveDirectoryCb              rmdir;
		public CreateSymbolicLinkCb           symlink;
		public RenamePathCb                   rename;
		public CreateHardLinkCb               link;
		public ChangePathPermissionsCb        chmod;
		public ChangePathOwnerCb              chown;
		public TruncateFileb                  truncate;
		public ChangePathTimesCb              utime;
		public OpenHandleCb                   open;
		public ReadHandleCb                   read;
		public WriteHandleCb                  write;
		public GetFileSystemStatusCb          statfs;
		public FlushHandleCb                  flush;
		public ReleaseHandleCb                release;
		public SynchronizeHandleCb            fsync;
		public SetPathExtendedAttributeCb     setxattr;
		public GetPathExtendedAttributeCb     getxattr;
		public ListPathExtendedAttributesCb   listxattr;
		public RemovePathExtendedAttributeCb  removexattr;
		public OpenDirectoryCb                opendir;
		public ReadDirectoryCb                readdir;
		public ReleaseDirectoryCb             releasedir;
		public SynchronizeDirectoryCb         fsyncdir;
		public InitCb                         init;
		public AccessPathCb                   access;
		public CreateHandleCb                 create;
		public TruncateHandleCb               ftruncate;
		public GetHandleStatusCb              fgetattr;
	}

	[Map ("struct fuse_args")]
	[StructLayout (LayoutKind.Sequential)]
	class Args {
		public int argc;
		public IntPtr argv;
		public int allocated;
	}

	public abstract class FileSystem : IDisposable {

		const string LIB = "MonoFuseHelper";

		[DllImport (LIB, SetLastError=true)]
		private static extern IntPtr mfh_fuse_new (int fd, Args args, IntPtr ops);

		[DllImport (LIB, SetLastError=true)]
		private static extern int mfh_fuse_get_context ([In, Out] FileSystemOperationContext context);

		[DllImport (LIB, SetLastError=true)]
		private static extern int mfh_fuse_mount (
				[MarshalAs (UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(FileNameMarshaler))]
				string path, Args args);

		[DllImport (LIB, SetLastError=true)]
		private static extern int mfh_fuse_unmount (
				[MarshalAs (UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(FileNameMarshaler))]
				string path);

		[DllImport (LIB, SetLastError=true)]
		private static extern void mfh_fuse_destroy (IntPtr fusep);

		[DllImport (LIB, SetLastError=true)]
		private static extern int mfh_fuse_loop (IntPtr fusep);

		[DllImport (LIB, SetLastError=true)]
		private static extern int mfh_fuse_loop_mt (IntPtr fusep);

		[DllImport (LIB, SetLastError=true)]
		private static extern void mfh_fuse_exit (IntPtr fusep);

		[DllImport (LIB, SetLastError=false)]
		private static extern int mfh_invoke_filler (IntPtr filler, IntPtr buf,
				[MarshalAs (UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(FileNameMarshaler))]
				string path, IntPtr stbuf, long offset);

		[DllImport (LIB, SetLastError=false)]
		private static extern void mfh_show_fuse_help (string appname);

		private string mountPoint;
		private int fd = -1;
		private IntPtr fusep = IntPtr.Zero;
		Dictionary <string, string> opts = new Dictionary <string, string> ();
		private Operations ops;
		private IntPtr opsp;

		protected FileSystem (string mountPoint)
		{
			this.mountPoint = mountPoint;
		}

		protected FileSystem ()
		{
		}

		protected FileSystem (string[] args)
		{
			string[] unhandled = ParseFuseArguments (args);
			MountPoint = unhandled [unhandled.Length - 1];
		}

		public IDictionary <string, string> FuseOptions {
			get {return opts;}
		}

		public bool EnableFuseDebugOutput {
			get {return GetBool ("debug");}
			set {Set ("debug", value ? "" : null);}
		}

		public bool AllowAccessToOthers {
			get {return GetBool ("allow_other");}
			set {Set ("allow_other", value ? "" : null);}
		}

		public bool AllowAccessToRoot {
			get {return GetBool ("allow_root");}
			set {Set ("allow_root", value ? "" : null);}
		}

		public bool AllowMountOverNonEmptyDirectory {
			get {return GetBool ("nonempty");}
			set {Set ("nonempty", value ? "" : null);}
		}

		public bool EnableKernelPermissionChecking {
			get {return GetBool ("default_permissions");}
			set {Set ("default_permissions", value ? "" : null);}
		}

		public string Name {
			get {return GetString ("fsname");}
			set {Set ("fsname", value);}
		}

		public bool EnableLargeReadRequests {
			get {return GetBool ("large_read");}
			set {Set ("large_read", value ? "" : null);}
		}

		public int MaxReadSize {
			get {return (int) GetLong ("max_read");}
			set {Set ("max_read", value.ToString ());}
		}

		public bool ImmediatePathRemoval {
			get {return GetBool ("hard_remove");}
			set {Set ("hard_remove", value ? "" : null);}
		}

		public bool SetsInodes {
			get {return GetBool ("use_ino");}
			set {Set ("use_ino", value ? "" : null);}
		}

		public bool ReaddirSetsInode {
			get {return GetBool ("readdir_ino");}
			set {Set ("readdir_ino", value ? "" : null);}
		}

		public bool EnableDirectIO {
			get {return GetBool ("direct_io");}
			set {Set ("direct_io", value ? "" : null);}
		}

		public bool EnableKernelCache {
			get {return GetBool ("kernel_cache");}
			set {Set ("kernel_cache", value ? "" : null);}
		}

		public FilePermissions DefaultUmask {
			get {
				string umask = GetString ("umask") ?? "0000";
				return NativeConvert.FromOctalPermissionString (umask);
			}
			set {
				Set ("umask", NativeConvert.ToOctalPermissionString (value));
			}
		}

		public long DefaultUserId {
			get {return GetLong ("uid");}
			set {Set ("uid", value.ToString ());}
		}

		public long DefaultGroupId {
			get {return GetLong ("gid");}
			set {Set ("gid", value.ToString ());}
		}

		public double PathTimeout {
			get {return (int) GetDouble ("entry_timeout");}
			set {Set ("entry_timeout", value.ToString ());}
		}

		public double DeletedPathTimeout {
			get {return (int) GetDouble ("negative_timeout");}
			set {Set ("negative_timeout", value.ToString ());}
		}

		public double AttributeTimeout {
			get {return (int) GetDouble ("attr_timeout");}
			set {Set ("attr_timeout", value.ToString ());}
		}

		private bool GetBool (string key)
		{
			return opts.ContainsKey (key);
		}

		private double GetDouble (string key)
		{
			if (opts.ContainsKey (key))
				return double.Parse (opts [key]);
			return 0.0;
		}

		private string GetString (string key)
		{
			if (opts.ContainsKey (key))
				return opts [key];
			return "";
		}

		private long GetLong (string key)
		{
			if (opts.ContainsKey (key))
				return long.Parse (opts [key]);
			return 0;
		}

		private void Set (string key, string value)
		{
			if (value == null) {
				opts.Remove (key);
				return;
			}
			opts [key] = value;
		}

		public string MountPoint {
			get {return mountPoint;}
			set {mountPoint = value;}
		}

		private bool multithreaded = true;
		public bool MultiThreaded {
			get {return multithreaded;}
			set {multithreaded = value;}
		}

		const string NameValueRegex = @"(?<Name>\w+)(\s*=\s*(?<Value>.*))?";
		const string OptRegex = @"^-o\s*(" + NameValueRegex + ")?$";

		public string[] ParseFuseArguments (string[] args)
		{
			List<string> unhandled = new List<string> ();
			Regex o = new Regex (OptRegex);
			Regex nv = new Regex (NameValueRegex);
			bool interpret = true;

			for (int i = 0; i < args.Length; ++i) {
				if (!interpret) {
					unhandled.Add (args [i]);
					continue;
				}
				Match m = o.Match (args [i]);
				if (m.Success) {
					if (!m.Groups ["Name"].Success) {
						m = nv.Match (args [++i]);
						if (!m.Success)
							throw new ArgumentException ("args");
					}
					opts [m.Groups ["Name"].Value] = 
						m.Groups ["Value"].Success ? m.Groups ["Value"].Value : "";
				}
				else if (args [i] == "-d") {
					opts ["debug"] = "";
				}
				else if (args [i] == "-s") {
					multithreaded = false;
				}
				else if (args [i] == "-f") {
					// foreground operation; ignore 
					// (we can only do foreground operation anyway)
				}
				else if (args [i] == "--") {
					interpret = false;
				}
				else {
					unhandled.Add (args [i]);
				}
			}
			return unhandled.ToArray ();
		}

		public static void ShowFuseHelp (string appname)
		{
			mfh_show_fuse_help (appname);
		}

		private void Create ()
		{
			if (mountPoint == null)
				throw new InvalidOperationException ("MountPoint must not be null");
			string[] _args = GetFuseArgs ();
			Args args = new Args ();
			bool unmount = false;
			try {
				args.argc = _args.Length;
				args.argv = AllocArgv (_args);
				args.allocated = 1;
				this.ops = GetOperations ();
				fd = mfh_fuse_mount (mountPoint, args);
				if (fd == -1) {
					throw new NotSupportedException (
							string.Format ("Unable to mount directory `{0}'; " +
								"try running `/sbin/modprobe fuse' as the root user", mountPoint));
				}
				unmount = true;
				this.opsp = UnixMarshal.AllocHeap (Marshal.SizeOf (ops));
				Marshal.StructureToPtr (ops, opsp, false);
				fusep = mfh_fuse_new (fd, args, opsp);
				if (fusep == IntPtr.Zero) {
					this.ops = null;
					throw new NotSupportedException ("Unable to create FUSE object: " + 
							"is the fuse kernel module installed?");
				}
				unmount = false;
			}
			finally {
				FreeArgv (args.argc, args.argv);
				if (unmount)
					mfh_fuse_unmount (mountPoint);
			}
		}

		private string[] GetFuseArgs ()
		{
			string[] args = new string [opts.Keys.Count + 1];
			int i = 0;
			args [i++] = Environment.GetCommandLineArgs () [0];
			foreach (string key in opts.Keys) {
				if (key == "debug") {
					args [i++] = "-d";
					continue;
				}
				string v = opts [key];
				string a = "-o" + key;
				if (v.Length > 0) {
					a += "=" + v.ToString ();
				}
				args [i++] = a;
			}
			Console.WriteLine ("FUSE Arguments");
			foreach (string s in args)
				Console.WriteLine ("\t" + s);
			return args;
		}

		private static IntPtr AllocArgv (string[] args)
		{
			IntPtr argv = UnixMarshal.AllocHeap ((args.Length+1) * IntPtr.Size);
			int i;
			for (i = 0; i < args.Length; ++i) {
				Marshal.WriteIntPtr (argv, i*IntPtr.Size, 
						UnixMarshal.StringToHeap (args [i]));
			}
			Marshal.WriteIntPtr (argv, args.Length*IntPtr.Size, IntPtr.Zero);
			return argv;
		}

		private static void FreeArgv (int argc, IntPtr argv)
		{
			if (argv == IntPtr.Zero)
				return;
			for (int i = 0; i < argc; ++i) {
				IntPtr p = Marshal.ReadIntPtr (argv, i * IntPtr.Size);
				UnixMarshal.FreeHeap (p);
			}
			UnixMarshal.FreeHeap (argv);
		}

		delegate void CopyOperation (Operations to, FileSystem from);
		static readonly Dictionary <string, CopyOperation> operations;

		static FileSystem ()
		{
			operations = new Dictionary <string, CopyOperation> ();

			operations.Add ("OnGetPathStatus", 
					delegate (Operations to, FileSystem from) {to.getattr = from._OnGetPathStatus;});
			operations.Add ("OnReadSymbolicLink", 
					delegate (Operations to, FileSystem from) {to.readlink = from._OnReadSymbolicLink;});
			operations.Add ("OnCreateSpecialFile", 
					delegate (Operations to, FileSystem from) {to.mknod = from._OnCreateSpecialFile;});
			operations.Add ("OnCreateDirectory", 
					delegate (Operations to, FileSystem from) {to.mkdir = from._OnCreateDirectory;});
			operations.Add ("OnRemoveFile", 
					delegate (Operations to, FileSystem from) {to.unlink = from._OnRemoveFile;});
			operations.Add ("OnRemoveDirectory", 
					delegate (Operations to, FileSystem from) {to.rmdir = from._OnRemoveDirectory;});
			operations.Add ("OnCreateSymbolicLink", 
					delegate (Operations to, FileSystem from) {to.symlink = from._OnCreateSymbolicLink;});
			operations.Add ("OnRenamePath", 
					delegate (Operations to, FileSystem from) {to.rename = from._OnRenamePath;});
			operations.Add ("OnCreateHardLink", 
					delegate (Operations to, FileSystem from) {to.link = from._OnCreateHardLink;});
			operations.Add ("OnChangePathPermissions", 
					delegate (Operations to, FileSystem from) {to.chmod = from._OnChangePathPermissions;});
			operations.Add ("OnChangePathOwner", 
					delegate (Operations to, FileSystem from) {to.chown = from._OnChangePathOwner;});
			operations.Add ("OnTruncateFile", 
					delegate (Operations to, FileSystem from) {to.truncate = from._OnTruncateFile;});
			operations.Add ("OnChangePathTimes", 
					delegate (Operations to, FileSystem from) {to.utime = from._OnChangePathTimes;});
			operations.Add ("OnOpenHandle", 
					delegate (Operations to, FileSystem from) {to.open = from._OnOpenHandle;});
			operations.Add ("OnReadHandle", 
					delegate (Operations to, FileSystem from) {to.read = from._OnReadHandle;});
			operations.Add ("OnWriteHandle", 
					delegate (Operations to, FileSystem from) {to.write = from._OnWriteHandle;});
			operations.Add ("OnGetFileSystemStatus", 
					delegate (Operations to, FileSystem from) {to.statfs = from._OnGetFileSystemStatus;});
			operations.Add ("OnFlushHandle", 
					delegate (Operations to, FileSystem from) {to.flush = from._OnFlushHandle;});
			operations.Add ("OnReleaseHandle", 
					delegate (Operations to, FileSystem from) {to.release = from._OnReleaseHandle;});
			operations.Add ("OnSynchronizeHandle", 
					delegate (Operations to, FileSystem from) {to.fsync = from._OnSynchronizeHandle;});
			operations.Add ("OnSetPathExtendedAttribute", 
					delegate (Operations to, FileSystem from) {to.setxattr = from._OnSetPathExtendedAttribute;});
			operations.Add ("OnGetPathExtendedAttribute", 
					delegate (Operations to, FileSystem from) {to.getxattr = from._OnGetPathExtendedAttribute;});
			operations.Add ("OnListPathExtendedAttributes", 
					delegate (Operations to, FileSystem from) {to.listxattr = from._OnListPathExtendedAttributes;});
			operations.Add ("OnRemovePathExtendedAttribute", 
					delegate (Operations to, FileSystem from) {to.removexattr = from._OnRemovePathExtendedAttribute;});
			operations.Add ("OnOpenDirectory", 
					delegate (Operations to, FileSystem from) {to.opendir = from._OnOpenDirectory;});
			operations.Add ("OnReadDirectory", 
					delegate (Operations to, FileSystem from) {to.readdir = from._OnReadDirectory;});
			operations.Add ("OnReleaseDirectory", 
					delegate (Operations to, FileSystem from) {to.releasedir = from._OnReleaseDirectory;});
			operations.Add ("OnSynchronizeDirectory", 
					delegate (Operations to, FileSystem from) {to.fsyncdir = from._OnSynchronizeDirectory;});
			operations.Add ("OnAccessPath", 
					delegate (Operations to, FileSystem from) {to.access = from._OnAccessPath;});
			operations.Add ("OnCreateHandle", 
					delegate (Operations to, FileSystem from) {to.create = from._OnCreateHandle;});
			operations.Add ("OnTruncateHandle", 
					delegate (Operations to, FileSystem from) {to.ftruncate = from._OnTruncateHandle;});
			operations.Add ("OnGetHandleStatus", 
					delegate (Operations to, FileSystem from) {to.fgetattr = from._OnGetHandleStatus;});
		}

 		private Operations GetOperations ()
 		{
 			Operations ops = new Operations ();

			ops.init = OnInit;
			foreach (string method in operations.Keys) {
				MethodInfo m = this.GetType().GetMethod (method, 
						BindingFlags.NonPublic | BindingFlags.Instance);
				MethodInfo bm = m.GetBaseDefinition ();
				if (m.DeclaringType == typeof(FileSystem) ||
						bm == null || bm.DeclaringType != typeof(FileSystem))
					continue;
				CopyOperation op = operations [method];
				op (ops, this);
			}

			ValidateOperations (ops);

 			return ops;
 		}
 
		private static void ValidateOperations (Operations ops)
		{
			// some methods need to be overridden in sets for sane operation
			if (ops.opendir != null && ops.releasedir == null)
				throw new InvalidOperationException (
						"OnReleaseDirectory() must be overridden if OnOpenDirectory() is overridden.");
		}

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (disposing)
				ops = null;

			if (opsp != IntPtr.Zero) {
				Marshal.DestroyStructure (opsp, typeof(Operations));
				UnixMarshal.FreeHeap (opsp);
				opsp = IntPtr.Zero;
			}

			if (fusep != IntPtr.Zero) {
				mfh_fuse_unmount (MountPoint);
				mfh_fuse_destroy (fusep);
				fusep = IntPtr.Zero;
			}
		}

		~FileSystem ()
		{
			Dispose (false);
		}

		public void Start ()
		{
			Create ();
			if (MultiThreaded) {
				mfh_fuse_loop_mt (fusep);
			}
			else {
				mfh_fuse_loop (fusep);
			}
		}

		public void Stop ()
		{
			mfh_fuse_exit (fusep);
		}

		protected static FileSystemOperationContext GetOperationContext ()
		{
			FileSystemOperationContext context = new FileSystemOperationContext ();
			int r = mfh_fuse_get_context (context);
			UnixMarshal.ThrowExceptionForLastErrorIf (r);
			return context;
		}

		[DllImport (LIB, SetLastError=true)]
		private static extern int Mono_Fuse_FromOpenedPathInfo (OpenedPathInfo source, IntPtr dest);

		[DllImport (LIB, SetLastError=true)]
		private static extern int Mono_Fuse_ToOpenedPathInfo (IntPtr source, [Out] OpenedPathInfo dest);

#if !HAVE_MONO_UNIX_NATIVE_COPY_FUNCS
		[DllImport (LIB, SetLastError=true)]
		private static extern int Mono_Fuse_FromStat (ref Stat source, IntPtr dest);

		[DllImport (LIB, SetLastError=true)]
		private static extern int Mono_Fuse_ToStat (IntPtr source, out Stat dest);

		[DllImport (LIB, SetLastError=true)]
		private static extern int Mono_Fuse_FromStatvfs (ref Statvfs source, IntPtr dest);

		[DllImport (LIB, SetLastError=true)]
		private static extern int Mono_Fuse_ToStatvfs (IntPtr source, out Statvfs dest);

		[DllImport (LIB, SetLastError=true)]
		private static extern int Mono_Fuse_FromUtimbuf (ref Utimbuf source, IntPtr dest);

		[DllImport (LIB, SetLastError=true)]
		private static extern int Mono_Fuse_ToUtimbuf (IntPtr source, out Utimbuf dest);
#endif

		private static void CopyStat (IntPtr source, out Stat dest)
		{
#if HAVE_MONO_UNIX_NATIVE_COPY_FUNCS
			if (!NativeConvert.TryCopy (source, out dest))
				throw new ArgumentOutOfRangeException ("Unable to copy `struct stat' into Mono.Unix.Native.Stat.");
#else
			Mono_Fuse_ToStat (source, out dest);
			dest.st_mode = NativeConvert.ToFilePermissions ((uint) dest.st_mode);
#endif
		}

		private static void CopyStat (ref Stat source, IntPtr dest)
		{
#if HAVE_MONO_UNIX_NATIVE_COPY_FUNCS
			if (!NativeConvert.TryCopy (ref source, dest))
				throw new ArgumentOutOfRangeException ("Unable to copy Mono.Unix.Native.Stat into `struct stat'.");
#else
			source.st_mode = (FilePermissions) 
				NativeConvert.FromFilePermissions (source.st_mode);
			Mono_Fuse_FromStat (ref source, dest);
#endif
		}

		private static void CopyStatvfs (IntPtr source, out Statvfs dest)
		{
#if HAVE_MONO_UNIX_NATIVE_COPY_FUNCS
			if (!NativeConvert.TryCopy (source, out dest))
				throw new ArgumentOutOfRangeException ("Unable to copy `struct statvfs' into Mono.Unix.Native.Statvfs.");
#else
			Mono_Fuse_ToStatvfs (source, out dest);
			dest.f_flag = NativeConvert.ToMountFlags ((ulong) dest.f_flag);
#endif
		}

		private static void CopyStatvfs (ref Statvfs source, IntPtr dest)
		{
#if HAVE_MONO_UNIX_NATIVE_COPY_FUNCS
			if (!NativeConvert.TryCopy (ref source, dest))
				throw new ArgumentOutOfRangeException ("Unable to copy Mono.Unix.Native.Statvfs into `struct statvfs'.");
#else
			source.f_flag = (MountFlags) NativeConvert.FromMountFlags (source.f_flag);
			Mono_Fuse_FromStatvfs (ref source, dest);
#endif
		}

		private static void CopyUtimbuf (IntPtr source, out Utimbuf dest)
		{
#if HAVE_MONO_UNIX_NATIVE_COPY_FUNCS
			if (!NativeConvert.TryCopy (source, out dest))
				throw new ArgumentOutOfRangeException ("Unable to copy `struct utimbuf' into Mono.Unix.Native.Utimbuf.");
#else
			Mono_Fuse_ToUtimbuf (source, out dest);
#endif
		}

		private static void CopyUtimbuf (ref Utimbuf source, IntPtr dest)
		{
#if HAVE_MONO_UNIX_NATIVE_COPY_FUNCS
			if (!NativeConvert.TryCopy (ref source, dest))
				throw new ArgumentOutOfRangeException ("Unable to copy Mono.Unix.Native.Utimbuf into `struct utimbuf'.");
#else
			Mono_Fuse_FromUtimbuf (ref source, dest);
#endif
		}

		private static void CopyOpenedPathInfo (IntPtr source, OpenedPathInfo dest)
		{
			Mono_Fuse_ToOpenedPathInfo (source, dest);
			dest.flags = NativeConvert.ToOpenFlags ((int) dest.flags);
		}

		private static void CopyOpenedPathInfo (OpenedPathInfo source, IntPtr dest)
		{
			source.flags = (OpenFlags) NativeConvert.FromOpenFlags (source.flags);
			Mono_Fuse_FromOpenedPathInfo (source, dest);
		}

		private int _OnGetPathStatus (string path, IntPtr stat)
		{
			Errno errno;
			try {
				Stat buf;
				CopyStat (stat, out buf);
				errno = OnGetPathStatus (path, out buf);
				if (errno == 0)
					CopyStat (ref buf, stat);
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		private int ConvertErrno (Errno e)
		{
			int r;
			if (NativeConvert.TryFromErrno (e, out r))
				return -r;
			return -1;
		}

		protected virtual Errno OnGetPathStatus (string path, out Stat stat)
		{
			stat = new Stat ();
			return Errno.ENOSYS;
		}

		private int _OnReadSymbolicLink (string path, IntPtr buf, ulong bufsize)
		{
			Errno errno;
			try {
				if (bufsize <= 1)
					return ConvertErrno (Errno.EINVAL);
				string target;
				errno = OnReadSymbolicLink (path, out target);
				if (errno == 0 && target != null) {
					byte[] b = encoding.GetBytes (target);
					if ((bufsize-1) < (ulong) b.Length) {
						errno = Errno.EINVAL;
					}
					else {
						Marshal.Copy (b, 0, buf, b.Length);
						Marshal.WriteByte (buf, b.Length, (byte) 0);
					}
				}
				else if (errno == 0 && target == null) {
					Trace.WriteLine ("OnReadSymbolicLink: error: 0 return value but target is `null'");
					errno = Errno.EIO;
				}
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		static Encoding encoding = new UTF8Encoding (false, true);

		protected virtual Errno OnReadSymbolicLink (string link, out string target)
		{
			target = null;
			return Errno.ENOSYS;
		}

		private int _OnCreateSpecialFile (string path, uint perms, ulong dev)
		{
			Errno errno;
			try {
				FilePermissions _perms = NativeConvert.ToFilePermissions (perms);
				errno = OnCreateSpecialFile (path, _perms, dev);
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		protected virtual Errno OnCreateSpecialFile (string file, FilePermissions perms, ulong dev)
		{
			return Errno.ENOSYS;
		}

		private int _OnCreateDirectory (string path, uint mode)
		{
			Errno errno;
			try {
				FilePermissions _mode = NativeConvert.ToFilePermissions (mode);
				errno = OnCreateDirectory (path, _mode);
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		protected virtual Errno OnCreateDirectory (string directory, FilePermissions mode)
		{
			return Errno.ENOSYS;
		}

		private int _OnRemoveFile (string path)
		{
			Errno errno;
			try {
				errno = OnRemoveFile (path);
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		protected virtual Errno OnRemoveFile (string file)
		{
			return Errno.ENOSYS;
		}

		private int _OnRemoveDirectory (string path)
		{
			Errno errno;
			try {
				errno = OnRemoveDirectory (path);
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		protected virtual Errno OnRemoveDirectory (string directory)
		{
			return Errno.ENOSYS;
		}

		private int _OnCreateSymbolicLink (string oldpath, string newpath)
		{
			Errno errno;
			try {
				errno = OnCreateSymbolicLink (oldpath, newpath);
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		protected virtual Errno OnCreateSymbolicLink (string target, string link)
		{
			return Errno.ENOSYS;
		}

		private int _OnRenamePath (string oldpath, string newpath)
		{
			Errno errno;
			try {
				errno = OnRenamePath (oldpath, newpath);
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		protected virtual Errno OnRenamePath (string oldpath, string newpath)
		{
			return Errno.ENOSYS;
		}

		private int _OnCreateHardLink (string oldpath, string newpath)
		{
			Errno errno;
			try {
				errno = OnCreateHardLink (oldpath, newpath);
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		protected virtual Errno OnCreateHardLink (string oldpath, string link)
		{
			return Errno.ENOSYS;
		}

		private int _OnChangePathPermissions (string path, uint mode)
		{
			Errno errno;
			try {
				FilePermissions _mode = NativeConvert.ToFilePermissions (mode);
				errno = OnChangePathPermissions (path, _mode);
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		protected virtual Errno OnChangePathPermissions (string path, FilePermissions mode)
		{
			return Errno.ENOSYS;
		}

		private int _OnChangePathOwner (string path, long owner, long group)
		{
			Errno errno;
			try {
				errno = OnChangePathOwner (path, owner, group);
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		protected virtual Errno OnChangePathOwner (string path, long owner, long group)
		{
			return Errno.ENOSYS;
		}

		private int _OnTruncateFile (string path, long length)
		{
			Errno errno;
			try {
				errno = OnTruncateFile (path, length);
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		protected virtual Errno OnTruncateFile (string file, long length)
		{
			return Errno.ENOSYS;
		}

		// TODO: can buf be null?
		private int _OnChangePathTimes (string path, IntPtr buf)
		{
			Errno errno;
			try {
				Utimbuf b;
				CopyUtimbuf (buf, out b);
				errno = OnChangePathTimes (path, ref b);
				if (errno == 0)
					CopyUtimbuf (ref b, buf);
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		protected virtual Errno OnChangePathTimes (string path, ref Utimbuf buf)
		{
			return Errno.ENOSYS;
		}

		private int _OnOpenHandle (string path, IntPtr fi)
		{
			Errno errno;
			try {
				OpenedPathInfo info = new OpenedPathInfo ();
				CopyOpenedPathInfo (fi, info);
				errno = OnOpenHandle (path, info);
				if (errno == 0)
					CopyOpenedPathInfo (info, fi);
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		protected virtual Errno OnOpenHandle (string file, OpenedPathInfo info)
		{
			return Errno.ENOSYS;
		}
 
		private int _OnReadHandle (string path, byte[] buf, ulong size, long offset, IntPtr fi, out int bytesWritten)
		{
			Errno errno;
			try {
				OpenedPathInfo info = new OpenedPathInfo ();
				CopyOpenedPathInfo (fi, info);
				errno = OnReadHandle (path, info, buf, offset, out bytesWritten);
				if (errno == 0)
					CopyOpenedPathInfo (info, fi);
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				bytesWritten = 0;
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		protected virtual Errno OnReadHandle (string file, OpenedPathInfo info, byte[] buf, long offset, out int bytesWritten)
		{
			bytesWritten = 0;
			return Errno.ENOSYS;
		}

		private int _OnWriteHandle (string path, byte[] buf, ulong size, long offset, IntPtr fi, out int bytesRead)
		{
			Errno errno;
			try {
				OpenedPathInfo info = new OpenedPathInfo ();
				CopyOpenedPathInfo (fi, info);
				errno = OnWriteHandle (path, info, buf, offset, out bytesRead);
				if (errno == 0)
					CopyOpenedPathInfo (info, fi);
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				bytesRead = 0;
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		protected virtual Errno OnWriteHandle (string file, OpenedPathInfo info, byte[] buf, long offset, out int bytesRead)
		{
			bytesRead = 0;
			return Errno.ENOSYS;
		}

		private int _OnGetFileSystemStatus (string path, IntPtr buf)
		{
			Errno errno;
			try {
				Statvfs b;
				CopyStatvfs (buf, out b);
				errno = OnGetFileSystemStatus (path, out b);
				if (errno == 0)
					CopyStatvfs (ref b, buf);
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		protected virtual Errno OnGetFileSystemStatus (string path, out Statvfs buf)
		{
			buf = new Statvfs ();
			return Errno.ENOSYS;
		}

		private int _OnFlushHandle (string path, IntPtr fi)
		{
			Errno errno;
			try {
				OpenedPathInfo info = new OpenedPathInfo ();
				CopyOpenedPathInfo (fi, info);
				errno = OnFlushHandle (path, info);
				CopyOpenedPathInfo (info, fi);
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		protected virtual Errno OnFlushHandle (string file, OpenedPathInfo info)
		{
			return Errno.ENOSYS;
		}

		private int _OnReleaseHandle (string path, IntPtr fi)
		{
			Errno errno;
			try {
				OpenedPathInfo info = new OpenedPathInfo ();
				CopyOpenedPathInfo (fi, info);
				errno = OnReleaseHandle (path, info);
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		protected virtual Errno OnReleaseHandle (string file, OpenedPathInfo info)
		{
			return Errno.ENOSYS;
		}

		private int _OnSynchronizeHandle (string path, bool onlyUserData, IntPtr fi)
		{
			Errno errno;
			try {
				OpenedPathInfo info = new OpenedPathInfo ();
				CopyOpenedPathInfo (fi, info);
				errno = OnSynchronizeHandle (path, info, onlyUserData);
				if (errno == 0)
					CopyOpenedPathInfo (info, fi);
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		protected virtual Errno OnSynchronizeHandle (string file, OpenedPathInfo info, bool onlyUserData)
		{
			return Errno.ENOSYS;
		}

		private int _OnSetPathExtendedAttribute (string path, string name, byte[] value, ulong size, int flags)
		{
			Errno errno;
			try {
				XattrFlags f = NativeConvert.ToXattrFlags (flags);
				errno = OnSetPathExtendedAttribute (path, name, value, f);
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		protected virtual Errno OnSetPathExtendedAttribute (string path, string name, byte[] value, XattrFlags flags)
		{
			return Errno.ENOSYS;
		}

		private int _OnGetPathExtendedAttribute (string path, string name, byte[] value, ulong size, out int bytesWritten)
		{
			Errno errno;
			try {
				errno = OnGetPathExtendedAttribute (path, name, value, out bytesWritten);
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				bytesWritten = 0;
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		protected virtual Errno OnGetPathExtendedAttribute (string path, string name, byte[] value, out int bytesWritten)
		{
			bytesWritten = 0;
			return Errno.ENOSYS;
		}

		private int _OnListPathExtendedAttributes (string path, byte[] list, ulong size,  out int bytesWritten)
		{
			Errno errno;
			try {
				bytesWritten = 0;
				string[] names;
				errno = OnListPathExtendedAttributes (path, out names);
				if (errno == 0 && names != null) {
					int bytesNeeded = 0;
					for (int i = 0; i < names.Length; ++i) {
						bytesNeeded += encoding.GetByteCount (names [i]) + 1;
					}
					if (size == 0)
						bytesWritten = bytesNeeded;
					if (size < (ulong) bytesNeeded) {
						errno = Errno.ERANGE;
					}
					else {
						int dest = 0;
						for (int i = 0; i < names.Length; ++i) {
							int b = encoding.GetBytes (names [i], 0, names [i].Length, 
									list, dest);
							list[dest+b] = (byte) '\0';
							dest += (b + 1);
						}
						bytesWritten = dest;
					}
				}
				else
					bytesWritten = 0;
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				bytesWritten = 0;
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		protected virtual Errno OnListPathExtendedAttributes (string path, out string[] names)
		{
			names = null;
			return Errno.ENOSYS;
		}

		private int _OnRemovePathExtendedAttribute (string path, string name)
		{
			Errno errno;
			try {
				errno = OnRemovePathExtendedAttribute (path, name);
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		protected virtual Errno OnRemovePathExtendedAttribute (string path, string name)
		{
			return Errno.ENOSYS;
		}

		private int _OnOpenDirectory (string path, IntPtr fi)
		{
			Errno errno;
			try {
				OpenedPathInfo info = new OpenedPathInfo ();
				CopyOpenedPathInfo (fi, info);
				errno = OnOpenDirectory (path, info);
				if (errno == 0)
					CopyOpenedPathInfo (info, fi);
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		protected virtual Errno OnOpenDirectory (string directory, OpenedPathInfo info)
		{
			return Errno.ENOSYS;
		}

		private object directoryLock = new object ();

		private Dictionary<string, EntryEnumerator> directoryReaders = 
			new Dictionary <string, EntryEnumerator> ();

		private Random directoryKeys = new Random ();

		private int _OnReadDirectory (string path, IntPtr buf, IntPtr filler, 
				long offset, IntPtr fi, IntPtr stbuf)
		{
			Errno errno = 0;
			try {
				if (offset == 0)
					GetDirectoryEnumerator (path, fi, out offset, out errno);
				if (errno != 0)
					return ConvertErrno (errno);

				EntryEnumerator entries = null;
				lock (directoryLock) {
					string key = offset.ToString ();
					if (directoryReaders.ContainsKey (key))
						entries = directoryReaders [key];
				}

				// FUSE will invoke _OnReadDirectory at least twice, but if there were
				// very few entries then the enumerator will get cleaned up during the
				// first call, so this is (1) expected, and (2) ignorable.
				if (entries == null) {
					return 0;
				}

				bool cleanup = FillEntries (filler, buf, stbuf, offset, entries);

				if (cleanup) {
					entries.Dispose ();
					lock (directoryLock) {
						directoryReaders.Remove (offset.ToString ());
					}
				}
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		private void GetDirectoryEnumerator (string path, IntPtr fi, out long offset, out Errno errno)
		{
			OpenedPathInfo info = new OpenedPathInfo ();
			CopyOpenedPathInfo (fi, info);

			offset = -1;

			IEnumerable<DirectoryEntry> paths;
			errno = OnReadDirectory (path, info, out paths);
			if (errno != 0)
				return;
			if (paths == null) {
				Trace.WriteLine ("OnReadDirectory: errno = 0 but paths is null!");
				errno = Errno.EIO;
				return;
			}
			IEnumerator<DirectoryEntry> e = paths.GetEnumerator ();
			if (e == null) {
				Trace.WriteLine ("OnReadDirectory: errno = 0 but enumerator is null!");
				errno = Errno.EIO;
				return;
			}
			int key;
			lock (directoryLock) {
				do {
					key = directoryKeys.Next (1, int.MaxValue);
				} while (directoryReaders.ContainsKey (key.ToString()));
				directoryReaders [key.ToString()] = new EntryEnumerator (e);
			}

			CopyOpenedPathInfo (info, fi);

			offset = key;
			errno  = 0;
		}

		class EntryEnumerator : IEnumerator<DirectoryEntry> {
			private IEnumerator<DirectoryEntry> entries;
			bool repeat;

			public EntryEnumerator (IEnumerator<DirectoryEntry> entries)
			{
				this.entries = entries;
			}

			public DirectoryEntry Current {
				get {return entries.Current;}
			}

			object IEnumerator.Current {
				get {return Current;}
			}

			public bool Repeat {
				set {repeat = value;}
			}

			public bool MoveNext ()
			{
				if (repeat) {
					repeat = false;
					return true;
				}
				return entries.MoveNext ();
			}

			public void Reset ()
			{
				throw new InvalidOperationException ();
			}

			public void Dispose ()
			{
				entries.Dispose ();
			}
		}

		private bool FillEntries (IntPtr filler, IntPtr buf, IntPtr stbuf, 
				long offset, EntryEnumerator entries)
		{
			while (entries.MoveNext ()) {
				DirectoryEntry entry = entries.Current;
				IntPtr _stbuf = IntPtr.Zero;
				if (entry.Stat.st_ino != 0) {
					CopyStat (ref entry.Stat, stbuf);
					_stbuf = stbuf;
				}
				int r = mfh_invoke_filler (filler, buf, entry.Name, _stbuf, offset);
				if (r != 0) {
					entries.Repeat = true;
					return false;
				}
			}
			return true;
		}

		protected virtual Errno OnReadDirectory (string directory, OpenedPathInfo info, 
				out IEnumerable<DirectoryEntry> paths)
		{
			paths = null;
			return Errno.ENOSYS;
		}

		private int _OnReleaseDirectory (string path, IntPtr fi)
		{
			Errno errno;
			try {
				OpenedPathInfo info = new OpenedPathInfo ();
				CopyOpenedPathInfo (fi, info);
				errno = OnReleaseDirectory (path, info);
				if (errno == 0)
					CopyOpenedPathInfo (info, fi);
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		protected virtual Errno OnReleaseDirectory (string directory, OpenedPathInfo info)
		{
			return Errno.ENOSYS;
		}

		private int _OnSynchronizeDirectory (string path, bool onlyUserData, IntPtr fi)
		{
			Errno errno;
			try {
				OpenedPathInfo info = new OpenedPathInfo ();
				CopyOpenedPathInfo (fi, info);
				errno = OnSynchronizeDirectory (path, info, onlyUserData);
				if (errno == 0)
					CopyOpenedPathInfo (info, fi);
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		protected virtual Errno OnSynchronizeDirectory (string directory, OpenedPathInfo info, bool onlyUserData)
		{
			return Errno.ENOSYS;
		}

		private IntPtr OnInit ()
		{
			return opsp;
		}

		private int _OnAccessPath (string path, int mode)
		{
			Errno errno;
			try {
				AccessModes _mode = NativeConvert.ToAccessModes (mode);
				errno = OnAccessPath (path, _mode);
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		protected virtual Errno OnAccessPath (string path, AccessModes mode)
		{
			return Errno.ENOSYS;
		}

		private int _OnCreateHandle (string path, uint mode, IntPtr fi)
		{
			Errno errno;
			try {
				OpenedPathInfo info = new OpenedPathInfo ();
				CopyOpenedPathInfo (fi, info);
				FilePermissions _mode = NativeConvert.ToFilePermissions (mode);
				errno = OnCreateHandle (path, info, _mode);
				if (errno == 0)
					CopyOpenedPathInfo (info, fi);
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		protected virtual Errno OnCreateHandle (string file, OpenedPathInfo info, FilePermissions mode)
		{
			return Errno.ENOSYS;
		}

		private int _OnTruncateHandle (string path, long length, IntPtr fi)
		{
			Errno errno;
			try {
				OpenedPathInfo info = new OpenedPathInfo ();
				CopyOpenedPathInfo (fi, info);
				errno = OnTruncateHandle (path, info, length);
				if (errno == 0)
					CopyOpenedPathInfo (info, fi);
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		protected virtual Errno OnTruncateHandle (string file, OpenedPathInfo info, long length)
		{
			return Errno.ENOSYS;
		}

		private int _OnGetHandleStatus (string path, IntPtr buf, IntPtr fi)
		{
			Errno errno;
			try {
				OpenedPathInfo info = new OpenedPathInfo ();
				CopyOpenedPathInfo (fi, info);
				Stat b;
				CopyStat (buf, out b);
				errno = OnGetHandleStatus (path, info, out b);
				if (errno == 0) {
					CopyStat (ref b, buf);
					CopyOpenedPathInfo (info, fi);
				}
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		protected virtual Errno OnGetHandleStatus (string file, OpenedPathInfo info, out Stat buf)
		{
			buf = new Stat ();
			return Errno.ENOSYS;
		}
	}
}

