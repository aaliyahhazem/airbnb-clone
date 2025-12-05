-- Fix missing FK constraint on Favorites table
-- This script adds the missing FK from Favorites to Listings

USE AirbnbCloneDb;
GO

-- Check if FK already exists
IF NOT EXISTS (
    SELECT 1 
    FROM sys.foreign_keys 
    WHERE name = 'FK_Favorites_Listings_ListingId' 
    AND parent_object_id = OBJECT_ID('Favorites')
)
BEGIN
    PRINT 'Adding FK_Favorites_Listings_ListingId constraint...';
    
    ALTER TABLE [Favorites]
    ADD CONSTRAINT [FK_Favorites_Listings_ListingId]
   FOREIGN KEY ([ListingId])
    REFERENCES [Listings] ([Id])
        ON DELETE NO ACTION;
        
    PRINT 'FK constraint added successfully.';
END
ELSE
BEGIN
    PRINT 'FK_Favorites_Listings_ListingId already exists.';
END
GO

-- Verify the constraint was added
SELECT 
    fk.name AS FK_Name,
    OBJECT_NAME(fk.parent_object_id) AS Table_Name,
    COL_NAME(fc.parent_object_id, fc.parent_column_id) AS Column_Name,
    OBJECT_NAME(fk.referenced_object_id) AS Referenced_Table,
    COL_NAME(fc.referenced_object_id, fc.referenced_column_id) AS Referenced_Column
FROM 
    sys.foreign_keys AS fk
INNER JOIN 
    sys.foreign_key_columns AS fc ON fk.object_id = fc.constraint_object_id
WHERE 
    OBJECT_NAME(fk.parent_object_id) = 'Favorites'
    AND fk.name = 'FK_Favorites_Listings_ListingId';
GO
