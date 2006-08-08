/* functions that belong on libMonoPosixHelper.so but currently are not */

#define _XOPEN_SOURCE 600

#include <glib/gtypes.h>
#include <mono/posix/limits.h>
#include <mono/posix/helper.h>
#include <sys/types.h>
#include <unistd.h>
#include <string.h>

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


