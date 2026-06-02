-- ===========================================================
-- 06_ProjectContactsAndGroupContacts.sql
-- Migration: connect Subscribers to Projects and add the
--            GroupContacts junction table.
--
-- Re-runnable / idempotent. Run AFTER 04_ProjectsAndGroups.sql
-- and 05_AlterSubscribersPhoneNumber.sql.
--
-- Final relationship rules enforced here:
--   * Subscribers.ProjectId  -> Projects.ProjectId       (FK)
--   * Groups.ProjectId       -> Projects.ProjectId       (FK, already exists)
--   * Groups.ProjectId                                 (UNIQUE)  -- one project -> one group
--   * GroupContacts          -- junction: many-to-many
--        composite PK (GroupId, ContactId)
--        FK GroupId    -> Groups.GroupId
--        FK ContactId  -> Subscribers.Id
--
-- Data-migration notes
-- --------------------
-- * Subscribers.ProjectId is added NULLABLE so that the migration
--   succeeds on databases that already contain rows. The service
--   layer treats ProjectId as required on every Create/Update, so
--   the column will be populated for new inserts immediately.
--   Existing rows keep ProjectId = NULL until they are updated
--   (the service can be extended later to bulk-backfill).
-- * Groups.ProjectId is forced NOT NULL + UNIQUE here, which
--   enforces "one project can have only one group" at the database
--   level. The script also collapses any pre-existing duplicate
--   (ProjectId, GroupId) rows by deleting all but the oldest group
--   per project BEFORE adding the UNIQUE constraint. This is a
--   destructive cleanup of pre-existing duplicates only.
-- * GroupContacts is a brand-new table; no data migration.
-- ===========================================================

USE ContactsDB;
GO

SET XACT_ABORT ON;
SET NOCOUNT ON;
GO

-- ----------------------------------------------------------------
-- 1. Subscribers.ProjectId -- add the column if it does not exist.
-- ----------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE  object_id = OBJECT_ID('dbo.Subscribers')
      AND  name = 'ProjectId'
)
BEGIN
    ALTER TABLE dbo.Subscribers ADD ProjectId INT NULL;
END
GO

-- ----------------------------------------------------------------
-- 2. Subscribers.ProjectId -- enforce NOT NULL once the column
--    exists but only if there are no NULLs remaining. If NULLs
--    are still present the migration is halted with a clear
--    message so the operator can backfill the data first.
-- ----------------------------------------------------------------
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE  object_id = OBJECT_ID('dbo.Subscribers')
      AND  name = 'ProjectId'
      AND  is_nullable = 1
)
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.Subscribers WHERE ProjectId IS NULL)
    BEGIN
        ;THROW 50030, 'Cannot enforce NOT NULL on Subscribers.ProjectId: existing rows have NULL values. Backfill them first.', 1;
    END

    ALTER TABLE dbo.Subscribers ALTER COLUMN ProjectId INT NOT NULL;
END
GO

-- ----------------------------------------------------------------
-- 3. Subscribers.ProjectId -- FK to Projects.ProjectId.
-- ----------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE  name = 'FK_Subscribers_Projects_ProjectId'
      AND  parent_object_id = OBJECT_ID('dbo.Subscribers')
)
BEGIN
    ALTER TABLE dbo.Subscribers
        ADD CONSTRAINT FK_Subscribers_Projects_ProjectId
        FOREIGN KEY (ProjectId)
        REFERENCES  dbo.Projects (ProjectId);
END
GO

-- ----------------------------------------------------------------
-- 4. Index on Subscribers.ProjectId (FK index, used by
--    sp_GetContactsByProjectId).
-- ----------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE  name = 'IX_Subscribers_ProjectId'
      AND  object_id = OBJECT_ID('dbo.Subscribers')
)
BEGIN
    CREATE INDEX IX_Subscribers_ProjectId
        ON dbo.Subscribers (ProjectId);
END
GO

-- ----------------------------------------------------------------
-- 5. Groups.ProjectId -- enforce NOT NULL.
--    One project -> one group, so ProjectId is now mandatory.
-- ----------------------------------------------------------------
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE  object_id = OBJECT_ID('dbo.Groups')
      AND  name = 'ProjectId'
      AND  is_nullable = 1
)
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.Groups WHERE ProjectId IS NULL)
    BEGIN
        ;THROW 50031, 'Cannot enforce NOT NULL on Groups.ProjectId: existing groups have NULL values. Assign them to a project first.', 1;
    END

    ALTER TABLE dbo.Groups ALTER COLUMN ProjectId INT NOT NULL;
END
GO

-- ----------------------------------------------------------------
-- 6. Groups.ProjectId -- UNIQUE. Destructive dedup pass FIRST:
--    keep the lowest GroupId per project and delete the rest.
--    Only runs when the IX_Groups_ProjectId is not yet unique.
-- ----------------------------------------------------------------
IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE  object_id = OBJECT_ID('dbo.Groups')
      AND  name = 'IX_Groups_ProjectId'
)
BEGIN
    ;WITH Duplicates AS (
        SELECT GroupId,
               ROW_NUMBER() OVER (PARTITION BY ProjectId ORDER BY GroupId ASC) AS rn
        FROM   dbo.Groups
    )
    DELETE g
    FROM   dbo.Groups g
    INNER JOIN Duplicates d ON g.GroupId = d.GroupId
    WHERE  d.rn > 1;
END
GO

-- Drop the old non-unique FK index; we will replace it with a UNIQUE index.
IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE  name = 'IX_Groups_ProjectId'
      AND  object_id = OBJECT_ID('dbo.Groups')
)
BEGIN
    DROP INDEX IX_Groups_ProjectId ON dbo.Groups;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE  name = 'UQ_Groups_ProjectId'
      AND  object_id = OBJECT_ID('dbo.Groups')
)
BEGIN
    -- UNIQUE constraint creates a backing index automatically.
    ALTER TABLE dbo.Groups
        ADD CONSTRAINT UQ_Groups_ProjectId UNIQUE (ProjectId);
END
GO

-- ----------------------------------------------------------------
-- 7. GroupContacts -- new junction table.
-- ----------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='GroupContacts' AND xtype='U')
BEGIN
    CREATE TABLE dbo.GroupContacts
    (
        GroupId    INT NOT NULL,
        ContactId  INT NOT NULL,

        CONSTRAINT PK_GroupContacts PRIMARY KEY CLUSTERED (GroupId, ContactId)
    );
END
GO

-- ----------------------------------------------------------------
-- 8. GroupContacts FKs and supporting indexes.
-- ----------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE  name = 'FK_GroupContacts_Groups_GroupId'
      AND  parent_object_id = OBJECT_ID('dbo.GroupContacts')
)
BEGIN
    ALTER TABLE dbo.GroupContacts
        ADD CONSTRAINT FK_GroupContacts_Groups_GroupId
        FOREIGN KEY (GroupId)
        REFERENCES  dbo.Groups (GroupId);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE  name = 'FK_GroupContacts_Subscribers_ContactId'
      AND  parent_object_id = OBJECT_ID('dbo.GroupContacts')
)
BEGIN
    ALTER TABLE dbo.GroupContacts
        ADD CONSTRAINT FK_GroupContacts_Subscribers_ContactId
        FOREIGN KEY (ContactId)
        REFERENCES  dbo.Subscribers (Id);
END
GO

-- Reverse-lookup index: "groups for a contact" (ContactId, GroupId).
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE  name = 'IX_GroupContacts_ContactId'
      AND  object_id = OBJECT_ID('dbo.GroupContacts')
)
BEGIN
    CREATE INDEX IX_GroupContacts_ContactId
        ON dbo.GroupContacts (ContactId);
END
GO

PRINT 'Migration 06_ProjectContactsAndGroupContacts.sql completed. ' +
      'Now re-run 07_UpdateSubscribersAndGroupContactsSPs.sql to refresh procedures.';
GO
