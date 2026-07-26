# Deployment Verification Checklist

This checklist should be completed before and after deploying KromicStore API to production.

## Pre-Deployment Checklist

### Code Quality & Build

- [ ] All code committed to version control
- [ ] Latest changes pulled and reviewed
- [ ] Solution builds successfully: `dotnet build KromicStore.sln --configuration Release`
- [ ] No build warnings related to code logic (only obsolete API warnings acceptable)
- [ ] All tests pass locally: `dotnet test tests/KromicStore.Tests/KromicStore.Tests.csproj`
- [ ] Code coverage >= 70% for services
- [ ] No hardcoded secrets in source code
- [ ] No local/debug configuration in committed files

### Docker & Container

- [ ] Dockerfile exists and is valid
- [ ] `.dockerignore` excludes unnecessary files
- [ ] Docker image builds successfully: `docker build -t kromic-store:latest .`
- [ ] Final image size < 500MB
- [ ] `scripts/entrypoint.sh` exists and is executable
- [ ] Health check endpoint responds: `docker run ... curl http://localhost:8080/health`
- [ ] Application starts without errors in container

### Configuration & Environment

- [ ] All required environment variables documented in `Environment-Setup.md`
- [ ] `.env.render.example` created with all variables
- [ ] No secrets in `.env.render.example` (only placeholders)
- [ ] `appsettings.Production.json` uses only environment variables
- [ ] Database connection string format verified
- [ ] Redis connection string format verified
- [ ] All external service credentials obtained (Razorpay, Google, Cloudinary, Brevo)

### Database & Migrations

- [ ] Database migrations created and tested locally
- [ ] Migration scripts handle both forward and rollback
- [ ] Test database successfully migrated
- [ ] Seed data (if any) included in migrations
- [ ] Database backup procedure documented
- [ ] Recovery procedure tested

### Dependencies & External Services

- [ ] Database server accessible and running
- [ ] Redis cache accessible and running
- [ ] All external API credentials valid and not expired
- [ ] Razorpay API keys verified (test vs. production)
- [ ] Google OAuth credentials configured and URLs whitelisted
- [ ] Cloudinary API verified
- [ ] Brevo email service configured with verified sender email
- [ ] Email templates created in Brevo and IDs documented
- [ ] Network firewall rules allow outbound requests to external services

### API Documentation & Health Checks

- [ ] Swagger/OpenAPI documentation generated
- [ ] Health check endpoints implemented: `/health`, `/health/ready`
- [ ] Health check includes database status
- [ ] Health check includes cache status
- [ ] Readiness check properly distinguishes from liveness check
- [ ] API documentation accessible at `/swagger`

### Security

- [ ] All API endpoints require authentication (except auth endpoints)
- [ ] Authorization policies enforced (TenantAdmin, SuperUser, Customer roles)
- [ ] Multi-tenancy verified - tenants cannot access other tenants' data
- [ ] Input validation on all endpoints
- [ ] Rate limiting enabled
- [ ] Sensitive data (passwords, tokens) never logged
- [ ] HTTPS forced in production configuration
- [ ] CORS configured for production domains only

### Documentation

- [ ] `docs/Environment-Setup.md` - complete and accurate
- [ ] `docs/Render-Deployment.md` - deployment guide complete
- [ ] `docs/Deployment-Checklist.md` - this document
- [ ] `docs/Troubleshooting.md` - common issues documented
- [ ] README.md updated with deployment instructions
- [ ] All breaking changes documented
- [ ] Rollback procedure documented

### Monitoring & Logging

- [ ] Structured logging configured (Serilog)
- [ ] Log levels set appropriately (Information for production)
- [ ] Correlation ID propagation working
- [ ] Error tracking configured (if using APM)
- [ ] Performance monitoring in place (optional)
- [ ] Alerts configured for critical failures

### Render Platform

- [ ] Render account created and verified
- [ ] GitHub repository connected to Render
- [ ] Docker runtime selected for deployment
- [ ] All environment variables added to Render dashboard
- [ ] PostgreSQL database created (or external DB configured)
- [ ] Redis cache created (or external cache configured)
- [ ] Region selected (e.g., oregon, frankfurt)
- [ ] Plan tier appropriate for expected traffic

### Team Readiness

- [ ] Deployment procedure communicated to team
- [ ] On-call support assigned for deployment day
- [ ] Rollback procedure reviewed with team
- [ ] Communication channel established for deployment updates
- [ ] Stakeholders notified of deployment window
- [ ] Any database changes reviewed by DBA

---

## Deployment Execution

### Pre-Deployment (30 minutes before)

- [ ] Verify database backups completed successfully
- [ ] Notify stakeholders of deployment start
- [ ] Confirm no business-critical operations in progress
- [ ] Review logs from last 24 hours for anomalies
- [ ] Ensure team members available for 2 hours post-deployment

### During Deployment

- [ ] Trigger deployment in Render dashboard
- [ ] Monitor build logs for errors
- [ ] Wait for "Your service is live" confirmation
- [ ] Monitor application logs for startup errors

### Immediately Post-Deployment (< 5 minutes)

- [ ] Health check endpoint returns 200: `curl https://<url>/health`
- [ ] Readiness check returns 200: `curl https://<url>/health/ready`
- [ ] Database migrations logged successfully
- [ ] No startup errors in application logs
- [ ] Service status shows "Live" in Render dashboard

---

## Post-Deployment Verification (30 minutes to 2 hours)

### Application Endpoints

- [ ] Authentication working: `POST /api/v1/auth/login`
- [ ] Sample query successful: `GET /api/v1/products`
- [ ] Creation endpoint successful: `POST /api/v1/products`
- [ ] Health check responds consistently
- [ ] Swagger documentation accessible at `/swagger`

### Data Integrity

- [ ] Database tables created and populated
- [ ] Seed data (if any) loaded successfully
- [ ] No data corruption from migration
- [ ] Test data not present in production

### External Service Integration

- [ ] Email sending works (test welcome email)
- [ ] OAuth login works (test Google sign-in)
- [ ] Payment integration functional (test endpoint, no actual charge)
- [ ] Media uploads work (test Cloudinary integration)
- [ ] Webhooks can be registered (test endpoint)

### Performance

- [ ] API response times acceptable (< 500ms for normal requests)
- [ ] No timeout errors in logs
- [ ] Database queries performant (check slow query logs)
- [ ] Cache hits working (verify Redis SET/GET)

### Security Verification

- [ ] Unauthenticated request to protected endpoint returns 401
- [ ] Cross-tenant data access blocked (401/403 response)
- [ ] API key authentication working
- [ ] Rate limiting active and blocking over-limit requests
- [ ] Sensitive data not exposed in logs or errors

### Error Handling

- [ ] Invalid request returns 400 with validation details
- [ ] Not found returns 404 (e.g., invalid product ID)
- [ ] Server errors return 500 with correlation ID
- [ ] Error responses follow standard format

### Monitoring & Alerts

- [ ] Logs appear in monitoring system (if configured)
- [ ] Alerts configured and tested
- [ ] Health check monitoring configured
- [ ] Error rate alert at 5% or less
- [ ] Response time alert configured

### Database & Cache

- [ ] Database connection pool working (5-25 connections)
- [ ] Redis connections established
- [ ] Cache keys created with tenant isolation
- [ ] Cache invalidation working
- [ ] No database connection leaks in logs

---

## Post-Deployment Sign-Off (Before Calling Complete)

- [ ] Load test completed (if applicable)
- [ ] All team members verified functionality in their area
- [ ] Product owner sign-off
- [ ] Ops/DevOps team confirm stability
- [ ] Support team aware of new features/changes
- [ ] Documentation updated and accessible
- [ ] Release notes published
- [ ] Team notified deployment complete

---

## Rollback Criteria & Procedure

### When to Rollback

Rollback immediately if any of these occur post-deployment:

- [ ] Health check failing (database/cache unavailable)
- [ ] Authentication broken (login not working)
- [ ] Data loss or corruption detected
- [ ] Critical API endpoint returning 5xx errors consistently
- [ ] External service integration completely non-functional
- [ ] Performance degradation > 50% from baseline

### Rollback Procedure

1. **Decision**: Declare rollback condition met
2. **Communication**: Notify all stakeholders immediately
3. **Execution**:
   - In Render dashboard, go to Deployments
   - Select previous stable deployment
   - Click "Deploy" to rollback
   - Monitor logs for clean shutdown and re-startup
4. **Verification**:
   - Health check endpoint returns 200
   - Previous functionality restored
   - No data consistency issues
5. **Investigation**:
   - Review logs from failed deployment
   - Identify root cause
   - Document issue and fix
   - Test fix locally
   - Plan re-deployment

### Recovery from Data Issues

If data corruption suspected:

1. **Stop serving traffic** (pause the service if needed)
2. **Assess damage** (query database for anomalies)
3. **Restore backup** (use point-in-time restore to before deployment)
4. **Verify data** (sample check several tables)
5. **Resume service** once confident in data integrity

---

## Post-Deployment Monitoring (First 24 Hours)

### Hourly Checks

- [ ] Service status is "Live"
- [ ] Error rate < 1%
- [ ] Response times normal (< 500ms p95)
- [ ] Database connection pool healthy
- [ ] No uncaught exceptions in logs

### Daily Checks

- [ ] Database size change normal (no unexpected growth)
- [ ] Backup completed successfully
- [ ] No unusual traffic patterns
- [ ] External service integrations stable
- [ ] Cache hit rate > 80%

### Weekly Checks (After first week)

- [ ] Overall system stability confirmed
- [ ] Performance baseline established
- [ ] Cost within expected range
- [ ] No escalated support tickets related to deployment
- [ ] Team feedback positive

---

## Success Criteria

Deployment considered successful when:

✅ All health checks passing
✅ All critical endpoints functional
✅ No critical errors in logs
✅ Response times within SLA
✅ No data integrity issues
✅ All team members sign-off
✅ No rollback required after 24 hours
✅ Performance meets or exceeds baseline

---

## Issue Template

If issues arise during deployment, document using this template:

```
## Issue: [Description]

### Severity: [Critical/High/Medium/Low]

### Impact: [Which features/users affected]

### Timeline:
- Detected at: [time]
- Root cause identified: [time]
- Resolution started: [time]
- Resolved at: [time]

### Root Cause: [Why this happened]

### Resolution: [What was done to fix]

### Prevention: [What will prevent recurrence]
```

---

**Document Version**: 1.0
**Last Updated**: 2024
**Next Review**: After each deployment
