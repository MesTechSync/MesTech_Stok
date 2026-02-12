CREATE TABLE [Categories] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NOT NULL,
    [Code] nvarchar(50) NOT NULL,
    [ParentCategoryId] int NULL,
    [ImageUrl] nvarchar(255) NULL,
    [Color] nvarchar(7) NULL,
    [Icon] nvarchar(50) NULL,
    [SortOrder] int NOT NULL,
    [IsActive] bit NOT NULL,
    [ShowInMenu] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedDate] datetime2 NULL,
    [CreatedBy] nvarchar(50) NULL,
    [ModifiedBy] nvarchar(50) NULL,
    [OpenCartCategoryId] int NULL,
    [LastSyncDate] datetime2 NULL,
    [SyncWithOpenCart] bit NOT NULL,
    CONSTRAINT [PK_Categories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Categories_Categories_ParentCategoryId] FOREIGN KEY ([ParentCategoryId]) REFERENCES [Categories] ([Id])
);
GO


CREATE TABLE [CompanySettings] (
    [Id] int NOT NULL IDENTITY,
    [CompanyName] nvarchar(200) NOT NULL,
    [TaxNumber] nvarchar(50) NULL,
    [Phone] nvarchar(20) NULL,
    [Email] nvarchar(150) NULL,
    [Address] nvarchar(1000) NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedDate] datetime2 NULL,
    CONSTRAINT [PK_CompanySettings] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Customers] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Code] nvarchar(50) NOT NULL,
    [CustomerType] nvarchar(20) NOT NULL,
    [ContactPerson] nvarchar(100) NULL,
    [Email] nvarchar(100) NULL,
    [Phone] nvarchar(20) NULL,
    [Mobile] nvarchar(20) NULL,
    [BillingAddress] nvarchar(500) NULL,
    [ShippingAddress] nvarchar(500) NULL,
    [City] nvarchar(100) NULL,
    [State] nvarchar(100) NULL,
    [PostalCode] nvarchar(20) NULL,
    [Country] nvarchar(100) NULL,
    [TaxNumber] nvarchar(20) NULL,
    [TaxOffice] nvarchar(50) NULL,
    [VatNumber] nvarchar(20) NULL,
    [IdentityNumber] nvarchar(11) NULL,
    [CreditLimit] decimal(18,2) NULL,
    [CurrentBalance] decimal(18,2) NOT NULL,
    [DiscountRate] decimal(5,2) NULL,
    [PaymentTermDays] int NOT NULL,
    [Currency] nvarchar(3) NOT NULL,
    [Segment] nvarchar(20) NULL,
    [Rating] int NULL,
    [IsVip] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [IsBlocked] bit NOT NULL,
    [BlockReason] nvarchar(200) NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedDate] datetime2 NULL,
    [LastOrderDate] datetime2 NULL,
    [BirthDate] datetime2 NULL,
    [CreatedBy] nvarchar(50) NULL,
    [ModifiedBy] nvarchar(50) NULL,
    [PreferredLanguage] nvarchar(10) NULL,
    [PreferredContactMethod] nvarchar(50) NULL,
    [AcceptsMarketing] bit NOT NULL,
    [Website] nvarchar(255) NULL,
    [FacebookProfile] nvarchar(100) NULL,
    [InstagramProfile] nvarchar(100) NULL,
    [LinkedInProfile] nvarchar(100) NULL,
    [Notes] nvarchar(1000) NULL,
    [DocumentUrls] nvarchar(500) NULL,
    [OpenCartCustomerId] int NULL,
    [LastSyncDate] datetime2 NULL,
    [SyncWithOpenCart] bit NOT NULL,
    CONSTRAINT [PK_Customers] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Permissions] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(50) NOT NULL,
    [Module] nvarchar(50) NOT NULL,
    [Description] nvarchar(200) NULL,
    [IsActive] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_Permissions] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Roles] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(50) NOT NULL,
    [Description] nvarchar(200) NULL,
    [IsActive] bit NOT NULL,
    [IsSystemRole] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedDate] datetime2 NULL,
    CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Suppliers] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Code] nvarchar(50) NOT NULL,
    [Description] nvarchar(500) NOT NULL,
    [ContactPerson] nvarchar(100) NULL,
    [Email] nvarchar(100) NULL,
    [Phone] nvarchar(20) NULL,
    [Mobile] nvarchar(20) NULL,
    [Fax] nvarchar(20) NULL,
    [Website] nvarchar(255) NULL,
    [Address] nvarchar(500) NULL,
    [City] nvarchar(100) NULL,
    [State] nvarchar(100) NULL,
    [PostalCode] nvarchar(20) NULL,
    [Country] nvarchar(100) NULL,
    [TaxNumber] nvarchar(20) NULL,
    [TaxOffice] nvarchar(50) NULL,
    [VatNumber] nvarchar(20) NULL,
    [TradeRegisterNumber] nvarchar(50) NULL,
    [PaymentTermDays] int NOT NULL,
    [CreditLimit] decimal(18,2) NULL,
    [CurrentBalance] decimal(18,2) NOT NULL,
    [DiscountRate] decimal(5,2) NULL,
    [Currency] nvarchar(3) NOT NULL,
    [IsActive] bit NOT NULL,
    [IsPreferred] bit NOT NULL,
    [Rating] int NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedDate] datetime2 NULL,
    [LastOrderDate] datetime2 NULL,
    [CreatedBy] nvarchar(50) NULL,
    [ModifiedBy] nvarchar(50) NULL,
    [Notes] nvarchar(1000) NULL,
    [DocumentUrls] nvarchar(500) NULL,
    CONSTRAINT [PK_Suppliers] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Users] (
    [Id] int NOT NULL IDENTITY,
    [Username] nvarchar(50) NOT NULL,
    [Email] nvarchar(100) NOT NULL,
    [PasswordHash] nvarchar(256) NOT NULL,
    [FirstName] nvarchar(100) NULL,
    [LastName] nvarchar(100) NULL,
    [Phone] nvarchar(20) NULL,
    [IsActive] bit NOT NULL,
    [IsEmailConfirmed] bit NOT NULL,
    [LastLoginDate] datetime2 NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedDate] datetime2 NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Warehouses] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Code] nvarchar(20) NOT NULL,
    [Description] nvarchar(500) NOT NULL,
    [Type] nvarchar(50) NOT NULL,
    [Address] nvarchar(500) NULL,
    [City] nvarchar(100) NULL,
    [State] nvarchar(100) NULL,
    [PostalCode] nvarchar(20) NULL,
    [Country] nvarchar(100) NULL,
    [ContactPerson] nvarchar(100) NULL,
    [Email] nvarchar(100) NULL,
    [Phone] nvarchar(20) NULL,
    [TotalArea] decimal(10,2) NULL,
    [UsableArea] decimal(10,2) NULL,
    [Height] decimal(10,2) NULL,
    [MaxCapacity] decimal(15,3) NULL,
    [CapacityUnit] nvarchar(20) NULL,
    [MinTemperature] decimal(5,2) NULL,
    [MaxTemperature] decimal(5,2) NULL,
    [MinHumidity] decimal(5,2) NULL,
    [MaxHumidity] decimal(5,2) NULL,
    [HasClimateControl] bit NOT NULL,
    [HasSecuritySystem] bit NOT NULL,
    [HasFireProtection] bit NOT NULL,
    [HasLoadingDock] bit NOT NULL,
    [HasRacking] bit NOT NULL,
    [HasForklift] bit NOT NULL,
    [OperatingHours] nvarchar(100) NULL,
    [Is24Hours] bit NOT NULL,
    [MonthlyCost] decimal(18,2) NULL,
    [CostPerSquareMeter] decimal(10,4) NULL,
    [CostCenter] nvarchar(20) NULL,
    [IsActive] bit NOT NULL,
    [IsDefault] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedDate] datetime2 NULL,
    [CreatedBy] nvarchar(50) NULL,
    [ModifiedBy] nvarchar(50) NULL,
    [Notes] nvarchar(1000) NULL,
    CONSTRAINT [PK_Warehouses] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Orders] (
    [Id] int NOT NULL IDENTITY,
    [OrderNumber] nvarchar(50) NOT NULL,
    [CustomerId] int NOT NULL,
    [Status] int NOT NULL,
    [Type] nvarchar(20) NOT NULL,
    [OrderDate] datetime2 NOT NULL,
    [RequiredDate] datetime2 NULL,
    [SubTotal] decimal(18,2) NOT NULL,
    [TaxAmount] decimal(18,2) NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [TaxRate] decimal(5,2) NOT NULL,
    [PaymentStatus] nvarchar(20) NOT NULL,
    [Notes] nvarchar(1000) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [ModifiedDate] datetime2 NULL,
    [LastModifiedAt] datetime2 NULL,
    [CreatedBy] nvarchar(50) NULL,
    [CustomerName] nvarchar(100) NULL,
    [CustomerEmail] nvarchar(100) NULL,
    [OpenCartOrderId] int NULL,
    CONSTRAINT [PK_Orders] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Orders_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [RolePermissions] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] int NOT NULL,
    [PermissionId] int NOT NULL,
    [GrantedDate] datetime2 NOT NULL,
    [GrantedByUserId] int NULL,
    [PermissionId1] int NULL,
    CONSTRAINT [PK_RolePermissions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RolePermissions_Permissions_PermissionId] FOREIGN KEY ([PermissionId]) REFERENCES [Permissions] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_RolePermissions_Permissions_PermissionId1] FOREIGN KEY ([PermissionId1]) REFERENCES [Permissions] ([Id]),
    CONSTRAINT [FK_RolePermissions_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_RolePermissions_Users_GrantedByUserId] FOREIGN KEY ([GrantedByUserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL
);
GO


CREATE TABLE [UserRoles] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [RoleId] int NOT NULL,
    [AssignedDate] datetime2 NOT NULL,
    [AssignedByUserId] int NULL,
    CONSTRAINT [PK_UserRoles] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserRoles_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_UserRoles_Users_AssignedByUserId] FOREIGN KEY ([AssignedByUserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Products] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NOT NULL,
    [SKU] nvarchar(50) NOT NULL,
    [Barcode] nvarchar(50) NOT NULL,
    [GTIN] nvarchar(14) NULL,
    [UPC] nvarchar(20) NULL,
    [EAN] nvarchar(20) NULL,
    [PurchasePrice] decimal(18,2) NOT NULL,
    [SalePrice] decimal(18,2) NOT NULL,
    [ListPrice] decimal(18,2) NULL,
    [TaxRate] decimal(5,2) NOT NULL,
    [Stock] int NOT NULL,
    [MinimumStock] int NOT NULL,
    [MaximumStock] int NOT NULL,
    [ReorderLevel] int NOT NULL,
    [ReorderQuantity] int NOT NULL,
    [CategoryId] int NOT NULL,
    [SupplierId] int NULL,
    [Weight] decimal(10,3) NULL,
    [Length] decimal(10,2) NULL,
    [Width] decimal(10,2) NULL,
    [Height] decimal(10,2) NULL,
    [WeightUnit] nvarchar(10) NULL,
    [DimensionUnit] nvarchar(10) NULL,
    [Location] nvarchar(50) NULL,
    [Shelf] nvarchar(20) NULL,
    [Bin] nvarchar(20) NULL,
    [WarehouseId] int NULL,
    [IsActive] bit NOT NULL,
    [IsDiscontinued] bit NOT NULL,
    [IsSerialized] bit NOT NULL,
    [IsBatchTracked] bit NOT NULL,
    [IsPerishable] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedDate] datetime2 NULL,
    [ExpiryDate] datetime2 NULL,
    [LastStockUpdate] datetime2 NULL,
    [CreatedBy] nvarchar(50) NULL,
    [ModifiedBy] nvarchar(50) NULL,
    [ImageUrl] nvarchar(255) NULL,
    [ImageUrls] nvarchar(500) NULL,
    [DocumentUrls] nvarchar(500) NULL,
    [Brand] nvarchar(50) NULL,
    [Model] nvarchar(50) NULL,
    [Color] nvarchar(50) NULL,
    [Size] nvarchar(20) NULL,
    [Notes] nvarchar(1000) NULL,
    [Tags] nvarchar(200) NULL,
    [OpenCartProductId] int NULL,
    [LastSyncDate] datetime2 NULL,
    [LastModifiedAt] datetime2 NULL,
    [SyncWithOpenCart] bit NOT NULL,
    CONSTRAINT [PK_Products] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Products_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Products_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]),
    CONSTRAINT [FK_Products_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id])
);
GO


CREATE TABLE [OrderItems] (
    [Id] int NOT NULL IDENTITY,
    [OrderId] int NOT NULL,
    [ProductId] int NOT NULL,
    [ProductName] nvarchar(200) NOT NULL,
    [ProductSKU] nvarchar(50) NOT NULL,
    [Quantity] int NOT NULL,
    [UnitPrice] decimal(18,2) NOT NULL,
    [TotalPrice] decimal(18,2) NOT NULL,
    [TaxRate] decimal(5,2) NOT NULL,
    [TaxAmount] decimal(18,2) NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_OrderItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_OrderItems_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_OrderItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [StockMovements] (
    [Id] int NOT NULL IDENTITY,
    [ProductId] int NOT NULL,
    [Quantity] int NOT NULL,
    [PreviousStock] int NOT NULL,
    [NewStock] int NOT NULL,
    [NewStockLevel] int NOT NULL,
    [MovementType] nvarchar(50) NOT NULL,
    [Reason] nvarchar(100) NULL,
    [Notes] nvarchar(200) NULL,
    [UnitCost] decimal(18,2) NULL,
    [TotalCost] decimal(18,2) NULL,
    [OrderId] int NULL,
    [SupplierId] int NULL,
    [CustomerId] int NULL,
    [FromWarehouseId] int NULL,
    [ToWarehouseId] int NULL,
    [FromLocation] nvarchar(50) NULL,
    [ToLocation] nvarchar(50) NULL,
    [DocumentNumber] nvarchar(50) NULL,
    [DocumentUrl] nvarchar(255) NULL,
    [Date] datetime2 NOT NULL,
    [ProcessedBy] nvarchar(50) NULL,
    [ApprovedBy] nvarchar(50) NULL,
    [ApprovedDate] datetime2 NULL,
    [IsApproved] bit NOT NULL,
    [IsReversed] bit NOT NULL,
    [ReversalMovementId] int NULL,
    [BatchNumber] nvarchar(50) NULL,
    [SerialNumber] nvarchar(50) NULL,
    [ExpiryDate] datetime2 NULL,
    [ScannedBarcode] nvarchar(50) NULL,
    [IsScannedMovement] bit NOT NULL,
    [UserId] int NULL,
    [WarehouseId] int NULL,
    [WarehouseId1] int NULL,
    CONSTRAINT [PK_StockMovements] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StockMovements_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]),
    CONSTRAINT [FK_StockMovements_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]),
    CONSTRAINT [FK_StockMovements_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_StockMovements_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]),
    CONSTRAINT [FK_StockMovements_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_StockMovements_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id]),
    CONSTRAINT [FK_StockMovements_Warehouses_WarehouseId1] FOREIGN KEY ([WarehouseId1]) REFERENCES [Warehouses] ([Id])
);
GO


CREATE INDEX [IX_Categories_ParentCategoryId] ON [Categories] ([ParentCategoryId]);
GO


CREATE INDEX [IX_OrderItems_OrderId] ON [OrderItems] ([OrderId]);
GO


CREATE INDEX [IX_OrderItems_ProductId] ON [OrderItems] ([ProductId]);
GO


CREATE INDEX [IX_Orders_CustomerId] ON [Orders] ([CustomerId]);
GO


CREATE UNIQUE INDEX [IX_Permissions_Name_Module] ON [Permissions] ([Name], [Module]);
GO


CREATE INDEX [IX_Products_CategoryId] ON [Products] ([CategoryId]);
GO


CREATE INDEX [IX_Products_SupplierId] ON [Products] ([SupplierId]);
GO


CREATE INDEX [IX_Products_WarehouseId] ON [Products] ([WarehouseId]);
GO


CREATE INDEX [IX_RolePermissions_GrantedByUserId] ON [RolePermissions] ([GrantedByUserId]);
GO


CREATE INDEX [IX_RolePermissions_PermissionId] ON [RolePermissions] ([PermissionId]);
GO


CREATE INDEX [IX_RolePermissions_PermissionId1] ON [RolePermissions] ([PermissionId1]);
GO


CREATE INDEX [IX_RolePermissions_RoleId] ON [RolePermissions] ([RoleId]);
GO


CREATE UNIQUE INDEX [IX_Roles_Name] ON [Roles] ([Name]);
GO


CREATE INDEX [IX_StockMovements_CustomerId] ON [StockMovements] ([CustomerId]);
GO


CREATE INDEX [IX_StockMovements_OrderId] ON [StockMovements] ([OrderId]);
GO


CREATE INDEX [IX_StockMovements_ProductId] ON [StockMovements] ([ProductId]);
GO


CREATE INDEX [IX_StockMovements_SupplierId] ON [StockMovements] ([SupplierId]);
GO


CREATE INDEX [IX_StockMovements_UserId] ON [StockMovements] ([UserId]);
GO


CREATE INDEX [IX_StockMovements_WarehouseId] ON [StockMovements] ([WarehouseId]);
GO


CREATE INDEX [IX_StockMovements_WarehouseId1] ON [StockMovements] ([WarehouseId1]);
GO


CREATE INDEX [IX_UserRoles_AssignedByUserId] ON [UserRoles] ([AssignedByUserId]);
GO


CREATE INDEX [IX_UserRoles_RoleId] ON [UserRoles] ([RoleId]);
GO


CREATE INDEX [IX_UserRoles_UserId] ON [UserRoles] ([UserId]);
GO


CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
GO


CREATE UNIQUE INDEX [IX_Users_Username] ON [Users] ([Username]);
GO


