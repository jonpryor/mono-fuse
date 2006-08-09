using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Mono.Unix;
using Mono.Unix.Native;

[assembly:Mono.Unix.Native.Header (Includes="fuse.h", Defines="FUSE_USE_VERSION=25")]

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
		public OpenFlags Flags;
		public int WritePage;
		public bool DirectIO;
		public bool KeepCache;
		public ulong FileHandle;
	}

	delegate Errno GetFileAttributesCb (string path, ref Stat stat);
	delegate Errno ReadSymbolicLinkCb (string path, StringBuilder buf, ulong bufsize);
	delegate Errno CreateFileNodeCb (string path, FilePermissions perms, ulong dev);
	delegate Errno CreateDirectoryCb (string path, FilePermissions mode);
	delegate Errno RemoveFileCb (string path);
	delegate Errno RemoveDirectoryCb (string path);
	delegate Errno CreateSymbolicLinkCb (string oldpath, string newpath);
	delegate Errno RenameFileCb (string oldpath, string newpath);
	delegate Errno CreateHardlinkCb (string oldpath, string newpath);
	delegate Errno ChangePermissionsCb (string path, FilePermissions mode);
	delegate Errno ChangeOwnerCb (string path, long owner, long group);
	delegate Errno TruncateCb (string path, long length);
	delegate Errno ChangeTimesCb (string path, ref Utimbuf buf);
	delegate Errno OpenCb (string path, OpenedFileInfo info); 
	delegate int ReadCb (string path, 
			[In, Out, MarshalAs (UnmanagedType.LPArray, ArraySubType=UnmanagedType.U1, SizeParamIndex=2)]
			byte[] buf, ulong size, long offset, OpenedFileInfo info);
	delegate int WriteCb (string path, 
			[In, Out, MarshalAs (UnmanagedType.LPArray, ArraySubType=UnmanagedType.U1, SizeParamIndex=2)]
			byte[] buf, ulong size, long offset, OpenedFileInfo info);
	delegate Errno GetFileSystemStatisticsCb (string path, ref Statvfs buf);
	delegate Errno FlushCb (string path, OpenedFileInfo info);
	delegate Errno ReleaseCb (string path, OpenedFileInfo info);
	delegate Errno SynchronizeFileDescriptorCb (string path, bool onlyUserData, OpenedFileInfo info);
	delegate Errno SetExtendedAttributesCb (string path, string name, 
			[In, Out, MarshalAs (UnmanagedType.LPArray, ArraySubType=UnmanagedType.U1, SizeParamIndex=3)]
			byte[] value, ulong size, XattrFlags flags);
	delegate Errno GetExtendedAttributesCb (string path, string name, 
			[In, Out, MarshalAs (UnmanagedType.LPArray, ArraySubType=UnmanagedType.U1, SizeParamIndex=3)]
			byte[] value, ulong size);
	delegate Errno ListExtendedAttributesCb (string path, 
			[In, Out, MarshalAs (UnmanagedType.LPArray, ArraySubType=UnmanagedType.U1, SizeParamIndex=2)]
			byte[] list, ulong size);
	delegate Errno RemoveExtendedAttributesCb (string path, string name);
	delegate Errno OpenDirectoryCb (string path, OpenedFileInfo info);
	public delegate bool FillDirectoryCb (IntPtr buf, string name, IntPtr stbuf, long offset);
	delegate Errno ReadDirectoryCb (string path, out IntPtr paths, OpenedFileInfo info);
	delegate Errno CloseDirectoryCb (string path, OpenedFileInfo info);
	delegate Errno SynchronizeDirectoryCb (string path, bool onlyUserData, OpenedFileInfo info);
	delegate IntPtr InitCb ();
	delegate Errno AccessCb (string path, AccessModes mode);
	delegate Errno CreateCb (string path, FilePermissions mode, OpenedFileInfo info);
	delegate Errno TruncateFileDescriptorCb (string path, long length, OpenedFileInfo info);
	delegate Errno GetFileDescriptorAttributesCb (string path, ref Stat buf, OpenedFileInfo info);

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
		private static extern int mfh_get_fuse_context (FileSystemOperationContext context);

		[DllImport (LIB, SetLastError=true)]
		private static extern int mfh_mount (string path, Args args);

		[DllImport (LIB, SetLastError=true)]
		private static extern int mfh_unmount (string path);

		[DllImport (LIB, SetLastError=true)]
		private static extern void mfh_destroy (IntPtr fusep);

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
			Parse (args);
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

		private void Parse (string[] args)
		{
			Regex o = new Regex (OptRegex);
			Regex nv = new Regex (NameValueRegex);

			for (int i = 0; i < args.Length; ++i) {
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
				else {
					mountPoint = args [i];
				}
			}
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
				fd = mfh_mount (mountPoint, args);
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
					mfh_unmount (mountPoint);
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
				mfh_unmount (MountPoint);
				mfh_destroy (fusep);
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
			int r = mfh_get_fuse_context (context);
			UnixMarshal.ThrowExceptionForLastErrorIf (r);
			return context;
		}

		private Errno _OnGetFileAttributes (string path, ref Stat stat)
		{
			try {
				return OnGetFileAttributes (path, ref stat);
			}
			catch {
				return Errno.EIO;
			}
		}

		protected virtual Errno OnGetFileAttributes (string path, ref Stat stat)
		{
			return Errno.ENOSYS;
		}

		private Errno _OnReadSymbolicLink (string path, StringBuilder buf, ulong bufsize)
		{
			try {
				return OnReadSymbolicLink (path, buf, bufsize);
			}
			catch {
				return Errno.EIO;
			}
		}

		protected virtual Errno OnReadSymbolicLink (string path, StringBuilder buf, ulong bufsize)
		{
			return Errno.ENOSYS;
		}

		private Errno _OnCreateFileNode (string path, FilePermissions perms, ulong dev)
		{
			try {
				return OnCreateFileNode (path, perms, dev);
			}
			catch {
				return Errno.EIO;
			}
		}

		protected virtual Errno OnCreateFileNode (string path, FilePermissions perms, ulong dev)
		{
			return Errno.ENOSYS;
		}

		private Errno _OnCreateDirectory (string path, FilePermissions mode)
		{
			try {
				return OnCreateDirectory (path, mode);
			}
			catch {
				return Errno.EIO;
			}
		}

		protected virtual Errno OnCreateDirectory (string path, FilePermissions mode)
		{
			return Errno.ENOSYS;
		}

		private Errno _OnRemoveFile (string path)
		{
			try {
				return OnRemoveFile (path);
			}
			catch {
				return Errno.EIO;
			}
		}

		protected virtual Errno OnRemoveFile (string path)
		{
			return Errno.ENOSYS;
		}

		private Errno _OnRemoveDirectory (string path)
		{
			try {
				return OnRemoveDirectory (path);
			}
			catch {
				return Errno.EIO;
			}
		}

		protected virtual Errno OnRemoveDirectory (string path)
		{
			return Errno.ENOSYS;
		}

		private Errno _OnCreateSymbolicLink (string oldpath, string newpath)
		{
			try {
				return OnCreateSymbolicLink (oldpath, newpath);
			}
			catch {
				return Errno.EIO;
			}
		}

		protected virtual Errno OnCreateSymbolicLink (string oldpath, string newpath)
		{
			return Errno.ENOSYS;
		}

		private Errno _OnRenameFile (string oldpath, string newpath)
		{
			try {
				return OnRenameFile (oldpath, newpath);
			}
			catch {
				return Errno.EIO;
			}
		}

		protected virtual Errno OnRenameFile (string oldpath, string newpath)
		{
			return Errno.ENOSYS;
		}

		private Errno _OnCreateHardLink (string oldpath, string newpath)
		{
			try {
				return OnCreateHardLink (oldpath, newpath);
			}
			catch {
				return Errno.EIO;
			}
		}

		protected virtual Errno OnCreateHardLink (string oldpath, string newpath)
		{
			return Errno.ENOSYS;
		}

		private Errno _OnChangePermissions (string path, FilePermissions mode)
		{
			try {
				return OnChangePermissions (path, mode);
			}
			catch {
				return Errno.EIO;
			}
		}

		protected virtual Errno OnChangePermissions (string path, FilePermissions mode)
		{
			return Errno.ENOSYS;
		}

		private Errno _OnChangeOwner (string path, long owner, long group)
		{
			try {
				return OnChangeOwner (path, owner, group);
			}
			catch {
				return Errno.EIO;
			}
		}

		protected virtual Errno OnChangeOwner (string path, long owner, long group)
		{
			return Errno.ENOSYS;
		}

		private Errno _OnTruncateFile (string path, long length)
		{
			try {
				return OnTruncateFile (path, length);
			}
			catch {
				return Errno.EIO;
			}
		}

		protected virtual Errno OnTruncateFile (string path, long length)
		{
			return Errno.ENOSYS;
		}

		private Errno _OnChangeTimes (string path, ref Utimbuf buf)
		{
			try {
				return OnChangeTimes (path, ref buf);
			}
			catch {
				return Errno.EIO;
			}
		}

		protected virtual Errno OnChangeTimes (string path, ref Utimbuf buf)
		{
			return Errno.ENOSYS;
		}

		private Errno _OnOpen (string path, OpenedFileInfo info)
		{
			try {
				return OnOpen (path, info);
			}
			catch {
				return Errno.EIO;
			}
		}

		protected virtual Errno OnOpen (string path, OpenedFileInfo info)
		{
			return Errno.ENOSYS;
		}
 
		private int _OnRead (string path, byte[] buf, ulong size, long offset, OpenedFileInfo info)
		{
			try {
				return OnRead (path, buf, offset, info);
			}
			catch {
				return - (int) Errno.EIO;
			}
		}

		protected virtual int OnRead (string path, byte[] buf, long offset, OpenedFileInfo info)
		{
			return 0;
		}

		private int _OnWrite (string path, byte[] buf, ulong size, long offset, OpenedFileInfo info)
		{
			try {
				return OnWrite (path, buf, offset, info);
			}
			catch {
				return - (int) Errno.EIO;
			}
		}

		protected virtual int OnWrite (string path, byte[] buf, long offset, OpenedFileInfo info)
		{
			return 0;
		}

		private Errno _OnGetFileSystemStatistics (string path, ref Statvfs buf)
		{
			try {
				return OnGetFileSystemStatistics (path, ref buf);
			}
			catch {
				return Errno.EIO;
			}
		}

		protected virtual Errno OnGetFileSystemStatistics (string path, ref Statvfs buf)
		{
			return Errno.ENOSYS;
		}

		private Errno _OnFlush (string path, OpenedFileInfo info)
		{
			try {
				return OnFlush (path, info);
			}
			catch {
				return Errno.EIO;
			}
		}

		protected virtual Errno OnFlush (string path, OpenedFileInfo info)
		{
			return Errno.ENOSYS;
		}

		private Errno _OnRelease (string path, OpenedFileInfo info)
		{
			try {
				return OnRelease (path, info);
			}
			catch {
				return Errno.EIO;
			}
		}

		protected virtual Errno OnRelease (string path, OpenedFileInfo info)
		{
			return Errno.ENOSYS;
		}

		private Errno _OnSynchronizeFileDescriptor (string path, bool onlyUserData, OpenedFileInfo info)
		{
			try {
				return OnSynchronizeFileDescriptor (path, onlyUserData, info);
			}
			catch {
				return Errno.EIO;
			}
		}

		protected virtual Errno OnSynchronizeFileDescriptor (string path, bool onlyUserData, OpenedFileInfo info)
		{
			return Errno.ENOSYS;
		}

		private Errno _OnSetExtendedAttributes (string path, string name, byte[] value, ulong size, XattrFlags flags)
		{
			try {
				return OnSetExtendedAttributes (path, name, value, flags);
			}
			catch {
				return Errno.EIO;
			}
		}

		protected virtual Errno OnSetExtendedAttributes (string path, string name, byte[] value, XattrFlags flags)
		{
			return Errno.ENOSYS;
		}

		private Errno _OnGetExtendedAttributes (string path, string name, byte[] value, ulong size)
		{
			try {
				return OnGetExtendedAttributes (path, name, value);
			}
			catch {
				return Errno.EIO;
			}
		}

		protected virtual Errno OnGetExtendedAttributes (string path, string name, byte[] value)
		{
			return Errno.ENOSYS;
		}

		private Errno _OnListExtendedAttributes (string path, byte[] list, ulong size)
		{
			try {
				return OnListExtendedAttributes (path, list);
			}
			catch {
				return Errno.EIO;
			}
		}

		protected virtual Errno OnListExtendedAttributes (string path, byte[] list)
		{
			return Errno.ENOSYS;
		}

		private Errno _OnRemoveExtendedAttributes (string path, string name)
		{
			try {
				return OnRemoveExtendedAttributes (path, name);
			}
			catch {
				return Errno.EIO;
			}
		}

		protected virtual Errno OnRemoveExtendedAttributes (string path, string name)
		{
			return Errno.ENOSYS;
		}

		private Errno _OnOpenDirectory (string path, OpenedFileInfo info)
		{
			try {
				return OnOpenDirectory (path, info);
			}
			catch {
				return Errno.EIO;
			}
		}

		protected virtual Errno OnOpenDirectory (string path, OpenedFileInfo info)
		{
			return Errno.ENOSYS;
		}

		private Errno _OnReadDirectory (string path, out IntPtr paths, OpenedFileInfo info)
		{
			paths = IntPtr.Zero;
			try {
				string[] _paths;
				Errno r = OnReadDirectory (path, out _paths, info);
				if (_paths != null) {
					paths = AllocArgv (_paths);
				}
				return r;
			}
			catch {
				return Errno.EIO;
			}
		}

		protected virtual Errno OnReadDirectory (string path, [Out] out string[] paths, OpenedFileInfo info)
		{
			paths = null;
			return Errno.ENOSYS;
		}

		private Errno _OnCloseDirectory (string path, OpenedFileInfo info)
		{
			try {
				return OnCloseDirectory (path, info);
			}
			catch {
				return Errno.EIO;
			}
		}

		protected virtual Errno OnCloseDirectory (string path, OpenedFileInfo info)
		{
			return Errno.ENOSYS;
		}

		private Errno _OnSynchronizeDirectory (string path, bool onlyUserData, OpenedFileInfo info)
		{
			try {
				return OnSynchronizeDirectory (path, onlyUserData, info);
			}
			catch {
				return Errno.EIO;
			}
		}

		protected virtual Errno OnSynchronizeDirectory (string path, bool onlyUserData, OpenedFileInfo info)
		{
			return Errno.ENOSYS;
		}

		private IntPtr OnInit ()
		{
			return opsp;
		}

		private Errno _OnAccess (string path, AccessModes mode)
		{
			try {
				return OnAccess (path, mode);
			}
			catch {
				return Errno.EIO;
			}
		}

		protected virtual Errno OnAccess (string path, AccessModes mode)
		{
			return Errno.ENOSYS;
		}

		private Errno _OnCreate (string path, FilePermissions mode, OpenedFileInfo info)
		{
			try {
				return OnCreate (path, mode, info);
			}
			catch {
				return Errno.EIO;
			}
		}

		protected virtual Errno OnCreate (string path, FilePermissions mode, OpenedFileInfo info)
		{
			return Errno.ENOSYS;
		}

		private Errno _OnTruncateFileDescriptor (string path, long length, OpenedFileInfo info)
		{
			try {
				return OnTruncateFileDescriptor (path, length, info);
			}
			catch {
				return Errno.EIO;
			}
		}

		protected virtual Errno OnTruncateFileDescriptor (string path, long length, OpenedFileInfo info)
		{
			return Errno.ENOSYS;
		}

		private Errno _OnGetFileDescriptorAttributes (string path, ref Stat buf, OpenedFileInfo info)
		{
			try {
				return OnGetFileDescriptorAttributes (path, ref buf, info);
			}
			catch {
				return Errno.EIO;
			}
		}

		protected virtual Errno OnGetFileDescriptorAttributes (string path, ref Stat buf, OpenedFileInfo info)
		{
			return Errno.ENOSYS;
		}
	}
}

