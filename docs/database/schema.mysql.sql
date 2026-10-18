-- The database schema of the Comments app in MySQL syntax, to open in MySQL Workbench:
--   File -> Import -> Reverse Engineer MySQL Create Script... -> choose this file -> the EER diagram is drawn.
--
-- The app itself runs on MS SQL Server. Its real schema is schema.sqlserver.sql, generated from the
-- EF Core migrations. This file has the same tables, columns, keys and indexes with the nearest MySQL types
-- (nvarchar -> VARCHAR utf8mb4, nvarchar(max) -> LONGTEXT, datetime2 -> DATETIME(6), IDENTITY -> AUTO_INCREMENT).
-- The EF Core service table __EFMigrationsHistory is left out.

CREATE SCHEMA IF NOT EXISTS `CommentsDb` DEFAULT CHARACTER SET utf8mb4;
USE `CommentsDb`;

-- A person who writes comments. Found again by the pair name + e-mail, so a returning author is not duplicated.
CREATE TABLE `Users` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `UserName` VARCHAR(50) NOT NULL COMMENT 'Latin letters and digits only',
  `Email` VARCHAR(254) NOT NULL,
  `HomePage` VARCHAR(2048) NULL COMMENT 'http or https address, optional',
  PRIMARY KEY (`Id`),
  UNIQUE INDEX `IX_Users_UserName_Email` (`UserName`, `Email`)
) ENGINE = InnoDB;

-- A comment or a reply. A reply points to its parent comment, so replies can go down without a limit.
CREATE TABLE `Comments` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `UserId` INT NOT NULL,
  `ParentId` INT NULL COMMENT 'NULL for a top-level comment, the parent comment for a reply',
  `Text` LONGTEXT NOT NULL COMMENT 'Sanitized HTML: only <a href title>, <code>, <i>, <strong>',
  `CreatedAt` DATETIME(6) NOT NULL COMMENT 'UTC',
  `IpAddress` VARCHAR(45) NULL COMMENT 'Of the author, to identify the client',
  `UserAgent` VARCHAR(512) NULL COMMENT 'Of the author, to identify the client',
  PRIMARY KEY (`Id`),
  INDEX `IX_Comments_ParentId_CreatedAt` (`ParentId`, `CreatedAt`),
  INDEX `IX_Comments_UserId` (`UserId`),
  CONSTRAINT `FK_Comments_Comments_ParentId`
    FOREIGN KEY (`ParentId`) REFERENCES `Comments` (`Id`) ON DELETE NO ACTION,
  CONSTRAINT `FK_Comments_Users_UserId`
    FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE NO ACTION
) ENGINE = InnoDB;

-- The one file a comment may have: an image (made not bigger than 320x240) or a TXT file up to 100 KB
CREATE TABLE `Attachments` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `CommentId` INT NOT NULL,
  `OriginalFileName` VARCHAR(255) NOT NULL COMMENT 'As the visitor named it, only shown as text',
  `StoredFileName` VARCHAR(100) NOT NULL COMMENT 'Generated name on the disk',
  `ContentType` VARCHAR(100) NOT NULL,
  `Size` BIGINT NOT NULL COMMENT 'Bytes',
  `Type` VARCHAR(10) NOT NULL COMMENT 'Image or Text',
  `Status` VARCHAR(20) NOT NULL DEFAULT 'Ready' COMMENT 'Processing, Ready or Failed (images are resized in the background)',
  PRIMARY KEY (`Id`),
  UNIQUE INDEX `IX_Attachments_CommentId` (`CommentId`),
  UNIQUE INDEX `IX_Attachments_StoredFileName` (`StoredFileName`),
  CONSTRAINT `FK_Attachments_Comments_CommentId`
    FOREIGN KEY (`CommentId`) REFERENCES `Comments` (`Id`) ON DELETE CASCADE
) ENGINE = InnoDB;

-- The content of uploaded files, when they are kept in the database instead of on disk
-- (for hosts whose disk is wiped on restart). Linked to Attachments by the file name, not by a foreign key:
-- an image waiting for processing is kept as "<StoredFileName>.upload".
CREATE TABLE `StoredFiles` (
  `FileName` VARCHAR(107) NOT NULL COMMENT 'Attachments.StoredFileName, or the same name + .upload',
  `Content` LONGBLOB NOT NULL,
  PRIMARY KEY (`FileName`)
) ENGINE = InnoDB;
