/*
 * mph.c: MonoPosixHelper transplanted functions.  
 *        Useful for when running on an older version of Mono.
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

#ifndef HAVE_MONO_UNIX_NATIVE_COPY_FUNCS

#include <config.h>

#include <sys/stat.h>
#include <sys/statvfs.h>
#include <sys/types.h>
#include <string.h>
#include <utime.h>

#include <map.h>

int
Mono_Fuse_FromStat (struct Mono_Unix_Native_Stat *from, void *_to)
{
	struct stat *to = _to;
	memset (to, 0, sizeof(*to));

	to->st_dev     = from->st_dev;
	to->st_ino     = from->st_ino;
	to->st_mode    = from->st_mode;
	to->st_nlink   = from->st_nlink;
	to->st_uid     = from->st_uid;
	to->st_gid     = from->st_gid;
	to->st_rdev    = from->st_rdev;
	to->st_size    = from->st_size;
	to->st_blksize = from->st_blksize;
	to->st_blocks  = from->st_blocks;
	to->st_atime   = from->st_atime_;
	to->st_mtime   = from->st_mtime_;
	to->st_ctime   = from->st_ctime_;

	return 0;
}

int
Mono_Fuse_ToStat (void *_from, struct Mono_Unix_Native_Stat *to)
{
	struct stat *from = _from;
	memset (to, 0, sizeof(*to));

	to->st_dev     = from->st_dev;
	to->st_ino     = from->st_ino;
	to->st_mode    = from->st_mode;
	to->st_nlink   = from->st_nlink;
	to->st_uid     = from->st_uid;
	to->st_gid     = from->st_gid;
	to->st_rdev    = from->st_rdev;
	to->st_size    = from->st_size;
	to->st_blksize = from->st_blksize;
	to->st_blocks  = from->st_blocks;
	to->st_atime_  = from->st_atime;
	to->st_mtime_  = from->st_mtime;
	to->st_ctime_  = from->st_ctime;

	return 0;
}

int
Mono_Fuse_FromUtimbuf (struct Mono_Unix_Native_Utimbuf *from, void *_to)
{
	struct utimbuf *to = _to;;
	memset (to, 0, sizeof(*to));

	to->actime  = from->actime;
	to->modtime = from->modtime;

	return 0;
}


int
Mono_Fuse_ToUtimbuf (void *_from, struct Mono_Unix_Native_Utimbuf *to)
{
	struct utimbuf *from = _from;
	memset (to, 0, sizeof(*to));

	to->actime  = from->actime;
	to->modtime = from->modtime;

	return 0;
}

int
Mono_Fuse_ToStatvfs (void *_from, struct Mono_Unix_Native_Statvfs *to)
{
	struct statvfs *from = _from;
	memset (to, 0, sizeof(*to));
	to->f_bsize   = from->f_bsize;
	to->f_frsize  = from->f_frsize;
	to->f_blocks  = from->f_blocks;
	to->f_bfree   = from->f_bfree;
	to->f_bavail  = from->f_bavail;
	to->f_files   = from->f_files;
	to->f_ffree   = from->f_ffree;
	to->f_favail  = from->f_favail;
	to->f_fsid    = from->f_fsid;
	to->f_namemax =	from->f_namemax;
	to->f_flag    = from->f_flag;

	return 0;
}

int
Mono_Fuse_FromStatvfs (struct Mono_Unix_Native_Statvfs *from, void *_to)
{
	struct statvfs *to = _to;
	memset (to, 0, sizeof(*to));
	to->f_bsize   = from->f_bsize;
	to->f_frsize  = from->f_frsize;
	to->f_blocks  = from->f_blocks;
	to->f_bfree   = from->f_bfree;
	to->f_bavail  = from->f_bavail;
	to->f_files   = from->f_files;
	to->f_ffree   = from->f_ffree;
	to->f_favail  = from->f_favail;
	to->f_fsid    = from->f_fsid;
	to->f_namemax =	from->f_namemax;
	to->f_flag    = from->f_flag;

	return 0;
}

#endif /* ndef HAVE_MONO_UNIX_NATIVE_COPY_FUNCS */

