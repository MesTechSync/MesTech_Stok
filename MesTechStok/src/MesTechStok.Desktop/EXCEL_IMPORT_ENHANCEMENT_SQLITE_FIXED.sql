-- =================================================================
-- EXCEL IMPORT ENHANCEMENT FOR SQLITE
-- MesTechStok Desktop Application - SQLite Database Enhancements
-- Purpose: Add missing columns and indexes for Excel import functionality
-- Database: SQLite
-- =================================================================

-- SQLite doesn't support PRINT statements, using comments instead

-- =================================================================
-- 1. ADD MISSING PRODUCT COLUMNS FOR EXCEL IMPORT
-- =================================================================

-- Adding missing Product columns for Excel import...

-- Check and add Code column if it doesn't exist
ALTER TABLE Products ADD COLUMN Code TEXT;

-- Check and add ModifiedBy column if it doesn't exist  
ALTER TABLE Products ADD COLUMN ModifiedBy TEXT;

-- Check and add ImageUrl column if it doesn't exist
ALTER TABLE Products ADD COLUMN ImageUrl TEXT;

-- Check and add Color column if it doesn't exist
ALTER TABLE Products ADD COLUMN Color TEXT;

-- Check and add Brand column if it doesn't exist
ALTER TABLE Products ADD COLUMN Brand TEXT;

-- Check and add CreatedBy column if it doesn't exist
ALTER TABLE Products ADD COLUMN CreatedBy TEXT;

-- Check and add Icon column if it doesn't exist
ALTER TABLE Products ADD COLUMN Icon TEXT;

-- Check and add AdditionalImageUrls column if it doesn't exist
ALTER TABLE Products ADD COLUMN AdditionalImageUrls TEXT;

-- =================================================================
-- 2. ADD OPENCART INTEGRATION COLUMNS
-- =================================================================

-- Adding OpenCart integration columns...

-- Add OpenCart integration columns
ALTER TABLE Products ADD COLUMN OpenCartCategoryId INTEGER;

ALTER TABLE Products ADD COLUMN ParentCategoryId INTEGER;

ALTER TABLE Products ADD COLUMN ShowInMenu INTEGER DEFAULT 1;

ALTER TABLE Products ADD COLUMN SortOrder INTEGER DEFAULT 0;

ALTER TABLE Products ADD COLUMN LastSyncDate TEXT; -- SQLite stores dates as TEXT

ALTER TABLE Products ADD COLUMN SyncWithOpenCart INTEGER DEFAULT 1;

ALTER TABLE Products ADD COLUMN OpenCartProductId INTEGER;

-- =================================================================
-- 3. ADD REGULATORY AND COMPLIANCE COLUMNS
-- =================================================================

-- Adding regulatory and compliance columns...

-- Add regulatory compliance fields
ALTER TABLE Products ADD COLUMN TaxRate REAL DEFAULT 18.0;

ALTER TABLE Products ADD COLUMN UsageInstructions TEXT;

ALTER TABLE Products ADD COLUMN ImporterInfo TEXT;

ALTER TABLE Products ADD COLUMN ManufacturerInfo TEXT;

-- =================================================================
-- 4. ADD CONCURRENCY AND SYNC COLUMNS
-- =================================================================

-- Adding concurrency and sync columns...

-- Add synchronization and versioning columns
ALTER TABLE Products ADD COLUMN SyncedAt TEXT; -- SQLite stores dates as TEXT

ALTER TABLE Products ADD COLUMN LastModified TEXT DEFAULT (datetime('now')); -- SQLite datetime function

-- =================================================================
-- 5. CREATE INDEXES FOR PERFORMANCE
-- =================================================================

-- Creating performance indexes...

-- Create indexes for frequently queried columns
CREATE INDEX IF NOT EXISTS IX_Products_SKU ON Products(SKU);

CREATE INDEX IF NOT EXISTS IX_Products_Barcode ON Products(Barcode);

CREATE INDEX IF NOT EXISTS IX_Products_Code ON Products(Code);

CREATE INDEX IF NOT EXISTS IX_Products_Name ON Products(Name);

CREATE INDEX IF NOT EXISTS IX_Products_CategoryId ON Products(CategoryId);

CREATE INDEX IF NOT EXISTS IX_Products_WarehouseId ON Products(WarehouseId);

-- Composite indexes for common queries
CREATE INDEX IF NOT EXISTS IX_Products_Name_IsActive ON Products(Name, IsActive);

CREATE INDEX IF NOT EXISTS IX_Products_CategoryId_IsActive ON Products(CategoryId, IsActive);

-- =================================================================
-- 6. CREATE EXCEL IMPORT LOG TABLE
-- =================================================================

-- Creating Excel import log table...

-- Create table for tracking Excel import operations
CREATE TABLE IF NOT EXISTS ExcelImportLogs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    FileName TEXT NOT NULL,
    ImportedBy TEXT,
    ImportDate TEXT NOT NULL DEFAULT (datetime('now')),
    TotalRows INTEGER NOT NULL DEFAULT 0,
    SuccessfulRows INTEGER NOT NULL DEFAULT 0,
    FailedRows INTEGER NOT NULL DEFAULT 0,
    ErrorMessage TEXT,
    FileSize INTEGER NOT NULL DEFAULT 0,
    ProcessingTimeMs INTEGER NOT NULL DEFAULT 0,
    Status TEXT NOT NULL DEFAULT 'Started' 
        CHECK (Status IN ('Started', 'Completed', 'Failed', 'Cancelled'))
);

-- Indexes for Excel import logs
CREATE INDEX IF NOT EXISTS IX_ExcelImportLogs_ImportDate ON ExcelImportLogs(ImportDate);
CREATE INDEX IF NOT EXISTS IX_ExcelImportLogs_Status ON ExcelImportLogs(Status);
CREATE INDEX IF NOT EXISTS IX_ExcelImportLogs_ImportedBy ON ExcelImportLogs(ImportedBy);

-- =================================================================
-- 7. CREATE EXCEL IMPORT ERROR DETAILS TABLE
-- =================================================================

-- Creating Excel import error details table...

-- Create table for detailed error tracking during Excel import
CREATE TABLE IF NOT EXISTS ExcelImportErrorDetails (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ImportLogId INTEGER NOT NULL,
    RowNumber INTEGER NOT NULL,
    ColumnName TEXT,
    ErrorType TEXT NOT NULL 
        CHECK (ErrorType IN ('ValidationError', 'FormatError', 'DuplicateKey', 'ReferenceError', 'DataTooLong')),
    ErrorMessage TEXT NOT NULL,
