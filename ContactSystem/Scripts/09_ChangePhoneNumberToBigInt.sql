-- ===========================================================
-- 09_ChangePhoneNumberToBigInt.sql
--
-- Migration: change dbo.Contacts.PhoneNumber from NVARCHAR(25)
--            to BIGINT.
--
-- Why
-- ---
-- Phone numbers are conceptually numeric (E.164 digits only,
-- no sign, no decimals). Storing them as BIGINT is more compact
-- (8 bytes vs ~50 bytes with NVARCHAR) and aligns the storage
-- with the data shape that the API already enforces via
--     CK_Contacts_PhoneNumber_Digits     (digits only)
--     CK_Contacts_PhoneNumber_Composition
--         (CountryCode without '+' + NationalNumber)
-- Both of those CHECK constraints operate on the string form;
-- they no longer apply once the column is numeric and are
-- dropped here. The digits-only guarantee is now inherent to
-- the BIGINT data type.
--
-- Re-runnable / idempotent. Safe to execute multiple times.
-- Run AFTER 08_RenameSubscribersToContacts.sql.
--
-- Pre-requisites / data safety
-- ----------------------------
-- * All existing PhoneNumber values must be parseable as
--   BIGINT. E.164 digits only -> max 15 digits, well within
--   BIGINT (max 19 digits). Existing data migrates safely
--   via the implicit conversion that ALTER COLUMN performs.
-- * If any row has a non-numeric PhoneNumber this script
--   will fail at ALTER COLUMN with a conversion error; the
--   operator must clean the data first.
-- ===========================================================

USE ContactsDB;
GO

SET XACT_ABORT ON;
SET NOCOUNT ON;
GO

-- ----------------------------------------------------------------
-- 1. Drop the two string-form CHECK constraints. They reference
--    string functions (LIKE, REPLACE, LEN) that don't apply to
--    a numeric column.
-- ----------------------------------------------------------------
IF EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE  name = 'CK_Contacts_PhoneNumber_Digits'
       AND parent_object_id = OBJECT_ID('dbo.Contacts')
)
BEGIN
    ALTER TABLE dbo.Contacts DROP CONSTRAINT CK_Contacts_PhoneNumber_Digits;
END
GO

IF EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE  name = 'CK_Contacts_PhoneNumber_Composition'
       AND parent_object_id = OBJECT_ID('dbo.Contacts')
)
BEGIN
    ALTER TABLE dbo.Contacts DROP CONSTRAINT CK_Contacts_PhoneNumber_Composition;
END
GO

-- ----------------------------------------------------------------
-- 2. Drop the UNIQUE constraint so we can change the column
--    type. SQL Server cannot change the type of a column that
--    participates in an index (UNIQUE) without rebuilding the
--    index, so we drop and recreate it explicitly.
-- ----------------------------------------------------------------
IF EXISTS (
    SELECT 1 FROM sys.key_constraints
    WHERE  name = 'UQ_Contacts_PhoneNumber'
       AND parent_object_id = OBJECT_ID('dbo.Contacts')
)
BEGIN
    ALTER TABLE dbo.Contacts DROP CONSTRAINT UQ_Contacts_PhoneNumber;
END
GO

-- ----------------------------------------------------------------
-- 3. ALTER COLUMN to BIGINT NOT NULL.
--    Existing NVARCHAR digits-only values are converted implicitly.
-- ----------------------------------------------------------------
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE  object_id = OBJECT_ID('dbo.Contacts')
       AND  name      = 'PhoneNumber'
       AND  system_type_id <> 127   -- 127 = bigint
)
BEGIN
    ALTER TABLE dbo.Contacts
        ALTER COLUMN PhoneNumber BIGINT NOT NULL;
END
GO

-- ----------------------------------------------------------------
-- 4. Re-create the UNIQUE constraint on the numeric column.
--    UNIQUE on a BIGINT column is backed by a b-tree index of
--    8-byte keys.
-- ----------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.key_constraints
    WHERE  name = 'UQ_Contacts_PhoneNumber'
       AND parent_object_id = OBJECT_ID('dbo.Contacts')
)
BEGIN
    ALTER TABLE dbo.Contacts
        ADD CONSTRAINT UQ_Contacts_PhoneNumber UNIQUE (PhoneNumber);
END
GO

-- ----------------------------------------------------------------
-- 5. Add a defensive CHECK that the stored number is positive.
--    BIGINT allows negatives, but a phone number must be > 0.
-- ----------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE  name = 'CK_Contacts_PhoneNumber_Positive'
       AND parent_object_id = OBJECT_ID('dbo.Contacts')
)
BEGIN
    ALTER TABLE dbo.Contacts
        ADD CONSTRAINT CK_Contacts_PhoneNumber_Positive
            CHECK (PhoneNumber > 0);
END
GO

-- ----------------------------------------------------------------
-- 6. Re-create the stored procedures so their @PhoneNumber
--    parameter is BIGINT. Only the procedures that take a
--    PhoneNumber parameter are touched; the SELECT-only
--    procedures work unchanged because the column type is
--    reflected automatically.
-- ----------------------------------------------------------------

-- sp_CreateContact -------------------------------------------------
IF OBJECT_ID('dbo.sp_CreateContact', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_CreateContact;
GO
CREATE PROCEDURE dbo.sp_CreateContact
    @FirstName       NVARCHAR(50),
    @LastName        NVARCHAR(50),
    @CountryCode     NVARCHAR(5),
    @NationalNumber  NVARCHAR(20),
    @PhoneNumber     BIGINT,
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

    IF @PhoneNumber IS NULL OR @PhoneNumber <= 0
    BEGIN
        ;THROW 50060, 'Invalid @PhoneNumber supplied to sp_CreateContact (must be > 0).', 1;
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

-- sp_UpdateContact -------------------------------------------------
IF OBJECT_ID('dbo.sp_UpdateContact', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_UpdateContact;
GO
CREATE PROCEDURE dbo.sp_UpdateContact
    @ContactId       INT,
    @FirstName       NVARCHAR(50) = NULL,
    @LastName        NVARCHAR(50) = NULL,
    @CountryCode     NVARCHAR(5)  = NULL,
    @NationalNumber  NVARCHAR(20) = NULL,
    @PhoneNumber     BIGINT       = NULL,
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

    IF @PhoneNumber IS NOT NULL AND @PhoneNumber <= 0
    BEGIN
        ;THROW 50061, 'Invalid @PhoneNumber supplied to sp_UpdateContact (must be > 0 when provided).', 1;
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

PRINT 'Migration 09_ChangePhoneNumberToBigInt.sql completed.';
GO
