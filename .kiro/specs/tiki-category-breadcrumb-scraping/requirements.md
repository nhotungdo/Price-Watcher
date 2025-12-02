# Requirements Document

## Introduction

This feature implements a comprehensive system for scraping and synchronizing Tiki's hierarchical product categories through breadcrumb navigation. The system will automatically extract category information from Tiki product pages, maintain a multi-level category hierarchy in the database, and ensure products are correctly classified according to Tiki's standard categorization (e.g., Electronics - Refrigeration > TV > Samsung TV).

## Glossary

- **Breadcrumb**: A navigation element on Tiki product pages showing the hierarchical path from root category to the current product's category (e.g., "Home > Electronics > TV > Samsung TV")
- **Category Hierarchy**: A tree structure where categories can have parent-child relationships, supporting unlimited depth levels
- **TikiCrawler**: The service component responsible for fetching and parsing product data from Tiki.vn
- **CategorySyncService**: The backend service that processes scraped breadcrumb data and synchronizes it with the database
- **Get-or-Create Algorithm**: A database operation pattern that checks if a record exists, returns it if found, or creates a new one if not found
- **Category Path**: The complete sequence of categories from root to leaf (e.g., "Electronics > TV > Samsung TV")
- **Leaf Category**: The most specific category in a hierarchy path (the final category before the product)

## Requirements

### Requirement 1

**User Story:** As a system administrator, I want the crawler to extract breadcrumb navigation from Tiki product pages, so that products can be automatically categorized according to Tiki's standard hierarchy.

#### Acceptance Criteria

1. WHEN the TikiCrawler fetches a product detail page THEN the system SHALL extract the complete breadcrumb navigation path from the HTML
2. WHEN breadcrumb data is extracted THEN the system SHALL parse it into an ordered list of category names preserving the hierarchy order
3. WHEN the breadcrumb contains special characters or Unicode text THEN the system SHALL handle them correctly without data corruption
4. WHEN the breadcrumb extraction fails THEN the system SHALL log the error and continue processing without crashing
5. WHEN a product page has no breadcrumb THEN the system SHALL assign the product to a default "Uncategorized" category

### Requirement 2

**User Story:** As a database administrator, I want the category table to support unlimited depth parent-child relationships, so that any level of category hierarchy can be represented accurately.

#### Acceptance Criteria

1. WHEN a category is created THEN the system SHALL allow it to reference a parent category through ParentCategoryId
2. WHEN a category has no parent THEN the system SHALL store NULL in ParentCategoryId to indicate it is a root category
3. WHEN querying a category THEN the system SHALL provide access to its parent category and all child categories
4. WHEN a category is deleted THEN the system SHALL handle orphaned child categories according to configured cascade rules
5. WHEN building a category tree THEN the system SHALL support recursive queries to traverse the entire hierarchy

### Requirement 3

**User Story:** As a backend developer, I want a CategorySyncService that implements the get-or-create algorithm, so that scraped categories are efficiently synchronized with the database without creating duplicates.

#### Acceptance Criteria

1. WHEN processing a breadcrumb path THEN the CategorySyncService SHALL iterate through each category level from root to leaf
2. WHEN checking for a category existence THEN the system SHALL query by CategoryName and ParentCategoryId to ensure uniqueness within the same parent
3. WHEN a category already exists THEN the system SHALL retrieve its CategoryId without creating a duplicate
4. WHEN a category does not exist THEN the system SHALL create a new category record with the appropriate ParentCategoryId
5. WHEN the sync process completes THEN the system SHALL return the CategoryId of the leaf category for product assignment

### Requirement 4

**User Story:** As a system architect, I want the category synchronization to be transactional, so that partial failures do not leave the database in an inconsistent state.

#### Acceptance Criteria

1. WHEN processing a breadcrumb path THEN the system SHALL execute all database operations within a single transaction
2. WHEN any database operation fails during sync THEN the system SHALL roll back all changes made in that transaction
3. WHEN a transaction is rolled back THEN the system SHALL log the error with complete context information
4. WHEN a transaction completes successfully THEN the system SHALL commit all changes atomically
5. WHEN concurrent requests process the same category THEN the system SHALL handle race conditions without creating duplicates

### Requirement 5

**User Story:** As a product manager, I want products to be linked to their most specific category, so that users can browse products by detailed categories.

#### Acceptance Criteria

1. WHEN a product is scraped THEN the system SHALL assign it to the leaf category from the breadcrumb path
2. WHEN updating product category THEN the system SHALL store the CategoryId in the Products table
3. WHEN a product's category changes THEN the system SHALL update the CategoryId to reflect the new classification
4. WHEN querying products by category THEN the system SHALL return all products directly assigned to that category
5. WHEN a category has no products THEN the system SHALL still maintain the category in the hierarchy for future use

### Requirement 6

**User Story:** As a data analyst, I want to query products by category hierarchy, so that I can analyze product distribution across different category levels.

#### Acceptance Criteria

1. WHEN querying products by a parent category THEN the system SHALL optionally include products from all descendant categories
2. WHEN building category statistics THEN the system SHALL calculate product counts for each category including subcategories
3. WHEN displaying category trees THEN the system SHALL show the full path from root to each category
4. WHEN a category path is requested THEN the system SHALL return the ordered list of ancestor categories
5. WHEN categories are displayed THEN the system SHALL include metadata such as product count and depth level

### Requirement 7

**User Story:** As a system maintainer, I want comprehensive logging of the category synchronization process, so that I can troubleshoot issues and monitor system health.

#### Acceptance Criteria

1. WHEN a new category is created THEN the system SHALL log the category name, parent, and assigned ID
2. WHEN an existing category is found THEN the system SHALL log the matched category information at debug level
3. WHEN breadcrumb extraction fails THEN the system SHALL log the product URL and error details at warning level
4. WHEN database operations fail THEN the system SHALL log the exception with full stack trace at error level
5. WHEN sync operations complete THEN the system SHALL log summary statistics including categories processed and created

### Requirement 8

**User Story:** As a performance engineer, I want the category synchronization to be optimized for bulk operations, so that large-scale crawling does not degrade system performance.

#### Acceptance Criteria

1. WHEN processing multiple products THEN the system SHALL cache category lookups to minimize database queries
2. WHEN the cache is used THEN the system SHALL maintain consistency with the database state
3. WHEN bulk syncing categories THEN the system SHALL batch database operations where possible
4. WHEN category queries are executed THEN the system SHALL use appropriate database indexes for optimal performance
5. WHEN the cache grows large THEN the system SHALL implement eviction policies to prevent memory issues
