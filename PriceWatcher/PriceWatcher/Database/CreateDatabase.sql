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
    CurrentPrice DECIMAL(18,2) NULL,
    Rating FLOAT NULL,
    ReviewCount INT NULL,
    ShopName NVARCHAR(200) NULL,
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
PRINT 'Tables created: Platforms, Users, Products, PriceSnapshots, SearchHistories, SystemLogs, Categories, PriceAlerts, CrawlJobs, Reviews';
PRINT 'Indexes created for performance optimization';
PRINT 'Seed data inserted for Platforms';
PRINT 'Views and Stored Procedures created';
GO

