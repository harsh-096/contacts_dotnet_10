-- ===========================================================
-- 02_CreateTable.sql
-- Table: Subscribers
-- Run after 01_CreateDatabase.sql
--
-- PhoneNumber storage layout:
--   CountryCode    -> includes the leading '+', e.g. '+91'  (NVARCHAR(5))
--   NationalNumber -> digits only, no country code,         (NVARCHAR(20))
--                     e.g. '9087648930'
--   PhoneNumber    -> full digits only (no '+'), equals
--                     REPLACE(CountryCode,'+','') + NationalNumber,
--                     e.g. '919087648930'                   (NVARCHAR(25))
--   The PhoneNumber column is UNIQUE (duplicates rejected).
-- ===========================================================

USE ContactsDB;
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Subscribers' AND xtype='U')
BEGIN
    CREATE TABLE Subscribers
    (
        Id              INT             IDENTITY(1,1) NOT NULL,
        FirstName       NVARCHAR(50)    NOT NULL,
        LastName        NVARCHAR(50)    NOT NULL,
        CountryCode     NVARCHAR(5)     NOT NULL,
        NationalNumber  NVARCHAR(20)    NOT NULL,
        PhoneNumber     NVARCHAR(25)    NOT NULL,
        IsSubscribed    BIT             NOT NULL     CONSTRAINT DF_Subscribers_IsSubscribed DEFAULT (1),
        CreatedDate     DATETIME2(7)    NOT NULL     CONSTRAINT DF_Subscribers_CreatedDate  DEFAULT (SYSUTCDATETIME()),
        UpdatedDate     DATETIME2(7)    NULL,

        CONSTRAINT PK_Subscribers PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Subscribers_PhoneNumber UNIQUE (PhoneNumber),
        CONSTRAINT CK_Subscribers_CountryCode_Plus
            CHECK (CountryCode LIKE '+%' AND LEN(CountryCode) BETWEEN 2 AND 5),
        CONSTRAINT CK_Subscribers_NationalNumber_Digits
            CHECK (NationalNumber NOT LIKE '%[^0-9]%' AND LEN(NationalNumber) >= 4),
        CONSTRAINT CK_Subscribers_PhoneNumber_Digits
            CHECK (PhoneNumber NOT LIKE '%[^0-9]%' AND LEN(PhoneNumber) >= 5),
        CONSTRAINT CK_Subscribers_PhoneNumber_Composition
            CHECK (PhoneNumber = REPLACE(CountryCode, '+', '') + NationalNumber)
    );
END
GO
