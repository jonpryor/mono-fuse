/* MonoFuseHelper */

#define FUSE_USE_VERSION 25

#include "map.h"
#include <fuse.h>
#include <errno.h>
#include <string.h>

#include <mono/posix/helper.h>

#define _mfh_get_private_data() \
		((struct Mono_Fuse_Operations*)(fuse_get_context())->private_data)

#define _mfh_return_unless_perms(in, out) G_STMT_START {       \
	if (Mono_Posix_ToFilePermissions (in, out) != 0)  \
		return -EINVAL;                                 \
	} G_STMT_END

static inline int
_convert_errno (int r)
{
	if (Mono_Posix_FromErrno (r, &r) != 0)
		return -ENOSYS;
	return -r;
}

static int
mfh_getattr (const char *path, struct stat *stat)
{
	struct Mono_Posix_Stat _stat;
	int r;

	if (Mono_Posix_ToStat (stat, &_stat) != 0)
		return -EINVAL;

	r = _mfh_get_private_data ()->getattr (path, &_stat);

	if (Mono_Posix_FromStat (&_stat, stat) != 0)
		return -EINVAL;
	
	return _convert_errno (r);
}

static int
mfh_readlink (const char *path, char* buf, size_t size)
{
	int r;

	r = _mfh_get_private_data ()->readlink (path, buf, size);

	return _convert_errno (r);
}

static int
mfh_mknod (const char *path, mode_t mode, dev_t dev)
{
	int r;
	unsigned int _mode;

	_mfh_return_unless_perms (mode, &_mode);

	r = _mfh_get_private_data ()->mknod (path, _mode, dev);

	return _convert_errno (r);
}

static int
mfh_mkdir (const char *path, mode_t mode)
{
	int r;
	unsigned int _mode;

	_mfh_return_unless_perms (mode, &_mode);

	r = _mfh_get_private_data ()->mkdir (path, _mode);

	return _convert_errno (r);
}

static int
mfh_unlink (const char *path)
{
	int r = _mfh_get_private_data ()->unlink (path);
	return _convert_errno (r);
}

static int
mfh_rmdir (const char *path)
{
	int r = _mfh_get_private_data ()->rmdir (path);
	return _convert_errno (r);
}

static int
mfh_symlink (const char *oldpath, const char *newpath)
{
	int r = _mfh_get_private_data ()->symlink (oldpath, newpath);
	return _convert_errno (r);
}

static int
mfh_rename (const char *oldpath, const char *newpath)
{
	int r = _mfh_get_private_data ()->rename (oldpath, newpath);
	return _convert_errno (r);
}

static int
mfh_link (const char *oldpath, const char *newpath)
{
	int r = _mfh_get_private_data ()->link (oldpath, newpath);
	return _convert_errno (r);
}

static int
mfh_chmod (const char *path, mode_t mode)
{
	int r;
	unsigned int _mode;

	_mfh_return_unless_perms (mode, &_mode);

	r = _mfh_get_private_data ()->chmod (path, _mode);

	return _convert_errno (r);
}

static int
mfh_chown (const char *path, uid_t uid, gid_t gid)
{
	int r = _mfh_get_private_data ()->chown (path, uid, gid);
	return _convert_errno (r);
}

static int
mfh_truncate (const char *path, off_t len)
{
	int r = _mfh_get_private_data ()->truncate (path, len);
	return _convert_errno (r);
}

static int
mfh_utime (const char *path, struct utimbuf *buf)
{
	struct Mono_Posix_Utimbuf _buf;
	int r;

	if (Mono_Posix_ToUtimbuf (buf, &_buf) != 0)
		return -EINVAL;

	r = _mfh_get_private_data ()->utime (path, &_buf);

	if (Mono_Posix_FromUtimbuf (&_buf, buf) != 0)
		return -EINVAL;

	return _convert_errno (r);
}

static int
_to_file_info (struct fuse_file_info *from, struct Mono_Fuse_OpenedFileInfo *to)
{
	memset (to, 0, sizeof (*to));

	if (Mono_Posix_ToOpenFlags (from->flags, &to->flags) != 0) {
		return -EINVAL;
	}

	to->WritePage   = from->writepage;
	to->DirectIO    = from->direct_io;
	to->KeepCache   = from->keep_cache;
	to->FileHandle  = from->fh;

	return 0;
}

static int
_from_file_info (struct Mono_Fuse_OpenedFileInfo *from, struct fuse_file_info *to)
{
	memset (to, 0, sizeof (*to));

	if (Mono_Posix_FromOpenFlags (from->flags, &to->flags) != 0) {
		return -EINVAL;
	}

	to->writepage   = from->WritePage;
	to->direct_io   = from->DirectIO ? 1 : 0;
	to->keep_cache  = from->KeepCache ? 1 : 0;
	to->fh          = from->FileHandle;

	return 0;
}

static int
mfh_open (const char *path, struct fuse_file_info *info)
{
	struct Mono_Fuse_OpenedFileInfo _info;
	int r;

	if (_to_file_info (info, &_info) != 0)
		return -EINVAL;

	r = _mfh_get_private_data ()->open (path, &_info);

	if (_from_file_info (&_info, info) != 0)
		return -EINVAL;

	return _convert_errno (r);
}

static int
mfh_read (const char *path, char *buf, size_t size, off_t offset, 
		struct fuse_file_info *info)
{
	struct Mono_Fuse_OpenedFileInfo _info;
	int r;

	if (_to_file_info (info, &_info) != 0)
		return -EINVAL;

	r = _mfh_get_private_data ()->read (path, buf, size, offset, &_info);

	if (_from_file_info (&_info, info) != 0)
		return -EINVAL;

	return _convert_errno (r);
}

static int
mfh_write (const char *path, const char *buf, size_t size, off_t offset,
		struct fuse_file_info *info)
{
	struct Mono_Fuse_OpenedFileInfo _info;
	int r;

	if (_to_file_info (info, &_info) != 0)
		return -EINVAL;

	r = _mfh_get_private_data ()->write (path, (char*) buf, size, offset, &_info);

	if (_from_file_info (&_info, info) != 0)
		return -EINVAL;

	return _convert_errno (r);
}

static int
mfh_statfs (const char *path, struct statvfs *buf)
{
	struct Mono_Posix_Statvfs _buf;
	int r;

	if (Mono_Posix_ToStatvfs (buf, &_buf) != 0)
		return -EINVAL;

	r = _mfh_get_private_data ()->statfs (path, &_buf);

	if (Mono_Posix_FromStatvfs (&_buf, buf) != 0)
		return -EINVAL;

	return _convert_errno (r);
}

static int
mfh_flush (const char *path, struct fuse_file_info *info)
{
	struct Mono_Fuse_OpenedFileInfo _info;
	int r;

	if (_to_file_info (info, &_info) != 0)
		return -EINVAL;

	r = _mfh_get_private_data ()->flush (path, &_info);

	if (_from_file_info (&_info, info) != 0)
		return -EINVAL;

	return _convert_errno (r);
}

static int
mfh_release (const char *path, struct fuse_file_info *info)
{
	struct Mono_Fuse_OpenedFileInfo _info;
	int r;

	if (_to_file_info (info, &_info) != 0)
		return -EINVAL;

	r = _mfh_get_private_data ()->release (path, &_info);

	if (_from_file_info (&_info, info) != 0)
		return -EINVAL;

	return _convert_errno (r);
}

static int
mfh_fsync (const char *path, int onlyUserData, struct fuse_file_info *info)
{
	struct Mono_Fuse_OpenedFileInfo _info;
	int r;

	if (_to_file_info (info, &_info) != 0)
		return -EINVAL;

	r = _mfh_get_private_data ()->fsync (path, onlyUserData, &_info);

	if (_from_file_info (&_info, info) != 0)
		return -EINVAL;

	return _convert_errno (r);
}

static int
mfh_setxattr (const char *path, const char *name, const char *value, size_t size, int flags)
{
	int _flags;
	int r;

	if (Mono_Posix_ToXattrFlags (flags, &_flags) != 0)
		return -EINVAL;

	r = _mfh_get_private_data ()->setxattr (path, name, (char*) value, size, flags);

	return _convert_errno (r);
}

static int
mfh_getxattr (const char *path, const char *name, char *buf, size_t size)
{
	int r = _mfh_get_private_data ()->getxattr (path, name, buf, size);
	return _convert_errno (r);
}

static int
mfh_listxattr (const char *path, char *buf, size_t size)
{
	int r = _mfh_get_private_data ()->listxattr (path, buf, size);
	return _convert_errno (r);
}

static int
mfh_removexattr (const char *path, const char *name)
{
	int r = _mfh_get_private_data ()->removexattr (path, name);
	return _convert_errno (r);
}

static int
mfh_opendir (const char *path, struct fuse_file_info *info)
{
	struct Mono_Fuse_OpenedFileInfo _info;
	int r;

	if (_to_file_info (info, &_info) != 0)
		return -EINVAL;

	r = _mfh_get_private_data ()->opendir (path, &_info);

	if (_from_file_info (&_info, info) != 0)
		return -EINVAL;

	return _convert_errno (r);
}

static int
mfh_readdir (const char *path, void* buf, fuse_fill_dir_t filler,
		off_t offset, struct fuse_file_info *info)
{
}

static int
mfh_releasedir (const char *path, struct fuse_file_info *info)
{
	struct Mono_Fuse_OpenedFileInfo _info;
	int r;

	if (_to_file_info (info, &_info) != 0)
		return -EINVAL;

	r = _mfh_get_private_data ()->releasedir (path, &_info);

	if (_from_file_info (&_info, info) != 0)
		return -EINVAL;

	return _convert_errno (r);
}

static int
mfh_fsyncdir (const char *path, int onlyUserData, struct fuse_file_info *info)
{
	struct Mono_Fuse_OpenedFileInfo _info;
	int r;

	if (_to_file_info (info, &_info) != 0)
		return -EINVAL;

	r = _mfh_get_private_data ()->fsyncdir (path, onlyUserData, &_info);

	if (_from_file_info (&_info, info) != 0)
		return -EINVAL;

	return _convert_errno (r);
}

static int
mfh_access (const char *path, int flags)
{
	int _flags, r;
	if (Mono_Posix_ToAccessModes (flags, &_flags) != 0)
		return -EINVAL;
	r = _mfh_get_private_data ()->access (path, _flags);
	return _convert_errno (r);
}

static int
mfh_create (const char *path, mode_t mode, struct fuse_file_info *info)
{
	struct Mono_Fuse_OpenedFileInfo _info;
	unsigned int _mode;
	int r;

	if (Mono_Posix_ToFilePermissions (mode, &_mode) != 0)
		return -EINVAL;
	if (_to_file_info (info, &_info) != 0)
		return -EINVAL;

	r = _mfh_get_private_data ()->create (path, _mode,  &_info);

	if (_from_file_info (&_info, info) != 0)
		return -EINVAL;

	return _convert_errno (r);
}

static int
mfh_ftruncate (const char *path, off_t len, struct fuse_file_info *info)
{
	struct Mono_Fuse_OpenedFileInfo _info;
	int r;

	if (_to_file_info (info, &_info) != 0)
		return -EINVAL;

	r = _mfh_get_private_data ()->ftruncate (path, len, &_info);

	if (_from_file_info (&_info, info) != 0)
		return -EINVAL;

	return _convert_errno (r);
}

static int
mfh_fgetattr (const char *path, struct stat *stat, struct fuse_file_info *info)
{
	struct Mono_Fuse_OpenedFileInfo _info;
	struct Mono_Posix_Stat _stat;
	int r;

	if (Mono_Posix_ToStat (stat, &_stat) != 0)
		return -EINVAL;
	if (_to_file_info (info, &_info) != 0)
		return -EINVAL;

	r = _mfh_get_private_data ()->fgetattr (path, &_stat, &_info);

	if (Mono_Posix_FromStat (&_stat, stat) != 0)
		return -EINVAL;
	if (_from_file_info (&_info, info) != 0)
		return -EINVAL;

	return _convert_errno (r);
}

static void
_to_fuse_operations (struct Mono_Fuse_Operations *from, struct fuse_operations *to)
{
	memset (to, 0, sizeof(*to));

	if (from->getattr)      to->getattr     = mfh_getattr;
	if (from->readlink)     to->readlink    = mfh_readlink;
	if (from->mknod)        to->mknod       = mfh_mknod;
	if (from->mkdir)        to->mkdir       = mfh_mkdir;
	if (from->unlink)       to->unlink      = mfh_unlink;
	if (from->rmdir)        to->rmdir       = mfh_rmdir;
	if (from->symlink)      to->symlink     = mfh_symlink;
	if (from->rename)       to->rename      = mfh_rename;
	if (from->link)         to->link        = mfh_link;
	if (from->chmod)        to->chmod       = mfh_chmod;
	if (from->chown)        to->chown       = mfh_chown;
	if (from->truncate)     to->truncate    = mfh_truncate;
	if (from->utime)        to->utime       = mfh_utime;
	if (from->open)         to->open        = mfh_open;
	if (from->read)         to->read        = mfh_read;
	if (from->write)        to->write       = mfh_write;
	if (from->statfs)       to->statfs      = mfh_statfs;
	if (from->flush)        to->flush       = mfh_flush;
	if (from->release)      to->release     = mfh_release;
	if (from->fsync)        to->fsync       = mfh_fsync;
	if (from->setxattr)     to->setxattr    = mfh_setxattr;
	if (from->getxattr)     to->getxattr    = mfh_getxattr;
	if (from->listxattr)    to->listxattr   = mfh_listxattr;
	if (from->removexattr)  to->removexattr = mfh_removexattr;
	if (from->opendir)      to->opendir     = mfh_opendir;
	if (from->readdir)      to->readdir     = mfh_readdir;
	if (from->releasedir)   to->releasedir  = mfh_releasedir;
	if (from->fsyncdir)     to->fsyncdir    = mfh_fsyncdir;
	/* if (from->destroy)      to->destroy     = mfh_destroy; */
	if (from->access)       to->access      = mfh_access;
	if (from->create)       to->create      = mfh_create;
	if (from->ftruncate)    to->ftruncate   = mfh_ftruncate;
	if (from->fgetattr)     to->fgetattr    = mfh_fgetattr;
}

void*
mfh_fuse_new (int fd, struct Mono_Fuse_Args* args, struct Mono_Fuse_Operations* ops)
{
	struct fuse_operations _ops;
	struct fuse_args _args;
	struct fuse *fuse;

	_to_fuse_operations (ops, &_ops);

	if (Mono_Fuse_FromArgs (args, &_args) != 0)
		return NULL;

	fuse = fuse_new (fd, &_args, &_ops, sizeof(_ops));

	Mono_Fuse_ToArgs (&_args, args);
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
	if (from == NULL) {
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
	r = fuse_mount (path, &_args);
	Mono_Fuse_ToArgs (&_args, args);
	return r;
}

int mfh_unmount (const char* path)
{
	fuse_unmount (path);
	return 0;
}

