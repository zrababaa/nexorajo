-- Creates the tables the .NET app owns inside the shared legacy database (smpp_bulk_db_new).
--
-- Embedded into SMPP.Infrastructure and run by DatabaseBootstrapper on startup when the app finds
-- the daemon's tables but no EF migration history. Also runnable by hand:
--
--   mysql -u <user> -p smpp_bulk_db_new < CreateAppTables.sql
--
-- Why this exists instead of the EF migrations: those describe a database EF created from scratch.
-- Replayed against smpp_bulk_db_new they would RENAME a fresh `Histories` table onto the live
-- `historys`, CREATE TABLE `under_process` over the one the SMPP daemon polls, and add a foreign
-- key on `historys.creater_id` that legacy rows cannot satisfy. MySQL does not roll back DDL, so a
-- failure halfway leaves the daemon's tables in pieces.
--
-- This script therefore creates ONLY the app-owned tables and never touches `historys` or
-- `under_process` - the app maps onto those two exactly as the daemon already defines them.
-- The final INSERTs mark the baseline migrations as applied so EF agrees the schema is current.
--
-- Safe to re-run: every statement is IF NOT EXISTS / INSERT IGNORE.
--
-- Statements are split on trailing semicolons by DatabaseBootstrapper, so keep every statement
-- terminated by a `;` at end of line, and do not introduce DELIMITER blocks or stored procedures -
-- DELIMITER is a mysql client directive and does not execute over ADO.NET. The table-name case
-- collision check that used to live here is now in DatabaseBootstrapper, where it runs on every
-- boot rather than only when someone remembers to run this file.

-- ---------------------------------------------------------------------------
-- Identity
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `AspNetRoles` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(256) CHARACTER SET utf8mb4 NULL,
  `NormalizedName` varchar(256) CHARACTER SET utf8mb4 NULL,
  `ConcurrencyStamp` longtext CHARACTER SET utf8mb4 NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `RoleNameIndex` (`NormalizedName`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `AspNetUsers` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `FullName` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
  `MobileNo` longtext CHARACTER SET utf8mb4 NULL,
  `Role` int NOT NULL,
  `IsActive` tinyint(1) NOT NULL,
  `Balance` decimal(18,4) NOT NULL,
  `RatePerMessage` decimal(18,4) NOT NULL,
  `SenderId` varchar(20) CHARACTER SET utf8mb4 NULL,
  `DateFrom` date NULL,
  `DateTo` date NULL,
  `CreatedByUserId` int NULL,
  `ApiToken` varchar(128) CHARACTER SET utf8mb4 NULL,
  `ApiSecret` varchar(128) CHARACTER SET utf8mb4 NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UserName` varchar(256) CHARACTER SET utf8mb4 NULL,
  `NormalizedUserName` varchar(256) CHARACTER SET utf8mb4 NULL,
  `Email` varchar(256) CHARACTER SET utf8mb4 NULL,
  `NormalizedEmail` varchar(256) CHARACTER SET utf8mb4 NULL,
  `EmailConfirmed` tinyint(1) NOT NULL,
  `PasswordHash` longtext CHARACTER SET utf8mb4 NULL,
  `SecurityStamp` longtext CHARACTER SET utf8mb4 NULL,
  `ConcurrencyStamp` longtext CHARACTER SET utf8mb4 NULL,
  `PhoneNumber` longtext CHARACTER SET utf8mb4 NULL,
  `PhoneNumberConfirmed` tinyint(1) NOT NULL,
  `TwoFactorEnabled` tinyint(1) NOT NULL,
  `LockoutEnd` datetime(6) NULL,
  `LockoutEnabled` tinyint(1) NOT NULL,
  `AccessFailedCount` int NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_AspNetUsers_ApiToken` (`ApiToken`),
  UNIQUE KEY `UserNameIndex` (`NormalizedUserName`),
  KEY `EmailIndex` (`NormalizedEmail`),
  KEY `IX_AspNetUsers_CreatedByUserId` (`CreatedByUserId`),
  CONSTRAINT `FK_AspNetUsers_AspNetUsers_CreatedByUserId` FOREIGN KEY (`CreatedByUserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `AspNetRoleClaims` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `RoleId` int NOT NULL,
  `ClaimType` longtext CHARACTER SET utf8mb4 NULL,
  `ClaimValue` longtext CHARACTER SET utf8mb4 NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_AspNetRoleClaims_RoleId` (`RoleId`),
  CONSTRAINT `FK_AspNetRoleClaims_AspNetRoles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `AspNetRoles` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `AspNetUserClaims` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `UserId` int NOT NULL,
  `ClaimType` longtext CHARACTER SET utf8mb4 NULL,
  `ClaimValue` longtext CHARACTER SET utf8mb4 NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_AspNetUserClaims_UserId` (`UserId`),
  CONSTRAINT `FK_AspNetUserClaims_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `AspNetUserLogins` (
  `LoginProvider` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
  `ProviderKey` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
  `ProviderDisplayName` longtext CHARACTER SET utf8mb4 NULL,
  `UserId` int NOT NULL,
  PRIMARY KEY (`LoginProvider`,`ProviderKey`),
  KEY `IX_AspNetUserLogins_UserId` (`UserId`),
  CONSTRAINT `FK_AspNetUserLogins_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `AspNetUserRoles` (
  `UserId` int NOT NULL,
  `RoleId` int NOT NULL,
  PRIMARY KEY (`UserId`,`RoleId`),
  KEY `IX_AspNetUserRoles_RoleId` (`RoleId`),
  CONSTRAINT `FK_AspNetUserRoles_AspNetRoles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `AspNetRoles` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_AspNetUserRoles_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `AspNetUserTokens` (
  `UserId` int NOT NULL,
  `LoginProvider` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
  `Name` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
  `Value` longtext CHARACTER SET utf8mb4 NULL,
  PRIMARY KEY (`UserId`,`LoginProvider`,`Name`),
  CONSTRAINT `FK_AspNetUserTokens_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ---------------------------------------------------------------------------
-- Application tables
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `Campaigns` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
  `ExternalCampaignCode` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
  `Numbers` longtext CHARACTER SET utf8mb4 NOT NULL,
  `SourceType` int NOT NULL,
  `SendSpeedMinSeconds` int NULL,
  `SendSpeedMaxSeconds` int NULL,
  `CreatedByUserId` int NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_Campaigns_ExternalCampaignCode` (`ExternalCampaignCode`),
  KEY `IX_Campaigns_CreatedByUserId` (`CreatedByUserId`),
  CONSTRAINT `FK_Campaigns_AspNetUsers_CreatedByUserId` FOREIGN KEY (`CreatedByUserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `Payments` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Amount` decimal(18,4) NOT NULL,
  `Method` int NOT NULL,
  `TransactionRef` varchar(150) CHARACTER SET utf8mb4 NULL,
  `ProofFilePath` varchar(500) CHARACTER SET utf8mb4 NULL,
  `Note` text CHARACTER SET utf8mb4 NULL,
  `Status` int NOT NULL,
  `SubmittedByUserId` int NOT NULL,
  `ReviewedByUserId` int NULL,
  `ReviewedAt` datetime(6) NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) NULL,
  `ReviewNote` text CHARACTER SET utf8mb4 NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_Payments_ReviewedByUserId` (`ReviewedByUserId`),
  KEY `IX_Payments_Status` (`Status`),
  KEY `IX_Payments_SubmittedByUserId` (`SubmittedByUserId`),
  CONSTRAINT `FK_Payments_AspNetUsers_ReviewedByUserId` FOREIGN KEY (`ReviewedByUserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_Payments_AspNetUsers_SubmittedByUserId` FOREIGN KEY (`SubmittedByUserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `SpamKeywords` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Keyword` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
  `KeywordType` int NOT NULL,
  `CreatedByUserId` int NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_SpamKeywords_CreatedByUserId` (`CreatedByUserId`),
  CONSTRAINT `FK_SpamKeywords_AspNetUsers_CreatedByUserId` FOREIGN KEY (`CreatedByUserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `Transactions` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `UserId` int NOT NULL,
  `CreatedByUserId` int NOT NULL,
  `Amount` decimal(18,4) NOT NULL,
  `Kind` int NOT NULL,
  `Source` int NOT NULL,
  `RelatedBatchId` varchar(50) CHARACTER SET utf8mb4 NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_Transactions_CreatedByUserId` (`CreatedByUserId`),
  KEY `IX_Transactions_UserId_CreatedAt` (`UserId`,`CreatedAt`),
  CONSTRAINT `FK_Transactions_AspNetUsers_CreatedByUserId` FOREIGN KEY (`CreatedByUserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_Transactions_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ---------------------------------------------------------------------------
-- Migration bookkeeping
--
-- Marks the baseline migrations applied without running them, so MigrateAsync does not try to
-- build historys/under_process a second time. Migrations added after this point apply normally.
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
  `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
  `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
  PRIMARY KEY (`MigrationId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`) VALUES ('20260726081323_InitialCreate', '8.0.10'), ('20260727050336_AddPaymentReviewNote', '8.0.10'), ('20260803121726_ReplaceOutboundQueueWithUnderProcess', '8.0.10');
