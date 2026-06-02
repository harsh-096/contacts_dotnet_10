-- ===========================================================
-- 01_CreateDatabase.sql
-- Database: ContactsDB
-- Run this script first. It will create the database if it
-- does not already exist and switch context to it.
-- ===========================================================

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'ContactsDB')
BEGIN
    CREATE DATABASE ContactsDB;
END
GO

USE ContactsDB;
GO
