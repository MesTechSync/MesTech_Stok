-- =================================================================
-- EXCEL IMPORT ENHANCEMENT FOR SQL SERVER
-- MesTechStok Desktop Application - SQL Server Database Enhancements
-- Purpose: Add missing columns and indexes for Excel import functionality
-- Database: SQL Server
-- =================================================================

USE MesTechStok;
GO

PRINT 'Starting Excel Import Enhancement...';

-- =================================================================
-- 1. ADD MISSING PRODUCT COLUMNS FOR EXCEL IMPORT
-- =================================================================

PRINT '1. Adding missing Product columns for Excel import...';

-- Check and add Code column if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'Code')
BEGIN
    ALTER TABLE Products ADD Code NVARCHAR(100) NULL;
    PRINT '✅ Code column added';
END
ELSE
    PRINT '⏭ Code column already exists';

-- Check and add ModifiedBy column if it doesn't exist  
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'ModifiedBy')
BEGIN
    ALTER TABLE Products ADD ModifiedBy NVARCHAR(100) NULL;
    PRINT '✅ ModifiedBy column added';
END
ELSE
    PRINT '⏭ ModifiedBy column already exists';

-- Check and add ImageUrl column if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'ImageUrl')
BEGIN
    ALTER TABLE Products ADD ImageUrl NVARCHAR(1000) NULL;
    PRINT '✅ ImageUrl column added';
END
ELSE
    PRINT '⏭ ImageUrl column already exists';

-- Check and add Color column if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'Color')
BEGIN
    ALTER TABLE Products ADD Color NVARCHAR(50) NULL;
    PRINT '✅ Color column added';
END
ELSE
    PRINT '⏭ Color column already exists';

-- Check and add Brand column if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'Brand')
BEGIN
    ALTER TABLE Products ADD Brand NVARCHAR(100) NULL;
    PRINT '✅ Brand column added';
END
ELSE
    PRINT '⏭ Brand column already exists';

-- Check and add CreatedBy column if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'CreatedBy')
BEGIN
    ALTER TABLE Products ADD CreatedBy NVARCHAR(100) NULL;
    PRINT '✅ CreatedBy column added';
END
ELSE
    PRINT '⏭ CreatedBy column already exists';

-- Check and add Icon column if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'Icon')
BEGIN
    ALTER TABLE Products ADD Icon NVARCHAR(500) NULL;
    PRINT '✅ Icon column added';
END
ELSE
    PRINT '⏭ Icon column already exists';

-- Check and add AdditionalImageUrls column if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'AdditionalImageUrls')
BEGIN
    ALTER TABLE Products ADD AdditionalImageUrls NVARCHAR(MAX) NULL;
    PRINT '✅ AdditionalImageUrls column added';
END
ELSE
    PRINT '⏭ AdditionalImageUrls column already exists';

-- =================================================================
-- 2. ADD OPENCART INTEGRATION COLUMNS
-- =================================================================

PRINT '2. Adding OpenCart integration columns...';

-- Add OpenCart integration columns
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'OpenCartCategoryId')
BEGIN
    ALTER TABLE Products ADD OpenCartCategoryId INT NULL;
    PRINT '✅ OpenCartCategoryId column added';
END
ELSE
    PRINT '⏭ OpenCartCategoryId column already exists';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'ParentCategoryId')
BEGIN
    ALTER TABLE Products ADD ParentCategoryId INT NULL;
    PRINT '✅ ParentCategoryId column added';
END
ELSE
    PRINT '⏭ ParentCategoryId column already exists';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'ShowInMenu')
BEGIN
    ALTER TABLE Products ADD ShowInMenu BIT NOT NULL DEFAULT 1;
    PRINT '✅ ShowInMenu column added';
END
ELSE
    PRINT '⏭ ShowInMenu column already exists';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'SortOrder')
BEGIN
    ALTER TABLE Products ADD SortOrder INT NOT NULL DEFAULT 0;
    PRINT '✅ SortOrder column added';
END
ELSE
    PRINT '⏭ SortOrder column already exists';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'LastSyncDate')
BEGIN
    ALTER TABLE Products ADD LastSyncDate DATETIME2 NULL;
    PRINT '✅ LastSyncDate column added';
END
ELSE
    PRINT '⏭ LastSyncDate column already exists';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'SyncWithOpenCart')
BEGIN
    ALTER TABLE Products ADD SyncWithOpenCart BIT NOT NULL DEFAULT 1;
    PRINT '✅ SyncWithOpenCart column added';
END
ELSE
    PRINT '⏭ SyncWithOpenCart column already exists';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'OpenCartProductId')
BEGIN
    ALTER TABLE Products ADD OpenCartProductId INT NULL;
    PRINT '✅ OpenCartProductId column added';
END
ELSE
    PRINT '⏭ OpenCartProductId column already exists';

-- =================================================================
-- 3. ADD REGULATORY AND COMPLIANCE COLUMNS
-- =================================================================

PRINT '3. Adding regulatory and compliance columns...';

-- Add regulatory compliance fields
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'TaxRate')
BEGIN
    ALTER TABLE Products ADD TaxRate DECIMAL(5,2) NOT NULL DEFAULT 18.0;
    PRINT '✅ TaxRate column added';
END
ELSE
    PRINT '⏭ TaxRate column already exists';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'UsageInstructions')
BEGIN
    ALTER TABLE Products ADD UsageInstructions NVARCHAR(1000) NULL;
    PRINT '✅ UsageInstructions column added';
END
ELSE
    PRINT '⏭ UsageInstructions column already exists';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'ImporterInfo')
BEGIN
    ALTER TABLE Products ADD ImporterInfo NVARCHAR(255) NULL;
    PRINT '✅ ImporterInfo column added';
END
ELSE
    PRINT '⏭ ImporterInfo column already exists';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'ManufacturerInfo')
BEGIN
    ALTER TABLE Products ADD ManufacturerInfo NVARCHAR(255) NULL;
    PRINT '✅ ManufacturerInfo column added';
END
ELSE
    PRINT '⏭ ManufacturerInfo column already exists';

-- =================================================================
-- 4. ADD CONCURRENCY AND SYNC COLUMNS
-- =================================================================

PRINT '4. Adding concurrency and sync columns...';

-- Add synchronization and versioning columns
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'SyncedAt')
BEGIN
    ALTER TABLE Products ADD SyncedAt DATETIME2 NULL;
    PRINT '✅ SyncedAt column added';
END
ELSE
    PRINT '⏭ SyncedAt column already exists';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'LastModified')
BEGIN
    ALTER TABLE Products ADD LastModified DATETIME2 NOT NULL DEFAULT GETDATE();
    PRINT '✅ LastModified column added';
END
ELSE
    PRINT '⏭ LastModified column already exists';

-- =================================================================
-- 5. CREATE INDEXES FOR PERFORMANCE
-- =================================================================

PRINT '5. Creating performance indexes...';

-- Create indexes for frequently queried columns
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Products_SKU' AND object_id = OBJECT_ID('Products'))
BEGIN
    CREATE INDEX IX_Products_SKU ON Products(SKU);
    PRINT '✅ SKU index created';
END
ELSE
    PRINT '⏭ SKU index already exists';

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Products_Barcode' AND object_id = OBJECT_ID('Products'))
BEGIN
    CREATE INDEX IX_Products_Barcode ON Products(Barcode);
    PRINT '✅ Barcode index created';
END
ELSE
    PRINT '⏭ Barcode index already exists';

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Products_Code' AND object_id = OBJECT_ID('Products'))
BEGIN
    CREATE INDEX IX_Products_Code ON Products(Code);
    PRINT '✅ Code index created';
END
ELSE
    PRINT '⏭ Code index already exists';

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Products_Name' AND object_id = OBJECT_ID('Products'))
BEGIN
    CREATE INDEX IX_Products_Name ON Products(Name);
    PRINT '✅ Name index created';
END
ELSE
    PRINT '⏭ Name index already exists';

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Products_CategoryId' AND object_id = OBJECT_ID('Products'))
BEGIN
    CREATE INDEX IX_Products_CategoryId ON Products(CategoryId);
    PRINT '✅ CategoryId index created';
END
ELSE
    PRINT '⏭ CategoryId index already exists';

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Products_WarehouseId' AND object_id = OBJECT_ID('Products'))
BEGIN
    CREATE INDEX IX_Products_WarehouseId ON Products(WarehouseId);
    PRINT '✅ WarehouseId index created';
END
ELSE
    PRINT '⏭ WarehouseId index already exists';

-- Composite indexes for common queries
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Products_Name_IsActive' AND object_id = OBJECT_ID('Products'))
BEGIN
    CREATE INDEX IX_Products_Name_IsActive ON Products(Name, IsActive);
    PRINT '✅ Name+IsActive composite index created';
END
ELSE
    PRINT '⏭ Name+IsActive composite index already exists';

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Products_CategoryId_IsActive' AND object_id = OBJECT_ID('Products'))
BEGIN
    CREATE INDEX IX_Products_CategoryId_IsActive ON Products(CategoryId, IsActive);
    PRINT '✅ CategoryId+IsActive composite index created';
END
ELSE
    PRINT '⏭ CategoryId+IsActive composite index already exists';

-- =================================================================
-- 6. CREATE EXCEL IMPORT LOG TABLE
-- =================================================================

PRINT '6. Creating Excel import log table...';

-- Create table for tracking Excel import operations
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ExcelImportLogs')
BEGIN
    CREATE TABLE ExcelImportLogs (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        FileName NVARCHAR(255) NOT NULL,
        ImportedBy NVARCHAR(100) NULL,
        ImportDate DATETIME2 NOT NULL DEFAULT GETDATE(),
        TotalRows INT NOT NULL DEFAULT 0,
        SuccessfulRows INT NOT NULL DEFAULT 0,
        FailedRows INT NOT NULL DEFAULT 0,
        ErrorMessage NVARCHAR(MAX) NULL,
        FileSize BIGINT NOT NULL DEFAULT 0,
        ProcessingTimeMs BIGINT NOT NULL DEFAULT 0,
        Status NVARCHAR(20) NOT NULL DEFAULT 'Started' 
            CHECK (Status IN ('Started', 'Completed', 'Failed', 'Cancelled'))
    );
    
    -- Indexes for Excel import logs
    CREATE INDEX IX_ExcelImportLogs_ImportDate ON ExcelImportLogs(ImportDate);
    CREATE INDEX IX_ExcelImportLogs_Status ON ExcelImportLogs(Status);
    CREATE INDEX IX_ExcelImportLogs_ImportedBy ON ExcelImportLogs(ImportedBy);
    
    PRINT '✅ ExcelImportLogs table and indexes created';
END
ELSE
    PRINT '⏭ ExcelImportLogs table already exists';

-- =================================================================
-- 7. CREATE EXCEL IMPORT ERROR DETAILS TABLE
-- =================================================================

PRINT '7. Creating Excel import error details table...';

-- Create table for detailed error tracking during Excel import
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ExcelImportErrorDetails')
BEGIN
    CREATE TABLE ExcelImportErrorDetails (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        ImportLogId BIGINT NOT NULL,
        RowNumber INT NOT NULL,
        ColumnName NVARCHAR(100) NULL,
        ErrorType NVARCHAR(50) NOT NULL 
            CHECK (ErrorType IN ('ValidationError', 'FormatError', 'DuplicateKey', 'ReferenceError', 'DataTooLong')),
        ErrorMessage NVARCHAR(MAX) NOT NULL,
        AttemptedValue NVARCHAR(MAX) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        FOREIGN KEY (ImportLogId) REFERENCES ExcelImportLogs(Id) ON DELETE CASCADE
    );
    
    -- Indexes for error details
    CREATE INDEX IX_ExcelImportErrorDetails_ImportLogId ON ExcelImportErrorDetails(ImportLogId);
    CREATE INDEX IX_ExcelImportErrorDetails_RowNumber ON ExcelImportErrorDetails(RowNumber);
    CREATE INDEX IX_ExcelImportErrorDetails_ErrorType ON ExcelImportErrorDetails(ErrorType);
    
    PRINT '✅ ExcelImportErrorDetails table and indexes created';
END
ELSE
    PRINT '⏭ ExcelImportErrorDetails table already exists';

-- =================================================================
-- 8. UPDATE STATISTICS
-- =================================================================

PRINT '8. Updating statistics...';

UPDATE STATISTICS Products;
PRINT '✅ Products statistics updated';

-- =================================================================
-- 9. VALIDATION AND COMPLETION
-- =================================================================

PRINT '9. Validation and completion...';

-- Count existing products
DECLARE @ProductCount INT;
SELECT @ProductCount = COUNT(*) FROM Products;
PRINT 'Total products in database: ' + CAST(@ProductCount AS NVARCHAR(10));

-- Display completion message
PRINT '';
PRINT '=== EXCEL IMPORT ENHANCEMENT COMPLETED SUCCESSFULLY ===';
PRINT 'All required columns, indexes, and tables have been created.';
PRINT 'System is ready for enhanced Excel import functionality.';
PRINT '';

GO