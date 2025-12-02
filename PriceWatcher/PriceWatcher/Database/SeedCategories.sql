-- Seed default categories for PriceWatcher
-- Run this script to populate initial categories

-- Check if categories already exist
IF NOT EXISTS (SELECT 1 FROM Categories WHERE CategoryName = 'Electronics')
BEGIN
    -- Root Categories
    INSERT INTO Categories (CategoryName, ParentCategoryId, IconUrl, CreatedAt) VALUES
    ('Electronics', NULL, 'https://cdn-icons-png.flaticon.com/512/684/684908.png', GETUTCDATE()),
    ('Fashion', NULL, 'https://cdn-icons-png.flaticon.com/512/3050/3050155.png', GETUTCDATE()),
    ('Home & Living', NULL, 'https://cdn-icons-png.flaticon.com/512/1946/1946488.png', GETUTCDATE()),
    ('Beauty & Personal Care', NULL, 'https://cdn-icons-png.flaticon.com/512/3081/3081840.png', GETUTCDATE()),
    ('Sports & Outdoors', NULL, 'https://cdn-icons-png.flaticon.com/512/857/857418.png', GETUTCDATE()),
    ('Books & Media', NULL, 'https://cdn-icons-png.flaticon.com/512/2702/2702154.png', GETUTCDATE()),
    ('Toys & Games', NULL, 'https://cdn-icons-png.flaticon.com/512/2553/2553642.png', GETUTCDATE()),
    ('Food & Beverages', NULL, 'https://cdn-icons-png.flaticon.com/512/3075/3075977.png', GETUTCDATE()),
    ('Health & Wellness', NULL, 'https://cdn-icons-png.flaticon.com/512/2913/2913133.png', GETUTCDATE()),
    ('Automotive', NULL, 'https://cdn-icons-png.flaticon.com/512/3097/3097108.png', GETUTCDATE());

    -- Electronics Subcategories
    DECLARE @ElectronicsId INT = (SELECT CategoryId FROM Categories WHERE CategoryName = 'Electronics');
    INSERT INTO Categories (CategoryName, ParentCategoryId, IconUrl, CreatedAt) VALUES
    ('Smartphones', @ElectronicsId, NULL, GETUTCDATE()),
    ('Laptops & Computers', @ElectronicsId, NULL, GETUTCDATE()),
    ('Tablets', @ElectronicsId, NULL, GETUTCDATE()),
    ('Headphones & Audio', @ElectronicsId, NULL, GETUTCDATE()),
    ('Cameras & Photography', @ElectronicsId, NULL, GETUTCDATE()),
    ('TVs & Monitors', @ElectronicsId, NULL, GETUTCDATE()),
    ('Gaming', @ElectronicsId, NULL, GETUTCDATE()),
    ('Smart Home', @ElectronicsId, NULL, GETUTCDATE());

    -- Fashion Subcategories
    DECLARE @FashionId INT = (SELECT CategoryId FROM Categories WHERE CategoryName = 'Fashion');
    INSERT INTO Categories (CategoryName, ParentCategoryId, IconUrl, CreatedAt) VALUES
    ('Men''s Clothing', @FashionId, NULL, GETUTCDATE()),
    ('Women''s Clothing', @FashionId, NULL, GETUTCDATE()),
    ('Shoes', @FashionId, NULL, GETUTCDATE()),
    ('Bags & Accessories', @FashionId, NULL, GETUTCDATE()),
    ('Watches', @FashionId, NULL, GETUTCDATE()),
    ('Jewelry', @FashionId, NULL, GETUTCDATE());

    -- Home & Living Subcategories
    DECLARE @HomeId INT = (SELECT CategoryId FROM Categories WHERE CategoryName = 'Home & Living');
    INSERT INTO Categories (CategoryName, ParentCategoryId, IconUrl, CreatedAt) VALUES
    ('Furniture', @HomeId, NULL, GETUTCDATE()),
    ('Kitchen & Dining', @HomeId, NULL, GETUTCDATE()),
    ('Bedding & Bath', @HomeId, NULL, GETUTCDATE()),
    ('Home Decor', @HomeId, NULL, GETUTCDATE()),
    ('Lighting', @HomeId, NULL, GETUTCDATE()),
    ('Storage & Organization', @HomeId, NULL, GETUTCDATE());

    -- Beauty Subcategories
    DECLARE @BeautyId INT = (SELECT CategoryId FROM Categories WHERE CategoryName = 'Beauty & Personal Care');
    INSERT INTO Categories (CategoryName, ParentCategoryId, IconUrl, CreatedAt) VALUES
    ('Skincare', @BeautyId, NULL, GETUTCDATE()),
    ('Makeup', @BeautyId, NULL, GETUTCDATE()),
    ('Haircare', @BeautyId, NULL, GETUTCDATE()),
    ('Fragrances', @BeautyId, NULL, GETUTCDATE()),
    ('Personal Care', @BeautyId, NULL, GETUTCDATE());

    -- Sports Subcategories
    DECLARE @SportsId INT = (SELECT CategoryId FROM Categories WHERE CategoryName = 'Sports & Outdoors');
    INSERT INTO Categories (CategoryName, ParentCategoryId, IconUrl, CreatedAt) VALUES
    ('Fitness Equipment', @SportsId, NULL, GETUTCDATE()),
    ('Sportswear', @SportsId, NULL, GETUTCDATE()),
    ('Outdoor Gear', @SportsId, NULL, GETUTCDATE()),
    ('Cycling', @SportsId, NULL, GETUTCDATE()),
    ('Team Sports', @SportsId, NULL, GETUTCDATE());

    PRINT 'Categories seeded successfully!';
END
ELSE
BEGIN
    PRINT 'Categories already exist. Skipping seed.';
END
