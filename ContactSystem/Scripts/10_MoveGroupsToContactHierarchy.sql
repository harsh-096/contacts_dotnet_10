-- ===========================================================
-- 10_RestoreGroupProjectRelation.sql
--
-- Migration: restore Groups as children of a Project (not a
--            Contact) and keep the GroupContacts junction table
--            for many-to-many Contact membership.
--
-- Drops the UQ_Groups_ProjectId constraint (added in script 06)
-- so a project can have MANY groups (e.g. "DatabaseDesign",
-- "Frontend", "Backend"). Each group can contain many contacts
-- via the GroupContacts junction, and each contact can belong
-- to many groups.
--
-- Re-runnable / idempotent. Safe to execute multiple times.
-- ===========================================================

USE ContactsDB;
GO

SET XACT_ABORT ON;
SET NOCOUNT ON;
GO

-- ----------------------------------------------------------------
-- 1. Drop UQ_Groups_ProjectId so one project can host many groups.
-- ----------------------------------------------------------------
IF EXISTS (
    SELECT 1 FROM sys.key_constraints
    WHERE  name = 'UQ_Groups_ProjectId'
       AND parent_object_id = OBJECT_ID('dbo.Groups')
)
BEGIN
    ALTER TABLE dbo.Groups DROP CONSTRAINT UQ_Groups_ProjectId;
    PRINT 'Dropped UQ_Groups_ProjectId.';
END
GO

-- ----------------------------------------------------------------
-- 2. If a ContactId column exists on Groups (from a prior run of
--    the old script 10), remove it so Groups stays project-owned.
-- ----------------------------------------------------------------
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE  object_id = OBJECT_ID('dbo.Groups')
       AND  name      = 'ContactId'
)
BEGIN
    IF EXISTS (
        SELECT 1 FROM sys.foreign_keys
        WHERE  name = 'FK_Groups_Contacts_ContactId'
           AND parent_object_id = OBJECT_ID('dbo.Groups')
    )
    BEGIN
        ALTER TABLE dbo.Groups DROP CONSTRAINT FK_Groups_Contacts_ContactId;
    END

    IF EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE  name = 'IX_Groups_ContactId'
           AND object_id = OBJECT_ID('dbo.Groups')
    )
    BEGIN
        DROP INDEX IX_Groups_ContactId ON dbo.Groups;
    END

    ALTER TABLE dbo.Groups DROP COLUMN ContactId;
    PRINT 'Dropped Groups.ContactId column.';
END
GO

-- ----------------------------------------------------------------
-- 3. Ensure Groups.ProjectId is NOT NULL and has its FK.
-- ----------------------------------------------------------------
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE  object_id = OBJECT_ID('dbo.Groups')
       AND  name       = 'ProjectId'
       AND  is_nullable = 1
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.Groups WHERE ProjectId IS NULL)
    BEGIN
        ALTER TABLE dbo.Groups ALTER COLUMN ProjectId INT NOT NULL;
        PRINT 'Made Groups.ProjectId NOT NULL.';
    END
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE  name = 'FK_Groups_Projects_ProjectId'
       AND parent_object_id = OBJECT_ID('dbo.Groups')
)
BEGIN
    ALTER TABLE dbo.Groups
        ADD CONSTRAINT FK_Groups_Projects_ProjectId
        FOREIGN KEY (ProjectId)
        REFERENCES  dbo.Projects (ProjectId);
    PRINT 'Added FK_Groups_Projects_ProjectId.';
END
GO

-- ----------------------------------------------------------------
-- 4. Recreate the FK index for ProjectId.
-- ----------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE  name = 'IX_Groups_ProjectId'
       AND object_id = OBJECT_ID('dbo.Groups')
)
BEGIN
    CREATE INDEX IX_Groups_ProjectId
        ON dbo.Groups (ProjectId);
    PRINT 'Created IX_Groups_ProjectId.';
END
GO

-- ----------------------------------------------------------------
-- 5. Ensure GroupContacts junction table still exists.
-- ----------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='GroupContacts' AND xtype='U')
BEGIN
    CREATE TABLE dbo.GroupContacts
    (
        GroupId    INT NOT NULL,
        ContactId  INT NOT NULL,
        CONSTRAINT PK_GroupContacts PRIMARY KEY CLUSTERED (GroupId, ContactId)
    );

    IF NOT EXISTS (
        SELECT 1 FROM sys.foreign_keys
        WHERE  name = 'FK_GroupContacts_Groups_GroupId'
           AND parent_object_id = OBJECT_ID('dbo.GroupContacts')
    )
    BEGIN
        ALTER TABLE dbo.GroupContacts
            ADD CONSTRAINT FK_GroupContacts_Groups_GroupId
            FOREIGN KEY (GroupId) REFERENCES dbo.Groups (GroupId);
    END

    IF NOT EXISTS (
        SELECT 1 FROM sys.foreign_keys
        WHERE  name = 'FK_GroupContacts_Contacts_ContactId'
           AND parent_object_id = OBJECT_ID('dbo.GroupContacts')
    )
    BEGIN
        ALTER TABLE dbo.GroupContacts
            ADD CONSTRAINT FK_GroupContacts_Contacts_ContactId
            FOREIGN KEY (ContactId) REFERENCES dbo.Contacts (ContactId);
    END

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE  name = 'IX_GroupContacts_ContactId'
           AND object_id = OBJECT_ID('dbo.GroupContacts')
    )
    BEGIN
        CREATE INDEX IX_GroupContacts_ContactId
            ON dbo.GroupContacts (ContactId);
    END

    PRINT 'Created GroupContacts junction table.';
END
GO

-- ----------------------------------------------------------------
-- 6. Recreate stored procedures for Groups (ProjectId-based).
-- ----------------------------------------------------------------

-- sp_GetAllGroups -----------------------------------------------
IF OBJECT_ID('dbo.sp_GetAllGroups', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetAllGroups;
GO
CREATE PROCEDURE dbo.sp_GetAllGroups
AS
BEGIN
    SET NOCOUNT ON;
    SELECT GroupId, GroupName, ProjectId
    FROM   dbo.Groups
    ORDER BY GroupId DESC;
END
GO

-- sp_GetGroupById ------------------------------------------------
IF OBJECT_ID('dbo.sp_GetGroupById', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetGroupById;
GO
CREATE PROCEDURE dbo.sp_GetGroupById
    @GroupId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT GroupId, GroupName, ProjectId
    FROM   dbo.Groups
    WHERE  GroupId = @GroupId;
END
GO

-- sp_CreateGroup ------------------------------------------------
IF OBJECT_ID('dbo.sp_CreateGroup', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_CreateGroup;
GO
CREATE PROCEDURE dbo.sp_CreateGroup
    @GroupName  VARCHAR(255),
    @ProjectId  INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @ProjectId IS NULL OR @ProjectId <= 0
    BEGIN
        ;THROW 50020, 'Invalid @ProjectId supplied to sp_CreateGroup.', 1;
        RETURN;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        IF NOT EXISTS (SELECT 1 FROM dbo.Projects WHERE ProjectId = @ProjectId)
        BEGIN
            ;THROW 50045, 'Referenced Project does not exist.', 1;
            ROLLBACK TRANSACTION;
            RETURN;
        END

        INSERT INTO dbo.Groups (GroupName, ProjectId)
        VALUES (@GroupName, @ProjectId);

        SELECT CAST(SCOPE_IDENTITY() AS INT) AS NewId;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- sp_UpdateGroup ------------------------------------------------
IF OBJECT_ID('dbo.sp_UpdateGroup', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_UpdateGroup;
GO
CREATE PROCEDURE dbo.sp_UpdateGroup
    @GroupId    INT          = NULL,
    @GroupName  VARCHAR(255) = NULL,
    @ProjectId  INT          = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @GroupId IS NULL OR @GroupId <= 0
    BEGIN
        ;THROW 50021, 'Invalid @GroupId supplied to sp_UpdateGroup.', 1;
        RETURN;
    END

    IF @ProjectId IS NOT NULL AND @ProjectId <= 0
    BEGIN
        ;THROW 50047, 'Invalid @ProjectId supplied to sp_UpdateGroup.', 1;
        RETURN;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        IF @ProjectId IS NOT NULL
           AND NOT EXISTS (SELECT 1 FROM dbo.Projects WHERE ProjectId = @ProjectId)
        BEGIN
            ;THROW 50048, 'Referenced Project does not exist.', 1;
            ROLLBACK TRANSACTION;
            RETURN;
        END

        UPDATE TOP (1) dbo.Groups
        SET    GroupName  = ISNULL(@GroupName,  GroupName),
               ProjectId  = ISNULL(@ProjectId,  ProjectId)
        WHERE  GroupId = @GroupId;

        SELECT @@ROWCOUNT AS RowsAffected;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- sp_DeleteGroup ------------------------------------------------
IF OBJECT_ID('dbo.sp_DeleteGroup', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_DeleteGroup;
GO
CREATE PROCEDURE dbo.sp_DeleteGroup
    @GroupId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DELETE FROM dbo.GroupContacts WHERE GroupId = @GroupId;

        DELETE FROM dbo.Groups WHERE GroupId = @GroupId;

        SELECT @@ROWCOUNT AS RowsAffected;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- sp_GetGroupsByProjectId ---------------------------------------
IF OBJECT_ID('dbo.sp_GetGroupsByProjectId', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetGroupsByProjectId;
GO
CREATE PROCEDURE dbo.sp_GetGroupsByProjectId
    @ProjectId INT
AS
BEGIN
    SET NOCOUNT ON;

    IF @ProjectId IS NULL OR @ProjectId <= 0
    BEGIN
        ;THROW 50022, 'Invalid @ProjectId supplied to sp_GetGroupsByProjectId.', 1;
        RETURN;
    END

    SELECT GroupId, GroupName, ProjectId
    FROM   dbo.Groups
    WHERE  ProjectId = @ProjectId
    ORDER BY GroupId DESC;
END
GO

-- sp_DeleteGroupsByProjectId ------------------------------------
IF OBJECT_ID('dbo.sp_DeleteGroupsByProjectId', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_DeleteGroupsByProjectId;
GO
CREATE PROCEDURE dbo.sp_DeleteGroupsByProjectId
    @ProjectId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @ProjectId IS NULL OR @ProjectId <= 0
    BEGIN
        ;THROW 50023, 'Invalid @ProjectId supplied to sp_DeleteGroupsByProjectId.', 1;
        RETURN;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        DELETE gc FROM dbo.GroupContacts gc
        INNER JOIN dbo.Groups g ON g.GroupId = gc.GroupId
        WHERE g.ProjectId = @ProjectId;

        DELETE FROM dbo.Groups WHERE ProjectId = @ProjectId;

        SELECT @@ROWCOUNT AS RowsAffected;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- ----------------------------------------------------------------
-- 7. Junction table stored procedures.
-- ----------------------------------------------------------------

-- sp_AddContactToGroup ------------------------------------------
IF OBJECT_ID('dbo.sp_AddContactToGroup', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_AddContactToGroup;
GO
CREATE PROCEDURE dbo.sp_AddContactToGroup
    @GroupId   INT,
    @ContactId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @GroupId IS NULL OR @GroupId <= 0 OR @ContactId IS NULL OR @ContactId <= 0
    BEGIN
        ;THROW 50050, 'Invalid @GroupId / @ContactId supplied to sp_AddContactToGroup.', 1;
        RETURN;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        IF NOT EXISTS (SELECT 1 FROM dbo.Groups WHERE GroupId = @GroupId)
        BEGIN
            ;THROW 50051, 'Group does not exist.', 1;
            ROLLBACK TRANSACTION;
            RETURN;
        END

        IF NOT EXISTS (SELECT 1 FROM dbo.Contacts WHERE ContactId = @ContactId)
        BEGIN
            ;THROW 50052, 'Contact does not exist.', 1;
            ROLLBACK TRANSACTION;
            RETURN;
        END

        IF EXISTS (SELECT 1 FROM dbo.GroupContacts WHERE GroupId = @GroupId AND ContactId = @ContactId)
        BEGIN
            ;THROW 50054, 'This contact is already a member of the group.', 1;
            ROLLBACK TRANSACTION;
            RETURN;
        END

        INSERT INTO dbo.GroupContacts (GroupId, ContactId)
        VALUES (@GroupId, @ContactId);

        SELECT 1 AS RowsAffected;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- sp_RemoveContactFromGroup --------------------------------------
IF OBJECT_ID('dbo.sp_RemoveContactFromGroup', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_RemoveContactFromGroup;
GO
CREATE PROCEDURE dbo.sp_RemoveContactFromGroup
    @GroupId   INT,
    @ContactId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @GroupId IS NULL OR @GroupId <= 0 OR @ContactId IS NULL OR @ContactId <= 0
    BEGIN
        ;THROW 50055, 'Invalid @GroupId / @ContactId supplied to sp_RemoveContactFromGroup.', 1;
        RETURN;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        DELETE FROM dbo.GroupContacts
        WHERE  GroupId = @GroupId AND ContactId = @ContactId;

        SELECT @@ROWCOUNT AS RowsAffected;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- sp_GetContactsByGroupId ----------------------------------------
IF OBJECT_ID('dbo.sp_GetContactsByGroupId', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetContactsByGroupId;
GO
CREATE PROCEDURE dbo.sp_GetContactsByGroupId
    @GroupId INT
AS
BEGIN
    SET NOCOUNT ON;

    IF @GroupId IS NULL OR @GroupId <= 0
    BEGIN
        ;THROW 50056, 'Invalid @GroupId supplied to sp_GetContactsByGroupId.', 1;
        RETURN;
    END

    SELECT c.ContactId, c.FirstName, c.LastName, c.CountryCode,
           c.NationalNumber, c.PhoneNumber, c.ProjectId,
           c.IsSubscribed, c.CreatedDate, c.UpdatedDate
    FROM   dbo.Contacts c
    INNER JOIN dbo.GroupContacts gc ON gc.ContactId = c.ContactId
    WHERE  gc.GroupId = @GroupId
    ORDER BY c.ContactId DESC;
END
GO

-- sp_GetGroupsByContactId ----------------------------------------
IF OBJECT_ID('dbo.sp_GetGroupsByContactId', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetGroupsByContactId;
GO
CREATE PROCEDURE dbo.sp_GetGroupsByContactId
    @ContactId INT
AS
BEGIN
    SET NOCOUNT ON;

    IF @ContactId IS NULL OR @ContactId <= 0
    BEGIN
        ;THROW 50057, 'Invalid @ContactId supplied to sp_GetGroupsByContactId.', 1;
        RETURN;
    END

    SELECT g.GroupId, g.GroupName, g.ProjectId
    FROM   dbo.Groups g
    INNER JOIN dbo.GroupContacts gc ON gc.GroupId = g.GroupId
    WHERE  gc.ContactId = @ContactId
    ORDER BY g.GroupId DESC;
END
GO

-- ----------------------------------------------------------------
-- 8. Drop legacy Contact-ownership SPs that no longer apply.
-- ----------------------------------------------------------------
IF OBJECT_ID('dbo.sp_DeleteGroupsByContactId',   'P') IS NOT NULL DROP PROCEDURE dbo.sp_DeleteGroupsByContactId;

PRINT 'Migration 10_RestoreGroupProjectRelation.sql completed.';
GO
