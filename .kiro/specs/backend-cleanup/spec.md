# Backend Cleanup Spec - Architecture Consolidation

**Status**: Spec Creation  
**Priority**: High (Production Readiness)  
**Effort**: 1-2 weeks  
**Risk**: Medium (Data migration + endpoint changes)

---

## Executive Summary

Consolidate KromicStore's fragmented architecture into clean, maintainable layers:
1. **Single Theme System** - Merge `ThemeEntity` + `TenantTheme` into one cohesive model
2. **Eliminate Field Duplication** - Clear ownership boundaries between Tenant and Storefront
3. **Centralized Configuration** - Store values in appropriate entities, not scattered

**Key Principle**: Keep existing endpoints working; refactor underlying architecture.

---

## Decision Summary

### Theme Architecture: Option B (Hybrid) + Consolidation

**Final Model**:
- **One `Theme` entity** (rename from `ThemeEntity`)
- **Three layers**:
  1. **Platform Themes** (owned by SuperUser): Default/public themes
  2. **Tenant Themes** (owned by Tenant): Custom themes + cloned variants
  3. **Storefront Theme Reference** (owned by Storefront): Points to active theme

**Why**: Allows tenant customization while maintaining clean single-entity architecture. No `TenantTheme` table; everything in `Theme` with ownership/scope fields.

**Tenant Permissions**:
- Create/modify themes they own
- Use platform themes
- Clone and customize platform themes
- Share with SU for approval/publication

**SuperUser Permissions**:
- Create/modify public platform themes
- Review and publish tenant themes as templates
- Manage all themes in system

---

## Part 1: Theme Architecture Consolidation

### Current State

```
ThemeEntity (Platform templates)
├─ Id, Name, Slug, Description
├─ DefinitionJson (colors, fonts, layouts)
└─ Used by: ThemeController, Storefront.ThemeId

TenantTheme (Tenant customization)
├─ Id, TenantId, Name, Version
├─ Color fields (individually stored)
├─ Font fields (individually stored)
└─ No Controller, orphaned
```

### Target State

```
Theme (Single consolidated entity)
├─ Id, TenantId (null = platform theme)
├─ Name, Slug, Description, Version
├─ DefinitionJson (full theme definition)
├─ IsPublic (true = can be used by other tenants)
├─ SourceThemeId (if cloned from another theme)
├─ OwnerTenantId (who created/owns this theme)
├─ CreatedBy (UserId), LastModifiedBy (UserId)
└─ Timestamps

Storefront
├─ (unchanged)
├─ ActiveThemeId (points to Theme)
└─ (other fields)
```

### Migration Strategy

**Phase 1A: Update Theme Entity**
- [ ] Add `TenantId`, `IsPublic`, `SourceThemeId`, `OwnerTenantId` fields to `ThemeEntity`
- [ ] Rename `ThemeEntity` to `Theme`
- [ ] Create migration to add new fields
- [ ] Populate `TenantId = null` for existing platform themes
- [ ] Populate `OwnerTenantId = SuperUser` for existing themes

**Phase 1B: Deprecate TenantTheme**
- [ ] Keep `TenantTheme` table (don't drop yet)
- [ ] Create migration to copy `TenantTheme` data into `Theme` table
- [ ] Set migration status flag to track completion
- [ ] Update queries to use `Theme` instead of `TenantTheme`

**Phase 1C: Update Controllers**
- [ ] Update `ThemeController` to handle both platform and tenant themes
- [ ] Add endpoints for tenant theme management (create, clone, list)
- [ ] Add permission checks (tenant can only access own themes or public)
- [ ] Keep existing endpoints working (backward compatibility)

**Phase 1D: Cleanup**
- [ ] After verification, remove `TenantTheme` from DbContext
- [ ] Create migration to drop `TenantThemes` table (optional, phase 2)

---

## Part 2: Field Duplication Resolution

### Current State (Duplication Issues)

```
Tenant
├─ ContactEmail (platform/billing)
├─ ContactPhone (customer-facing???)
├─ LogoUrl (platform branding???)
└─ Name (tenant company name)

Storefront
├─ ContactEmail (customer-facing)
├─ ContactPhone (customer-facing)
├─ LogoUrl (storefront header)
├─ Name (store display name)
└─ Other storefront settings
```

**Problem**: No clear ownership, data inconsistency, confusing for developers.

### Target State (Clear Ownership)

```
Tenant (Platform-level, rarely changes)
├─ Name (company name)
├─ ContactEmail (for platform/billing communications)
├─ Subdomain
├─ Subscription info
└─ (remove ContactPhone, LogoUrl)

Storefront (Customer-facing, frequently changes)
├─ Name (store display name, can differ from tenant name)
├─ ContactEmail (customer-facing, can differ from tenant)
├─ ContactPhone (customer-facing contact)
├─ LogoUrl (storefront header, changes often)
├─ Address (physical store location)
├─ Currency, Country
├─ BrandColor, Copyright
└─ Other storefront-specific settings
```

### Migration Strategy

**Phase 2A: Data Consolidation**
- [ ] Create migration to add `ContactPhone`, `LogoUrl` to existing Storefronts (if missing)
- [ ] Copy `Tenant.ContactPhone` → corresponding `Storefront.ContactPhone` (if set)
- [ ] Copy `Tenant.LogoUrl` → corresponding `Storefront.LogoUrl` (if set)
- [ ] Update domain models (remove fields from Tenant)

**Phase 2B: Update Endpoints**
- [ ] Verify `TenantController` GET/PUT work with consolidated fields
- [ ] Verify `StorefrontController` GET/PUT work with consolidated fields
- [ ] Add validation: prevent storefront fields in tenant endpoints
- [ ] Update API documentation

**Phase 2C: Frontend Updates**
- [ ] `Config.tsx` → redirect tenant config calls to `StorefrontController` for store settings
- [ ] `StorefrontSettings.tsx` → continue using `StorefrontController`
- [ ] Remove redundant API calls

---

## Part 3: Configuration System Cleanup

### Current State (Scattered)

```
TenantConfiguration (Key-Value store)
├─ Used for: Feature flags, system settings, AND
├─ Also used for: Store name, logo (WRONG!)
└─ Generic key-value pairs

Storefront Entity
├─ Has: Name, LogoUrl, ContactInfo, BrandColor
└─ Sometimes overridden by TenantConfiguration keys

ConfigController
├─ Endpoint: GET/PUT /api/v1/config/{key}
└─ No validation on what keys are allowed
```

**Problem**: Configuration scattered across two places; no clear boundaries.

### Target State (Centralized)

```
TenantConfiguration (Platform/System only)
├─ Feature flags (notifications:enabled, webhooks:enabled, etc.)
├─ System settings (rate limits, cache TTL, etc.)
├─ Platform policies (only SuperUser can set)
└─ NO storefront-specific settings

Storefront Entity (Storefront settings)
├─ Name, LogoUrl, ContactInfo (storefront-specific)
├─ Theme, Currency, Country (storefront-specific)
└─ Single source of truth for storefront data

TenantConfiguration (Tenant-level settings)
├─ Billing info, subscription limits
├─ Platform feature overrides (if applicable)
└─ Tenant-specific policies
```

### Migration Strategy

**Phase 3A: Identify Misplaced Settings**
- [ ] Audit all `TenantConfiguration` keys in production
- [ ] Categorize: Platform, Tenant-level, or Storefront
- [ ] Create migration plan for each category

**Phase 3B: Move Storefront Settings**
- [ ] Identify storefront settings in `TenantConfiguration` (e.g., `store:name`, `store:logo`)
- [ ] Move to `Storefront` entity
- [ ] Create migration to copy values
- [ ] Update queries to read from `Storefront`

**Phase 3C: Update ConfigController**
- [ ] Add validation to prevent storefront keys
- [ ] Add validation to prevent tenant-specific keys
- [ ] Document allowed configuration keys
- [ ] Return error if invalid key attempted

**Phase 3D: Add Safeguards**
- [ ] Add database constraints (if possible)
- [ ] Add application-level validation
- [ ] Add logging for configuration changes
- [ ] Add documentation

---

## Implementation Plan

### Task Breakdown

#### Sprint 1: Theme Architecture
1. **Update Theme Entity Model**
   - Add `TenantId`, `IsPublic`, `SourceThemeId`, `OwnerTenantId` fields
   - Create EF Core migration
   - Rename `ThemeEntity` → `Theme`

2. **Data Migration**
   - Create migration to populate new fields
   - Migrate `TenantTheme` data to `Theme` table
   - Verify data integrity

3. **Update ThemeController**
   - Add tenant theme CRUD endpoints
   - Add permission checks
   - Keep backward-compatible routes

4. **Update Storefront References**
   - Update `Storefront.ThemeId` to reference new `Theme` entity
   - Update queries to filter by ownership

#### Sprint 2: Field Duplication
1. **Data Consolidation**
   - Create migration to add missing fields to Storefront
   - Copy data from Tenant to Storefront where applicable
   - Remove duplicate fields from Tenant entity model

2. **Update Controllers**
   - Update `TenantController` to exclude removed fields
   - Update `StorefrontController` to handle all storefront data
   - Add validation

3. **Frontend Alignment**
   - Update `Config.tsx` to use `StorefrontController`
   - Remove redundant API calls
   - Test UI flows

#### Sprint 3: Configuration Cleanup
1. **Audit & Categorize**
   - List all current `TenantConfiguration` keys
   - Categorize each key
   - Create migration plan

2. **Move Settings**
   - Create migrations to move values to appropriate entities
   - Update services to read from correct locations
   - Add validation

3. **Update ConfigController**
   - Add key validation
   - Add documentation
   - Add safeguards

#### Sprint 4: Testing & Cleanup
1. **Integration Testing**
   - Test theme creation/modification
   - Test storefront CRUD
   - Test configuration access

2. **Backward Compatibility**
   - Verify old endpoints still work
   - Test API client compatibility
   - Document deprecations

3. **Data Verification**
   - Verify all data migrated correctly
   - Check for data loss
   - Validate constraints

---

## Success Criteria

### Architecture
- [x] Single `Theme` entity consolidates `ThemeEntity` + `TenantTheme`
- [x] Clear ownership boundaries between Tenant, Storefront, Themes
- [x] No field duplication
- [x] Configuration stored in appropriate entities

### Functionality
- [x] Existing endpoints work (backward compatible)
- [x] New tenant theme endpoints work
- [x] Theme inheritance/cloning works
- [x] Configuration access validated

### Data Quality
- [x] All data migrated correctly
- [x] No orphaned records
- [x] No data loss
- [x] Constraints enforced

### Testing
- [x] Unit tests for new logic
- [x] Integration tests for migrations
- [x] End-to-end tests for critical flows
- [x] All tests pass

### Documentation
- [x] API endpoints documented
- [x] Architecture documented
- [x] Migration guide created
- [x] Frontend integration guide updated

---

## Risks & Mitigation

| Risk | Impact | Mitigation |
|------|--------|-----------|
| Data loss during migration | High | Backup DB before migration, test on staging |
| Backward compatibility breaks | High | Keep old endpoints, add deprecation warnings |
| Tenant confusion with new endpoints | Medium | Clear documentation, API guide |
| Performance issues from consolidation | Medium | Add indexes, optimize queries |
| Frontend not updated in time | High | Coordinate with frontend team |

---

## Success Definition

**Production Ready When**:
1. ✅ Theme system unified (no confusion between ThemeEntity and TenantTheme)
2. ✅ Clear entity ownership (Tenant vs Storefront vs Configuration clear)
3. ✅ All tests passing (unit, integration, e2e)
4. ✅ No existing endpoints broken (backward compatible)
5. ✅ Documentation complete and accurate
6. ✅ Data migrated and verified
7. ✅ Frontend updated and tested

---

## Files to Modify

### Domain Entities
- [ ] `Theme.cs` (merge ThemeEntity and TenantTheme logic)
- [ ] `Tenant.cs` (remove duplicate fields)
- [ ] `Storefront.cs` (add fields if needed)
- [ ] `TenantConfiguration.cs` (add documentation)

### Migrations
- [ ] Create EF Core migrations for schema changes
- [ ] Create data migration scripts
- [ ] Document rollback procedures

### Controllers
- [ ] `ThemeController.cs` (update, add new endpoints)
- [ ] `TenantController.cs` (remove duplicate fields)
- [ ] `StorefrontController.cs` (add validation)
- [ ] `ConfigController.cs` (add validation)

### Services
- [ ] `ThemeService.cs` (if exists, update)
- [ ] `TenantService.cs` (update)
- [ ] `StorefrontService.cs` (update)
- [ ] `ConfigurationService.cs` (add validation)

### Tests
- [ ] Create tests for new theme functionality
- [ ] Create tests for field consolidation
- [ ] Create tests for configuration validation
- [ ] Verify backward compatibility

---

## Next Steps

1. **Review & Approve This Spec**
2. **Create Database Backups** (staging/prod)
3. **Start Sprint 1: Theme Architecture**
4. **Parallel: Update Frontend Documentation**
5. **Testing & Verification**
6. **Deploy to Staging**
7. **Production Deployment**

