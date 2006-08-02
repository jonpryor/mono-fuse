/* MonoFuseHelper */
#include "map.h"
#include <fuse.h>
#include <errno.h>

static int
mfh_getattr (const char *path, struct stat *stat)
{
}

static int
mfh_readlink (const char *path, char* buf, size_t size)
{
}

static int
mfh_mknod (const char *path, mode_t mode, dev_t dev)
{
}

static int
mfh_mkdir (const char *path, mode_t mode)
{
}

static int
mfh_unlink (const char *path)
{
}

static int
mfh_rmdir (const char *path)
{
}

static int
mfh_symlink (const char *oldpath, const char *newpath)
{
}

static int
mfh_rename (const char *oldpath, const char *newpath)
{
}

static int
mfh_link (const char *oldpath, const char *newpath)
{
}

static int
mfh_chmod (const char *path, mode_t mode)
{
}

static int
mfh_chown (const char *path, uid_t uid, gid_t gid)
{
}

static int
mfh_truncate (const char *path, off_t len)
{
}

static int
mfh_utime (const char *path, struct utimbuf *buf)
{
}

static int
mfh_open (const char *path, struct fuse_file_info *info)
{
}

static int
mfh_read (const char *path, char *buf, size_t size, off_t offset, 
		struct fuse_file_info *info)
{
}

static int
mfh_write (const char *path, const char *buf, size_t size, off_t offset,
		struct fuse_file_info *info)
{
}

static int
mfh_statfs (const char *path, struct statvfs *buf)
{
}

static int
mfh_flush (const char *path, struct fuse_file_info *info)
{
}

static int
mfh_release (const char *path, struct fuse_file_info *info)
{
}

static int
mfh_fsync (const char *path, int onlyUserData, struct fuse_file_info *info)
{
}

static int
mfh_setxattr (const char *path, const char *name, const char *value, size_t size, int flags)
{
}

static int
mfh_getxattr (const char *path, const char *name, char *buf, size_t size)
{
}

static int
mfh_listxattr (const char *path, char *buf, size_t size)
{
}

static int
mfh_removexattr (const char *path, const char *name)
{
}

static int
mfh_opendir (const char *path, struct fuse_file_info *info)
{
}

static int
mfh_readdir (const char *path, void* buf, fuse_fill_dir_t filler,
		off_t offset, struct fuse_file_info *info)
{
}

static int
mfh_releasedir (const char *path, struct fuse_file_info *info)
{
}

static int
mfh_fsyncdir (const char *path, int onlyUserData, struct fuse_file_info *info)
{
}

static void*
mfh_init (void)
{
}

static void
mfh_destroy (void* private_data)
{
}

static int
mfh_access (const char *path, int flags)
{
}

static int
mfh_create (const char *path, mode_t mode, struct fuse_file_info *info)
{
}

static int
mfh_ftruncate (const char *path, off_t len, struct fuse_file_info *info)
{
}

static int
mfh_fgetattr (const char *path, struct stat *stat, struct fuse_file_info *info)
{
}

void*
mfh_fuse_new (int fd, struct Mono_Fuse_Args* args, struct Mono_Fuse_Operations* ops)
{
	struct fuse_operations _ops;
	struct fuse_args _args;
	struct fuse *fuse;

	if (Mono_Fuse_FromOperations (ops, &_ops) != 0)
		return NULL;
	if (Mono_Fuse_FromArgs (args, &_args) != 0)
		return NULL;

	fuse = fuse_new (fd, &_args, &_ops, sizeof(_ops));
	return fuse;
}

void
mfh_destroy (void* fusep)
{
	fuse_destroy (fusep);
}

int
mfh_get_fuse_context (struct Mono_Fuse_FileSystemOperationContext* context)
{
	struct fuse_context *from = fuse_get_context ();
	if (from == null) {
		errno = ENOTSUP;
		return -1;
	}

	context->fuse         = from->fuse;
	context->UserId       = from->uid;
	context->GroupId      = from->gid;
	context->ProcessId    = from->pid;
	context->PrivateData  = from->private_data;

	return 0;
}

int
mfh_mount (const char* path, struct Mono_Fuse_Args* args)
{
	struct fuse_args _args;
	int r;
	Mono_Fuse_FromArgs (args, &_args);
	r = fuse_mount (path, _args);
	Mono_Fuse_ToArgs (&_args, args);
	return r;
}

int mfh_unmount (const char* path)
{
	fuse_unmount (path);
	return 0;
}

