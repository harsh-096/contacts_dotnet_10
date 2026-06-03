-- ===========================================================
-- 11_ClearAllData.sql
--
-- Destructive: wipes every row from every data table in
-- ContactsDB and resets IDENTITY counters.
--
-- Re-runnable / idempotent. Safe to execute multiple times.
--
-- Approach
-- --------
-- All FKs and CHECK constraints are temporarily disabled
-- inside one transaction so the delete order does not matter
-- and circular / deeply nested FKs cannot abort the wipe.
-- Constraints are re-enabled with WITH CHECK after the delete
-- so the catalog is fully re-validated.
--
-- What is NOT touched
-- -------------------
--   * The database itself (ContactsDB).
--   * The table definitions (Projects / Contacts / Groups /
--     GroupContacts if it still exists).
--   * The stored procedures, views, indexes, constraints.
--   * Any DDL history (this is a data wipe, not a schema wipe).
-- ===========================================================

USE ContactsDB;
GO

SET XACT_ABORT ON;
SET NOCOUNT ON;
GO

BEGIN TRANSACTION;

-- 1. Disable all constraints on every data table so DELETE cannot fail on FKs.
IF OBJECT_ID('dbo.GroupContacts', 'U') IS NOT NULL
    ALTER TABLE dbo.GroupContacts NOCHECK CONSTRAINT ALL;

IF OBJECT_ID('dbo.Groups', 'U') IS NOT NULL
    ALTER TABLE dbo.Groups NOCHECK CONSTRAINT ALL;

IF OBJECT_ID('dbo.Contacts', 'U') IS NOT NULL
    ALTER TABLE dbo.Contacts NOCHECK CONSTRAINT ALL;

IF OBJECT_ID('dbo.Projects', 'U') IS NOT NULL
    ALTER TABLE dbo.Projects NOCHECK CONSTRAINT ALL;

-- 2. Wipe rows. Order does not matter while constraints are off.
IF OBJECT_ID('dbo.GroupContacts', 'U') IS NOT NULL
BEGIN
    DELETE FROM dbo.GroupContacts;
    IF COLUMNPROPERTY(OBJECT_ID('dbo.GroupContacts'), 'GroupContactId', 'IsIdentity') = 1
        DBCC CHECKIDENT ('dbo.GroupContacts', RESEED, 0);
    PRINT 'Cleared dbo.GroupContacts.';
END

IF OBJECT_ID('dbo.Groups', 'U') IS NOT NULL
BEGIN
    DELETE FROM dbo.Groups;
    IF COLUMNPROPERTY(OBJECT_ID('dbo.Groups'), 'GroupId', 'IsIdentity') = 1
        DBCC CHECKIDENT ('dbo.Groups', RESEED, 0);
    PRINT 'Cleared dbo.Groups.';
END

IF OBJECT_ID('dbo.Contacts', 'U') IS NOT NULL
BEGIN
    DELETE FROM dbo.Contacts;
    IF COLUMNPROPERTY(OBJECT_ID('dbo.Contacts'), 'ContactId', 'IsIdentity') = 1
        DBCC CHECKIDENT ('dbo.Contacts', RESEED, 0);
    PRINT 'Cleared dbo.Contacts.';
END

IF OBJECT_ID('dbo.Projects', 'U') IS NOT NULL
BEGIN
    DELETE FROM dbo.Projects;
    IF COLUMNPROPERTY(OBJECT_ID('dbo.Projects'), 'ProjectId', 'IsIdentity') = 1
        DBCC CHECKIDENT ('dbo.Projects', RESEED, 0);
    PRINT 'Cleared dbo.Projects.';
END

-- 3. Re-enable and re-validate every constraint. WITH CHECK forces a full
--    re-validation against the (now empty) tables so the catalog is correct.
IF OBJECT_ID('dbo.Projects', 'U') IS NOT NULL
    ALTER TABLE dbo.Projects WITH CHECK CHECK CONSTRAINT ALL;

IF OBJECT_ID('dbo.Contacts', 'U') IS NOT NULL
    ALTER TABLE dbo.Contacts WITH CHECK CHECK CONSTRAINT ALL;

IF OBJECT_ID('dbo.Groups', 'U') IS NOT NULL
    ALTER TABLE dbo.Groups WITH CHECK CHECK CONSTRAINT ALL;

IF OBJECT_ID('dbo.GroupContacts', 'U') IS NOT NULL
    ALTER TABLE dbo.GroupContacts WITH CHECK CHECK CONSTRAINT ALL;

COMMIT TRANSACTION;
GO

-- Final summary so the operator can see the result inline.
DECLARE @SummarySql NVARCHAR(500);
SET @SummarySql = 'SELECT ''Projects'' AS TableName, COUNT(*) AS RowsRemaining FROM dbo.Projects
UNION ALL
SELECT ''Contacts'',          COUNT(*) FROM dbo.Contacts
UNION ALL
SELECT ''Groups'',            COUNT(*) FROM dbo.Groups';
IF OBJECT_ID('dbo.GroupContacts', 'U') IS NOT NULL
    SET @SummarySql = @SummarySql + ' UNION ALL SELECT ''GroupContacts'', COUNT(*) FROM dbo.GroupContacts';
EXEC sp_executesql @SummarySql;
GO

PRINT 'Migration 11_ClearAllData.sql completed.';
GO
