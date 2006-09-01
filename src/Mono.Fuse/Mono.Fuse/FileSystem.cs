using System;
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
	public class FileSystemOperationContext {
		private IntPtr fuse;
		[Map ("uid_t")] public long UserId;
		[Map ("gid_t")] public long GroupId;
		[Map ("pid_t")] public long ProcessId;
		public IntPtr PrivateData;
	}

	[Map]
	[StructLayout (LayoutKind.Sequential)]
	public class OpenedFileInfo {
		internal OpenFlags flags;
		private int   write_page;
		private bool  direct_io;
		private bool  keep_cache;
		private ulong file_handle;

		public OpenFlags OpenFlags {
			get {return flags;}
			set {flags = value;}
		}

		public bool OpenReadOnly {
			get {return ((OpenFlags) ((int) flags & 3)) == OpenFlags.O_RDONLY;}
		}

		public bool OpenWriteOnly {
			get {return ((OpenFlags) ((int) flags & 3)) == OpenFlags.O_WRONLY;}
		}

		public bool OpenReadWrite {
			get {return ((OpenFlags) ((int) flags & 3)) == OpenFlags.O_RDWR;}
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

		public long FileHandle {
			get {return (long) file_handle;}
			set {file_handle = (ulong) value;}
		}
	}

	delegate int GetFileAttributesCb (string path, IntPtr stat);
	delegate int ReadSymbolicLinkCb (string path, StringBuilder buf, ulong bufsize);
	delegate int CreateFileNodeCb (string path, uint perms, ulong dev);
	delegate int CreateDirectoryCb (string path, uint mode);
	delegate int RemoveFileCb (string path);
	delegate int RemoveDirectoryCb (string path);
	delegate int CreateSymbolicLinkCb (string oldpath, string newpath);
	delegate int RenameFileCb (string oldpath, string newpath);
	delegate int CreateHardlinkCb (string oldpath, string newpath);
	delegate int ChangePermissionsCb (string path, uint mode);
	delegate int ChangeOwnerCb (string path, long owner, long group);
	delegate int TruncateCb (string path, long length);
	delegate int ChangeTimesCb (string path, IntPtr buf);
	delegate int OpenCb (string path, OpenedFileInfo info); 
	delegate int ReadCb (string path, 
			[Out, MarshalAs (UnmanagedType.LPArray, ArraySubType=UnmanagedType.U1, SizeParamIndex=2)]
			byte[] buf, ulong size, long offset, OpenedFileInfo info, out int bytesRead);
	delegate int WriteCb (string path, 
			[In, MarshalAs (UnmanagedType.LPArray, ArraySubType=UnmanagedType.U1, SizeParamIndex=2)]
			byte[] buf, ulong size, long offset, OpenedFileInfo info, out int bytesRead);
	delegate int GetFileSystemStatisticsCb (string path, IntPtr buf);
	delegate int FlushCb (string path, OpenedFileInfo info);
	delegate int ReleaseCb (string path, OpenedFileInfo info);
	delegate int SynchronizeFileDescriptorCb (string path, bool onlyUserData, OpenedFileInfo info);
	delegate int SetExtendedAttributesCb (string path, string name, 
			[In, MarshalAs (UnmanagedType.LPArray, ArraySubType=UnmanagedType.U1, SizeParamIndex=3)]
			byte[] value, ulong size, int flags);
	delegate int GetExtendedAttributesCb (string path, string name, 
			[Out, MarshalAs (UnmanagedType.LPArray, ArraySubType=UnmanagedType.U1, SizeParamIndex=3)]
			byte[] value, ulong size, out int bytesWritten);
	delegate int ListExtendedAttributesCb (string path, 
			[Out, MarshalAs (UnmanagedType.LPArray, ArraySubType=UnmanagedType.U1, SizeParamIndex=2)]
			byte[] list, ulong size, out int bytesWritten);
	delegate int RemoveExtendedAttributesCb (string path, string name);
	delegate int OpenDirectoryCb (string path, OpenedFileInfo info);
	public delegate bool FillDirectoryCb (IntPtr buf, string name, IntPtr stbuf, long offset);
	delegate int ReadDirectoryCb (string path, out IntPtr paths, OpenedFileInfo info);
	delegate int CloseDirectoryCb (string path, OpenedFileInfo info);
	delegate int SynchronizeDirectoryCb (string path, bool onlyUserData, OpenedFileInfo info);
	delegate IntPtr InitCb ();
	delegate int AccessCb (string path, int mode);
	delegate int CreateCb (string path, uint mode, OpenedFileInfo info);
	delegate int TruncateFileDescriptorCb (string path, long length, OpenedFileInfo info);
	delegate int GetFileDescriptorAttributesCb (string path, IntPtr buf, OpenedFileInfo info);

	[Map]
	[StructLayout (LayoutKind.Sequential)]
	class Operations {
		public GetFileAttributesCb            getattr;
		public ReadSymbolicLinkCb             readlink;
		public CreateFileNodeCb               mknod;
		public CreateDirectoryCb              mkdir;
		public RemoveFileCb                   unlink;
		public RemoveDirectoryCb              rmdir;
		public CreateSymbolicLinkCb           symlink;
		public RenameFileCb                   rename;
		public CreateHardlinkCb               link;
		public ChangePermissionsCb            chmod;
		public ChangeOwnerCb                  chown;
		public TruncateCb                     truncate;
		public ChangeTimesCb                  utime;
		public OpenCb                         open;
		public ReadCb                         read;
		public WriteCb                        write;
		public GetFileSystemStatisticsCb      statfs;
		public FlushCb                        flush;
		public ReleaseCb                      release;
		public SynchronizeFileDescriptorCb    fsync;
		public SetExtendedAttributesCb        setxattr;
		public GetExtendedAttributesCb        getxattr;
		public ListExtendedAttributesCb       listxattr;
		public RemoveExtendedAttributesCb     removexattr;
		public OpenDirectoryCb                opendir;
		public ReadDirectoryCb                readdir;
		public CloseDirectoryCb               releasedir;
		public SynchronizeDirectoryCb         fsyncdir;
		public InitCb                         init;
		public AccessCb                       access;
		public CreateCb                       create;
		public TruncateFileDescriptorCb       ftruncate;
		public GetFileDescriptorAttributesCb  fgetattr;
	}

	[Map ("struct fuse_args")]
	[StructLayout (LayoutKind.Sequential)]
	class Args {
		public int argc;
		public IntPtr argv;
		public int allocated;
	}

	public class FileSystem : IDisposable {

		const string LIB = "MonoFuseHelper";

		[DllImport (LIB, SetLastError=true)]
		private static extern IntPtr mfh_fuse_new (int fd, Args args, IntPtr ops);

		[DllImport (LIB, SetLastError=true)]
		private static extern int mfh_fuse_get_context (FileSystemOperationContext context);

		[DllImport (LIB, SetLastError=true)]
		private static extern int mfh_fuse_mount (string path, Args args);

		[DllImport (LIB, SetLastError=true)]
		private static extern int mfh_fuse_unmount (string path);

		[DllImport (LIB, SetLastError=true)]
		private static extern void mfh_fuse_destroy (IntPtr fusep);

		[DllImport (LIB, SetLastError=true)]
		private static extern int mfh_fuse_loop (IntPtr fusep);

		[DllImport (LIB, SetLastError=true)]
		private static extern int mfh_fuse_loop_mt (IntPtr fusep);

		[DllImport (LIB, SetLastError=true)]
		private static extern void mfh_fuse_exit (IntPtr fusep);

		private string mountPoint;
		private int fd = -1;
		private IntPtr fusep = IntPtr.Zero;
		Dictionary <string, string> opts = new Dictionary <string, string> ();
		private Operations ops;
		private IntPtr opsp;

		public FileSystem (string mountPoint)
		{
			this.mountPoint = mountPoint;
		}

		public FileSystem ()
		{
		}

		public FileSystem (string[] args)
		{
			string[] unhandled = ParseFuseArguments (args);
			MountPoint = unhandled [unhandled.Length - 1];
		}

		public IDictionary <string, string> Options {
			get {return opts;}
		}

		public bool EnableDebugOutput {
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

		public string FileSystemName {
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

		public bool ImmediateRemoval {
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

		public bool DirectIO {
			get {return GetBool ("direct_io");}
			set {Set ("direct_io", value ? "" : null);}
		}

		public string Umask {
			get {return GetString ("umask");}
			set {Set ("umask", value);}
		}

		public long UserId {
			get {return GetLong ("uid");}
			set {Set ("uid", value.ToString ());}
		}

		public long GroupId {
			get {return GetLong ("gid");}
			set {Set ("gid", value.ToString ());}
		}

		public int EntryTimeout {
			get {return (int) GetLong ("entry_timeout");}
			set {Set ("entry_timeout", value.ToString ());}
		}

		public int DeletedNameTimeout {
			get {return (int) GetLong ("negative_timeout");}
			set {Set ("negative_timeout", value.ToString ());}
		}

		public int AttributeTimeout {
			get {return (int) GetLong ("attr_timeout");}
			set {Set ("attr_timeout", value.ToString ());}
		}

		private bool GetBool (string key)
		{
			return opts.ContainsKey (key);
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
				else if (args [i] == "--") {
					interpret = false;
				}
				else {
					unhandled.Add (args [i]);
				}
			}
			return unhandled.ToArray ();
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
				fd = mfh_fuse_mount (mountPoint, args);
				if (fd == -1)
					throw new Exception ("Unable to mount " + mountPoint + ".");
				unmount = true;
				Console.WriteLine ("# Getting Operations...");
				this.ops = GetOperations ();
				Console.WriteLine ("# Converting Operations...");
				this.opsp = UnixMarshal.AllocHeap (Marshal.SizeOf (ops));
				Marshal.StructureToPtr (ops, opsp, false);
				Console.WriteLine ("# Creating fuse object...");
				fusep = mfh_fuse_new (fd, args, opsp);
				if (fusep == IntPtr.Zero) {
					this.ops = null;
					throw new Exception ("Unable to create FUSE object.");
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

			operations.Add ("OnGetFileAttributes", 
					delegate (Operations to, FileSystem from) {to.getattr = from._OnGetFileAttributes;});
			operations.Add ("OnReadSymbolicLink", 
					delegate (Operations to, FileSystem from) {to.readlink = from._OnReadSymbolicLink;});
			operations.Add ("OnCreateFileNode", 
					delegate (Operations to, FileSystem from) {to.mknod = from._OnCreateFileNode;});
			operations.Add ("OnCreateDirectory", 
					delegate (Operations to, FileSystem from) {to.mkdir = from._OnCreateDirectory;});
			operations.Add ("OnRemoveFile", 
					delegate (Operations to, FileSystem from) {to.unlink = from._OnRemoveFile;});
			operations.Add ("OnRemoveDirectory", 
					delegate (Operations to, FileSystem from) {to.rmdir = from._OnRemoveDirectory;});
			operations.Add ("OnCreateSymbolicLink", 
					delegate (Operations to, FileSystem from) {to.symlink = from._OnCreateSymbolicLink;});
			operations.Add ("OnRenameFile", 
					delegate (Operations to, FileSystem from) {to.rename = from._OnRenameFile;});
			operations.Add ("OnCreateHardLink", 
					delegate (Operations to, FileSystem from) {to.link = from._OnCreateHardLink;});
			operations.Add ("OnChangePermissions", 
					delegate (Operations to, FileSystem from) {to.chmod = from._OnChangePermissions;});
			operations.Add ("OnChangeOwner", 
					delegate (Operations to, FileSystem from) {to.chown = from._OnChangeOwner;});
			operations.Add ("OnTruncateFile", 
					delegate (Operations to, FileSystem from) {to.truncate = from._OnTruncateFile;});
			operations.Add ("OnChangeTimes", 
					delegate (Operations to, FileSystem from) {to.utime = from._OnChangeTimes;});
			operations.Add ("OnOpen", 
					delegate (Operations to, FileSystem from) {to.open = from._OnOpen;});
			operations.Add ("OnRead", 
					delegate (Operations to, FileSystem from) {to.read = from._OnRead;});
			operations.Add ("OnWrite", 
					delegate (Operations to, FileSystem from) {to.write = from._OnWrite;});
			operations.Add ("OnGetFileSystemStatistics", 
					delegate (Operations to, FileSystem from) {to.statfs = from._OnGetFileSystemStatistics;});
			operations.Add ("OnFlush", 
					delegate (Operations to, FileSystem from) {to.flush = from._OnFlush;});
			operations.Add ("OnRelease", 
					delegate (Operations to, FileSystem from) {to.release = from._OnRelease;});
			operations.Add ("OnSynchronizeFileDescriptor", 
					delegate (Operations to, FileSystem from) {to.fsync = from._OnSynchronizeFileDescriptor;});
			operations.Add ("OnSetExtendedAttributes", 
					delegate (Operations to, FileSystem from) {to.setxattr = from._OnSetExtendedAttributes;});
			operations.Add ("OnGetExtendedAttributes", 
					delegate (Operations to, FileSystem from) {to.getxattr = from._OnGetExtendedAttributes;});
			operations.Add ("OnListExtendedAttributes", 
					delegate (Operations to, FileSystem from) {to.listxattr = from._OnListExtendedAttributes;});
			operations.Add ("OnRemoveExtendedAttributes", 
					delegate (Operations to, FileSystem from) {to.removexattr = from._OnRemoveExtendedAttributes;});
			operations.Add ("OnOpenDirectory", 
					delegate (Operations to, FileSystem from) {to.opendir = from._OnOpenDirectory;});
			operations.Add ("OnReadDirectory", 
					delegate (Operations to, FileSystem from) {to.readdir = from._OnReadDirectory;});
			operations.Add ("OnCloseDirectory", 
					delegate (Operations to, FileSystem from) {to.releasedir = from._OnCloseDirectory;});
			operations.Add ("OnSynchronizeDirectory", 
					delegate (Operations to, FileSystem from) {to.fsyncdir = from._OnSynchronizeDirectory;});
			operations.Add ("OnAccess", 
					delegate (Operations to, FileSystem from) {to.access = from._OnAccess;});
			operations.Add ("OnCreate", 
					delegate (Operations to, FileSystem from) {to.create = from._OnCreate;});
			operations.Add ("OnTruncateFileDescriptor", 
					delegate (Operations to, FileSystem from) {to.ftruncate = from._OnTruncateFileDescriptor;});
			operations.Add ("OnGetFileDescriptorAttributes", 
					delegate (Operations to, FileSystem from) {to.fgetattr = from._OnGetFileDescriptorAttributes;});
		}

		private Operations GetOperations ()
		{
			Operations ops = new Operations ();
			ops.init = OnInit;
			foreach (string method in operations.Keys) {
				MethodInfo m = this.GetType().GetMethod (method, 
						BindingFlags.NonPublic | BindingFlags.Instance);
				if (m.DeclaringType == typeof(FileSystem))
					continue;
				CopyOperation op = operations [method];
				op (ops, this);
			}
			return ops;
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
			mfh_fuse_loop (fusep);
		}

		public void Exit ()
		{
			mfh_fuse_exit (fusep);
		}
		public void StartMultithreaded ()
		{
			Create ();
			mfh_fuse_loop_mt (fusep);
		}

		protected static FileSystemOperationContext GetOperationContext ()
		{
			FileSystemOperationContext context = new FileSystemOperationContext ();
			int r = mfh_fuse_get_context (context);
			UnixMarshal.ThrowExceptionForLastErrorIf (r);
			return context;
		}

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
			NativeConvert.Copy (source, out dest);
#else
			Mono_Fuse_ToStat (source, out dest);
			dest.st_mode = NativeConvert.ToFilePermissions ((uint) dest.st_mode);
#endif
		}

		private static void CopyStat (ref Stat source, IntPtr dest)
		{
#if HAVE_MONO_UNIX_NATIVE_COPY_FUNCS
			NativeConvert.Copy (ref source, dest);
#else
			source.st_mode = (FilePermissions) 
				NativeConvert.FromFilePermissions (source.st_mode);
			Mono_Fuse_FromStat (ref source, dest);
#endif
		}

		private static void CopyStatvfs (IntPtr source, out Statvfs dest)
		{
#if HAVE_MONO_UNIX_NATIVE_COPY_FUNCS
			NativeConvert.Copy (source, out dest);
#else
			Mono_Fuse_ToStatvfs (source, out dest);
			dest.f_flag = NativeConvert.ToMountFlags ((ulong) dest.f_flag);
#endif
		}

		private static void CopyStatvfs (ref Statvfs source, IntPtr dest)
		{
#if HAVE_MONO_UNIX_NATIVE_COPY_FUNCS
			NativeConvert.Copy (ref source, dest);
#else
			source.f_flag = (MountFlags) NativeConvert.FromMountFlags (source.f_flag);
			Mono_Fuse_FromStatvfs (ref source, dest);
#endif
		}

		private static void CopyUtimbuf (IntPtr source, out Utimbuf dest)
		{
#if HAVE_MONO_UNIX_NATIVE_COPY_FUNCS
			NativeConvert.Copy (source, out dest);
#else
			Mono_Fuse_ToUtimbuf (source, out dest);
#endif
		}

		private static void CopyUtimbuf (ref Utimbuf source, IntPtr dest)
		{
#if HAVE_MONO_UNIX_NATIVE_COPY_FUNCS
			NativeConvert.Copy (ref source, dest);
#else
			Mono_Fuse_FromUtimbuf (ref source, dest);
#endif
		}

		private int _OnGetFileAttributes (string path, IntPtr stat)
		{
			Errno errno;
			try {
				Stat buf;
				CopyStat (stat, out buf);
				errno = OnGetFileAttributes (path, out buf);
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

		protected virtual Errno OnGetFileAttributes (string path, out Stat stat)
		{
			stat = new Stat ();
			return Errno.ENOSYS;
		}

		private int _OnReadSymbolicLink (string path, StringBuilder buf, ulong bufsize)
		{
			Errno errno;
			try {
				errno = OnReadSymbolicLink (path, buf, bufsize);
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		protected virtual Errno OnReadSymbolicLink (string path, StringBuilder buf, ulong bufsize)
		{
			return Errno.ENOSYS;
		}

		private int _OnCreateFileNode (string path, uint perms, ulong dev)
		{
			Errno errno;
			try {
				FilePermissions _perms = NativeConvert.ToFilePermissions (perms);
				errno = OnCreateFileNode (path, _perms, dev);
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		protected virtual Errno OnCreateFileNode (string path, FilePermissions perms, ulong dev)
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

		protected virtual Errno OnCreateDirectory (string path, FilePermissions mode)
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

		protected virtual Errno OnRemoveFile (string path)
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

		protected virtual Errno OnRemoveDirectory (string path)
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

		protected virtual Errno OnCreateSymbolicLink (string oldpath, string newpath)
		{
			return Errno.ENOSYS;
		}

		private int _OnRenameFile (string oldpath, string newpath)
		{
			Errno errno;
			try {
				errno = OnRenameFile (oldpath, newpath);
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		protected virtual Errno OnRenameFile (string oldpath, string newpath)
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

		protected virtual Errno OnCreateHardLink (string oldpath, string newpath)
		{
			return Errno.ENOSYS;
		}

		private int _OnChangePermissions (string path, uint mode)
		{
			Errno errno;
			try {
				FilePermissions _mode = NativeConvert.ToFilePermissions (mode);
				errno = OnChangePermissions (path, _mode);
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		protected virtual Errno OnChangePermissions (string path, FilePermissions mode)
		{
			return Errno.ENOSYS;
		}

		private int _OnChangeOwner (string path, long owner, long group)
		{
			Errno errno;
			try {
				errno = OnChangeOwner (path, owner, group);
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		protected virtual Errno OnChangeOwner (string path, long owner, long group)
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

		protected virtual Errno OnTruncateFile (string path, long length)
		{
			return Errno.ENOSYS;
		}

		private int _OnChangeTimes (string path, IntPtr buf)
		{
			Errno errno;
			try {
				Utimbuf b;
				CopyUtimbuf (buf, out b);
				errno = OnChangeTimes (path, ref b);
				if (errno == 0)
					CopyUtimbuf (ref b, buf);
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		protected virtual Errno OnChangeTimes (string path, ref Utimbuf buf)
		{
			return Errno.ENOSYS;
		}

		private int _OnOpen (string path, OpenedFileInfo info)
		{
			Errno errno;
			try {
				ConvertOpenFlags (info);
				errno = OnOpen (path, info);
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		private static void ConvertOpenFlags (OpenedFileInfo info)
		{
			info.flags = NativeConvert.ToOpenFlags ((int) info.flags);
		}

		protected virtual Errno OnOpen (string path, OpenedFileInfo info)
		{
			return Errno.ENOSYS;
		}
 
		private int _OnRead (string path, byte[] buf, ulong size, long offset, OpenedFileInfo info, out int bytesWritten)
		{
			Errno errno;
			try {
				ConvertOpenFlags (info);
				errno = OnRead (path, buf, offset, info, out bytesWritten);
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				bytesWritten = 0;
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		protected virtual Errno OnRead (string path, byte[] buf, long offset, OpenedFileInfo info, out int bytesWritten)
		{
			bytesWritten = 0;
			return Errno.ENOSYS;
		}

		private int _OnWrite (string path, byte[] buf, ulong size, long offset, OpenedFileInfo info, out int bytesRead)
		{
			Errno errno;
			try {
				ConvertOpenFlags (info);
				errno = OnWrite (path, buf, offset, info, out bytesRead);
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				bytesRead = 0;
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		protected virtual Errno OnWrite (string path, byte[] buf, long offset, OpenedFileInfo info, out int bytesRead)
		{
			bytesRead = 0;
			return Errno.ENOSYS;
		}

		private int _OnGetFileSystemStatistics (string path, IntPtr buf)
		{
			Errno errno;
			try {
				Statvfs b;
				CopyStatvfs (buf, out b);
				errno = OnGetFileSystemStatistics (path, out b);
				if (errno == 0)
					CopyStatvfs (ref b, buf);
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		protected virtual Errno OnGetFileSystemStatistics (string path, out Statvfs buf)
		{
			buf = new Statvfs ();
			return Errno.ENOSYS;
		}

		private int _OnFlush (string path, OpenedFileInfo info)
		{
			Errno errno;
			try {
				ConvertOpenFlags (info);
				errno = OnFlush (path, info);
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		protected virtual Errno OnFlush (string path, OpenedFileInfo info)
		{
			return Errno.ENOSYS;
		}

		private int _OnRelease (string path, OpenedFileInfo info)
		{
			Errno errno;
			try {
				ConvertOpenFlags (info);
				errno = OnRelease (path, info);
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		protected virtual Errno OnRelease (string path, OpenedFileInfo info)
		{
			return Errno.ENOSYS;
		}

		private int _OnSynchronizeFileDescriptor (string path, bool onlyUserData, OpenedFileInfo info)
		{
			Errno errno;
			try {
				ConvertOpenFlags (info);
				errno = OnSynchronizeFileDescriptor (path, onlyUserData, info);
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		protected virtual Errno OnSynchronizeFileDescriptor (string path, bool onlyUserData, OpenedFileInfo info)
		{
			return Errno.ENOSYS;
		}

		private int _OnSetExtendedAttributes (string path, string name, byte[] value, ulong size, int flags)
		{
			Errno errno;
			try {
				XattrFlags f = NativeConvert.ToXattrFlags (flags);
				errno = OnSetExtendedAttributes (path, name, value, f);
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		protected virtual Errno OnSetExtendedAttributes (string path, string name, byte[] value, XattrFlags flags)
		{
			return Errno.ENOSYS;
		}

		private int _OnGetExtendedAttributes (string path, string name, byte[] value, ulong size, out int bytesWritten)
		{
			Errno errno;
			try {
				errno = OnGetExtendedAttributes (path, name, value, out bytesWritten);
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				bytesWritten = 0;
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		protected virtual Errno OnGetExtendedAttributes (string path, string name, byte[] value, out int bytesWritten)
		{
			bytesWritten = 0;
			return Errno.ENOSYS;
		}

		private int _OnListExtendedAttributes (string path, byte[] list, ulong size,  out int bytesWritten)
		{
			Errno errno;
			try {
				errno = OnListExtendedAttributes (path, list, out bytesWritten);
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				bytesWritten = 0;
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		protected virtual Errno OnListExtendedAttributes (string path, byte[] list, out int bytesWritten)
		{
			bytesWritten = 0;
			return Errno.ENOSYS;
		}

		private int _OnRemoveExtendedAttributes (string path, string name)
		{
			Errno errno;
			try {
				errno = OnRemoveExtendedAttributes (path, name);
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		protected virtual Errno OnRemoveExtendedAttributes (string path, string name)
		{
			return Errno.ENOSYS;
		}

		private int _OnOpenDirectory (string path, OpenedFileInfo info)
		{
			Errno errno;
			try {
				ConvertOpenFlags (info);
				errno = OnOpenDirectory (path, info);
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		protected virtual Errno OnOpenDirectory (string path, OpenedFileInfo info)
		{
			return Errno.ENOSYS;
		}

		private int _OnReadDirectory (string path, out IntPtr paths, OpenedFileInfo info)
		{
			paths = IntPtr.Zero;
			Errno errno;
			try {
				ConvertOpenFlags (info);
				string[] _paths;
				errno = OnReadDirectory (path, out _paths, info);
				if (_paths != null) {
					paths = AllocArgv (_paths);
				}
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		protected virtual Errno OnReadDirectory (string path, [Out] out string[] paths, OpenedFileInfo info)
		{
			paths = null;
			return Errno.ENOSYS;
		}

		private int _OnCloseDirectory (string path, OpenedFileInfo info)
		{
			Errno errno;
			try {
				ConvertOpenFlags (info);
				errno = OnCloseDirectory (path, info);
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		protected virtual Errno OnCloseDirectory (string path, OpenedFileInfo info)
		{
			return Errno.ENOSYS;
		}

		private int _OnSynchronizeDirectory (string path, bool onlyUserData, OpenedFileInfo info)
		{
			Errno errno;
			try {
				ConvertOpenFlags (info);
				errno = OnSynchronizeDirectory (path, onlyUserData, info);
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		protected virtual Errno OnSynchronizeDirectory (string path, bool onlyUserData, OpenedFileInfo info)
		{
			return Errno.ENOSYS;
		}

		private IntPtr OnInit ()
		{
			return opsp;
		}

		private int _OnAccess (string path, int mode)
		{
			Errno errno;
			try {
				AccessModes _mode = NativeConvert.ToAccessModes (mode);
				errno = OnAccess (path, _mode);
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		protected virtual Errno OnAccess (string path, AccessModes mode)
		{
			return Errno.ENOSYS;
		}

		private int _OnCreate (string path, uint mode, OpenedFileInfo info)
		{
			Errno errno;
			try {
				ConvertOpenFlags (info);
				FilePermissions _mode = NativeConvert.ToFilePermissions (mode);
				errno = OnCreate (path, _mode, info);
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		protected virtual Errno OnCreate (string path, FilePermissions mode, OpenedFileInfo info)
		{
			return Errno.ENOSYS;
		}

		private int _OnTruncateFileDescriptor (string path, long length, OpenedFileInfo info)
		{
			Errno errno;
			try {
				ConvertOpenFlags (info);
				errno = OnTruncateFileDescriptor (path, length, info);
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		protected virtual Errno OnTruncateFileDescriptor (string path, long length, OpenedFileInfo info)
		{
			return Errno.ENOSYS;
		}

		private int _OnGetFileDescriptorAttributes (string path, IntPtr buf, OpenedFileInfo info)
		{
			Errno errno;
			try {
				ConvertOpenFlags (info);
				Stat b;
				CopyStat (buf, out b);
				errno = OnGetFileDescriptorAttributes (path, out b, info);
				if (errno == 0)
					CopyStat (ref b, buf);
			}
			catch (Exception e) {
				Trace.WriteLine (e.ToString());
				errno = Errno.EIO;
			}
			return ConvertErrno (errno);
		}

		protected virtual Errno OnGetFileDescriptorAttributes (string path, out Stat buf, OpenedFileInfo info)
		{
			buf = new Stat ();
			return Errno.ENOSYS;
		}
	}
}

