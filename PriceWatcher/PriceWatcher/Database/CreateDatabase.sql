-- =============================================
-- PriceWatcher Database Creation Script
-- Created based on Models
-- =============================================

USE master;
GO

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'PriceWatcherDB')
BEGIN
    CREATE DATABASE PriceWatcherDB;
END
GO

USE PriceWatcherDB;
GO

-- =============================================
-- Drop existing tables if they exist (in reverse order of dependencies)
-- =============================================
IF OBJECT_ID('RolePermissions', 'U') IS NOT NULL
    DROP TABLE RolePermissions;
GO

IF OBJECT_ID('UserRoles', 'U') IS NOT NULL
    DROP TABLE UserRoles;
GO

IF OBJECT_ID('Permissions', 'U') IS NOT NULL
    DROP TABLE Permissions;
GO

IF OBJECT_ID('AdminRoles', 'U') IS NOT NULL
    DROP TABLE AdminRoles;
GO

IF OBJECT_ID('UserPreferences', 'U') IS NOT NULL
    DROP TABLE UserPreferences;
GO

IF OBJECT_ID('AffiliateLinks', 'U') IS NOT NULL
    DROP TABLE AffiliateLinks;
GO

IF OBJECT_ID('StoreListings', 'U') IS NOT NULL
    DROP TABLE StoreListings;
GO

IF OBJECT_ID('Stores', 'U') IS NOT NULL
    DROP TABLE Stores;
GO

IF OBJECT_ID('Favorites', 'U') IS NOT NULL
    DROP TABLE Favorites;
GO

IF OBJECT_ID('DiscountCodes', 'U') IS NOT NULL
    DROP TABLE DiscountCodes;
GO

IF OBJECT_ID('ProductNews', 'U') IS NOT NULL
    DROP TABLE ProductNews;
GO
IF OBJECT_ID('CartItems', 'U') IS NOT NULL
    DROP TABLE CartItems;
GO

IF OBJECT_ID('Carts', 'U') IS NOT NULL
    DROP TABLE Carts;
GO

IF OBJECT_ID('PriceSnapshots', 'U') IS NOT NULL
    DROP TABLE PriceSnapshots;
GO

IF OBJECT_ID('Products', 'U') IS NOT NULL
    DROP TABLE Products;
GO

IF OBJECT_ID('SearchHistories', 'U') IS NOT NULL
    DROP TABLE SearchHistories;
GO

IF OBJECT_ID('ProductMappings', 'U') IS NOT NULL
    DROP TABLE ProductMappings;
GO

IF OBJECT_ID('Users', 'U') IS NOT NULL
    DROP TABLE Users;
GO

IF OBJECT_ID('Platforms', 'U') IS NOT NULL
    DROP TABLE Platforms;
GO

IF OBJECT_ID('SystemLogs', 'U') IS NOT NULL
    DROP TABLE SystemLogs;
GO

IF OBJECT_ID('PriceAlerts', 'U') IS NOT NULL
    DROP TABLE PriceAlerts;
GO

IF OBJECT_ID('Categories', 'U') IS NOT NULL
    DROP TABLE Categories;
GO

IF OBJECT_ID('Reviews', 'U') IS NOT NULL
    DROP TABLE Reviews;
GO

IF OBJECT_ID('CrawlJobs', 'U') IS NOT NULL
    DROP TABLE CrawlJobs;
GO

-- =============================================
-- Create Tables
-- =============================================

-- Platforms Table
CREATE TABLE Platforms (
    PlatformId INT IDENTITY(1,1) PRIMARY KEY,
    PlatformName NVARCHAR(50) NOT NULL,
    Domain NVARCHAR(100) NULL,
    ColorCode VARCHAR(20) NULL
);
GO

-- Users Table
CREATE TABLE Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(100) NULL,
    Email VARCHAR(100) NOT NULL UNIQUE,
    AvatarUrl NVARCHAR(500) NULL,
    GoogleId VARCHAR(100) NULL,
    PasswordHash VARBINARY(64) NULL,
    PasswordSalt VARBINARY(16) NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    LastLogin DATETIME NULL
);
GO

-- Products Table
CREATE TABLE Products (
    ProductId INT IDENTITY(1,1) PRIMARY KEY,
    PlatformId INT NULL,
    ExternalId VARCHAR(100) NULL,
    ProductName NVARCHAR(500) NOT NULL,
    OriginalUrl NVARCHAR(MAX) NOT NULL,
    AffiliateUrl NVARCHAR(MAX) NULL,
    AffiliateProvider NVARCHAR(50) NULL,
    AffiliateExpiry DATETIME NULL,
    ImageUrl NVARCHAR(MAX) NULL,
    Description NVARCHAR(MAX) NULL,
    CurrentPrice DECIMAL(18,2) NULL,
    OriginalPrice DECIMAL(18,2) NULL,
    DiscountRate INT NULL,
    StockStatus NVARCHAR(50) NULL,
    Rating FLOAT NULL,
    ReviewCount INT NULL,
    SoldQuantity INT NULL,
    ShopName NVARCHAR(200) NULL,
    ShippingInfo NVARCHAR(200) NULL,
    LastUpdated DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_Products_Platforms FOREIGN KEY (PlatformId) 
        REFERENCES Platforms(PlatformId)
);
GO

-- PriceSnapshots Table
CREATE TABLE PriceSnapshots (
    SnapshotId INT IDENTITY(1,1) PRIMARY KEY,
    ProductId INT NULL,
    Price DECIMAL(18,2) NOT NULL,
    OriginalPrice DECIMAL(18,2) NULL,
    ShippingInfo NVARCHAR(200) NULL,
    RecordedAt DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_PriceSnapshots_Products FOREIGN KEY (ProductId) 
        REFERENCES Products(ProductId) ON DELETE CASCADE
);
GO

-- SearchHistories Table
CREATE TABLE SearchHistories (
    HistoryId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NULL,
    SearchType VARCHAR(20) NULL,
    InputContent NVARCHAR(MAX) NULL,
    DetectedKeyword NVARCHAR(200) NULL,
    BestPriceFound DECIMAL(18,2) NULL,
    SearchTime DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_SearchHistories_Users FOREIGN KEY (UserId) 
        REFERENCES Users(UserId) ON DELETE CASCADE
);
GO

-- ProductMappings Table
CREATE TABLE ProductMappings (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SourceUrl NVARCHAR(1000) NOT NULL,
    SourceProductId NVARCHAR(200) NULL,
    MatchedCandidatesJson NVARCHAR(MAX) NULL,
    LastSeen DATETIME NOT NULL DEFAULT GETDATE()
);
GO

-- SystemLogs Table
CREATE TABLE SystemLogs (
    LogId INT IDENTITY(1,1) PRIMARY KEY,
    Level VARCHAR(20) NULL,
    Message NVARCHAR(MAX) NULL,
    CreatedAt DATETIME DEFAULT GETDATE()
);
GO

-- Categories Table
CREATE TABLE Categories (
    CategoryId INT IDENTITY(1,1) PRIMARY KEY,
    CategoryName NVARCHAR(200) NOT NULL,
    ParentCategoryId INT NULL,
    IconUrl NVARCHAR(500) NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_Categories_Parent FOREIGN KEY (ParentCategoryId) 
        REFERENCES Categories(CategoryId)
);
GO

-- PriceAlerts Table
CREATE TABLE PriceAlerts (
    AlertId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    ProductId INT NOT NULL,
    TargetPrice DECIMAL(18,2) NOT NULL,
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME DEFAULT GETDATE(),
    LastNotifiedAt DATETIME NULL,
    CONSTRAINT FK_PriceAlerts_Users FOREIGN KEY (UserId) 
        REFERENCES Users(UserId) ON DELETE CASCADE,
    CONSTRAINT FK_PriceAlerts_Products FOREIGN KEY (ProductId) 
        REFERENCES Products(ProductId) ON DELETE CASCADE
);
GO

-- CrawlJobs Table
CREATE TABLE CrawlJobs (
    JobId INT IDENTITY(1,1) PRIMARY KEY,
    ProductId INT NOT NULL,
    PlatformId INT NOT NULL,
    Status NVARCHAR(50) NOT NULL, -- 'Pending', 'Processing', 'Completed', 'Failed'
    LastTriedAt DATETIME NULL,
    RetryCount INT DEFAULT 0,
    CreatedAt DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_CrawlJobs_Products FOREIGN KEY (ProductId) 
        REFERENCES Products(ProductId) ON DELETE CASCADE,
    CONSTRAINT FK_CrawlJobs_Platforms FOREIGN KEY (PlatformId) 
        REFERENCES Platforms(PlatformId)
);
GO

-- Carts Table
CREATE TABLE Carts (
    CartId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NULL,
    AnonymousId UNIQUEIDENTIFIER NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
    ExpiresAt DATETIME NULL,
    CONSTRAINT FK_Carts_Users FOREIGN KEY (UserId)
        REFERENCES Users(UserId) ON DELETE CASCADE
);
GO

CREATE UNIQUE INDEX IX_Carts_UserId ON Carts(UserId) WHERE UserId IS NOT NULL;
CREATE UNIQUE INDEX IX_Carts_AnonymousId ON Carts(AnonymousId) WHERE AnonymousId IS NOT NULL;
GO

-- CartItems Table
CREATE TABLE CartItems (
    CartItemId INT IDENTITY(1,1) PRIMARY KEY,
    CartId INT NOT NULL,
    ProductId INT NULL,
    ProductName NVARCHAR(500) NOT NULL,
    PlatformId INT NULL,
    PlatformName NVARCHAR(100) NULL,
    ImageUrl NVARCHAR(1000) NULL,
    ProductUrl NVARCHAR(1000) NULL,
    Price DECIMAL(18,2) NOT NULL,
    OriginalPrice DECIMAL(18,2) NULL,
    Quantity INT NOT NULL DEFAULT 1,
    AddedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
    MetadataJson NVARCHAR(MAX) NULL,
    CONSTRAINT FK_CartItems_Carts FOREIGN KEY (CartId)
        REFERENCES Carts(CartId) ON DELETE CASCADE
);
GO

CREATE INDEX IX_CartItems_CartId ON CartItems(CartId);
CREATE INDEX IX_CartItems_Cart_Product ON CartItems(CartId, ProductId, PlatformId);
GO

-- Reviews Table
CREATE TABLE Reviews (
    ReviewId INT IDENTITY(1,1) PRIMARY KEY,
    ProductId INT NOT NULL,
    UserId INT NOT NULL,
    Stars INT NOT NULL CHECK (Stars >= 1 AND Stars <= 5),
    Content NVARCHAR(MAX) NULL,
    IsVerifiedPurchase BIT DEFAULT 0,
    CreatedAt DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_Reviews_Products FOREIGN KEY (ProductId) 
        REFERENCES Products(ProductId) ON DELETE CASCADE,
    CONSTRAINT FK_Reviews_Users FOREIGN KEY (UserId) 
        REFERENCES Users(UserId) ON DELETE CASCADE
);
GO

CREATE TABLE Stores (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    PlatformId INT NOT NULL,
    Rating DECIMAL(18,2) NULL,
    IsVerified BIT NOT NULL DEFAULT 0,
    IsTrusted BIT NOT NULL DEFAULT 0,
    IsOfficial BIT NOT NULL DEFAULT 0,
    TotalSales INT NOT NULL DEFAULT 0,
    ResponseRate DECIMAL(18,2) NULL,
    ResponseTimeHours INT NULL,
    StoreUrl NVARCHAR(MAX) NULL,
    LogoUrl NVARCHAR(MAX) NULL,
    Description NVARCHAR(MAX) NULL,
    JoinedDate DATETIME NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
    LastUpdated DATETIME NULL,
    CONSTRAINT FK_Stores_Platforms FOREIGN KEY (PlatformId)
        REFERENCES Platforms(PlatformId)
);
GO

CREATE TABLE StoreListings (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ProductId INT NOT NULL,
    PlatformId INT NOT NULL,
    StoreName NVARCHAR(200) NOT NULL,
    StoreRating DECIMAL(18,2) NULL,
    IsVerified BIT NOT NULL DEFAULT 0,
    IsTrusted BIT NOT NULL DEFAULT 0,
    IsOfficial BIT NOT NULL DEFAULT 0,
    Price DECIMAL(18,2) NOT NULL,
    OriginalPrice DECIMAL(18,2) NULL,
    ShippingCost DECIMAL(18,2) NULL,
    DeliveryDays INT NULL,
    Stock INT NULL,
    IsFreeShipping BIT NOT NULL DEFAULT 0,
    StoreUrl NVARCHAR(MAX) NULL,
    TotalSales INT NULL,
    LastUpdated DATETIME NOT NULL DEFAULT GETUTCDATE(),
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_StoreListings_Products FOREIGN KEY (ProductId)
        REFERENCES Products(ProductId) ON DELETE CASCADE,
    CONSTRAINT FK_StoreListings_Platforms FOREIGN KEY (PlatformId)
        REFERENCES Platforms(PlatformId)
);
GO

CREATE TABLE Favorites (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    ProductId INT NOT NULL,
    CollectionName NVARCHAR(200) NULL,
    Notes NVARCHAR(MAX) NULL,
    TargetPrice DECIMAL(18,2) NULL,
    NotifyOnPriceDrop BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
    LastViewedAt DATETIME NULL,
    CONSTRAINT FK_Favorites_Users FOREIGN KEY (UserId)
        REFERENCES Users(UserId) ON DELETE CASCADE,
    CONSTRAINT FK_Favorites_Products FOREIGN KEY (ProductId)
        REFERENCES Products(ProductId) ON DELETE CASCADE
);
GO

CREATE TABLE DiscountCodes (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Code NVARCHAR(100) NOT NULL,
    PlatformId INT NULL,
    ProductId INT NULL,
    Description NVARCHAR(MAX) NULL,
    DiscountType NVARCHAR(50) NOT NULL DEFAULT 'percentage',
    DiscountValue DECIMAL(18,2) NULL,
    MinPurchase DECIMAL(18,2) NULL,
    MaxDiscount DECIMAL(18,2) NULL,
    ExpiresAt DATETIME NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    SuccessCount INT NOT NULL DEFAULT 0,
    TotalUses INT NOT NULL DEFAULT 0,
    SubmittedByUserId INT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
    LastVerifiedAt DATETIME NULL,
    CONSTRAINT FK_DiscountCodes_Platforms FOREIGN KEY (PlatformId)
        REFERENCES Platforms(PlatformId),
    CONSTRAINT FK_DiscountCodes_Products FOREIGN KEY (ProductId)
        REFERENCES Products(ProductId),
    CONSTRAINT FK_DiscountCodes_Users FOREIGN KEY (SubmittedByUserId)
        REFERENCES Users(UserId)
);
GO

CREATE TABLE ProductNews (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ProductId INT NULL,
    Title NVARCHAR(500) NOT NULL,
    Content NVARCHAR(MAX) NULL,
    SourceUrl NVARCHAR(MAX) NULL,
    ImageUrl NVARCHAR(MAX) NULL,
    Author NVARCHAR(200) NULL,
    PublishedAt DATETIME NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_ProductNews_Products FOREIGN KEY (ProductId)
        REFERENCES Products(ProductId) ON DELETE CASCADE
);
GO

CREATE TABLE AdminRoles (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE()
);
GO

CREATE TABLE Permissions (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Resource NVARCHAR(100) NOT NULL,
    Action NVARCHAR(50) NOT NULL,
    Description NVARCHAR(MAX) NULL
);
GO

CREATE TABLE RolePermissions (
    RoleId INT NOT NULL,
    PermissionId INT NOT NULL,
    CONSTRAINT PK_RolePermissions PRIMARY KEY (RoleId, PermissionId),
    CONSTRAINT FK_RolePermissions_Roles FOREIGN KEY (RoleId)
        REFERENCES AdminRoles(Id) ON DELETE CASCADE,
    CONSTRAINT FK_RolePermissions_Permissions FOREIGN KEY (PermissionId)
        REFERENCES Permissions(Id) ON DELETE CASCADE
);
GO

CREATE TABLE UserRoles (
    UserId INT NOT NULL,
    RoleId INT NOT NULL,
    AssignedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
    AssignedByUserId INT NULL,
    CONSTRAINT PK_UserRoles PRIMARY KEY (UserId, RoleId),
    CONSTRAINT FK_UserRoles_Users FOREIGN KEY (UserId)
        REFERENCES Users(UserId) ON DELETE CASCADE,
    CONSTRAINT FK_UserRoles_AdminRoles FOREIGN KEY (RoleId)
        REFERENCES AdminRoles(Id) ON DELETE CASCADE,
    CONSTRAINT FK_UserRoles_AssignedBy FOREIGN KEY (AssignedByUserId)
        REFERENCES Users(UserId)
);
GO

CREATE TABLE AffiliateLinks (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ProductId INT NOT NULL,
    PlatformId INT NOT NULL,
    AffiliateUrl NVARCHAR(MAX) NOT NULL,
    AffiliateCode NVARCHAR(100) NULL,
    ClickCount INT NOT NULL DEFAULT 0,
    ConversionCount INT NOT NULL DEFAULT 0,
    Revenue DECIMAL(18,2) NOT NULL DEFAULT 0,
    CommissionRate DECIMAL(18,2) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
    LastClickedAt DATETIME NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_AffiliateLinks_Products FOREIGN KEY (ProductId)
        REFERENCES Products(ProductId) ON DELETE CASCADE,
    CONSTRAINT FK_AffiliateLinks_Platforms FOREIGN KEY (PlatformId)
        REFERENCES Platforms(PlatformId)
);
GO

CREATE TABLE UserPreferences (
    UserId INT NOT NULL PRIMARY KEY,
    PreferredCategories NVARCHAR(MAX) NULL,
    PreferredPlatforms NVARCHAR(MAX) NULL,
    PriceRange NVARCHAR(MAX) NULL,
    NotificationSettings NVARCHAR(MAX) NULL,
    EmailNotifications BIT NOT NULL DEFAULT 1,
    TelegramNotifications BIT NOT NULL DEFAULT 0,
    PushNotifications BIT NOT NULL DEFAULT 0,
    Language NVARCHAR(10) NULL DEFAULT 'vi',
    Currency NVARCHAR(10) NULL DEFAULT 'VND',
    LastUpdated DATETIME NULL,
    CONSTRAINT FK_UserPreferences_Users FOREIGN KEY (UserId)
        REFERENCES Users(UserId) ON DELETE CASCADE
);
GO

-- =============================================
-- Create Indexes
-- =============================================

-- Index on Users.Email (already unique, but explicit index for performance)
CREATE NONCLUSTERED INDEX IX_Users_Email ON Users(Email);
GO

-- Index on Products.PlatformId for faster joins
CREATE NONCLUSTERED INDEX IX_Products_PlatformId ON Products(PlatformId);
GO

-- Index on Products.ExternalId for faster lookups
CREATE NONCLUSTERED INDEX IX_Products_ExternalId ON Products(ExternalId);
GO

-- Index on PriceSnapshots.ProductId for faster queries
CREATE NONCLUSTERED INDEX IX_PriceSnapshots_ProductId ON PriceSnapshots(ProductId);
GO

-- Index on PriceSnapshots.RecordedAt for time-based queries
CREATE NONCLUSTERED INDEX IX_PriceSnapshots_RecordedAt ON PriceSnapshots(RecordedAt);
GO

-- Index on SearchHistories.UserId for faster user history queries
CREATE NONCLUSTERED INDEX IX_SearchHistories_UserId ON SearchHistories(UserId);
GO

-- Index on SearchHistories.SearchTime for time-based queries
CREATE NONCLUSTERED INDEX IX_SearchHistories_SearchTime ON SearchHistories(SearchTime);
GO

-- Index on SystemLogs.CreatedAt for log queries
CREATE NONCLUSTERED INDEX IX_SystemLogs_CreatedAt ON SystemLogs(CreatedAt);
GO

-- Index on SystemLogs.Level for filtering logs by level
CREATE NONCLUSTERED INDEX IX_SystemLogs_Level ON SystemLogs(Level);
GO

-- Index on ProductMappings.SourceUrl for faster lookups
CREATE NONCLUSTERED INDEX IX_ProductMappings_SourceUrl ON ProductMappings(SourceUrl);
GO

-- Index on Categories.ParentCategoryId
CREATE NONCLUSTERED INDEX IX_Categories_ParentCategoryId ON Categories(ParentCategoryId);
GO

-- Index on PriceAlerts.UserId
CREATE NONCLUSTERED INDEX IX_PriceAlerts_UserId ON PriceAlerts(UserId);
GO

-- Index on PriceAlerts.ProductId
CREATE NONCLUSTERED INDEX IX_PriceAlerts_ProductId ON PriceAlerts(ProductId);
GO

-- Index on CrawlJobs.Status
CREATE NONCLUSTERED INDEX IX_CrawlJobs_Status ON CrawlJobs(Status);
GO

-- Index on Reviews.ProductId
CREATE NONCLUSTERED INDEX IX_Reviews_ProductId ON Reviews(ProductId);
GO

CREATE NONCLUSTERED INDEX IX_Stores_PlatformId ON Stores(PlatformId);
GO

CREATE NONCLUSTERED INDEX IX_StoreListings_ProductId ON StoreListings(ProductId);
GO

CREATE NONCLUSTERED INDEX IX_StoreListings_PlatformId ON StoreListings(PlatformId);
GO

CREATE NONCLUSTERED INDEX IX_Favorites_UserId ON Favorites(UserId);
GO

CREATE NONCLUSTERED INDEX IX_Favorites_ProductId ON Favorites(ProductId);
GO

CREATE NONCLUSTERED INDEX IX_DiscountCodes_Code ON DiscountCodes(Code);
GO

CREATE NONCLUSTERED INDEX IX_DiscountCodes_ProductId ON DiscountCodes(ProductId);
GO

CREATE NONCLUSTERED INDEX IX_DiscountCodes_PlatformId ON DiscountCodes(PlatformId);
GO

CREATE NONCLUSTERED INDEX IX_ProductNews_ProductId ON ProductNews(ProductId);
GO

CREATE NONCLUSTERED INDEX IX_ProductNews_PublishedAt ON ProductNews(PublishedAt);
GO

CREATE NONCLUSTERED INDEX IX_AffiliateLinks_ProductId ON AffiliateLinks(ProductId);
GO

CREATE NONCLUSTERED INDEX IX_AffiliateLinks_PlatformId ON AffiliateLinks(PlatformId);
GO

CREATE NONCLUSTERED INDEX IX_UserRoles_UserId ON UserRoles(UserId);
GO

CREATE NONCLUSTERED INDEX IX_RolePermissions_PermissionId ON RolePermissions(PermissionId);
GO

-- =============================================
-- Insert Seed Data
-- =============================================

-- Insert default platforms
INSERT INTO Platforms (PlatformName, Domain, ColorCode) VALUES
    (N'Shopee', 'shopee.vn', '#EE4D2D'),
    (N'Lazada', 'lazada.vn', '#0F146D'),
    (N'Tiki', 'tiki.vn', '#189EFF');
GO

-- Insert default categories
INSERT INTO Categories (CategoryName, IconUrl) VALUES
    (N'Nhà Sách Tiki', 'https://img.icons8.com/3d-fluency/94/books.png'),
    (N'Nhà Cửa - Đời Sống', 'https://img.icons8.com/3d-fluency/94/home.png'),
    (N'Điện Thoại - Máy Tính Bảng', 'https://img.icons8.com/3d-fluency/94/iphone.png'),
    (N'Đồ Chơi - Mẹ & Bé', 'https://img.icons8.com/3d-fluency/94/teddy-bear.png'),
    (N'Thiết Bị Số - Phụ Kiện Số', 'https://img.icons8.com/3d-fluency/94/headphones.png'),
    (N'Điện Gia Dụng', 'https://img.icons8.com/3d-fluency/94/washing-machine.png'),
    (N'Làm Đẹp - Sức Khỏe', 'https://img.icons8.com/3d-fluency/94/lipstick.png'),
    (N'Ô Tô - Xe Máy - Xe Đạp', 'https://img.icons8.com/3d-fluency/94/car.png'),
    (N'Thời Trang Nam', 'https://img.icons8.com/3d-fluency/94/t-shirt.png'),
    (N'Thời Trang Nữ', 'https://img.icons8.com/3d-fluency/94/dress.png'),
    (N'Giày - Dép Nam', 'https://img.icons8.com/3d-fluency/94/sneakers.png'),
    (N'Giày - Dép Nữ', 'https://img.icons8.com/3d-fluency/94/womens-shoe.png'),
    (N'Túi Thời Trang', 'https://img.icons8.com/3d-fluency/94/handbag.png');
GO

-- =============================================
-- Create Views (Optional - for common queries)
-- =============================================

-- View for Product with latest price
CREATE VIEW vw_ProductsWithLatestPrice AS
SELECT 
    p.ProductId,
    p.PlatformId,
    pl.PlatformName,
    p.ProductName,
    p.CurrentPrice,
    p.Rating,
    p.ReviewCount,
    p.ShopName,
    p.LastUpdated,
    ps.Price AS LatestSnapshotPrice,
    ps.RecordedAt AS LatestPriceDate
FROM Products p
LEFT JOIN Platforms pl ON p.PlatformId = pl.PlatformId
LEFT JOIN (
    SELECT ProductId, Price, RecordedAt,
           ROW_NUMBER() OVER (PARTITION BY ProductId ORDER BY RecordedAt DESC) AS rn
    FROM PriceSnapshots
) ps ON p.ProductId = ps.ProductId AND ps.rn = 1;
GO

-- View for User search statistics
CREATE VIEW vw_UserSearchStats AS
SELECT 
    u.UserId,
    u.FullName,
    u.Email,
    COUNT(sh.HistoryId) AS TotalSearches,
    MIN(sh.SearchTime) AS FirstSearch,
    MAX(sh.SearchTime) AS LastSearch,
    AVG(sh.BestPriceFound) AS AvgBestPrice
FROM Users u
LEFT JOIN SearchHistories sh ON u.UserId = sh.UserId
GROUP BY u.UserId, u.FullName, u.Email;
GO

-- =============================================
-- Stored Procedures (Optional - for common operations)
-- =============================================

-- Stored Procedure: Get Product Price History
CREATE PROCEDURE sp_GetProductPriceHistory
    @ProductId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        SnapshotId,
        Price,
        RecordedAt
    FROM PriceSnapshots
    WHERE ProductId = @ProductId
    ORDER BY RecordedAt DESC;
END
GO

-- Stored Procedure: Get User Search History
CREATE PROCEDURE sp_GetUserSearchHistory
    @UserId INT,
    @PageNumber INT = 1,
    @PageSize INT = 20
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;
    
    SELECT 
        HistoryId,
        SearchType,
        InputContent,
        DetectedKeyword,
        BestPriceFound,
        SearchTime
    FROM SearchHistories
    WHERE UserId = @UserId
    ORDER BY SearchTime DESC
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY;
    
    SELECT COUNT(*) AS TotalCount
    FROM SearchHistories
    WHERE UserId = @UserId;
END
GO

-- Stored Procedure: Clean Old Price Snapshots (older than specified days)
CREATE PROCEDURE sp_CleanOldPriceSnapshots
    @DaysToKeep INT = 90
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @CutoffDate DATETIME = DATEADD(DAY, -@DaysToKeep, GETDATE());
    
    DELETE FROM PriceSnapshots
    WHERE RecordedAt < @CutoffDate;
    
    SELECT @@ROWCOUNT AS DeletedRows;
END
GO

-- =============================================
-- Script completed successfully
-- =============================================
PRINT 'Database PriceWatcherDB created successfully!';
PRINT 'Tables created: Platforms, Users, Products, PriceSnapshots, SearchHistories, SystemLogs, Categories, PriceAlerts, CrawlJobs, Reviews, Stores, StoreListings, Favorites, DiscountCodes, ProductNews, AdminRoles, Permissions, RolePermissions, UserRoles, AffiliateLinks, UserPreferences';
PRINT 'Indexes created for performance optimization';
PRINT 'Seed data inserted for Platforms';
PRINT 'Views and Stored Procedures created';
GO
