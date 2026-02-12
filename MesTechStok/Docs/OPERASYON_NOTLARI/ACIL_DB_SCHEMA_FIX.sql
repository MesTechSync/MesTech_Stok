-- 🚨 ACİL VERİTABANI ŞEMA OLUŞTURMA
-- HATA: Invalid object name 'OfflineQueue' - ÇÖZÜM KOMUTLARI

USE [MesTechStok]
GO

-- 1. OfflineQueue tablosunu kontrol et, yoksa oluştur
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[OfflineQueue]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[OfflineQueue](
        [Id] [bigint] IDENTITY(1,1) NOT NULL,
        [Channel] [nvarchar](50) NOT NULL,
        [Direction] [nvarchar](20) NOT NULL,
        [Data] [nvarchar](max) NOT NULL,
        [Status] [nvarchar](20) NOT NULL DEFAULT ('Pending'),
        [CreatedDate] [datetime2](7) NOT NULL DEFAULT (GETUTCDATE()),
        [ProcessedDate] [datetime2](7) NULL,
        CONSTRAINT [PK_OfflineQueue] PRIMARY KEY CLUSTERED ([Id] ASC)
    )
    PRINT '✅ OfflineQueue tablosu oluşturuldu'
END
ELSE
BEGIN
    PRINT 'ℹ️ OfflineQueue tablosu zaten mevcut'
END
GO

-- 2. Users tablosunu kontrol et, yoksa oluştur  
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Users](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [Username] [nvarchar](100) NOT NULL,
        [Email] [nvarchar](255) NOT NULL,
        [PasswordHash] [nvarchar](500) NOT NULL,
        [FirstName] [nvarchar](100) NULL,
        [LastName] [nvarchar](100) NULL,
        [IsActive] [bit] NOT NULL DEFAULT (1),
        [CreatedDate] [datetime2](7) NOT NULL DEFAULT (GETUTCDATE()),
        [UpdatedDate] [datetime2](7) NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [IX_Users_Username] UNIQUE NONCLUSTERED ([Username] ASC),
        CONSTRAINT [IX_Users_Email] UNIQUE NONCLUSTERED ([Email] ASC)
    )
    PRINT '✅ Users tablosu oluşturuldu'
END
ELSE
BEGIN
    PRINT 'ℹ️ Users tablosu zaten mevcut'
END
GO

-- 3. Roles tablosu - Entity Framework User modeli için gerekli
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Roles]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Roles](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [Name] [nvarchar](50) NOT NULL,
        [Description] [nvarchar](200) NULL,
        [IsActive] [bit] NOT NULL DEFAULT (1),
        [IsSystemRole] [bit] NOT NULL DEFAULT (0),
        [CreatedDate] [datetime2](7) NOT NULL DEFAULT (GETUTCDATE()),
        [ModifiedDate] [datetime2](7) NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [IX_Roles_Name] UNIQUE NONCLUSTERED ([Name] ASC)
    )
    PRINT '✅ Roles tablosu oluşturuldu'
END
ELSE
BEGIN
    PRINT 'ℹ️ Roles tablosu zaten mevcut'
END
GO

-- 4. UserRoles tablosu - User-Role many-to-many ilişki için
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UserRoles]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[UserRoles](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [UserId] [int] NOT NULL,
        [RoleId] [int] NOT NULL,
        [AssignedDate] [datetime2](7) NOT NULL DEFAULT (GETUTCDATE()),
        [AssignedByUserId] [int] NULL,
        CONSTRAINT [PK_UserRoles] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_UserRoles_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]),
        CONSTRAINT [FK_UserRoles_Roles] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles]([Id])
    )
    PRINT '✅ UserRoles tablosu oluşturuldu'
END
ELSE
BEGIN
    PRINT 'ℹ️ UserRoles tablosu zaten mevcut'
END
GO

-- 5. ApiCallLogs tablosu - Monitoring için
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ApiCallLogs]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[ApiCallLogs](
        [Id] [bigint] IDENTITY(1,1) NOT NULL,
        [ServiceName] [nvarchar](100) NOT NULL,
        [Method] [nvarchar](50) NOT NULL,
        [Url] [nvarchar](500) NOT NULL,
        [Request] [nvarchar](max) NULL,
        [Response] [nvarchar](max) NULL,
        [StatusCode] [int] NOT NULL,
        [Duration] [int] NOT NULL,
        [CreatedDate] [datetime2](7) NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_ApiCallLogs] PRIMARY KEY CLUSTERED ([Id] ASC)
    )
    PRINT '✅ ApiCallLogs tablosu oluşturuldu'
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CircuitStateLogs]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[CircuitStateLogs](
        [Id] [bigint] IDENTITY(1,1) NOT NULL,
        [ServiceName] [nvarchar](100) NOT NULL,
        [State] [nvarchar](20) NOT NULL,
        [Reason] [nvarchar](500) NULL,
        [CreatedDate] [datetime2](7) NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_CircuitStateLogs] PRIMARY KEY CLUSTERED ([Id] ASC)
    )
    PRINT '✅ CircuitStateLogs tablosu oluşturuldu'
END
GO

PRINT '🎯 Veritabanı şema kontrolü tamamlandı!'
