# Implementation Plan

- [ ] 1. Add database constraints for category uniqueness
  - Create migration script to add unique indexes on Categories table
  - Add index for (CategoryName, ParentCategoryId) where ParentCategoryId IS NOT NULL
  - Add index for (CategoryName) where ParentCategoryId IS NULL
  - Test constraint enforcement with duplicate inserts
  - _Requirements: 2.1, 2.2, 3.2_

- [ ] 2. Implement BreadcrumbParser service
  - [ ] 2.1 Create IBreadcrumbParser interface and implementation class
    - Define interface with ParseBreadcrumbFromJson and ParseBreadcrumbFromHtml methods
    - Implement JSON parsing to extract breadcrumb array from Tiki API response
    - Implement HTML parsing using HtmlAgilityPack for fallback scenarios
    - Add Unicode character handling and normalization
    - Return empty list when breadcrumb not found
    - _Requirements: 1.1, 1.2, 1.3, 1.5_

  - [ ]* 2.2 Write property test for breadcrumb order preservation
    - **Property 1: Breadcrumb extraction preserves order**
    - **Validates: Requirements 1.2**

  - [ ]* 2.3 Write property test for Unicode handling
    - **Property 10: Unicode handling correctness**
    - **Validates: Requirements 1.3**

  - [ ]* 2.4 Write unit tests for BreadcrumbParser
    - Test parsing valid JSON with breadcrumb array
    - Test parsing HTML with breadcrumb schema markup
    - Test handling missing breadcrumb (returns empty list)
    - Test malformed JSON/HTML (graceful failure)
    - Test whitespace trimming and normalization
    - _Requirements: 1.1, 1.2, 1.3, 1.4_

- [ ] 3. Implement CategorySyncService with caching
  - [ ] 3.1 Create ICategorySyncService interface and implementation
    - Define interface with SyncCategoryPathAsync, GetOrCreateCategoryAsync, and ClearCache methods
    - Inject IMemoryCache and PriceWatcherDbContext
    - Implement cache key generation format: "cat:{name}:{parentId}"
    - Configure cache with 1-hour sliding expiration
    - _Requirements: 3.1, 8.1, 8.2_

  - [ ] 3.2 Implement GetOrCreateCategoryAsync method
    - Check cache first for existing category
    - Query database by CategoryName and ParentCategoryId if cache miss
    - Create new category if not found
    - Store result in cache before returning
    - Handle DbUpdateException for duplicate key violations
    - Add comprehensive logging (Debug for cache hits, Info for creates)
    - _Requirements: 3.2, 3.3, 3.4, 4.5, 7.1, 7.2_

  - [ ] 3.3 Implement SyncCategoryPathAsync method with transactions
    - Start database transaction with ReadCommitted isolation
    - Iterate through category path from root to leaf
    - Call GetOrCreateCategoryAsync for each level
    - Track parent ID as iterating through path
    - Commit transaction on success
    - Rollback transaction on any failure
    - Return leaf category ID
    - _Requirements: 3.1, 4.1, 4.2, 4.3, 4.4_

  - [ ]* 3.4 Write property test for category uniqueness
    - **Property 2: Category uniqueness within parent**
    - **Validates: Requirements 3.2**

  - [ ]* 3.5 Write property test for sync idempotence
    - **Property 3: Get-or-create idempotence**
    - **Validates: Requirements 3.3, 3.4**

  - [ ]* 3.6 Write property test for transaction atomicity
    - **Property 4: Transaction atomicity**
    - **Validates: Requirements 4.1, 4.2**

  - [ ]* 3.7 Write property test for concurrent sync safety
    - **Property 9: Concurrent sync safety**
    - **Validates: Requirements 4.5**

  - [ ]* 3.8 Write unit tests for CategorySyncService
    - Test creating new category path (all new categories)
    - Test syncing existing category path (all categories exist)
    - Test mixed path (some exist, some new)
    - Test cache hit and miss scenarios
    - Test transaction rollback on database error
    - Test null/empty category name handling
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 4.1, 4.2, 8.1, 8.2_

- [ ] 4. Enhance TikiCrawler to extract and sync categories
  - [ ] 4.1 Update CrawledProduct DTO
    - Add CategoryPath property (List<string>)
    - Add CategoryId property (int?)
    - _Requirements: 1.2, 5.1_

  - [ ] 4.2 Modify TikiCrawler to extract breadcrumb data
    - Inject IBreadcrumbParser into TikiCrawler constructor
    - After parsing product JSON, call ParseBreadcrumbFromJson
    - Store breadcrumb path in CrawledProduct.CategoryPath
    - Log warning if breadcrumb extraction fails
    - _Requirements: 1.1, 1.2, 1.4, 7.3_

  - [ ] 4.3 Integrate CategorySyncService into TikiCrawler
    - Inject ICategorySyncService into TikiCrawler constructor
    - After extracting breadcrumb, call SyncCategoryPathAsync
    - Handle empty breadcrumb by using default "Uncategorized" category
    - Store returned category ID in CrawledProduct.CategoryId
    - Log sync completion with category details
    - _Requirements: 3.1, 3.5, 5.1, 7.1, 7.5_

  - [ ]* 4.4 Write property test for product assignment correctness
    - **Property 8: Product assignment correctness**
    - **Validates: Requirements 5.1, 5.2**

  - [ ]* 4.5 Write integration tests for TikiCrawler
    - Test end-to-end crawl with breadcrumb extraction and sync
    - Test handling products without breadcrumbs
    - Test with real Tiki product URLs (sample set)
    - _Requirements: 1.1, 1.2, 3.1, 5.1_

- [ ] 5. Update product persistence to save category assignments
  - [ ] 5.1 Modify product save logic to include CategoryId
    - Update ProductService or relevant service to accept CategoryId
    - Ensure Product.CategoryId is set from CrawledProduct.CategoryId
    - Save product with category assignment
    - _Requirements: 5.1, 5.2, 5.3_

  - [ ]* 5.2 Write unit tests for product category assignment
    - Test saving product with valid CategoryId
    - Test updating product CategoryId
    - Test querying products by CategoryId
    - _Requirements: 5.1, 5.2, 5.4_

- [ ] 6. Implement CategoryQueryService for advanced queries
  - [ ] 6.1 Create ICategoryQueryService interface and implementation
    - Define interface with GetProductsInCategoryTreeAsync, GetCategoryPathAsync, GetCategoryStatisticsAsync, GetDescendantCategoriesAsync
    - Inject PriceWatcherDbContext
    - _Requirements: 6.1, 6.3, 6.4_

  - [ ] 6.2 Implement GetProductsInCategoryTreeAsync
    - Recursively get all descendant category IDs
    - Query products where CategoryId is in the set
    - Return list of products
    - _Requirements: 6.1_

  - [ ] 6.3 Implement GetCategoryPathAsync
    - Start from specified category
    - Traverse parent relationships to root
    - Build ordered list from root to specified category
    - _Requirements: 6.3, 6.4_

  - [ ] 6.4 Implement GetCategoryStatisticsAsync
    - Count direct products in category
    - Count products in descendant categories if includeDescendants is true
    - Count descendant categories
    - Calculate depth level
    - Build path from root
    - Return CategoryStatistics DTO
    - _Requirements: 6.2, 6.5_

  - [ ] 6.5 Implement GetDescendantCategoriesAsync
    - Recursively query all child categories
    - Return flattened list of descendants
    - _Requirements: 6.1_

  - [ ]* 6.6 Write property test for parent-child consistency
    - **Property 5: Parent-child relationship consistency**
    - **Validates: Requirements 2.1, 2.2**

  - [ ]* 6.7 Write property test for path completeness
    - **Property 6: Category path completeness**
    - **Validates: Requirements 2.5**

  - [ ]* 6.8 Write unit tests for CategoryQueryService
    - Test getting products from category tree
    - Test building category path to root
    - Test calculating statistics with and without descendants
    - Test handling categories with no products
    - Test deep hierarchy traversal (10+ levels)
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5_

- [ ] 7. Register new services in dependency injection
  - Register IBreadcrumbParser as scoped service
  - Register ICategorySyncService as scoped service
  - Register ICategoryQueryService as scoped service
  - Update Program.cs with service registrations
  - _Requirements: All_

- [ ] 8. Add comprehensive logging throughout the system
  - [ ] 8.1 Add structured logging to BreadcrumbParser
    - Log breadcrumb extraction attempts
    - Log successful extractions with category count
    - Log failures with product URL context
    - _Requirements: 7.3, 7.4_

  - [ ] 8.2 Add structured logging to CategorySyncService
    - Log new category creations with details
    - Log cache hits at debug level
    - Log transaction rollbacks with full context
    - Log sync completion with summary statistics
    - _Requirements: 7.1, 7.2, 7.4, 7.5_

  - [ ] 8.3 Add structured logging to TikiCrawler integration
    - Log category sync initiation
    - Log successful category assignments
    - Log fallback to uncategorized
    - _Requirements: 7.3, 7.5_

- [ ] 9. Create default "Uncategorized" category
  - Write migration or seed script to create "Uncategorized" root category
  - Ensure it exists before any crawling begins
  - Document the CategoryId for reference in code
  - _Requirements: 1.5_

- [ ] 10. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ]* 11. Performance testing and optimization
  - [ ]* 11.1 Benchmark category sync performance
    - Test sync with 1000 products
    - Measure average sync time per product
    - Measure cache hit rate over time
    - _Requirements: 8.1, 8.2, 8.4_

  - [ ]* 11.2 Test deep hierarchy performance
    - Create test data with 10+ level hierarchies
    - Measure query performance for GetCategoryPathAsync
    - Measure query performance for GetProductsInCategoryTreeAsync
    - _Requirements: 2.5, 6.1, 6.3_

  - [ ]* 11.3 Test concurrent crawler scenarios
    - Run multiple crawler instances simultaneously
    - Verify no duplicate categories created
    - Measure transaction contention
    - _Requirements: 4.5, 8.3_

- [ ]* 12. Create monitoring and alerting configuration
  - Define metrics to track (categories created, cache hit rate, sync time, etc.)
  - Set up alerts for high error rates
  - Create dashboard for category growth and distribution
  - _Requirements: 7.5, 8.4_

- [ ] 13. Final checkpoint - Verify end-to-end functionality
  - Ensure all tests pass, ask the user if questions arise.
