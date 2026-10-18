namespace Comments.Infrastructure.Files;

// Where uploaded files are kept: the FileStorage:Provider setting
public enum FileStorageProvider
{
    // A folder on disk (FileStorage:RootPath), a Docker volume in docker-compose
    Disk,

    // The StoredFiles table, for hosts whose disk is wiped on every restart
    Database,
}
