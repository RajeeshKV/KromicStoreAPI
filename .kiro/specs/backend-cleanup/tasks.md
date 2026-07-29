# Backend Cleanup Tasks

## Sprint 1: Theme Architecture Consolidation

### Phase 1A: Update Theme Entity Model

- [ ] Update Theme entity: add TenantId field
- [ ] Update Theme entity: add IsPublic field
- [ ] Update Theme entity: add SourceThemeId field
- [ ] Update Theme entity: add OwnerTenantId field
- [ ] Update Theme entity: add CreatedBy (UserId) field
- [ ] Update Theme entity: add LastModifiedBy (UserId) field
- [ ] Rename ThemeEntity to Theme in domain model
- [ ] Update all usages of ThemeEntity to Theme
- [ ] Create EF Core migration for new fields

### Phase 1B: Data Migration

- [ ] Create migration script to populate TenantId = null for platform themes
- [ ] Create migration script to populate OwnerTenantId = SuperUser for existing themes
- [ ] Create migration script to copy TenantTheme data to Theme table
- [ ] Verify data integrity after migration
- [ ] Test migration rollback procedure
- [ ] Document migration completion status

### Phase 1C: Update ThemeController

- [ ] Add POST /api/v1/themes (create tenant theme)
- [ ] Add PUT /api/v1/themes/{id} (update theme)
- [ ] Add GET /api/v1/themes (list themes - platform + tenant)
- [ ] Add GET /api/v1/themes/{id} (get theme details)
- [ ] Add POST /api/v1/themes/{id}/clone (clone theme)
- [ ] Add DELETE /api/v1/themes/{id} (delete theme)
- [ ] Add permission checks: tenant can only access own/public themes
- [ ] Add permission checks: SuperUser can access all themes
- [ ] Add validation: prevent theme name conflicts
- [ ] Keep existing endpoints working (backward compatibility)
- [ ] Add unit tests for new controller methods
- [ ] Add integration tests for theme CRUD

### Phase 1D: Update Storefront References

- [ ] Update Storefront.ThemeId to reference Theme entity
- [ ] Update queries to filter themes by TenantId + IsPublic
- [ ] Update StorefrontController to list available themes
- [ ] Add validation: theme must be accessible to tenant
- [ ] Create migration to fix existing storefront theme references

---

## Sprint 2: Field Duplication Resolution

### Phase 2A: Tenant Entity Updates

- [ ] Remove ContactPhone from Tenant entity
- [ ] Remove LogoUrl from Tenant entity (or mark as platform-only)
- [ ] Create EF Core migration for field removal
- [ ] Create data migration to backup removed data to Storefront
- [ ] Add soft-delete or archive flag if needed

### Phase 2B: Storefront Entity Updates

- [ ] Add ContactPhone to Storefront entity (if missing)
- [ ] Add LogoUrl to Storefront entity (if missing)
- [ ] Create EF Core migration for new fields
- [ ] Create data migration to populate from Tenant if applicable

### Phase 2C: Update Controllers

- [ ] Update TenantController GET: exclude removed fields
- [ ] Update TenantController PUT: prevent setting storefront-only fields
- [ ] Update StorefrontController GET: include all storefront fields
- [ ] Update StorefrontController PUT: handle all storefront fields
- [ ] Add validation: clear error messages for field misplacement
- [ ] Add API documentation for field ownership

### Phase 2D: Update Services

- [ ] Update TenantService to work with new schema
- [ ] Update StorefrontService to work with new schema
- [ ] Update any queries that accessed removed fields
- [ ] Add unit tests for updated services
- [ ] Add integration tests for field consolidation

---

## Sprint 3: Configuration System Cleanup

### Phase 3A: Audit Configuration

- [ ] List all TenantConfiguration keys in codebase
- [ ] List all TenantConfiguration keys in production database
- [ ] Categorize each key: Platform / Tenant-level / Storefront
- [ ] Document current usage of each key
- [ ] Identify misplaced keys (storefront in TenantConfiguration)

### Phase 3B: Move Settings

- [ ] Create EF Core migrations for schema changes
- [ ] Create data migration to move storefront settings to Storefront entity
- [ ] Create data migration to move tenant settings appropriately
- [ ] Update ConfigurationService to read from correct locations
- [ ] Add unit tests for migrated settings
- [ ] Verify backward compatibility

### Phase 3C: Update ConfigController

- [ ] Add validation: only allow platform configuration keys
- [ ] Add validation: prevent storefront keys in config endpoint
- [ ] Add validation: prevent tenant-specific keys in platform config
- [ ] Return descriptive error if invalid key attempted
- [ ] Add documentation: list allowed configuration keys
- [ ] Add logging for all configuration changes
- [ ] Create tests for validation logic

### Phase 3D: Add Safeguards

- [ ] Add database constraints to prevent invalid data
- [ ] Add application-level validation in ConfigurationService
- [ ] Add audit logging for configuration access
- [ ] Add configuration change notifications
- [ ] Create monitoring for configuration abuse
- [ ] Add documentation in API guide

---

## Sprint 4: Testing & Verification

### Phase 4A: Integration Testing

- [ ] Test theme creation by tenant
- [ ] Test theme modification by tenant
- [ ] Test theme cloning by tenant
- [ ] Test theme deletion by tenant
- [ ] Test theme access control (tenant isolation)
- [ ] Test platform theme creation by SuperUser
- [ ] Test platform theme modification by SuperUser
- [ ] Test storefront CRUD operations
- [ ] Test configuration validation
- [ ] Test backward compatibility of old endpoints

### Phase 4B: Data Verification

- [ ] Verify all theme data migrated correctly
- [ ] Verify no orphaned records
- [ ] Verify no data loss
- [ ] Verify constraint enforcement
- [ ] Verify field ownership boundaries
- [ ] Verify tenant isolation
- [ ] Check database integrity

### Phase 4C: Performance Testing

- [ ] Check theme query performance
- [ ] Check configuration access performance
- [ ] Check storefront query performance
- [ ] Add indexes if needed
- [ ] Monitor for N+1 queries
- [ ] Test with large datasets

### Phase 4D: Documentation & Cleanup

- [ ] Document new API endpoints
- [ ] Document field ownership boundaries
- [ ] Document configuration system
- [ ] Document data migration process
- [ ] Document rollback procedures
- [ ] Update architecture diagrams
- [ ] Create migration guide for frontend
- [ ] Remove old TenantTheme from DbContext (if ready)
- [ ] Create final cleanup migration (drop TenantTheme table - phase 2)

---

## Sprint 5: Frontend Updates & Deployment

### Phase 5A: Frontend Alignment

- [ ] Update Config.tsx to use new endpoints
- [ ] Update StorefrontSettings.tsx for consistency
- [ ] Update Theme.tsx for new theme management
- [ ] Remove redundant API calls
- [ ] Test UI flows end-to-end
- [ ] Verify error handling

### Phase 5B: Staging Deployment

- [ ] Deploy to staging environment
- [ ] Run full test suite
- [ ] Verify data migration
- [ ] Test with staging frontend
- [ ] Performance testing
- [ ] Security testing

### Phase 5C: Production Deployment

- [ ] Create database backup
- [ ] Deploy backend to production
- [ ] Verify migrations ran successfully
- [ ] Monitor application logs
- [ ] Test critical flows
- [ ] Deploy frontend updates
- [ ] Monitor for errors
- [ ] Verify performance

### Phase 5D: Rollback Planning

- [ ] Document rollback procedure
- [ ] Test rollback on staging
- [ ] Create rollback scripts
- [ ] Have team ready for deployment

---

## Cross-Cutting Concerns

### Backward Compatibility

- [ ] Old theme endpoints still work
- [ ] Old configuration endpoints still work
- [ ] Old tenant endpoints return expected data
- [ ] API clients not broken
- [ ] Deprecation warnings added to old endpoints (optional)

### Documentation

- [ ] API endpoint documentation updated
- [ ] Architecture documentation created
- [ ] Field ownership documented
- [ ] Configuration system documented
- [ ] Data migration guide created
- [ ] Rollback procedure documented
- [ ] Frontend integration guide updated

### Testing Strategy

- [ ] Unit tests for new models
- [ ] Unit tests for new controllers
- [ ] Unit tests for updated services
- [ ] Integration tests for data migrations
- [ ] Integration tests for full workflows
- [ ] End-to-end tests for critical paths
- [ ] Performance tests
- [ ] Security tests

### Code Quality

- [ ] No compiler warnings
- [ ] Code review completed
- [ ] All tests passing
- [ ] Code coverage >= 80%
- [ ] No code smells
- [ ] Documentation complete

