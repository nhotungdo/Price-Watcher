-- =============================================
-- Add Missing Columns to Products Table
-- =============================================

USE PriceWatcherDB;
GO

-- Add CategoryId column if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'CategoryId')
BEGIN
    ALTER TABLE Products
    ADD CategoryId INT NULL;
    
    -- Add foreign key constraint
    ALTER TABLE Products
    ADD CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryId) 
        REFERENCES Categories(CategoryId);
    
    -- Add index for performance
    CREATE NONCLUSTERED INDEX IX_Products_CategoryId ON Products(CategoryId);
    
    PRINT 'CategoryId column added successfully';
END
ELSE
BEGIN
    PRINT 'CategoryId column already exists';
END
GO

-- Add IsFreeShip column if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'IsFreeShip')
BEGIN
    ALTER TABLE Products
    ADD IsFreeShip BIT NULL;
    
    PRINT 'IsFreeShip column added successfully';
END
ELSE
BEGIN
    PRINT 'IsFreeShip column already exists';
END
GO

-- Add IsVerified column if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'IsVerified')
BEGIN
    ALTER TABLE Products
    ADD IsVerified BIT NULL;
    
    PRINT 'IsVerified column added successfully';
END
ELSE
BEGIN
    PRINT 'IsVerified column already exists';
END
GO

-- Add CreatedAt column if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'CreatedAt')
BEGIN
    ALTER TABLE Products
    ADD CreatedAt DATETIME NULL DEFAULT GETDATE();
    
    -- Update existing rows to have a CreatedAt value
    UPDATE Products
    SET CreatedAt = GETDATE()
    WHERE CreatedAt IS NULL;
    
    PRINT 'CreatedAt column added successfully';
END
ELSE
BEGIN
    PRINT 'CreatedAt column already exists';
END
GO

PRINT 'All missing columns have been added to Products table';
GO
