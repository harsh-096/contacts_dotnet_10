-- ===========================================================
-- 07_UpdateSubscribersAndGroupContactsSPs.sql
--
-- Updates the Subscribers stored procedures so they include
-- ProjectId, adds sp_GetContactsByProjectId, and creates the
-- GroupContacts (junction) procedures:
--   sp_AddContactToGroup
--   sp_RemoveContactFromGroup
--   sp_GetContactsByGroupId
--   sp_GetGroupsByContactId
--
-- Re-runnable / idempotent. Run AFTER 06_ProjectContactsAndGroupContacts.sql.
--
-- Conventions
--   * Parameterized inputs only (no string concatenation)
--   * SET NOCOUNT ON
--   * SET XACT_ABORT ON
--   * BEGIN TRY / BEGIN CATCH / BEGIN TRANSACTION
--   * Duplicate mappings rejected
--   * Cross-project assignment rejected (contact and group must
--     share the same ProjectId; UQ_Groups_ProjectId + the
--     Subscribers FK are the ultimate safety net)
-- ===========================================================

USE ContactsDB;
GO

SET XACT_ABORT ON;
SET NOCOUNT ON;
GO

-- ===========================================================
-- sp_GetAllSubscribers  (now returns ProjectId)
-- ===========================================================
IF OBJECT_ID('dbo.sp_GetAllSubscribers', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetAllSubscribers;
GO

CREATE PROCEDURE dbo.sp_GetAllSubscribers
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id,
           FirstName,
           LastName,
           CountryCode,
           NationalNumber,
           PhoneNumber,
           ProjectId,
           IsSubscribed,
           CreatedDate,
           UpdatedDate
    FROM   dbo.Subscribers
    ORDER BY Id DESC;
END
GO

-- ===========================================================
-- sp_GetSubscriberById  (now returns ProjectId)
-- ===========================================================
IF OBJECT_ID('dbo.sp_GetSubscriberById', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetSubscriberById;
GO

CREATE PROCEDURE dbo.sp_GetSubscriberById
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id,
           FirstName,
           LastName,
           CountryCode,
           NationalNumber,
           PhoneNumber,
           ProjectId,
           IsSubscribed,
           CreatedDate,
           UpdatedDate
    FROM   dbo.Subscribers
    WHERE  Id = @Id;
END
GO

-- ===========================================================
-- sp_CreateSubscriber  (ProjectId is REQUIRED)
-- Returns the new Id via SELECT.
-- ===========================================================
IF OBJECT_ID('dbo.sp_CreateSubscriber', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_CreateSubscriber;
GO

CREATE PROCEDURE dbo.sp_CreateSubscriber
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
        ;THROW 50040, 'Invalid @ProjectId supplied to sp_CreateSubscriber.', 1;
        RETURN;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Defense in depth: the project must exist.
        IF NOT EXISTS (SELECT 1 FROM dbo.Projects WHERE ProjectId = @ProjectId)
        BEGIN
            ;THROW 50041, 'Referenced Project does not exist.', 1;
            ROLLBACK TRANSACTION;
            RETURN;
        END

        INSERT INTO dbo.Subscribers
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

-- ===========================================================
-- sp_UpdateSubscriber  (ProjectId becomes patchable)
-- Returns affected-row count via SELECT.
-- ===========================================================
IF OBJECT_ID('dbo.sp_UpdateSubscriber', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_UpdateSubscriber;
GO

CREATE PROCEDURE dbo.sp_UpdateSubscriber
    @Id              INT,
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

    IF @Id IS NULL OR @Id <= 0
    BEGIN
        ;THROW 50001, 'Invalid @Id supplied to sp_UpdateSubscriber.', 1;
        RETURN;
    END

    IF @ProjectId IS NOT NULL AND @ProjectId <= 0
    BEGIN
        ;THROW 50042, 'Invalid @ProjectId supplied to sp_UpdateSubscriber.', 1;
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

        UPDATE TOP (1) dbo.Subscribers
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
        WHERE  Id = @Id;

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

-- ===========================================================
-- sp_DeleteSubscriber  (no schema change)
-- ===========================================================
IF OBJECT_ID('dbo.sp_DeleteSubscriber', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_DeleteSubscriber;
GO

CREATE PROCEDURE dbo.sp_DeleteSubscriber
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DELETE FROM dbo.Subscribers
        WHERE        Id = @Id;

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

-- ===========================================================
-- sp_GetContactsByProjectId
-- Returns every subscriber that belongs to a project.
-- ===========================================================
IF OBJECT_ID('dbo.sp_GetContactsByProjectId', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetContactsByProjectId;
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

    SELECT Id,
           FirstName,
           LastName,
           CountryCode,
           NationalNumber,
           PhoneNumber,
           ProjectId,
           IsSubscribed,
           CreatedDate,
           UpdatedDate
    FROM   dbo.Subscribers
    WHERE  ProjectId = @ProjectId
    ORDER BY Id DESC;
END
GO

-- ===========================================================
-- sp_CreateGroup  (ProjectId is now REQUIRED; UNIQUE(project_id) enforced)
-- ===========================================================
IF OBJECT_ID('dbo.sp_CreateGroup', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_CreateGroup;
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

        -- Enforce "one project = one group" before the UNIQUE index does.
        IF EXISTS (SELECT 1 FROM dbo.Groups WHERE ProjectId = @ProjectId)
        BEGIN
            ;THROW 50046, 'A group already exists for this project (one project can have only one group).', 1;
            ROLLBACK TRANSACTION;
            RETURN;
        END

        INSERT INTO dbo.Groups (GroupName, ProjectId)
        VALUES (@GroupName, @ProjectId);

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

-- ===========================================================
-- sp_UpdateGroup  (ProjectId patchable, must remain UNIQUE)
-- ===========================================================
IF OBJECT_ID('dbo.sp_UpdateGroup', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_UpdateGroup;
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

        -- Resolve the effective ProjectId to detect the unique-conflict case.
        DECLARE @EffectiveProjectId INT =
            COALESCE(@ProjectId, (SELECT ProjectId FROM dbo.Groups WHERE GroupId = @GroupId));

        IF @EffectiveProjectId IS NOT NULL
           AND EXISTS (
                SELECT 1 FROM dbo.Groups
                WHERE  ProjectId = @EffectiveProjectId
                  AND  GroupId  <> @GroupId
           )
        BEGIN
            ;THROW 50049, 'A different group already exists for the target project (one project can have only one group).', 1;
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
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- ===========================================================
-- sp_GetGroupsByProjectId  (re-defined for clarity; same name)
-- ===========================================================
IF OBJECT_ID('dbo.sp_GetGroupsByProjectId', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetGroupsByProjectId;
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

    -- With UNIQUE(ProjectId) this returns 0 or 1 row.
    SELECT GroupId,
           GroupName,
           ProjectId
    FROM   dbo.Groups
    WHERE  ProjectId = @ProjectId
    ORDER BY GroupId DESC;
END
GO

-- ===========================================================
-- GroupContacts procedures
-- ===========================================================

-- sp_AddContactToGroup
--   * Rejects duplicate mappings (PK already enforces it; this
--     produces a friendly error message via TRY/CATCH).
--   * Rejects mismatched project assignments.
-- ===========================================================
IF OBJECT_ID('dbo.sp_AddContactToGroup', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_AddContactToGroup;
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
        SELECT @ContactProjectId = ProjectId FROM dbo.Subscribers WHERE Id        = @ContactId;

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

-- sp_RemoveContactFromGroup
-- ===========================================================
IF OBJECT_ID('dbo.sp_RemoveContactFromGroup', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_RemoveContactFromGroup;
GO

CREATE PROCEDURE dbo.sp_RemoveContactFromGroup
    @GroupId   INT,
    @ContactId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @GroupId   IS NULL OR @GroupId   <= 0
       OR @ContactId IS NULL OR @ContactId <= 0
    BEGIN
        ;THROW 50055, 'Invalid @GroupId / @ContactId supplied to sp_RemoveContactFromGroup.', 1;
        RETURN;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        DELETE FROM dbo.GroupContacts
        WHERE  GroupId   = @GroupId
          AND  ContactId = @ContactId;

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

-- sp_GetContactsByGroupId
-- Returns the full subscriber rows (including phone numbers)
-- that are members of the supplied group.
-- ===========================================================
IF OBJECT_ID('dbo.sp_GetContactsByGroupId', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetContactsByGroupId;
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

    SELECT s.Id,
           s.FirstName,
           s.LastName,
           s.CountryCode,
           s.NationalNumber,
           s.PhoneNumber,
           s.ProjectId,
           s.IsSubscribed,
           s.CreatedDate,
           s.UpdatedDate
    FROM   dbo.Subscribers  s
    INNER JOIN dbo.GroupContacts gc ON gc.ContactId = s.Id
    WHERE  gc.GroupId = @GroupId
    ORDER BY s.Id DESC;
END
GO

-- sp_GetGroupsByContactId
-- Returns every group that the supplied contact is a member of.
-- ===========================================================
IF OBJECT_ID('dbo.sp_GetGroupsByContactId', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetGroupsByContactId;
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

    SELECT g.GroupId,
           g.GroupName,
           g.ProjectId
    FROM   dbo.Groups        g
    INNER JOIN dbo.GroupContacts gc ON gc.GroupId = g.GroupId
    WHERE  gc.ContactId = @ContactId
    ORDER BY g.GroupId DESC;
END
GO

PRINT 'Migration 07_UpdateSubscribersAndGroupContactsSPs.sql completed.';
GO
