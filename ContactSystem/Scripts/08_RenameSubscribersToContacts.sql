-- ===========================================================
-- 08_RenameSubscribersToContacts.sql
--
-- Migration: rename the Subscribers table and every related
-- object to the Contact naming convention.
--
-- Re-runnable / idempotent. Safe to execute multiple times.
-- Run AFTER 07_UpdateSubscribersAndGroupContactsSPs.sql.
--
-- What changes
-- ------------
-- Table     : dbo.Subscribers            -> dbo.Contacts
-- Column    : Id                         -> ContactId
-- Indexes   : PK_Subscribers             -> PK_Contacts
--             UQ_Subscribers_PhoneNumber -> UQ_Contacts_PhoneNumber
--             IX_Subscribers_ProjectId   -> IX_Contacts_ProjectId
-- FK        : FK_Subscribers_Projects_ProjectId
--                                  -> FK_Contacts_Projects_ProjectId
--             FK_GroupContacts_Subscribers_ContactId
--                                  -> FK_GroupContacts_Contacts_ContactId
-- Defaults  : DF_Subscribers_IsSubscribed -> DF_Contacts_IsSubscribed
--             DF_Subscribers_CreatedDate  -> DF_Contacts_CreatedDate
-- Checks    : CK_Subscribers_*            -> CK_Contacts_*
-- SPs       : sp_*Subscribers             -> sp_*Contacts
--             (sp_GetContactsByProjectId
--              and sp_GetContactsByGroupId
--              are untouched; they already use the new name.)
-- ===========================================================

USE ContactsDB;
GO

SET XACT_ABORT ON;
SET NOCOUNT ON;
GO

-- ----------------------------------------------------------------
-- 1. Drop FKs that point at Subscribers so we can rename the
--    table and its primary key.
-- ----------------------------------------------------------------
IF EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'FK_Subscribers_Projects_ProjectId'
      AND parent_object_id = OBJECT_ID('dbo.Subscribers')
)
BEGIN
    ALTER TABLE dbo.Subscribers DROP CONSTRAINT FK_Subscribers_Projects_ProjectId;
END
GO

IF EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'FK_GroupContacts_Subscribers_ContactId'
      AND parent_object_id = OBJECT_ID('dbo.GroupContacts')
)
BEGIN
    ALTER TABLE dbo.GroupContacts DROP CONSTRAINT FK_GroupContacts_Subscribers_ContactId;
END
GO

-- ----------------------------------------------------------------
-- 2. Drop the existing PK / UNIQUE / index objects that depend
--    on the column we are about to rename.
-- ----------------------------------------------------------------
IF EXISTS (
    SELECT 1 FROM sys.key_constraints
    WHERE name = 'PK_Subscribers'
      AND parent_object_id = OBJECT_ID('dbo.Subscribers')
)
BEGIN
    ALTER TABLE dbo.Subscribers DROP CONSTRAINT PK_Subscribers;
END
GO

IF EXISTS (
    SELECT 1 FROM sys.key_constraints
    WHERE name = 'UQ_Subscribers_PhoneNumber'
      AND parent_object_id = OBJECT_ID('dbo.Subscribers')
)
BEGIN
    ALTER TABLE dbo.Subscribers DROP CONSTRAINT UQ_Subscribers_PhoneNumber;
END
GO

IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Subscribers_ProjectId'
      AND object_id = OBJECT_ID('dbo.Subscribers')
)
BEGIN
    DROP INDEX IX_Subscribers_ProjectId ON dbo.Subscribers;
END
GO

-- ----------------------------------------------------------------
-- 3. Drop CHECK / DEFAULT constraints that we will recreate
--    under the new names.
-- ----------------------------------------------------------------
DECLARE @sql NVARCHAR(MAX) = N'';
SELECT @sql = @sql + N'ALTER TABLE dbo.Subscribers DROP CONSTRAINT '
                  + QUOTENAME(name) + N';' + CHAR(10)
FROM sys.check_constraints
WHERE parent_object_id = OBJECT_ID('dbo.Subscribers');

IF LEN(@sql) > 0 EXEC sp_executesql @sql;
GO

DECLARE @sql2 NVARCHAR(MAX) = N'';
SELECT @sql2 = @sql2 + N'ALTER TABLE dbo.Subscribers DROP CONSTRAINT '
                    + QUOTENAME(name) + N';' + CHAR(10)
FROM sys.default_constraints
WHERE parent_object_id = OBJECT_ID('dbo.Subscribers');

IF LEN(@sql2) > 0 EXEC sp_executesql @sql2;
GO

-- ----------------------------------------------------------------
-- 4. Rename Id -> ContactId.
-- ----------------------------------------------------------------
IF EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID('dbo.Subscribers')
          AND name = 'Id'
    )
   AND NOT EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID('dbo.Subscribers')
          AND name = 'ContactId'
    )
BEGIN
    EXEC sp_rename 'dbo.Subscribers.Id', 'ContactId', 'COLUMN';
END
GO

-- ----------------------------------------------------------------
-- 5. Rename the table Subscribers -> Contacts.
-- ----------------------------------------------------------------
IF EXISTS (
        SELECT 1 FROM sys.tables
        WHERE object_id = OBJECT_ID('dbo.Subscribers')
    )
   AND NOT EXISTS (
        SELECT 1 FROM sys.tables
        WHERE object_id = OBJECT_ID('dbo.Contacts')
    )
BEGIN
    EXEC sp_rename 'dbo.Subscribers', 'Contacts';
END
GO

-- ----------------------------------------------------------------
-- 6. Re-create the PK / UNIQUE / index / FK / CHECK / DEFAULT
--    objects under their new names.
-- ----------------------------------------------------------------

-- PK
IF NOT EXISTS (
    SELECT 1 FROM sys.key_constraints
    WHERE name = 'PK_Contacts'
      AND parent_object_id = OBJECT_ID('dbo.Contacts')
)
BEGIN
    ALTER TABLE dbo.Contacts
        ADD CONSTRAINT PK_Contacts PRIMARY KEY CLUSTERED (ContactId);
END
GO

-- UNIQUE on PhoneNumber
IF NOT EXISTS (
    SELECT 1 FROM sys.key_constraints
    WHERE name = 'UQ_Contacts_PhoneNumber'
      AND parent_object_id = OBJECT_ID('dbo.Contacts')
)
BEGIN
    ALTER TABLE dbo.Contacts
        ADD CONSTRAINT UQ_Contacts_PhoneNumber UNIQUE (PhoneNumber);
END
GO

-- FK index
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Contacts_ProjectId'
      AND object_id = OBJECT_ID('dbo.Contacts')
)
BEGIN
    CREATE INDEX IX_Contacts_ProjectId
        ON dbo.Contacts (ProjectId);
END
GO

-- FK to Projects
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'FK_Contacts_Projects_ProjectId'
      AND parent_object_id = OBJECT_ID('dbo.Contacts')
)
BEGIN
    ALTER TABLE dbo.Contacts
        ADD CONSTRAINT FK_Contacts_Projects_ProjectId
        FOREIGN KEY (ProjectId)
        REFERENCES  dbo.Projects (ProjectId);
END
GO

-- FK from GroupContacts to Contacts
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'FK_GroupContacts_Contacts_ContactId'
      AND parent_object_id = OBJECT_ID('dbo.GroupContacts')
)
BEGIN
    ALTER TABLE dbo.GroupContacts
        ADD CONSTRAINT FK_GroupContacts_Contacts_ContactId
        FOREIGN KEY (ContactId)
        REFERENCES  dbo.Contacts (ContactId);
END
GO

-- CHECK constraints (use the same logic as 02_CreateTable.sql)
IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CK_Contacts_CountryCode_Plus'
      AND parent_object_id = OBJECT_ID('dbo.Contacts')
)
BEGIN
    ALTER TABLE dbo.Contacts
        ADD CONSTRAINT CK_Contacts_CountryCode_Plus
            CHECK (CountryCode LIKE '+%' AND LEN(CountryCode) BETWEEN 2 AND 5);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CK_Contacts_NationalNumber_Digits'
      AND parent_object_id = OBJECT_ID('dbo.Contacts')
)
BEGIN
    ALTER TABLE dbo.Contacts
        ADD CONSTRAINT CK_Contacts_NationalNumber_Digits
            CHECK (NationalNumber NOT LIKE '%[^0-9]%' AND LEN(NationalNumber) >= 4);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CK_Contacts_PhoneNumber_Digits'
      AND parent_object_id = OBJECT_ID('dbo.Contacts')
)
BEGIN
    ALTER TABLE dbo.Contacts
        ADD CONSTRAINT CK_Contacts_PhoneNumber_Digits
            CHECK (PhoneNumber NOT LIKE '%[^0-9]%' AND LEN(PhoneNumber) >= 5);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CK_Contacts_PhoneNumber_Composition'
      AND parent_object_id = OBJECT_ID('dbo.Contacts')
)
BEGIN
    ALTER TABLE dbo.Contacts
        ADD CONSTRAINT CK_Contacts_PhoneNumber_Composition
            CHECK (PhoneNumber = REPLACE(CountryCode, '+', '') + NationalNumber);
END
GO

-- DEFAULT constraints
IF NOT EXISTS (
    SELECT 1 FROM sys.default_constraints
    WHERE name = 'DF_Contacts_IsSubscribed'
      AND parent_object_id = OBJECT_ID('dbo.Contacts')
)
BEGIN
    ALTER TABLE dbo.Contacts
        ADD CONSTRAINT DF_Contacts_IsSubscribed DEFAULT (1) FOR IsSubscribed;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.default_constraints
    WHERE name = 'DF_Contacts_CreatedDate'
      AND parent_object_id = OBJECT_ID('dbo.Contacts')
)
BEGIN
    ALTER TABLE dbo.Contacts
        ADD CONSTRAINT DF_Contacts_CreatedDate DEFAULT (SYSUTCDATETIME()) FOR CreatedDate;
END
GO

-- ----------------------------------------------------------------
-- 7. Rename stored procedures.
--    sp_GetContactsByProjectId / sp_GetContactsByGroupId already use
--    the "Contacts" name and are left untouched.
-- ----------------------------------------------------------------
IF OBJECT_ID('dbo.sp_GetAllSubscribers', 'P') IS NOT NULL
   AND OBJECT_ID('dbo.sp_GetAllContacts', 'P') IS NULL
BEGIN
    EXEC sp_rename 'dbo.sp_GetAllSubscribers', 'sp_GetAllContacts';
END
ELSE IF OBJECT_ID('dbo.sp_GetAllSubscribers', 'P') IS NOT NULL
   AND OBJECT_ID('dbo.sp_GetAllContacts', 'P') IS NOT NULL
BEGIN
    DROP PROCEDURE dbo.sp_GetAllSubscribers;
END
GO

IF OBJECT_ID('dbo.sp_GetSubscriberById', 'P') IS NOT NULL
   AND OBJECT_ID('dbo.sp_GetContactById', 'P') IS NULL
BEGIN
    EXEC sp_rename 'dbo.sp_GetSubscriberById', 'sp_GetContactById';
END
ELSE IF OBJECT_ID('dbo.sp_GetSubscriberById', 'P') IS NOT NULL
   AND OBJECT_ID('dbo.sp_GetContactById', 'P') IS NOT NULL
BEGIN
    DROP PROCEDURE dbo.sp_GetSubscriberById;
END
GO

IF OBJECT_ID('dbo.sp_CreateSubscriber', 'P') IS NOT NULL
   AND OBJECT_ID('dbo.sp_CreateContact', 'P') IS NULL
BEGIN
    EXEC sp_rename 'dbo.sp_CreateSubscriber', 'sp_CreateContact';
END
ELSE IF OBJECT_ID('dbo.sp_CreateSubscriber', 'P') IS NOT NULL
   AND OBJECT_ID('dbo.sp_CreateContact', 'P') IS NOT NULL
BEGIN
    DROP PROCEDURE dbo.sp_CreateSubscriber;
END
GO

IF OBJECT_ID('dbo.sp_UpdateSubscriber', 'P') IS NOT NULL
   AND OBJECT_ID('dbo.sp_UpdateContact', 'P') IS NULL
BEGIN
    EXEC sp_rename 'dbo.sp_UpdateSubscriber', 'sp_UpdateContact';
END
ELSE IF OBJECT_ID('dbo.sp_UpdateSubscriber', 'P') IS NOT NULL
   AND OBJECT_ID('dbo.sp_UpdateContact', 'P') IS NOT NULL
BEGIN
    DROP PROCEDURE dbo.sp_UpdateSubscriber;
END
GO

IF OBJECT_ID('dbo.sp_DeleteSubscriber', 'P') IS NOT NULL
   AND OBJECT_ID('dbo.sp_DeleteContact', 'P') IS NULL
BEGIN
    EXEC sp_rename 'dbo.sp_DeleteSubscriber', 'sp_DeleteContact';
END
ELSE IF OBJECT_ID('dbo.sp_DeleteSubscriber', 'P') IS NOT NULL
   AND OBJECT_ID('dbo.sp_DeleteContact', 'P') IS NOT NULL
BEGIN
    DROP PROCEDURE dbo.sp_DeleteSubscriber;
END
GO

-- ----------------------------------------------------------------
-- 8. Refresh the Subscriber-named stored procedures so their
--    internal references to the table / column match the new
--    names.  Re-creating them is the safest way to make sure
--    every reference is consistent.
-- ----------------------------------------------------------------
IF OBJECT_ID('dbo.sp_GetAllContacts', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetAllContacts;
GO
CREATE PROCEDURE dbo.sp_GetAllContacts
AS
BEGIN
    SET NOCOUNT ON;

    SELECT ContactId,
           FirstName,
           LastName,
           CountryCode,
           NationalNumber,
           PhoneNumber,
           ProjectId,
           IsSubscribed,
           CreatedDate,
           UpdatedDate
    FROM   dbo.Contacts
    ORDER BY ContactId DESC;
END
GO

IF OBJECT_ID('dbo.sp_GetContactById', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetContactById;
GO
CREATE PROCEDURE dbo.sp_GetContactById
    @ContactId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT ContactId,
           FirstName,
           LastName,
           CountryCode,
           NationalNumber,
           PhoneNumber,
           ProjectId,
           IsSubscribed,
           CreatedDate,
           UpdatedDate
    FROM   dbo.Contacts
    WHERE  ContactId = @ContactId;
END
GO

IF OBJECT_ID('dbo.sp_CreateContact', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_CreateContact;
GO
CREATE PROCEDURE dbo.sp_CreateContact
    @FirstName       NVARCHAR(50),
    @LastName        NVARCHAR(50),
    @CountryCode     NVARCHAR(5),
    @NationalNumber  NVARCHAR(20),
    @PhoneNumber     NVARCHAR(25),
    @ProjectId       INT,
    @IsSubscribed    BIT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @ProjectId IS NULL OR @ProjectId <= 0
    BEGIN
        ;THROW 50040, 'Invalid @ProjectId supplied to sp_CreateContact.', 1;
        RETURN;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        IF NOT EXISTS (SELECT 1 FROM dbo.Projects WHERE ProjectId = @ProjectId)
        BEGIN
            ;THROW 50041, 'Referenced Project does not exist.', 1;
            ROLLBACK TRANSACTION;
            RETURN;
        END

        INSERT INTO dbo.Contacts
            (FirstName, LastName, CountryCode, NationalNumber, PhoneNumber, ProjectId, IsSubscribed, CreatedDate)
        VALUES
            (@FirstName, @LastName, @CountryCode, @NationalNumber, @PhoneNumber, @ProjectId, @IsSubscribed, SYSUTCDATETIME());

        SELECT CAST(SCOPE_IDENTITY() AS INT) AS NewId;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

IF OBJECT_ID('dbo.sp_UpdateContact', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_UpdateContact;
GO
CREATE PROCEDURE dbo.sp_UpdateContact
    @ContactId       INT,
    @FirstName       NVARCHAR(50) = NULL,
    @LastName        NVARCHAR(50) = NULL,
    @CountryCode     NVARCHAR(5)  = NULL,
    @NationalNumber  NVARCHAR(20) = NULL,
    @PhoneNumber     NVARCHAR(25) = NULL,
    @ProjectId       INT          = NULL,
    @IsSubscribed    BIT          = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @ContactId IS NULL OR @ContactId <= 0
    BEGIN
        ;THROW 50001, 'Invalid @ContactId supplied to sp_UpdateContact.', 1;
        RETURN;
    END

    IF @ProjectId IS NOT NULL AND @ProjectId <= 0
    BEGIN
        ;THROW 50042, 'Invalid @ProjectId supplied to sp_UpdateContact.', 1;
        RETURN;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        IF @ProjectId IS NOT NULL
           AND NOT EXISTS (SELECT 1 FROM dbo.Projects WHERE ProjectId = @ProjectId)
        BEGIN
            ;THROW 50043, 'Referenced Project does not exist.', 1;
            ROLLBACK TRANSACTION;
            RETURN;
        END

        UPDATE TOP (1) dbo.Contacts
        SET    FirstName       = ISNULL(@FirstName,      FirstName),
               LastName        = ISNULL(@LastName,       LastName),
               CountryCode     = ISNULL(@CountryCode,    CountryCode),
               NationalNumber  = ISNULL(@NationalNumber, NationalNumber),
               PhoneNumber     = ISNULL(@PhoneNumber,    PhoneNumber),
               ProjectId       = ISNULL(@ProjectId,      ProjectId),
               IsSubscribed    = ISNULL(@IsSubscribed,   IsSubscribed),
               UpdatedDate     = CASE
                                    WHEN @FirstName       IS NOT NULL
                                      OR @LastName        IS NOT NULL
                                      OR @CountryCode     IS NOT NULL
                                      OR @NationalNumber  IS NOT NULL
                                      OR @PhoneNumber     IS NOT NULL
                                      OR @ProjectId       IS NOT NULL
                                      OR @IsSubscribed    IS NOT NULL
                                    THEN SYSUTCDATETIME()
                                    ELSE UpdatedDate
                                  END
        WHERE  ContactId = @ContactId;

        SELECT @@ROWCOUNT AS RowsAffected;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

IF OBJECT_ID('dbo.sp_DeleteContact', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_DeleteContact;
GO
CREATE PROCEDURE dbo.sp_DeleteContact
    @ContactId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DELETE FROM dbo.GroupContacts
        WHERE        ContactId = @ContactId;

        DELETE FROM dbo.Contacts
        WHERE        ContactId = @ContactId;

        SELECT @@ROWCOUNT AS RowsAffected;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- sp_GetContactsByProjectId and sp_GetContactsByGroupId are
-- already aligned with the Contact naming convention; we just
-- re-create them so the column aliases match the new ContactId
-- name.
IF OBJECT_ID('dbo.sp_GetContactsByProjectId', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetContactsByProjectId;
GO
CREATE PROCEDURE dbo.sp_GetContactsByProjectId
    @ProjectId INT
AS
BEGIN
    SET NOCOUNT ON;

    IF @ProjectId IS NULL OR @ProjectId <= 0
    BEGIN
        ;THROW 50044, 'Invalid @ProjectId supplied to sp_GetContactsByProjectId.', 1;
        RETURN;
    END

    SELECT ContactId,
           FirstName,
           LastName,
           CountryCode,
           NationalNumber,
           PhoneNumber,
           ProjectId,
           IsSubscribed,
           CreatedDate,
           UpdatedDate
    FROM   dbo.Contacts
    WHERE  ProjectId = @ProjectId
    ORDER BY ContactId DESC;
END
GO

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

    SELECT s.ContactId,
           s.FirstName,
           s.LastName,
           s.CountryCode,
           s.NationalNumber,
           s.PhoneNumber,
           s.ProjectId,
           s.IsSubscribed,
           s.CreatedDate,
           s.UpdatedDate
    FROM   dbo.Contacts      s
    INNER JOIN dbo.GroupContacts gc ON gc.ContactId = s.ContactId
    WHERE  gc.GroupId = @GroupId
    ORDER BY s.ContactId DESC;
END
GO

-- ----------------------------------------------------------------
-- 9. sp_AddContactToGroup still referenced dbo.Subscribers in its
--    original definition; re-create it so the lookup uses the
--    renamed Contacts table.
-- ----------------------------------------------------------------
IF OBJECT_ID('dbo.sp_AddContactToGroup', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_AddContactToGroup;
GO
CREATE PROCEDURE dbo.sp_AddContactToGroup
    @GroupId   INT,
    @ContactId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @GroupId   IS NULL OR @GroupId   <= 0
       OR @ContactId IS NULL OR @ContactId <= 0
    BEGIN
        ;THROW 50050, 'Invalid @GroupId / @ContactId supplied to sp_AddContactToGroup.', 1;
        RETURN;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @GroupProjectId   INT;
        DECLARE @ContactProjectId INT;

        SELECT @GroupProjectId   = ProjectId FROM dbo.Groups      WHERE GroupId   = @GroupId;
        SELECT @ContactProjectId = ProjectId FROM dbo.Contacts    WHERE ContactId = @ContactId;

        IF @GroupProjectId IS NULL
        BEGIN
            ;THROW 50051, 'Group does not exist.', 1;
            ROLLBACK TRANSACTION;
            RETURN;
        END

        IF @ContactProjectId IS NULL
        BEGIN
            ;THROW 50052, 'Contact does not exist.', 1;
            ROLLBACK TRANSACTION;
            RETURN;
        END

        IF @GroupProjectId <> @ContactProjectId
        BEGIN
            ;THROW 50053, 'Contact and Group must belong to the same Project.', 1;
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
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

PRINT 'Migration 08_RenameSubscribersToContacts.sql completed.';
GO
