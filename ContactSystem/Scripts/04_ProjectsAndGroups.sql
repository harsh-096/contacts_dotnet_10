-- ===========================================================
-- 04_ProjectsAndGroups.sql
-- Tables : Projects, Groups
-- FK      : Groups.ProjectId -> Projects.ProjectId
-- Index   : IX_Groups_ProjectId (FK index, used by sp_GetGroupsByProjectId)
-- Run after 03_StoredProcedures.sql
-- ===========================================================

USE ContactsDB;
GO

-- ===========================================================
-- Table: Projects
-- ===========================================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Projects' AND xtype='U')
BEGIN
    CREATE TABLE Projects
    (
        ProjectId    INT          IDENTITY(1,1) NOT NULL,
        ProjectName  VARCHAR(255) NOT NULL,

        CONSTRAINT PK_Projects PRIMARY KEY CLUSTERED (ProjectId)
    );
END
GO

-- ===========================================================
-- Table: Groups
-- ===========================================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Groups' AND xtype='U')
BEGIN
    CREATE TABLE Groups
    (
        GroupId    INT          IDENTITY(1,1) NOT NULL,
        GroupName  VARCHAR(255) NOT NULL,
        ProjectId  INT          NULL,        -- nullable: a group may exist without a project

        CONSTRAINT PK_Groups PRIMARY KEY CLUSTERED (GroupId)
    );
END

-- Make Groups.ProjectId nullable for databases that already provisioned it as NOT NULL.
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Groups')
      AND name = 'ProjectId'
      AND is_nullable = 0
)
BEGIN
    ALTER TABLE dbo.Groups ALTER COLUMN ProjectId INT NULL;
END
GO

-- ===========================================================
-- Foreign Key: Groups.ProjectId -> Projects.ProjectId
-- ===========================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'FK_Groups_Projects_ProjectId'
      AND parent_object_id = OBJECT_ID('dbo.Groups')
)
BEGIN
    ALTER TABLE dbo.Groups
        ADD CONSTRAINT FK_Groups_Projects_ProjectId
        FOREIGN KEY (ProjectId)
        REFERENCES  dbo.Projects (ProjectId);
END
GO

-- ===========================================================
-- Index: IX_Groups_ProjectId (FK index, used by sp_GetGroupsByProjectId)
-- ===========================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Groups_ProjectId'
      AND object_id = OBJECT_ID('dbo.Groups')
)
BEGIN
    CREATE INDEX IX_Groups_ProjectId
        ON dbo.Groups (ProjectId);
END
GO

-- ===========================================================
-- sp_GetAllProjects
-- ===========================================================
IF OBJECT_ID('dbo.sp_GetAllProjects', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetAllProjects;
GO

CREATE PROCEDURE dbo.sp_GetAllProjects
AS
BEGIN
    SET NOCOUNT ON;

    SELECT ProjectId,
           ProjectName
    FROM   dbo.Projects
    ORDER BY ProjectId DESC;
END
GO

-- ===========================================================
-- sp_GetProjectById
-- ===========================================================
IF OBJECT_ID('dbo.sp_GetProjectById', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetProjectById;
GO

CREATE PROCEDURE dbo.sp_GetProjectById
    @ProjectId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT ProjectId,
           ProjectName
    FROM   dbo.Projects
    WHERE  ProjectId = @ProjectId;
END
GO

-- ===========================================================
-- sp_CreateProject
-- Returns the newly inserted Id via SELECT.
-- ===========================================================
IF OBJECT_ID('dbo.sp_CreateProject', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_CreateProject;
GO

CREATE PROCEDURE dbo.sp_CreateProject
    @ProjectName VARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO dbo.Projects (ProjectName)
        VALUES (@ProjectName);

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
-- sp_UpdateProject
-- Updates EXACTLY ONE row identified by @ProjectId.
-- Returns the number of affected rows (0 = id not found, 1 = updated).
--
-- PATCH-style semantics: any parameter passed as NULL is treated as
-- "leave this column unchanged". Pass a value to overwrite.
--
-- Defenses:
--   * @ProjectId is rejected if NULL or <= 0.
--   * TOP (1) guarantees a single-row update even if the PK is ever
--     altered in the future.
-- ===========================================================
IF OBJECT_ID('dbo.sp_UpdateProject', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_UpdateProject;
GO

CREATE PROCEDURE dbo.sp_UpdateProject
    @ProjectId   INT          = NULL,
    @ProjectName VARCHAR(255) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @ProjectId IS NULL OR @ProjectId <= 0
    BEGIN
        ;THROW 50010, 'Invalid @ProjectId supplied to sp_UpdateProject.', 1;
        RETURN;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE TOP (1) dbo.Projects
        SET    ProjectName = ISNULL(@ProjectName, ProjectName)
        WHERE  ProjectId = @ProjectId;

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
-- sp_DeleteProject
-- Returns the number of affected rows.
-- ===========================================================
IF OBJECT_ID('dbo.sp_DeleteProject', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_DeleteProject;
GO

CREATE PROCEDURE dbo.sp_DeleteProject
    @ProjectId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DELETE FROM dbo.Projects
        WHERE        ProjectId = @ProjectId;

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
-- sp_GetAllGroups
-- ===========================================================
IF OBJECT_ID('dbo.sp_GetAllGroups', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetAllGroups;
GO

CREATE PROCEDURE dbo.sp_GetAllGroups
AS
BEGIN
    SET NOCOUNT ON;

    SELECT GroupId,
           GroupName,
           ProjectId
    FROM   dbo.Groups
    ORDER BY GroupId DESC;
END
GO

-- ===========================================================
-- sp_GetGroupById
-- ===========================================================
IF OBJECT_ID('dbo.sp_GetGroupById', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetGroupById;
GO

CREATE PROCEDURE dbo.sp_GetGroupById
    @GroupId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT GroupId,
           GroupName,
           ProjectId
    FROM   dbo.Groups
    WHERE  GroupId = @GroupId;
END
GO

-- ===========================================================
-- sp_CreateGroup
-- Returns the newly inserted Id via SELECT.
-- ===========================================================
IF OBJECT_ID('dbo.sp_CreateGroup', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_CreateGroup;
GO

CREATE PROCEDURE dbo.sp_CreateGroup
    @GroupName  VARCHAR(255),
    @ProjectId  INT          = NULL      -- optional: NULL allowed, otherwise must be > 0
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    -- @ProjectId is optional. If a value is supplied it must be > 0.
    IF @ProjectId IS NOT NULL AND @ProjectId <= 0
    BEGIN
        ;THROW 50020, 'Invalid @ProjectId supplied to sp_CreateGroup.', 1;
        RETURN;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

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
-- sp_UpdateGroup
-- Updates EXACTLY ONE row identified by @GroupId.
-- Returns the number of affected rows (0 = id not found, 1 = updated).
--
-- PATCH-style semantics: any parameter passed as NULL is treated as
-- "leave this column unchanged". Pass a value to overwrite.
--
-- Defenses:
--   * @GroupId is rejected if NULL or <= 0.
--   * TOP (1) guarantees a single-row update even if the PK is ever
--     altered in the future.
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

    BEGIN TRY
        BEGIN TRANSACTION;

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
-- sp_DeleteGroup
-- Returns the number of affected rows.
-- ===========================================================
IF OBJECT_ID('dbo.sp_DeleteGroup', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_DeleteGroup;
GO

CREATE PROCEDURE dbo.sp_DeleteGroup
    @GroupId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DELETE FROM dbo.Groups
        WHERE        GroupId = @GroupId;

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
-- sp_GetGroupsByProjectId
-- Returns every group whose ProjectId matches @ProjectId.
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

    SELECT GroupId,
           GroupName,
           ProjectId
    FROM   dbo.Groups
    WHERE  ProjectId = @ProjectId
    ORDER BY GroupId DESC;
END
GO
