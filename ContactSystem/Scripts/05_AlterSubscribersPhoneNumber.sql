-- ===========================================================
-- 05_AlterSubscribersPhoneNumber.sql
-- Migration: split single PhoneNumber column into
--   CountryCode + NationalNumber + PhoneNumber (digits only).
--
-- Re-runnable / idempotent: every step checks current schema state
-- before applying. Safe to execute multiple times.
--
-- Run AFTER 02_CreateTable.sql / 03_StoredProcedures.sql have
-- already been applied at least once against an existing DB.
-- For a brand-new database, 02_CreateTable.sql already creates
-- the final shape and this script becomes a no-op.
--
-- Data-migration assumption
-- -------------------------
-- Existing PhoneNumber values are stored in E.164 form, starting
-- with a '+' (e.g. '+919087648930', '+12025551234').
-- Country-code length is auto-detected with the following rules:
--   * '+1...'  (NANP)         -> 1-digit country code  -> '+1'
--   * '+7...'  (Russia / KZ)  -> 1-digit country code  -> '+7'
--   * everything else         -> 2-digit country code  -> first 3 chars
-- 3-digit country codes (e.g. +971, +880, +351) will be assigned a
-- 2-digit country code by default; review such rows manually after
-- the migration if your data contains them.
-- ===========================================================

USE ContactsDB;
GO

SET XACT_ABORT ON;
SET NOCOUNT ON;
GO

-- ----------------------------------------------------------------
-- 1. Drop the old UNIQUE constraint on PhoneNumber (if present).
--    We must drop it before renaming / reshaping the column.
-- ----------------------------------------------------------------
IF EXISTS (
    SELECT 1
    FROM   sys.key_constraints
    WHERE  name = 'UQ_Subscribers_PhoneNumber'
      AND  parent_object_id = OBJECT_ID('dbo.Subscribers')
)
BEGIN
    ALTER TABLE dbo.Subscribers
        DROP CONSTRAINT UQ_Subscribers_PhoneNumber;
END
GO

-- ----------------------------------------------------------------
-- 2. Rename existing PhoneNumber -> PhoneNumberOld (only if a
--    pre-migration PhoneNumber column still exists and the new
--    triple has not been created yet).
-- ----------------------------------------------------------------
IF EXISTS (
        SELECT 1 FROM sys.columns
        WHERE  object_id = OBJECT_ID('dbo.Subscribers')
          AND  name = 'PhoneNumber'
    )
   AND NOT EXISTS (
        SELECT 1 FROM sys.columns
        WHERE  object_id = OBJECT_ID('dbo.Subscribers')
          AND  name = 'CountryCode'
    )
BEGIN
    EXEC sp_rename 'dbo.Subscribers.PhoneNumber', 'PhoneNumberOld', 'COLUMN';
END
GO

-- ----------------------------------------------------------------
-- 3. Add the three new columns (nullable for now so we can backfill).
-- ----------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Subscribers') AND name = 'CountryCode')
BEGIN
    ALTER TABLE dbo.Subscribers ADD CountryCode NVARCHAR(5) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Subscribers') AND name = 'NationalNumber')
BEGIN
    ALTER TABLE dbo.Subscribers ADD NationalNumber NVARCHAR(20) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Subscribers') AND name = 'PhoneNumber')
BEGIN
    ALTER TABLE dbo.Subscribers ADD PhoneNumber NVARCHAR(25) NULL;
END
GO

-- ----------------------------------------------------------------
-- 4. Backfill the new columns from PhoneNumberOld (best-effort
--    country-code detection: +1 and +7 = 1-digit, otherwise 2-digit).
-- ----------------------------------------------------------------
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Subscribers') AND name = 'PhoneNumberOld')
BEGIN
    UPDATE dbo.Subscribers
    SET    CountryCode =
               CASE
                   WHEN LEFT(PhoneNumberOld, 2) IN ('+1', '+7') THEN LEFT(PhoneNumberOld, 2)
                   ELSE LEFT(PhoneNumberOld, 3)
               END
    WHERE  PhoneNumberOld IS NOT NULL
      AND  CountryCode IS NULL;

    UPDATE dbo.Subscribers
    SET    NationalNumber = SUBSTRING(PhoneNumberOld,
                                      LEN(CountryCode) + 1,
                                      LEN(PhoneNumberOld) - LEN(CountryCode) + 1),
           PhoneNumber    = REPLACE(PhoneNumberOld, '+', '')
    WHERE  PhoneNumberOld IS NOT NULL
      AND  (NationalNumber IS NULL OR PhoneNumber IS NULL);
END
GO

-- ----------------------------------------------------------------
-- 5. Drop the legacy PhoneNumberOld column once data is migrated.
-- ----------------------------------------------------------------
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Subscribers') AND name = 'PhoneNumberOld')
BEGIN
    ALTER TABLE dbo.Subscribers DROP COLUMN PhoneNumberOld;
END
GO

-- ----------------------------------------------------------------
-- 6. Enforce NOT NULL on the three new columns now that they are populated.
-- ----------------------------------------------------------------
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE  object_id = OBJECT_ID('dbo.Subscribers')
      AND  name = 'CountryCode' AND is_nullable = 1
)
BEGIN
    ALTER TABLE dbo.Subscribers ALTER COLUMN CountryCode NVARCHAR(5) NOT NULL;
END
GO

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE  object_id = OBJECT_ID('dbo.Subscribers')
      AND  name = 'NationalNumber' AND is_nullable = 1
)
BEGIN
    ALTER TABLE dbo.Subscribers ALTER COLUMN NationalNumber NVARCHAR(20) NOT NULL;
END
GO

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE  object_id = OBJECT_ID('dbo.Subscribers')
      AND  name = 'PhoneNumber' AND is_nullable = 1
)
BEGIN
    ALTER TABLE dbo.Subscribers ALTER COLUMN PhoneNumber NVARCHAR(25) NOT NULL;
END
GO

-- ----------------------------------------------------------------
-- 7. Re-create the UNIQUE constraint on the new PhoneNumber.
-- ----------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1
    FROM   sys.key_constraints
    WHERE  name = 'UQ_Subscribers_PhoneNumber'
      AND  parent_object_id = OBJECT_ID('dbo.Subscribers')
)
BEGIN
    ALTER TABLE dbo.Subscribers
        ADD CONSTRAINT UQ_Subscribers_PhoneNumber UNIQUE (PhoneNumber);
END
GO

-- ----------------------------------------------------------------
-- 8. Add CHECK constraints that enforce the data shape contract.
-- ----------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE  name = 'CK_Subscribers_CountryCode_Plus'
      AND  parent_object_id = OBJECT_ID('dbo.Subscribers')
)
BEGIN
    ALTER TABLE dbo.Subscribers
        ADD CONSTRAINT CK_Subscribers_CountryCode_Plus
            CHECK (CountryCode LIKE '+%' AND LEN(CountryCode) BETWEEN 2 AND 5);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE  name = 'CK_Subscribers_NationalNumber_Digits'
      AND  parent_object_id = OBJECT_ID('dbo.Subscribers')
)
BEGIN
    ALTER TABLE dbo.Subscribers
        ADD CONSTRAINT CK_Subscribers_NationalNumber_Digits
            CHECK (NationalNumber NOT LIKE '%[^0-9]%' AND LEN(NationalNumber) >= 4);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE  name = 'CK_Subscribers_PhoneNumber_Digits'
      AND  parent_object_id = OBJECT_ID('dbo.Subscribers')
)
BEGIN
    ALTER TABLE dbo.Subscribers
        ADD CONSTRAINT CK_Subscribers_PhoneNumber_Digits
            CHECK (PhoneNumber NOT LIKE '%[^0-9]%' AND LEN(PhoneNumber) >= 5);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE  name = 'CK_Subscribers_PhoneNumber_Composition'
      AND  parent_object_id = OBJECT_ID('dbo.Subscribers')
)
BEGIN
    ALTER TABLE dbo.Subscribers
        ADD CONSTRAINT CK_Subscribers_PhoneNumber_Composition
            CHECK (PhoneNumber = REPLACE(CountryCode, '+', '') + NationalNumber);
END
GO

-- ----------------------------------------------------------------
-- 9. After altering the columns the stored procedures referencing
--    PhoneNumber must be re-deployed. Re-run 03_StoredProcedures.sql
--    to refresh sp_GetAllSubscribers / sp_GetSubscriberById /
--    sp_CreateSubscriber / sp_UpdateSubscriber / sp_DeleteSubscriber.
-- ----------------------------------------------------------------
PRINT 'Migration 05_AlterSubscribersPhoneNumber.sql completed. '
      + 'Now re-run 03_StoredProcedures.sql to refresh subscriber procedures.';
GO
