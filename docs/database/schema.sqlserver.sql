-- The real database schema (MS SQL Server), generated from the EF Core migrations:
--   dotnet ef migrations script --project src/Comments.Infrastructure --startup-project src/Comments.Api --output docs/database/schema.sqlserver.sql
-- Don't edit it by hand: change the entities, add a migration and generate the file again.
-- For MySQL Workbench open schema.mysql.sql instead.

IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [Users] (
    [Id] int NOT NULL IDENTITY,
    [UserName] nvarchar(50) NOT NULL,
    [Email] nvarchar(254) NOT NULL,
    [HomePage] nvarchar(2048) NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);

CREATE UNIQUE INDEX [IX_Users_UserName_Email] ON [Users] ([UserName], [Email]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261008124322_AddUsers', N'10.0.13');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [Comments] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [ParentId] int NULL,
    [Text] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [IpAddress] nvarchar(45) NULL,
    [UserAgent] nvarchar(512) NULL,
    CONSTRAINT [PK_Comments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Comments_Comments_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [Comments] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Comments_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE INDEX [IX_Comments_ParentId_CreatedAt] ON [Comments] ([ParentId], [CreatedAt]);

CREATE INDEX [IX_Comments_UserId] ON [Comments] ([UserId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261008172825_AddComments', N'10.0.13');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [Attachments] (
    [Id] int NOT NULL IDENTITY,
    [CommentId] int NOT NULL,
    [OriginalFileName] nvarchar(255) NOT NULL,
    [StoredFileName] nvarchar(100) NOT NULL,
    [ContentType] nvarchar(100) NOT NULL,
    [Size] bigint NOT NULL,
    [Type] nvarchar(10) NOT NULL,
    CONSTRAINT [PK_Attachments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Attachments_Comments_CommentId] FOREIGN KEY ([CommentId]) REFERENCES [Comments] ([Id]) ON DELETE CASCADE
);

CREATE UNIQUE INDEX [IX_Attachments_CommentId] ON [Attachments] ([CommentId]);

CREATE UNIQUE INDEX [IX_Attachments_StoredFileName] ON [Attachments] ([StoredFileName]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261008175235_AddAttachments', N'10.0.13');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [Attachments] ADD [Status] nvarchar(20) NOT NULL DEFAULT N'Ready';

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261011125901_AddAttachmentStatus', N'10.0.13');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [StoredFiles] (
    [FileName] nvarchar(107) NOT NULL,
    [Content] varbinary(max) NOT NULL,
    CONSTRAINT [PK_StoredFiles] PRIMARY KEY ([FileName])
);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261018170540_AddStoredFiles', N'10.0.13');

COMMIT;
GO

