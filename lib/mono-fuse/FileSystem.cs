using System.Text;
using Mono.Unix.Native;

namespace Mono.Fuse {

class FileSystemOperationContext {
	private IntPtr fuse;
	public long UserId;
	public long GroupId;
	public long ProcessId;
	public IntPtr PrivateData;
}

class OpenedFileInfo {
	OpenFlags flags;
	int WritePage;
	bool DirectIO;
	bool KeepCache;
	ulong FileHandle;
}

class FileSystem {

	public FileSystem () {return fuse_new (fd, null, ops, op_size);}
	public FileSystem (string[] args) {return fuse_new (fd,args, ops, op_size);}
	public void Dispose (){fuse_destroy(this);}
	public void Loop () {fuse_loop(this);}
	public void Exit () {fuse_exit (this);}
	public void LoopMultithreaded () {fuse_loop_mt (this);}
	public static FileSystemOperationContext GetOperationContext () {return fuse_get_context();}

	virtual Errno OnGetFileNameAttributes (string path, ref Stat stat) {}
	virtual Errno OnReadSymbolicLink (string path, StringBuilder buf, ulong len) {}
	virtual Errno OnCreateFileNode (string path, FilePermissions perms, dev_t ?) {}
	virtual Errno OnCreateDirectory (string path, FilePermissions mode);
	virtual Errno OnRemoveFile (string path);
	virtual Errno OnRemoveDirectory (string path);
	virtual Errno OnCreateSymbolicLink (string foo, string bar);
	virtual Errno OnRenameFile (string, string);
	virtual Errno OnCreateHardlink (string, string);
	virtual Errno OnChangePermissions (string, FilePermissions);
	virtual Errno OnChangeOwner (string, long, long);
	virtual Errno OnTruncate (string, long);
	virtual Errno OnChangeTimes (string, ref Utimbuf);
	virtual Errno OnOpen (string, OpenedFileInfo);
	virtual Errno OnRead (string, byte[], ulong, long, OpenedFileInfo);
	virtual Errno OnWrite (string, byte[], ulong, long, OpenedFileInfo);
	virtual Errno OnGetFileSystemStatistics (string, ref Statvfs);
	virtual Errno OnFlush (string, OpenedFileInfo);
	virtual Errno OnRelease (string, OpenedFileInfo);
	virtual Errno OnSynchronizeFile (string, int, OpenedFileInfo);
	virtual Errno OnSetExtendedAttributes (string, string, byte[], ulong, int);
	virtual Errno OnGetExtendedAttributes (string, string, byte[], ulong);
	virtual Errno OnListExtendedAttributes (string, byte[], ulong);
	virtual Errno OnRemoveExtendedAttributes (string, byte[]);
	virtual Errno OnOpenDirectory (string, OpenedFileInfo);
	virtual Errno OnReadDirectory (string, void*, FuseFillDirT, long,
		OpenedFileInfo);
	virtual Errno OnReleaseDir (string, OpenedFileInfo);
	virtual Errno OnSynchronizeDirectory (string, int, OpenedFileInfo);
	virtual IntPtr OnInit ();
	virtual void OnDestroy (IntPtr);
	virtual Errno OnAccess (string, int);
	virtual Errno OnCreate (string, FilePermissions, OpenedFileInfo);
	virtual Errno OnTruncateFile (string, long, OpenedFileInfo);
	virtual Errno OnGetFileAttributes (string, ref Stat, OpenedFileInfo);
}

}
