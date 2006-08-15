/* functions that belong on libMonoPosixHelper.so but currently are not */

#define _XOPEN_SOURCE 600

#include <glib/gtypes.h>
#include <mono/posix/limits.h>
#include <mono/posix/helper.h>
#include <sys/types.h>
#include <sys/statvfs.h>
#include <unistd.h>
#include <string.h>
#include <utime.h>

int
Mono_Posix_FromStat (struct Mono_Posix_Stat *from, struct stat *to)
{
	mph_return_val_if_overflow (dev_t, from->st_dev, -1);
	mph_return_val_if_overflow (ino_t, from->st_ino, -1);
	mph_return_val_if_overflow (nlink_t, from->st_nlink, -1);
	mph_return_val_if_overflow (uid_t, from->st_uid, -1);
	mph_return_val_if_overflow (gid_t, from->st_gid, -1);
	mph_return_val_if_overflow (dev_t, from->st_rdev, -1);
	mph_return_val_if_overflow (off_t, from->st_size, -1);
	mph_return_val_if_overflow (blksize_t, from->st_blksize, -1);
	mph_return_val_if_overflow (blkcnt_t, from->st_blocks, -1);
	mph_return_val_if_overflow (time_t, from->st_atime_, -1);
	mph_return_val_if_overflow (time_t, from->st_mtime_, -1);
	mph_return_val_if_overflow (time_t, from->st_ctime_, -1);

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
Mono_Posix_ToStat (struct stat *from, struct Mono_Posix_Stat *to)
{
	mph_return_val_if_overflow (guint64, from->st_dev, -1);
	mph_return_val_if_overflow (guint64, from->st_ino, -1);
	mph_return_val_if_overflow (guint64, from->st_nlink, -1);
	mph_return_val_if_overflow (unsigned int, from->st_uid, -1);
	mph_return_val_if_overflow (unsigned int, from->st_gid, -1);
	mph_return_val_if_overflow (guint64, from->st_rdev, -1);
	mph_return_val_if_overflow (gint64, from->st_size, -1);
	mph_return_val_if_overflow (gint64, from->st_blksize, -1);
	mph_return_val_if_overflow (gint64, from->st_blocks, -1);
	mph_return_val_if_overflow (gint64, from->st_atime, -1);
	mph_return_val_if_overflow (gint64, from->st_mtime, -1);
	mph_return_val_if_overflow (gint64, from->st_ctime, -1);

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
Mono_Posix_FromStatvfs (struct Mono_Posix_Statvfs *from, struct statvfs *to)
{
	mph_return_val_if_overflow (unsigned long, from->f_bsize, -1);
	mph_return_val_if_overflow (unsigned long, from->f_frsize, -1);
	mph_return_val_if_overflow (fsblkcnt_t, from->f_blocks, -1);
	mph_return_val_if_overflow (fsblkcnt_t, from->f_bfree, -1);
	mph_return_val_if_overflow (fsblkcnt_t, from->f_bavail, -1);
	mph_return_val_if_overflow (fsfilcnt_t, from->f_files, -1);
	mph_return_val_if_overflow (fsfilcnt_t, from->f_ffree, -1);
	mph_return_val_if_overflow (fsfilcnt_t, from->f_favail, -1);
	mph_return_val_if_overflow (unsigned long, from->f_fsid, -1);
	mph_return_val_if_overflow (unsigned long, from->f_flag, -1);
	mph_return_val_if_overflow (unsigned long, from->f_namemax, -1);

	to->f_bsize   = from->f_bsize;
	to->f_frsize  = from->f_frsize;
	to->f_blocks  = from->f_blocks;
	to->f_bfree   = from->f_bfree;
	to->f_bavail  = from->f_bavail;
	to->f_files   = from->f_files;
	to->f_ffree   = from->f_ffree;
	to->f_favail  = from->f_favail;
	to->f_fsid    = from->f_fsid;
	to->f_flag    = from->f_flag;
	to->f_namemax = from->f_namemax;

	return 0;
}

int
Mono_Posix_ToStatvfs (struct statvfs *from, struct Mono_Posix_Statvfs *to)
{
	mph_return_val_if_overflow (guint64, from->f_bsize, -1);
	mph_return_val_if_overflow (guint64, from->f_frsize, -1);
	mph_return_val_if_overflow (guint64, from->f_blocks, -1);
	mph_return_val_if_overflow (guint64, from->f_bfree, -1);
	mph_return_val_if_overflow (guint64, from->f_bavail, -1);
	mph_return_val_if_overflow (guint64, from->f_files, -1);
	mph_return_val_if_overflow (guint64, from->f_ffree, -1);
	mph_return_val_if_overflow (guint64, from->f_favail, -1);
	mph_return_val_if_overflow (guint64, from->f_fsid, -1);
	mph_return_val_if_overflow (guint64, from->f_flag, -1);
	mph_return_val_if_overflow (guint64, from->f_namemax, -1);

	to->f_bsize   = from->f_bsize;
	to->f_frsize  = from->f_frsize;
	to->f_blocks  = from->f_blocks;
	to->f_bfree   = from->f_bfree;
	to->f_bavail  = from->f_bavail;
	to->f_files   = from->f_files;
	to->f_ffree   = from->f_ffree;
	to->f_favail  = from->f_favail;
	to->f_fsid    = from->f_fsid;
	to->f_flag    = from->f_flag;
	to->f_namemax = from->f_namemax;

	return 0;
}


int
Mono_Posix_FromUtimbuf (struct Mono_Posix_Utimbuf *from, struct utimbuf *to)
{
	mph_return_val_if_overflow (time_t, from->actime, -1);
	mph_return_val_if_overflow (time_t, from->modtime, -1);

	to->actime  = from->actime;
	to->modtime = from->modtime;

	return 0;
}

int
Mono_Posix_ToUtimbuf (struct utimbuf *from, struct Mono_Posix_Utimbuf *to)
{
	mph_return_val_if_overflow (gint64, from->actime, -1);
	mph_return_val_if_overflow (gint64, from->modtime, -1);

	to->actime  = from->actime;
	to->modtime = from->modtime;

	return 0;
}

#ifdef TEST
#include <stdio.h>
int
main ()
{
	struct stat buf;
	struct Mono_Posix_Stat mbuf;
	int r;

	buf.st_blksize = G_MAXINT64;

	r = Mono_Posix_ToStat (&buf, &mbuf);
	printf ("ToStat()=%i\n", r);
}
#endif
