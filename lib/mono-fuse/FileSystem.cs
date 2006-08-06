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
	delegate int ReadCb (string path, byte[] buf, ulong size, long offset, OpenedFileInfo info);
	delegate int WriteCb (string path, byte[] buf, ulong size, long offset, OpenedFileInfo info);
	delegate Errno GetFileSystemStatisticsCb (string path, ref Statvfs buf);
	delegate Errno FlushCb (string path, OpenedFileInfo info);
	delegate Errno ReleaseCb (string path, OpenedFileInfo info);
	delegate Errno SynchronizeFileDescriptorCb (string path, bool onlyUserData, OpenedFileInfo info);
	delegate Errno SetExtendedAttributesCb (string path, string name, byte[] value, ulong size, XattrFlags flags);
	delegate Errno GetExtendedAttributesCb (string path, string name, byte[] value, ulong size);
	delegate Errno ListExtendedAttributesCb (string path, byte[] list, ulong size);
	delegate Errno RemoveExtendedAttributesCb (string path, string name);
	delegate Errno OpenDirectoryCb (string path, OpenedFileInfo info);
	public delegate bool FillDirectoryCb (IntPtr buf, string name, IntPtr stbuf, long offset);
	delegate Errno ReadDirectoryCb (string path, [Out] out string[] paths, OpenedFileInfo info);
	delegate Errno CloseDirectoryCb (string path, OpenedFileInfo info);
	delegate Errno SynchronizeDirectoryCb (string path, bool onlyUserData, OpenedFileInfo info);
	delegate IntPtr InitCb ();
	delegate void DestroyCb (IntPtr privateData);
	delegate Errno AccessCb (string path, AccessModes mode);
	delegate Errno CreateCb (string path, FilePermissions mode, OpenedFileInfo info);
	delegate Errno TruncateFileDescriptorCb (string path, long length, OpenedFileInfo info);
	delegate Errno GetFileDescriptorAttributesCb (string path, ref Stat buf, OpenedFileInfo info);

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
		public DestroyCb                      destroy;
		public AccessCb                       access;
		public CreateCb                       create;
		public TruncateFileDescriptorCb       ftruncate;
		public GetFileDescriptorAttributesCb  fgetattr;
	}

	[Map ("struct fuse_args")]
	class Args {
		public int argc;
		public IntPtr argv;
		public int allocated;
	}

	public class FileSystem : IDisposable {

		const string LIB = "MonoFuseHelper";

		[DllImport (LIB, SetLastError=true)]
		private static extern IntPtr mfh_fuse_new (int fd, Args args, Operations ops);

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

		const string NameValueRegex = @"(?<Name>\w+)\s*(=\s*(?<Value>.*))$";
		const string OptRegex = @"^-o\s*(" + NameValueRegex + ")?";

		private void Parse (string[] args)
		{
			Regex o = new Regex (OptRegex);
			Regex nv = new Regex (NameValueRegex);

			for (int i = 0; i < args.Length; ++i) {
				Match m = o.Match (args [i]);
				if (m.Success) {
					if (m.Groups.Count == 0) {
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
			bool unmount = true;
			try {
				args.argc = _args.Length;
				args.argv = AllocArgv (_args);
				args.allocated = 1;
				fd = mfh_mount (mountPoint, args);
				if (fd == -1)
					throw new Exception ("Unable to mount " + mountPoint + ".");
				unmount = false;
				this.ops = GetOperations ();
				fusep = mfh_fuse_new (fd, args, ops);
				if (fusep == IntPtr.Zero) {
					this.ops = null;
					unmount = true;
					throw new Exception ("Unable to create FUSE object.");
				}
			}
			finally {
				FreeArgv (args.argc, args.argv);
				if (unmount)
					mfh_unmount (mountPoint);
			}
		}

		private string[] GetFuseArgs ()
		{
			string[] args = new string [opts.Keys.Count];
			int i = 0;
			foreach (string key in opts.Keys) {
				string v = opts [key];
				string a = "-o" + key;
				if (v.Length > 0) {
					a += "=" + v.ToString ();
				}
				args [i++] = a;
			}
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
					delegate (Operations to, FileSystem from) {to.getattr = from.OnGetFileAttributes;});
			operations.Add ("OnReadSymbolicLink", 
					delegate (Operations to, FileSystem from) {to.readlink = from.OnReadSymbolicLink;});
			operations.Add ("OnCreateFileNode", 
					delegate (Operations to, FileSystem from) {to.mknod = from.OnCreateFileNode;});
			operations.Add ("OnCreateDirectory", 
					delegate (Operations to, FileSystem from) {to.mkdir = from.OnCreateDirectory;});
			operations.Add ("OnRemoveFile", 
					delegate (Operations to, FileSystem from) {to.unlink = from.OnRemoveFile;});
			operations.Add ("OnRemoveDirectory", 
					delegate (Operations to, FileSystem from) {to.rmdir = from.OnRemoveDirectory;});
			operations.Add ("OnCreateSymbolicLink", 
					delegate (Operations to, FileSystem from) {to.symlink = from.OnCreateSymbolicLink;});
			operations.Add ("OnRenameFile", 
					delegate (Operations to, FileSystem from) {to.rename = from.OnRenameFile;});
			operations.Add ("OnCreateHardlink", 
					delegate (Operations to, FileSystem from) {to.link = from.OnCreateHardlink;});
			operations.Add ("OnChangePermissions", 
					delegate (Operations to, FileSystem from) {to.chmod = from.OnChangePermissions;});
			operations.Add ("OnChangeOwner", 
					delegate (Operations to, FileSystem from) {to.chown = from.OnChangeOwner;});
			operations.Add ("OnTruncateFile", 
					delegate (Operations to, FileSystem from) {to.truncate = from.OnTruncateFile;});
			operations.Add ("OnChangeTimes", 
					delegate (Operations to, FileSystem from) {to.utime = from.OnChangeTimes;});
			operations.Add ("OnOpen", 
					delegate (Operations to, FileSystem from) {to.open = from.OnOpen;});
			operations.Add ("OnRead", 
					delegate (Operations to, FileSystem from) {to.read = from.OnRead;});
			operations.Add ("OnWrite", 
					delegate (Operations to, FileSystem from) {to.write = from.OnWrite;});
			operations.Add ("OnGetFileSystemStatistics", 
					delegate (Operations to, FileSystem from) {to.statfs = from.OnGetFileSystemStatistics;});
			operations.Add ("OnFlush", 
					delegate (Operations to, FileSystem from) {to.flush = from.OnFlush;});
			operations.Add ("OnRelease", 
					delegate (Operations to, FileSystem from) {to.release = from.OnRelease;});
			operations.Add ("OnSynchronizeFileDescriptor", 
					delegate (Operations to, FileSystem from) {to.fsync = from.OnSynchronizeFileDescriptor;});
			operations.Add ("OnSetExtendedAttributes", 
					delegate (Operations to, FileSystem from) {to.setxattr = from.OnSetExtendedAttributes;});
			operations.Add ("OnGetExtendedAttributes", 
					delegate (Operations to, FileSystem from) {to.getxattr = from.OnGetExtendedAttributes;});
			operations.Add ("OnListExtendedAttributes", 
					delegate (Operations to, FileSystem from) {to.listxattr = from.OnListExtendedAttributes;});
			operations.Add ("OnRemoveExtendedAttributes", 
					delegate (Operations to, FileSystem from) {to.removexattr = from.OnRemoveExtendedAttributes;});
			operations.Add ("OnOpenDirectory", 
					delegate (Operations to, FileSystem from) {to.opendir = from.OnOpenDirectory;});
			operations.Add ("OnReadDirectory", 
					delegate (Operations to, FileSystem from) {to.readdir = from.OnReadDirectory;});
			operations.Add ("OnCloseDirectory", 
					delegate (Operations to, FileSystem from) {to.releasedir = from.OnCloseDirectory;});
			operations.Add ("OnSynchronizeDirectory", 
					delegate (Operations to, FileSystem from) {to.fsyncdir = from.OnSynchronizeDirectory;});
			operations.Add ("OnAccess", 
					delegate (Operations to, FileSystem from) {to.access = from.OnAccess;});
			operations.Add ("OnCreate", 
					delegate (Operations to, FileSystem from) {to.create = from.OnCreate;});
			operations.Add ("OnTruncateFileDescriptor", 
					delegate (Operations to, FileSystem from) {to.ftruncate = from.OnTruncateFileDescriptor;});
			operations.Add ("OnGetFileDescriptorAttributes", 
					delegate (Operations to, FileSystem from) {to.fgetattr = from.OnGetFileDescriptorAttributes;});
		}

		private Operations GetOperations ()
		{
			Operations ops = new Operations ();
			ops.init = OnInit;
			ops.destroy = OnDestroy;
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

		protected virtual Errno OnGetFileAttributes (string path, ref Stat stat)
		{
			return Errno.ENOSYS;
		}

		protected virtual Errno OnReadSymbolicLink (string path, StringBuilder buf, ulong bufsize)
		{
			return Errno.ENOSYS;
		}

		protected virtual Errno OnCreateFileNode (string path, FilePermissions perms, ulong dev)
		{
			return Errno.ENOSYS;
		}

		protected virtual Errno OnCreateDirectory (string path, FilePermissions mode)
		{
			return Errno.ENOSYS;
		}

		protected virtual Errno OnRemoveFile (string path)
		{
			return Errno.ENOSYS;
		}

		protected virtual Errno OnRemoveDirectory (string path)
		{
			return Errno.ENOSYS;
		}

		protected virtual Errno OnCreateSymbolicLink (string oldpath, string newpath)
		{
			return Errno.ENOSYS;
		}

		protected virtual Errno OnRenameFile (string oldpath, string newpath)
		{
			return Errno.ENOSYS;
		}

		protected virtual Errno OnCreateHardlink (string oldpath, string newpath)
		{
			return Errno.ENOSYS;
		}

		protected virtual Errno OnChangePermissions (string path, FilePermissions mode)
		{
			return Errno.ENOSYS;
		}

		protected virtual Errno OnChangeOwner (string path, long owner, long group)
		{
			return Errno.ENOSYS;
		}

		protected virtual Errno OnTruncateFile (string path, long length)
		{
			return Errno.ENOSYS;
		}

		protected virtual Errno OnChangeTimes (string path, ref Utimbuf buf)
		{
			return Errno.ENOSYS;
		}

		protected virtual Errno OnOpen (string path, OpenedFileInfo info)
		{
			return Errno.ENOSYS;
		}
 
		protected virtual int OnRead (string path, byte[] buf, ulong size, long offset, OpenedFileInfo info)
		{
			return 0;
		}

		protected virtual int OnWrite (string path, byte[] buf, ulong size, long offset, OpenedFileInfo info)
		{
			return 0;
		}

		protected virtual Errno OnGetFileSystemStatistics (string path, ref Statvfs buf)
		{
			return Errno.ENOSYS;
		}

		protected virtual Errno OnFlush (string path, OpenedFileInfo info)
		{
			return Errno.ENOSYS;
		}

		protected virtual Errno OnRelease (string path, OpenedFileInfo info)
		{
			return Errno.ENOSYS;
		}

		protected virtual Errno OnSynchronizeFileDescriptor (string path, bool onlyUserData, OpenedFileInfo info)
		{
			return Errno.ENOSYS;
		}

		protected virtual Errno OnSetExtendedAttributes (string path, string name, byte[] value, ulong size, XattrFlags flags)
		{
			return Errno.ENOSYS;
		}

		protected virtual Errno OnGetExtendedAttributes (string path, string name, byte[] value, ulong size)
		{
			return Errno.ENOSYS;
		}

		protected virtual Errno OnListExtendedAttributes (string path, byte[] list, ulong size)
		{
			return Errno.ENOSYS;
		}

		protected virtual Errno OnRemoveExtendedAttributes (string path, string name)
		{
			return Errno.ENOSYS;
		}

		protected virtual Errno OnOpenDirectory (string path, OpenedFileInfo info)
		{
			return Errno.ENOSYS;
		}

		protected virtual Errno OnReadDirectory (string path, out string[] paths, OpenedFileInfo info)
		{
			paths = null;
			return Errno.ENOSYS;
		}

		protected virtual Errno OnCloseDirectory (string path, OpenedFileInfo info)
		{
			return Errno.ENOSYS;
		}

		protected virtual Errno OnSynchronizeDirectory (string path, bool onlyUserData, OpenedFileInfo info)
		{
			return Errno.ENOSYS;
		}

		private IntPtr OnInit ()
		{
			IntPtr p = UnixMarshal.AllocHeap (Marshal.SizeOf (ops));
			Marshal.StructureToPtr (ops, p, false);
			return p;
		}

		private void OnDestroy (IntPtr privateData)
		{
			Marshal.DestroyStructure (privateData, typeof(Operations));
			UnixMarshal.FreeHeap (privateData);
		}

		protected virtual Errno OnAccess (string path, AccessModes mode)
		{
			return Errno.ENOSYS;
		}

		protected virtual Errno OnCreate (string path, FilePermissions mode, OpenedFileInfo info)
		{
			return Errno.ENOSYS;
		}

		protected virtual Errno OnTruncateFileDescriptor (string path, long length, OpenedFileInfo info)
		{
			return Errno.ENOSYS;
		}

		protected virtual Errno OnGetFileDescriptorAttributes (string path, ref Stat buf, OpenedFileInfo info)
		{
			return Errno.ENOSYS;
		}
	}
}

