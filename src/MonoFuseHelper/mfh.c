/*
 * mfh.c: MonoFuseHelper implementation.
 *
 * Authors:
 *   Jonathan Pryor  (jonpryor@vt.edu)
 *
 * Copyright (C) 2006 Jonathan Pryor
 */

/*
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

#define FUSE_USE_VERSION 26

#include <map.h>
#include <fuse.h>
#include <errno.h>
#include <string.h>
#include <stdlib.h>
#include <stdio.h>

static inline struct Mono_Fuse_Operations*
_mfh_get_private_data ()
{
	return (struct Mono_Fuse_Operations*) fuse_get_context()->private_data;
}

static int
mfh_getattr (const char *path, struct stat *stat)
{
	return _mfh_get_private_data ()->getattr (path, stat);
}

static int
mfh_readlink (const char *path, char* buf, size_t size)
{
	return _mfh_get_private_data ()->readlink (path, buf, size);
}

static int
mfh_mknod (const char *path, mode_t mode, dev_t dev)
{
	return _mfh_get_private_data ()->mknod (path, mode, dev);
}

static int
mfh_mkdir (const char *path, mode_t mode)
{
	return _mfh_get_private_data ()->mkdir (path, mode);
}

static int
mfh_unlink (const char *path)
{
	return _mfh_get_private_data ()->unlink (path);
}

static int
mfh_rmdir (const char *path)
{
	return _mfh_get_private_data ()->rmdir (path);
}

static int
mfh_symlink (const char *oldpath, const char *newpath)
{
	return _mfh_get_private_data ()->symlink (oldpath, newpath);
}

static int
mfh_rename (const char *oldpath, const char *newpath)
{
	return _mfh_get_private_data ()->rename (oldpath, newpath);
}

static int
mfh_link (const char *oldpath, const char *newpath)
{
	return _mfh_get_private_data ()->link (oldpath, newpath);
}

static int
mfh_chmod (const char *path, mode_t mode)
{
	return _mfh_get_private_data ()->chmod (path, mode);
}

static int
mfh_chown (const char *path, uid_t uid, gid_t gid)
{
	return _mfh_get_private_data ()->chown (path, uid, gid);
}

static int
mfh_truncate (const char *path, off_t len)
{
	return _mfh_get_private_data ()->truncate (path, len);
}

static int
mfh_utime (const char *path, struct utimbuf *buf)
{
	return _mfh_get_private_data ()->utime (path, buf);
}

int
Mono_Fuse_ToOpenedPathInfo (void *_from, struct Mono_Fuse_OpenedPathInfo *to)
{
	struct fuse_file_info *from = _from;
	memset (to, 0, sizeof (*to));

	to->flags        = from->flags;
	to->write_page   = from->writepage;
	to->direct_io    = from->direct_io;
	to->keep_cache   = from->keep_cache;
	to->file_handle  = from->fh;

	return 0;
}

int
Mono_Fuse_FromOpenedPathInfo (struct Mono_Fuse_OpenedPathInfo *from, void *_to)
{
	struct fuse_file_info *to = _to;
	memset (to, 0, sizeof (*to));

	to->flags       = from->flags;
	to->writepage   = from->write_page;
	to->direct_io   = from->direct_io ? 1 : 0;
	to->keep_cache  = from->keep_cache ? 1 : 0;
	to->fh          = from->file_handle;

	return 0;
}

static int
mfh_open (const char *path, struct fuse_file_info *info)
{
	int r;

	r = _mfh_get_private_data ()->open (path, info);

	return r;
}

static int
mfh_read (const char *path, char *buf, size_t size, off_t offset, 
		struct fuse_file_info *info)
{
	int r, bytesRead = 0;

	r = _mfh_get_private_data ()->read (path, (unsigned char*) buf, size, offset, 
			info, &bytesRead);

	if (r == 0 && bytesRead >= 0)
		return bytesRead;
	return r;
}

static int
mfh_write (const char *path, const char *buf, size_t size, off_t offset,
		struct fuse_file_info *info)
{
	int r, bytesWritten = 0;

	r = _mfh_get_private_data ()->write (path, (unsigned char*) buf, size, offset,
			info, &bytesWritten);

	if (r == 0 && bytesWritten >= 0)
		return bytesWritten;
	return r;
}

static int
mfh_statfs (const char *path, struct statvfs *buf)
{
	return _mfh_get_private_data ()->statfs (path, buf);
}

static int
mfh_flush (const char *path, struct fuse_file_info *info)
{
	int r;

	r = _mfh_get_private_data ()->flush (path, info);

	return r;
}

static int
mfh_release (const char *path, struct fuse_file_info *info)
{
	int r;

	r = _mfh_get_private_data ()->release (path, info);

	return r;
}

static int
mfh_fsync (const char *path, int onlyUserData, struct fuse_file_info *info)
{
	int r;

	r = _mfh_get_private_data ()->fsync (path, onlyUserData, info);

	return r;
}

static int
mfh_setxattr (const char *path, const char *name, const char *value, size_t size, int flags)
{
	return _mfh_get_private_data ()->setxattr (path, name, (unsigned char*) value, size, flags);
}

static int
mfh_getxattr (const char *path, const char *name, char *buf, size_t size)
{
	int r, bytesWritten = 0;
	r = _mfh_get_private_data ()->getxattr (path, name, (unsigned char *) buf, 
			size, &bytesWritten);
	if (r == 0 && bytesWritten >= 0)
		return bytesWritten;
	return r;
}

static int
mfh_listxattr (const char *path, char *buf, size_t size)
{
	int r, bytesWritten = 0;
	r = _mfh_get_private_data ()->listxattr (path, (unsigned char *) buf, size,
			&bytesWritten);
	if (r == 0 && bytesWritten >= 0)
		return bytesWritten;
	return r;
}

static int
mfh_removexattr (const char *path, const char *name)
{
	return _mfh_get_private_data ()->removexattr (path, name);
}

static int
mfh_opendir (const char *path, struct fuse_file_info *info)
{
	int r;

	r = _mfh_get_private_data ()->opendir (path, info);

	return r;
}

static int
mfh_readdir (const char *path, void* buf, fuse_fill_dir_t filler,
		off_t offset, struct fuse_file_info *info)
{
	int r;
	struct stat stbuf;

	r = _mfh_get_private_data ()->readdir (path, buf, filler, offset, info, &stbuf);

	return r;
}

int
mfh_invoke_filler (void *_filler, void *buf, const char *path, void *_stbuf, gint64 offset)
{
	struct stat     *stbuf = _stbuf;
	fuse_fill_dir_t filler = _filler;

	return filler (buf, path, stbuf, (off_t) offset);
}

static int
mfh_releasedir (const char *path, struct fuse_file_info *info)
{
	int r;

	r = _mfh_get_private_data ()->releasedir (path, info);

	return r;
}

static int
mfh_fsyncdir (const char *path, int onlyUserData, struct fuse_file_info *info)
{
	int r;

	r = _mfh_get_private_data ()->fsyncdir (path, onlyUserData, info);

	return r;
}

void
mfh_destroy (void* user_data)
{
	_mfh_get_private_data ()->destroy (user_data);
}

static int
mfh_access (const char *path, int flags)
{
	return _mfh_get_private_data ()->access (path, flags);
}

static int
mfh_create (const char *path, mode_t mode, struct fuse_file_info *info)
{
	int r;

	r = _mfh_get_private_data ()->create (path, mode, info);

	return r;
}

static int
mfh_ftruncate (const char *path, off_t len, struct fuse_file_info *info)
{
	int r;

	r = _mfh_get_private_data ()->ftruncate (path, len, info);

	return r;
}

static int
mfh_fgetattr (const char *path, struct stat *stat, struct fuse_file_info *info)
{
	int r;

	r = _mfh_get_private_data ()->fgetattr (path, stat, info);

	return r;
}

static int
mfh_lock (const char *path, struct fuse_file_info *info, int cmd, struct flock *lock)
{
	int r;

	r = _mfh_get_private_data ()->lock (path, info, cmd, lock);

	return r;
}

static int
mfh_bmap (const char *path, size_t blocksize, uint64_t *idx)
{
	int r;

	r = _mfh_get_private_data ()->bmap (path, blocksize, idx);

	return r;
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
	if (from->init)         to->init        = (void* (*)(struct fuse_conn_info*)) from->init;
	if (from->destroy)      to->destroy     = mfh_destroy;
	if (from->access)       to->access      = mfh_access;
	if (from->create)       to->create      = mfh_create;
	if (from->ftruncate)    to->ftruncate   = mfh_ftruncate;
	if (from->fgetattr)     to->fgetattr    = mfh_fgetattr;
	if (from->lock)         to->lock        = mfh_lock;
	if (from->bmap)         to->bmap        = mfh_bmap;
}

void
mfh_show_fuse_help (const char *appname)
{
	char *help[3];
	char *mountpoint;
	int mt, foreground;
	struct fuse_args args;
	struct fuse_operations ops = {};

	help [0] = (char*) appname;
	help [1] = "-ho";
	help [2] = NULL;

	memset (&args, 0, sizeof(args));

	args.argc = 2;
	args.argv = help;
	args.allocated = 0;

	fuse_parse_cmdline (&args, &mountpoint, &mt, &foreground);
	fuse_mount ("mountpoint", &args);
	fuse_new (NULL, &args, &ops, sizeof(ops), NULL);

	fuse_opt_free_args (&args);
}

int
mfh_fuse_get_context (struct Mono_Fuse_FileSystemOperationContext* context)
{
	struct fuse_context *from = fuse_get_context ();
	if (from == NULL) {
		errno = ENOTSUP;
		return -1;
	}

	context->fuse         = from->fuse;
	context->userId       = from->uid;
	context->groupId      = from->gid;
	context->processId    = from->pid;

	return 0;
}

int
mfh_fuse_main (int argc, void *argv, void* ops)
{
	struct Mono_Fuse_Operations *mops;
	struct fuse_operations fops;
	int r;

	mops = (struct Mono_Fuse_Operations*) ops;

	_to_fuse_operations (mops, &fops);

	r = fuse_main (argc, argv, &fops, NULL);

	return r;
}

void
mfh_fuse_exit (void *fusep)
{
	fuse_exit ((struct fuse*) fusep);
}

int
mfh_fuse_loop (void *fusep)
{
	return fuse_loop ((struct fuse*) fusep);
}

int
mfh_fuse_loop_mt (void *fusep)
{
	return fuse_loop_mt ((struct fuse*) fusep);
}
