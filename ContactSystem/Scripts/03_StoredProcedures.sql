-- ===========================================================
-- 03_StoredProcedures.sql
-- Run after 02_CreateTable.sql
--
-- Subscribers procedures use the normalized phone layout:
--   CountryCode    NVARCHAR(5)   -- includes '+', e.g. '+91'
--   NationalNumber NVARCHAR(20)  -- digits only,  e.g. '9087648930'
--   PhoneNumber    NVARCHAR(25)  -- digits only,  e.g. '919087648930'
-- ===========================================================

USE ContactsDB;
GO

-- ===========================================================
-- sp_GetAllSubscribers
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
           IsSubscribed,
           CreatedDate,
           UpdatedDate
    FROM   dbo.Subscribers
    ORDER BY Id DESC;
END
GO

-- ===========================================================
-- sp_GetSubscriberById
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
           IsSubscribed,
           CreatedDate,
           UpdatedDate
    FROM   dbo.Subscribers
    WHERE  Id = @Id;
END
GO

-- ===========================================================
-- sp_CreateSubscriber
-- Returns the newly inserted Id via SELECT.
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
    @IsSubscribed    BIT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO dbo.Subscribers
            (FirstName, LastName, CountryCode, NationalNumber, PhoneNumber, IsSubscribed, CreatedDate)
        VALUES
            (@FirstName, @LastName, @CountryCode, @NationalNumber, @PhoneNumber, @IsSubscribed, SYSUTCDATETIME());

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
-- sp_UpdateSubscriber
-- Updates EXACTLY ONE row identified by @Id.
-- Returns the number of affected rows (0 = id not found, 1 = updated).
--
-- PATCH-style semantics: any parameter passed as NULL is treated as
-- "leave this column unchanged". Pass a value to overwrite.
--
-- Note: CountryCode / NationalNumber / PhoneNumber are independent
-- columns at the DB level. The service layer is responsible for
-- recomputing PhoneNumber whenever CountryCode or NationalNumber
-- changes and for passing a consistent triple to this procedure.
--
-- Defenses:
--   * @Id is rejected if NULL or <= 0.
--   * TOP (1) guarantees a single-row update even if the PK is ever
--     altered in the future.
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

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE TOP (1) dbo.Subscribers
        SET    FirstName       = ISNULL(@FirstName,      FirstName),
               LastName        = ISNULL(@LastName,       LastName),
               CountryCode     = ISNULL(@CountryCode,    CountryCode),
               NationalNumber  = ISNULL(@NationalNumber, NationalNumber),
               PhoneNumber     = ISNULL(@PhoneNumber,    PhoneNumber),
               IsSubscribed    = ISNULL(@IsSubscribed,   IsSubscribed),
               -- Bump UpdatedDate only when at least one field was actually provided.
               UpdatedDate     = CASE
                                   WHEN @FirstName       IS NOT NULL
                                     OR @LastName        IS NOT NULL
                                     OR @CountryCode     IS NOT NULL
                                     OR @NationalNumber  IS NOT NULL
                                     OR @PhoneNumber     IS NOT NULL
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
-- sp_DeleteSubscriber
-- Returns the number of affected rows.
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
