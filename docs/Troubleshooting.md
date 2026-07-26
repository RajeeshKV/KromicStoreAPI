# Troubleshooting Guide

Comprehensive troubleshooting guide for common KromicStore API deployment and runtime issues.

## Startup Issues

### Application Won't Start

**Symptom**: "Service exited with code 1" or "Container exited"

**Diagnostics**:
```bash
# Check logs
docker logs <container-id>

# Look for:
# - "Missing required environment variable"
# - "Database connection failed"
# - "Migration failed"
```

**Solutions**:

1. **Missing Environment Variable**:
   - Error: `Missing required environment variable 'DATABASE_URL'`
   - Fix: Add all required environment variables from `.env.render.example`
   - Verify: Each variable has a non-empty value

2. **Invalid Environment Variable Format**:
   - Error: `Connection string format invalid`
   - Fix: Verify DATABASE_URL format: `postgresql://user:pass@host:port/database`
   - Check for: Special characters escaped in password

3. **Database Not Accessible**:
   - Error: `Failed to connect to database after 30 attempts`
   - Fix:
     - Verify DATABASE_URL points to running database
     - Check database firewall allows Render IP range
     - Test connection locally: `psql "<connection-string>"`
     - Increase retry timeout if database is slow

4. **Invalid JWT/Encryption Keys**:
   - Error: `JWT_SECRET must be at least 32 characters`
   - Fix: Generate new keys
     ```bash
     # Linux/Mac
     openssl rand -base64 32
     ```
   - Verify each key is 32+ characters

5. **Migration Failed**:
   - Error: `Database migrations failed`
   - Check logs for specific error
   - May indicate schema incompatibility
   - See Database Issues section below

### Slow Startup (> 60 seconds)

**Causes**: Database not accessible, network latency, resource constraints

**Solutions**:

1. **Database Connection Slow**:
   ```bash
   # Test directly
   time psql "<DATABASE_URL>" -c "SELECT 1"
   ```
   - Should complete in < 5 seconds
   - If slow, database or network issue

2. **Insufficient Memory**:
   - Check allocated memory in Render
   - Minimum 512MB, 1GB recommended
   - Upgrade if under minimum

3. **Redis Connection Slow**:
   - Check REDIS_URL reachable
   - Test: `redis-cli -h <host> -p 6379 PING`

---

## Health Check Failures

### `/health` Returns Non-200 Status

**Symptom**: Health check endpoint returns 500 or 503

**Solutions**:

1. **Application Not Running**:
   - Check service status in Render
   - Review startup logs
   - Restart service if needed

2. **Database Health Check Fails** (Readiness check):
   - Error: `{"status":"Unhealthy","checks":{"database":"Unhealthy"}}`
   - Fix: Verify DATABASE_URL and database connectivity
   - Check database is accepting connections
   - Review database logs for errors

3. **Redis Health Check Fails** (Readiness check):
   - Error: `{"status":"Degraded","checks":{"redis":"Unhealthy"}}`
   - Fix: Verify REDIS_URL is correct
   - Check Redis is running and accepting connections
   - Note: Degraded status doesn't fail service (cache optional)

---

## Database Issues

### Migrations Failed on Startup

**Symptom**: Logs show "Database migrations failed"

**Diagnostics**:
```bash
# Check specific error in logs - look for:
# - "Table already exists"
# - "Column incompatibility"
# - "Foreign key violation"
```

**Solutions**:

1. **Schema Already Exists**:
   - Error: "Relation ... already exists"
   - Cause: Database not clean or migration already ran
   - Fix: Drop and recreate database (caution: DELETES DATA)
   - For Render PostgreSQL:
     - Use Render dashboard to reset database
     - Or use `psql` to drop/recreate

2. **Incompatible Existing Data**:
   - Cause: Schema structure changed incompatibly
   - Fix:
     - Backup existing data
     - Drop and recreate schema
     - Restore data if needed

3. **Missing Permissions**:
   - Error: "Permission denied" during migration
   - Fix: Verify database user has appropriate permissions
   - User must have: CREATE, ALTER, DROP on schema

### Queries Running Slowly

**Symptom**: API requests taking > 1 second

**Diagnostics**:
```bash
# Enable query logging
# In appsettings.json:
{
  "Logging": {
    "LogLevel": {
      "Microsoft.EntityFrameworkCore": "Debug"
    }
  }
}
```

**Solutions**:

1. **Missing Database Indexes**:
   - Verify indexes created: `\d+ <table>` in psql
   - Add missing indexes via migration

2. **N+1 Query Problem**:
   - Symptom: Many similar queries for related data
   - Fix: Add `.Include()` in EF queries
   - Or: Add `.Select()` projections to fetch only needed columns

3. **Inefficient Query Plan**:
   - Get query execution plan: `EXPLAIN ANALYZE <query>`
   - May need different join strategy or indexes

4. **Database Connection Pool Exhaustion**:
   - Symptom: Queries timeout waiting for connection
   - Fix: Increase connection pool size in DATABASE_URL
   - Or: Optimize queries to reduce connection hold time

### Database Connection Pool Issues

**Symptom**: "Maximum pool size exceeded" or connection timeouts

**Diagnostics**:
```bash
# Check active connections
SELECT count(*) FROM pg_stat_activity;
```

**Solutions**:

1. **Connection Leak**:
   - Connections not returned to pool
   - Fix: Ensure all database operations dispose connections
   - Check for missing `using` statements

2. **Insufficient Pool Size**:
   - Increase max pool size in connection string
   - Default: 25, can increase to 50+ if needed
   - Monitor actual connections needed

3. **Long-Running Queries**:
   - Fix: Optimize queries to run faster
   - Or: Increase `Connection Timeout` value

---

## Authentication & Authorization Issues

### Login Failing with 401

**Symptom**: Authentication returns "Invalid credentials"

**Solutions**:

1. **User Not Found**:
   - Error: "User not found"
   - Fix: Create user account first
   - Or: Check email is spelled correctly

2. **Invalid Password**:
   - Verify password is correct (case-sensitive)
   - Reset password if forgotten

3. **Expired Token**:
   - Error: "Token has expired"
   - Cause: Session > 1 hour old
   - Fix: Use refresh token to get new access token

4. **Invalid JWT Configuration**:
   - Error: "JWT validation failed"
   - Fix: Verify JWT_SECRET is same in all instances
   - Check JWT_AUTHORITY matches token issuer
   - Ensure JWT_AUDIENCE matches

### Authorization Denied (403)

**Symptom**: Valid authentication but action returns 403 Forbidden

**Solutions**:

1. **Insufficient Role/Permission**:
   - User: `PATCH /api/v1/users/{id}` → 403
   - Cause: Endpoint requires Admin role, user is Customer
   - Fix: Use appropriate user role for action

2. **Cross-Tenant Access Attempt**:
   - Error: Trying to access another tenant's data
   - Cause: Multi-tenancy enforcement
   - Fix: Can only access own tenant's data

3. **Account Suspended**:
   - Cause: Tenant or user account suspended
   - Fix: Reactivate account (admin only)

---

## External Service Integration Issues

### Razorpay Payment Fails

**Symptom**: Payment creation returns error

**Diagnostics**:
```
Check logs for Razorpay API response:
- Status code (400, 401, 429, 500)
- Error message from Razorpay
```

**Solutions**:

1. **Invalid API Credentials**:
   - Error: "Unauthorized" (401)
   - Fix: Verify RAZORPAY_KEY and RAZORPAY_SECRET are correct
   - Check credentials are from production account (not test)

2. **Invalid Amount**:
   - Error: "Amount must be > 0"
   - Fix: Verify order total is positive number
   - Check currency is supported

3. **Duplicate Payment Attempt**:
   - Error: "Duplicate idempotency key"
   - Cause: Same payment submitted twice
   - Fix: Should auto-retry, check if idempotency key working

4. **Rate Limit Exceeded**:
   - Error: "Too many requests" (429)
   - Fix: Implement backoff and retry
   - Check Razorpay account rate limits

5. **Razorpay API Down**:
   - Error: Connection timeout, 5xx errors
   - Fix: Retry with exponential backoff
   - Check Razorpay status page

### Google OAuth Login Fails

**Symptom**: OAuth callback returns error

**Solutions**:

1. **Invalid Client ID/Secret**:
   - Error: "Invalid client"
   - Fix: Verify GOOGLE_CLIENT_ID and GOOGLE_CLIENT_SECRET
   - Get from: https://console.cloud.google.com/

2. **Redirect URL Not Whitelisted**:
   - Error: "Redirect URI mismatch"
   - Fix: Add callback URL to Google OAuth app settings
   - Format: `https://<your-domain>/api/v1/auth/oauth/google/callback`

3. **Expired Credentials**:
   - Cause: OAuth token expired
   - Fix: User must re-authenticate
   - Access tokens expire, refresh tokens get new access token

### Email Sending Fails (Brevo)

**Symptom**: Welcome email not received

**Diagnostics**:
```
Check logs for:
- SendEmailAsync result
- HTTP status from Brevo API
- Error message details
```

**Solutions**:

1. **Invalid API Key**:
   - Error: "Invalid API key"
   - Fix: Verify BREVO_API_KEY is correct
   - Get from: https://app.brevo.com/

2. **Unverified Sender Email**:
   - Error: "Sender email not verified"
   - Fix: Verify BREVO_SENDER_EMAIL in Brevo dashboard
   - Go to Senders > verify email address

3. **Invalid Template ID**:
   - Error: "Template not found"
   - Fix: Verify BREVO_WELCOME_EMAIL_TEMPLATE_ID
   - Get template IDs from Brevo dashboard

4. **Invalid Recipient Email**:
   - Error: "Invalid email format"
   - Fix: Validate email format before sending
   - Check for special characters

5. **Rate Limit Exceeded**:
   - Error: "Too many requests"
   - Fix: Check email sending quota in Brevo account
   - Upgrade account if needed

### Cloudinary Image Upload Fails

**Symptom**: Image upload returns error

**Solutions**:

1. **Invalid Credentials**:
   - Error: "Invalid credentials"
   - Fix: Verify CLOUDINARY_CLOUD_NAME, CLOUDINARY_API_KEY, CLOUDINARY_API_SECRET
   - Get from: https://cloudinary.com/console/

2. **File Too Large**:
   - Error: "File size limit exceeded"
   - Fix: File must be < 100MB
   - Check file size before upload

3. **Unsupported Format**:
   - Error: "Unsupported file type"
   - Fix: Only image formats supported (JPG, PNG, GIF, WebP)
   - Convert file to supported format

---

## Performance Issues

### High API Response Times

**Symptom**: Requests taking > 500ms

**Diagnostics**:
```bash
# Monitor response times
curl -w "@curl-format.txt" -o /dev/null -s https://<url>/api/v1/products
```

**Solutions**:

1. **Database Queries Slow**:
   - Enable query timing in logs
   - Check slow query logs in PostgreSQL
   - Add indexes on frequently queried columns

2. **Missing Cache**:
   - Verify Redis is running and connected
   - Check cache hit rate
   - Add caching to frequently accessed data

3. **Large Payload**:
   - Implement pagination (limit to 20-100 items per request)
   - Use projections to fetch only needed columns
   - Compress response with gzip

4. **CPU/Memory Constraints**:
   - Check resource usage in Render dashboard
   - Increase instance size if memory/CPU maxed
   - Enable horizontal scaling if needed

5. **Network Latency**:
   - Check latency to database, Redis, external services
   - Use VPN or private network if available
   - Consider database in same region as API

### Memory Leaks

**Symptom**: Memory usage gradually increases, service crashes

**Diagnostics**:
```
Monitor memory usage:
- Render dashboard: Memory graph should be stable
- Expected: 300-500MB for typical workload
- Bad: Continuous growth, reaching limit
```

**Solutions**:

1. **Unmanaged Resources Not Released**:
   - Ensure `using` statements on all IDisposable objects
   - Check HttpClient creation (should use IHttpClientFactory)

2. **Large Cache Without TTL**:
   - Verify Redis keys have expiration
   - Check cache key patterns for orphaned entries

3. **.NET Garbage Collection**:
   - Not usual issue, but can tune if needed
   - Consider: Increase Gen2 budget, adjust GC mode

---

## Error Handling Issues

### Cryptic Error Messages

**Symptom**: Users see "Internal Server Error 500" with no details

**Solutions**:

1. **Check Logs**:
   - Review application logs for actual error
   - Look for correlation ID in response header
   - Match correlation ID in logs

2. **Enable Detailed Logging**:
   - Set `LOG_LEVEL=Debug` (temporary)
   - Log will show more details
   - Revert to `Information` after debugging

3. **Sensitive Data in Errors**:
   - Passwords, tokens should never appear
   - Check error message doesn't expose database structure
   - Review Error Handling Middleware

### Validation Error Format

**Symptom**: Validation errors unclear or inconsistent

**Solutions**:

1. **Check Validation Response Format**:
   - Should be: `{ errors: { fieldName: ["error message"] } }`
   - Verify all validators follow format

2. **Missing Field Validation**:
   - Some fields not validating
   - Add FluentValidation rules for field

---

## Webhook Issues

### Webhook Not Firing

**Symptom**: Event occurs but webhook not sent

**Diagnostics**:
```
Check:
1. Webhook registered: GET /api/v1/webhooks
2. Event fired: Check event logs
3. Delivery attempted: GET /api/v1/webhooks/{id}/deliveries
```

**Solutions**:

1. **Webhook Not Registered**:
   - Fix: Register webhook endpoint first
   - Verify endpoint URL is accessible

2. **Event Not Matching**:
   - Webhook registered for OrderCreated
   - But PaymentProcessed event triggered
   - Fix: Register webhook for correct event type

3. **Endpoint Not Accessible**:
   - Error: "Connection refused" or timeout
   - Fix: Verify webhook endpoint URL is:
     - Publicly accessible (not localhost)
     - Responding with 200 status
     - HTTPS certificate valid

4. **Signature Verification Failed**:
   - Consumer rejecting delivery
   - Fix: Verify signature calculation
   - Check secret key matches webhook config
   - See Webhook Consumer Guide

### Webhook Delivery Failures

**Symptom**: Webhook deliveries in error state

**Solutions**:

1. **Check Retry Status**:
   - View delivery logs: `GET /api/v1/webhooks/{id}/deliveries`
   - Retries scheduled for: 1s, 10s, 100s, 1000s, 10000s

2. **Consumer Endpoint Error**:
   - Review consumer logs
   - Fix issues in consumer webhook handler
   - Redelivery can be triggered manually

3. **Network Connectivity**:
   - Consumer endpoint unreachable
   - Check: firewall rules, network access
   - Verify DNS resolution

---

## Data Issues

### Data Inconsistency

**Symptom**: Same data retrieved in different states

**Causes**: Race condition, caching issue

**Solutions**:

1. **Cache Staleness**:
   - Verify cache invalidation on writes
   - Check: UPDATE triggers cache clear
   - Manual clear if needed: `DELETE cache:*`

2. **Database Read Replica Lag**:
   - If using read replicas, data may be stale
   - Use primary for critical reads
   - Or: Implement read-after-write consistency

### Missing Data After Write

**Symptom**: Data submitted but not retrieved

**Solutions**:

1. **Transaction Not Committed**:
   - Verify SaveChangesAsync() called
   - Check for exceptions during save

2. **Data Filtered by Tenant**:
   - Ensure correct tenant context
   - Verify TenantId included in filter

3. **Cache Not Invalidated**:
   - Old cached data still being returned
   - Manually clear cache for key

---

## Monitoring & Alerts

### Setting Up Alerts

**For Render**:
1. Go to service settings
2. Click "Alerts"
3. Set alert conditions:
   - Service down: health check failures
   - High error rate: 5xx responses > 1%
   - Memory usage: > 90%
   - CPU usage: > 80%

**For Email Service**:
1. Configure email notifications
2. Add team members to notification list
3. Test alert triggers

### Checking Service Health

```bash
# Quick health check
curl -s https://<your-url>/health | jq .

# Detailed readiness check
curl -s https://<your-url>/health/ready | jq .

# Check if database is responding
curl -s https://<your-url>/health/ready | jq '.checks.database'

# Check if cache is responding
curl -s https://<your-url>/health/ready | jq '.checks.redis'
```

---

## Getting Help

### When to Contact Support

1. **Render Support**:
   - Infrastructure issues: database, networking, deployment
   - URL: https://support.render.com

2. **External Service Support**:
   - Razorpay: https://support.razorpay.com
   - Google: https://support.google.com
   - Cloudinary: https://support.cloudinary.com
   - Brevo: https://support.brevo.com

3. **KromicStore Team**:
   - Application-specific issues
   - Review logs and deployment checklist before reaching out
   - Provide: correlation ID, timestamp, user account

### Debug Information to Collect

When reporting issues, include:

```
- Service URL: https://...
- Timestamp of issue (UTC)
- HTTP status code
- Correlation ID (from response header X-Correlation-ID)
- Request method and path: GET /api/v1/...
- Request payload (if applicable)
- Error message
- Steps to reproduce
- Expected vs. actual behavior
```

---

## Common Resolution Times

| Issue | Typical Resolution Time |
|-------|------------------------|
| Missing environment variable | 1-5 minutes |
| Database connectivity | 5-15 minutes |
| API endpoint bug | 15-30 minutes |
| Deployment failure | 10-30 minutes |
| Performance degradation | 30-60 minutes |
| Data corruption | 1-4 hours |

---

**Document Version**: 1.0
**Last Updated**: 2024
**Maintained By**: DevOps Team
