-- ===========================================================
-- 02_CreateTable.sql
-- Table: Subscribers
-- Run after 01_CreateDatabase.sql
-- ===========================================================

USE ContactsDB;
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Subscribers' AND xtype='U')
BEGIN
    CREATE TABLE Subscribers
    (
        Id             INT             IDENTITY(1,1) NOT NULL,
        FirstName      NVARCHAR(50)    NOT NULL,
        LastName       NVARCHAR(50)    NOT NULL,
        PhoneNumber    NVARCHAR(20)    NOT NULL,
        IsSubscribed   BIT             NOT NULL     CONSTRAINT DF_Subscribers_IsSubscribed DEFAULT (1),
        CreatedDate    DATETIME2(7)    NOT NULL     CONSTRAINT DF_Subscribers_CreatedDate  DEFAULT (SYSUTCDATETIME()),
        UpdatedDate    DATETIME2(7)    NULL,

        CONSTRAINT PK_Subscribers PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Subscribers_PhoneNumber UNIQUE (PhoneNumber)
    );
END
GO
