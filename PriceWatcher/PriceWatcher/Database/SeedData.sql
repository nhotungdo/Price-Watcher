-- =============================================
-- PriceWatcher Database Seed Data
-- Sample data for testing and development
-- =============================================

USE PriceWatcherDB;
GO

-- =============================================
-- Clear existing data (optional - use with caution)
-- =============================================
-- Uncomment the following lines if you want to clear existing data first
/*
DELETE FROM PriceSnapshots;
DELETE FROM Products;
DELETE FROM SearchHistories;
DELETE FROM Users;
DELETE FROM SystemLogs;
-- Platforms are kept as they are seed data
*/

-- =============================================
-- Insert Sample Users
-- =============================================
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'admin@pricewatcher.com')
BEGIN
    INSERT INTO Users (FullName, Email, AvatarUrl, CreatedAt, LastLogin)
    VALUES 
        (N'Admin User', 'admin@pricewatcher.com', NULL, GETDATE(), GETDATE()),
        (N'Nguyễn Văn A', 'nguyenvana@example.com', NULL, GETDATE(), NULL),
        (N'Trần Thị B', 'tranthib@example.com', NULL, GETDATE(), NULL);
END
GO

-- =============================================
-- Insert Sample Products
-- =============================================
DECLARE @ShopeePlatformId INT = (SELECT PlatformId FROM Platforms WHERE PlatformName = N'Shopee');
DECLARE @LazadaPlatformId INT = (SELECT PlatformId FROM Platforms WHERE PlatformName = N'Lazada');
DECLARE @TikiPlatformId INT = (SELECT PlatformId FROM Platforms WHERE PlatformName = N'Tiki');

-- Sample Shopee Products
IF NOT EXISTS (SELECT 1 FROM Products WHERE ExternalId = 'SP001')
BEGIN
    INSERT INTO Products (PlatformId, ExternalId, ProductName, OriginalUrl, CurrentPrice, Rating, ReviewCount, ShopName, LastUpdated)
    VALUES 
        (@ShopeePlatformId, 'SP001', N'iPhone 15 Pro Max 256GB', 'https://shopee.vn/iphone-15-pro-max', 29990000, 4.8, 1250, N'Apple Store', GETDATE()),
        (@ShopeePlatformId, 'SP002', N'Samsung Galaxy S24 Ultra', 'https://shopee.vn/galaxy-s24-ultra', 27990000, 4.7, 890, N'Samsung Official', GETDATE()),
        (@ShopeePlatformId, 'SP003', N'MacBook Pro M3 14 inch', 'https://shopee.vn/macbook-pro-m3', 45990000, 4.9, 560, N'Apple Store', GETDATE());
    UPDATE Products SET 
        OriginalPrice = CurrentPrice,
        DiscountRate = 0,
        StockStatus = N'InStock',
        SoldQuantity = COALESCE(SoldQuantity, 0),
        ShippingInfo = N'Miễn phí'
    WHERE ExternalId IN ('SP001','SP002','SP003');
END
GO

-- Sample Lazada Products
IF NOT EXISTS (SELECT 1 FROM Products WHERE ExternalId = 'LZ001')
BEGIN
    INSERT INTO Products (PlatformId, ExternalId, ProductName, OriginalUrl, CurrentPrice, Rating, ReviewCount, ShopName, LastUpdated)
    VALUES 
        (@LazadaPlatformId, 'LZ001', N'AirPods Pro 2', 'https://lazada.vn/airpods-pro-2', 5990000, 4.6, 2340, N'Apple Store', GETDATE()),
        (@LazadaPlatformId, 'LZ002', N'Xiaomi Redmi Note 13 Pro', 'https://lazada.vn/redmi-note-13', 7990000, 4.5, 1560, N'Xiaomi Official', GETDATE());
    UPDATE Products SET 
        OriginalPrice = CurrentPrice,
        DiscountRate = 0,
        StockStatus = N'InStock',
        SoldQuantity = COALESCE(SoldQuantity, 0),
        ShippingInfo = N'Miễn phí'
    WHERE ExternalId IN ('LZ001','LZ002');
END
GO

-- Sample Tiki Products
IF NOT EXISTS (SELECT 1 FROM Products WHERE ExternalId = 'TK001')
BEGIN
    INSERT INTO Products (PlatformId, ExternalId, ProductName, OriginalUrl, CurrentPrice, Rating, ReviewCount, ShopName, LastUpdated)
    VALUES 
        (@TikiPlatformId, 'TK001', N'Dell XPS 15 Laptop', 'https://tiki.vn/dell-xps-15', 32990000, 4.7, 780, N'Tiki Trading', GETDATE()),
        (@TikiPlatformId, 'TK002', N'Logitech MX Master 3S', 'https://tiki.vn/logitech-mx-master', 2490000, 4.8, 1200, N'Tiki Trading', GETDATE());
    UPDATE Products SET 
        OriginalPrice = CurrentPrice,
        DiscountRate = 0,
        StockStatus = N'InStock',
        SoldQuantity = COALESCE(SoldQuantity, 0),
        ShippingInfo = N'Miễn phí'
    WHERE ExternalId IN ('TK001','TK002');
END
GO

-- =============================================
-- Insert Sample Price Snapshots
-- =============================================
DECLARE @Product1 INT = (SELECT ProductId FROM Products WHERE ExternalId = 'SP001');
DECLARE @Product2 INT = (SELECT ProductId FROM Products WHERE ExternalId = 'SP002');
DECLARE @Product3 INT = (SELECT ProductId FROM Products WHERE ExternalId = 'SP003');

-- Insert price history for Product 1 (iPhone 15 Pro Max)
IF @Product1 IS NOT NULL AND NOT EXISTS (SELECT 1 FROM PriceSnapshots WHERE ProductId = @Product1)
BEGIN
    INSERT INTO PriceSnapshots (ProductId, Price, RecordedAt)
    VALUES 
        (@Product1, 30990000, DATEADD(DAY, -30, GETDATE())),
        (@Product1, 30490000, DATEADD(DAY, -20, GETDATE())),
        (@Product1, 29990000, DATEADD(DAY, -10, GETDATE())),
        (@Product1, 29990000, GETDATE());
    UPDATE PriceSnapshots SET 
        OriginalPrice = Price,
        ShippingInfo = N'Miễn phí'
    WHERE ProductId = @Product1;
END
GO

-- Insert price history for Product 2 (Samsung Galaxy S24 Ultra)
IF @Product2 IS NOT NULL AND NOT EXISTS (SELECT 1 FROM PriceSnapshots WHERE ProductId = @Product2)
BEGIN
    INSERT INTO PriceSnapshots (ProductId, Price, RecordedAt)
    VALUES 
        (@Product2, 28990000, DATEADD(DAY, -25, GETDATE())),
        (@Product2, 28490000, DATEADD(DAY, -15, GETDATE())),
        (@Product2, 27990000, DATEADD(DAY, -5, GETDATE())),
        (@Product2, 27990000, GETDATE());
    UPDATE PriceSnapshots SET 
        OriginalPrice = Price,
        ShippingInfo = N'Miễn phí'
    WHERE ProductId = @Product2;
END
GO

-- =============================================
-- Insert Sample Search Histories
-- =============================================
DECLARE @User1 INT = (SELECT UserId FROM Users WHERE Email = 'admin@pricewatcher.com');
DECLARE @User2 INT = (SELECT UserId FROM Users WHERE Email = 'nguyenvana@example.com');

-- Sample search histories for User 1
IF @User1 IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SearchHistories WHERE UserId = @User1)
BEGIN
    INSERT INTO SearchHistories (UserId, SearchType, InputContent, DetectedKeyword, BestPriceFound, SearchTime)
    VALUES 
        (@User1, 'LINK', 'https://shopee.vn/iphone-15-pro-max', N'iPhone 15 Pro Max', 29990000, DATEADD(DAY, -5, GETDATE())),
        (@User1, 'IMAGE', NULL, N'Samsung Galaxy', 27990000, DATEADD(DAY, -3, GETDATE())),
        (@User1, 'KEYWORD', N'laptop gaming', N'laptop gaming', 32990000, DATEADD(DAY, -1, GETDATE()));
END
GO

-- Sample search histories for User 2
IF @User2 IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SearchHistories WHERE UserId = @User2)
BEGIN
    INSERT INTO SearchHistories (UserId, SearchType, InputContent, DetectedKeyword, BestPriceFound, SearchTime)
    VALUES 
        (@User2, 'KEYWORD', N'airpods', N'AirPods', 5990000, DATEADD(DAY, -7, GETDATE())),
        (@User2, 'LINK', 'https://tiki.vn/logitech-mx-master', N'Logitech MX Master', 2490000, DATEADD(DAY, -2, GETDATE()));
END
GO

-- =============================================
-- Insert Sample System Logs
-- =============================================
IF NOT EXISTS (SELECT 1 FROM SystemLogs WHERE Message LIKE '%Database initialized%')
BEGIN
    INSERT INTO SystemLogs (Level, Message, CreatedAt)
    VALUES 
        ('INFO', 'Database initialized successfully', GETDATE()),
        ('INFO', 'Seed data inserted', GETDATE()),
        ('INFO', 'Sample users created', GETDATE()),
        ('INFO', 'Sample products created', GETDATE());
END
GO

-- =============================================
-- Verification Queries
-- =============================================
PRINT 'Seed data insertion completed!';
PRINT '';
PRINT 'Summary:';
PRINT '--------';
PRINT 'Users: ' + CAST((SELECT COUNT(*) FROM Users) AS VARCHAR(10));
PRINT 'Platforms: ' + CAST((SELECT COUNT(*) FROM Platforms) AS VARCHAR(10));
PRINT 'Products: ' + CAST((SELECT COUNT(*) FROM Products) AS VARCHAR(10));
PRINT 'Price Snapshots: ' + CAST((SELECT COUNT(*) FROM PriceSnapshots) AS VARCHAR(10));
PRINT 'Search Histories: ' + CAST((SELECT COUNT(*) FROM SearchHistories) AS VARCHAR(10));
PRINT 'System Logs: ' + CAST((SELECT COUNT(*) FROM SystemLogs) AS VARCHAR(10));
GO

