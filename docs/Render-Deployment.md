# Render Deployment Guide

This guide walks through deploying KromicStore API to [Render](https://render.com).

## Prerequisites

- GitHub repository with KromicStore source code pushed
- Render account (sign up at https://render.com)
- All required environment variables documented and values ready
- PostgreSQL database ready or plan to use Render's managed PostgreSQL
- Redis cache ready or plan to use Render's managed Redis

## Deployment Steps

### Step 1: Connect GitHub Repository to Render

1. Go to [Render Dashboard](https://dashboard.render.com)
2. Click **"New +"** in top right
3. Select **"Web Service"**
4. Click **"Connect Account"** to authorize GitHub
5. Search for your `KromicStore` repository
6. Select the repository and click **"Connect"**

### Step 2: Configure Web Service

After connecting, fill in the following:

**Basic Configuration**:
- **Name**: `kromic-store-api` (or your preferred name)
- **Region**: Choose nearest region (e.g., `oregon`, `frankfurt`)
- **Branch**: `main` (or deployment branch)
- **Runtime**: Select **"Docker"**
- **Build Command**: Leave default (Dockerfile handles build)
- **Start Command**: `/app/entrypoint.sh` (or leave blank - Dockerfile ENTRYPOINT handles it)

**Plan**:
- **Plan**: Select `Standard` or higher (minimum recommended)
- **Memory**: 512 MB minimum, 1 GB recommended
- **Auto-scale**: Enable if expecting variable traffic

### Step 3: Configure Environment Variables

In the Render dashboard, add all required environment variables:

**Essential Variables**:
```
DATABASE_URL = postgresql://user:password@hostname:5432/database
REDIS_URL = redishost:6379
JWT_SECRET = <generate-random-32-char-string>
JWT_AUTHORITY = <your-auth-provider>
JWT_AUDIENCE = kromic-store-api
ENCRYPTION_KEY = <generate-random-32-char-string>
```

**Payment Gateway (Razorpay)**:
```
RAZORPAY_KEY = rzp_live_xxxxxxxxxxxx
RAZORPAY_SECRET = <razorpay-secret>
```

**OAuth (Google)**:
```
GOOGLE_CLIENT_ID = xxxx.apps.googleusercontent.com
GOOGLE_CLIENT_SECRET = <google-client-secret>
```

**Media Service (Cloudinary)**:
```
CLOUDINARY_CLOUD_NAME = <your-cloud-name>
CLOUDINARY_API_KEY = <api-key>
CLOUDINARY_API_SECRET = <api-secret>
```

**Email Service (Brevo)**:
```
BREVO_API_KEY = <api-key>
BREVO_SENDER_EMAIL = noreply@example.com
BREVO_WELCOME_EMAIL_TEMPLATE_ID = 1
BREVO_ORDER_CONFIRMATION_TEMPLATE_ID = 2
BREVO_PAYMENT_SUCCESS_TEMPLATE_ID = 3
```

**Application**:
```
ASPNETCORE_ENVIRONMENT = Production
LOG_LEVEL = Information
CORS_ALLOWED_ORIGINS = https://frontend.example.com
```

### Step 4: Configure Database

**Option A: Use Render's PostgreSQL**

1. In the same service form, look for **"Databases"** section
2. Click **"Create Database"**
3. Name: `kromicstore-postgres`
4. Database: `kromicstore`
5. User: `kromicuser`
6. Plan: `Standard`
7. Render will automatically populate `DATABASE_URL`

**Option B: Use External PostgreSQL**

If using external database:
1. Get connection string from your database provider
2. Set `DATABASE_URL` environment variable manually
3. Ensure database is initialized (migrations run on startup)
4. Verify network access allows Render IP ranges

### Step 5: Configure Redis Cache

**Option A: Use Render's Redis**

1. In the same service form, look for **"Redis"** section
2. Click **"Create Redis"**
3. Name: `kromicstore-redis`
4. Plan: `Standard`
5. Render will automatically populate `REDIS_URL`

**Option B: Use External Redis**

If using external cache:
1. Get connection string from your cache provider
2. Set `REDIS_URL` environment variable
3. Verify network access allows Render IP

### Step 6: Review and Deploy

1. **Review** all settings:
   - Runtime: Docker ✓
   - Build & Start commands set ✓
   - All environment variables configured ✓
   - Database and Redis configured ✓

2. **Click "Create Web Service"** to start deployment

3. **Monitor deployment**:
   - View logs in real-time
   - Wait for "Your service is live" message
   - Check health endpoint: `https://<service-url>/health`

### Step 7: Verify Deployment

After deployment completes:

1. **Check Service Status**:
   - Go to service dashboard
   - Verify "Live" status (blue indicator)

2. **Test Health Endpoints**:
   ```bash
   # Liveness check (should return 200)
   curl https://<service-url>/health

   # Readiness check (should return 200 with dependency status)
   curl https://<service-url>/health/ready
   ```

3. **Check API Swagger Documentation**:
   ```
   https://<service-url>/swagger
   ```

4. **Monitor Logs**:
   - Click "Logs" tab in Render dashboard
   - Should see startup messages:
     - "KromicStore API starting up"
     - "Database connection established"
     - "Database migrations completed successfully"
     - "Application ready to receive requests"

## Troubleshooting

### Deployment Fails with Docker Build Error

**Symptom**: Build fails with "Dockerfile not found" or similar

**Solution**:
- Ensure `Dockerfile` is in repository root
- Check `.dockerignore` doesn't exclude necessary files
- Review build logs for specific errors

### Service Starts but Health Check Fails

**Symptom**: "Service exited with code 1" in logs

**Causes**:
- Missing environment variable
- Database connection fails
- Migration errors

**Solution**:
1. Check logs: "Which environment variable is required?"
2. Verify all variables set in Render dashboard
3. Verify database connectivity from Render region
4. Check DATABASE_URL format (must be valid PostgreSQL URI)

### Slow Startup (> 60 seconds)

**Cause**: Database not accessible, causing timeout retries

**Solution**:
1. Verify DATABASE_URL is correct and database is running
2. Check network connectivity between Render and database
3. Ensure database firewall allows Render IP ranges
4. Increase start period in health check if needed

### Requests Timeout or 503 Errors

**Cause**: Service degraded due to failed health checks

**Solution**:
- Check `/health/ready` endpoint
- Verify database and cache connectivity
- Review error logs for specific failures
- Ensure sufficient memory allocated

### External Service Integration Fails (Razorpay, Google, etc.)

**Symptom**: 400/401 errors when calling external services

**Solution**:
1. Verify API keys are correct and not expired
2. Check service credentials in their respective dashboards
3. Ensure webhook endpoints are accessible
4. For OAuth, verify redirect URLs are configured

## Post-Deployment

### Enable Auto-Deploy from GitHub

1. Go to service settings in Render
2. Enable "Auto-deploy" for specified branch
3. Future git pushes automatically trigger deployment

### Configure Domain

1. In service settings, go to "Custom Domain"
2. Add your domain (e.g., `api.example.com`)
3. Update DNS records as directed by Render
4. SSL certificate auto-configured with Let's Encrypt

### Enable Monitoring

1. Install application monitoring (optional):
   - Azure Application Insights
   - New Relic
   - Datadog

2. Configure alerts for:
   - Service down (health check failures)
   - High error rate (5xx responses)
   - Slow response times
   - Database connection issues

### Backup Strategy

1. Configure PostgreSQL backups:
   - Render manages backups automatically (7-day retention)
   - Set custom backup schedule if needed

2. Document recovery procedure:
   - Backup location
   - Restore procedure
   - Time-to-recovery estimate

### Scale Configuration

If expecting high traffic:

1. **Vertical Scaling**: Increase instance memory/CPU
2. **Horizontal Scaling**: Enable auto-scale feature
   - Min instances: 1
   - Max instances: 3-5
   - Scale trigger: CPU/Memory threshold

## Environment Configuration Templates

### Production Environment
```env
ASPNETCORE_ENVIRONMENT=Production
LOG_LEVEL=Information
CORS_ALLOWED_ORIGINS=https://app.example.com,https://admin.example.com
```

### Staging Environment
```env
ASPNETCORE_ENVIRONMENT=Staging
LOG_LEVEL=Debug
CORS_ALLOWED_ORIGINS=https://staging-app.example.com
```

### Development-like Environment
```env
ASPNETCORE_ENVIRONMENT=Development
LOG_LEVEL=Debug
CORS_ALLOWED_ORIGINS=*
```

## Common Configuration Mistakes

❌ **Mistake**: Hardcoding secrets in `.env` file and committing to git
✅ **Fix**: Use Render's environment variable management, never commit secrets

❌ **Mistake**: Using `localhost` or `127.0.0.1` for database URL
✅ **Fix**: Use full hostname/IP that's accessible from Render

❌ **Mistake**: Not setting ASPNETCORE_ENVIRONMENT
✅ **Fix**: Explicitly set to `Production` for production deployments

❌ **Mistake**: Insufficient memory allocation
✅ **Fix**: Allocate minimum 512MB, 1GB recommended for .NET

❌ **Mistake**: Health check path incorrect
✅ **Fix**: Ensure health check path matches endpoint (`/health`)

## Updating the Application

### Option 1: Automatic Deploy (Recommended)

1. Push changes to main branch
2. Render automatically:
   - Triggers Docker build
   - Runs new image
   - Performs health check
   - Routes traffic to new instance

### Option 2: Manual Deploy

1. In Render dashboard, click "Deploy" button
2. Select commit to deploy
3. Follow same deployment flow

## Rollback Procedure

If deployment causes issues:

1. **Immediate Rollback**:
   - Click "Logs" in Render dashboard
   - Find last successful deployment hash
   - Click "Deploy" and select previous commit

2. **Post-Mortem**:
   - Review error logs
   - Identify root cause
   - Test locally before re-deploying

## Cost Estimation

**Estimated Monthly Cost** (as of 2024):

| Component | Tier | Cost |
|-----------|------|------|
| Web Service | Standard | $12 |
| PostgreSQL | Standard (5GB) | $30 |
| Redis | Standard (0.5GB) | $12 |
| **Total** | | **~$54/month** |

*Prices subject to change - verify on Render website*

## References

- [Render Documentation](https://render.com/docs)
- [Docker in Render](https://render.com/docs/deploy-docker)
- [PostgreSQL on Render](https://render.com/docs/databases)
- [Environment Variables](./Environment-Setup.md)
- [Deployment Checklist](./Deployment-Checklist.md)
- [Troubleshooting Guide](./Troubleshooting.md)

## Support

For deployment issues:

1. Check [Render status page](https://status.render.com)
2. Review logs in Render dashboard
3. Consult [Troubleshooting Guide](./Troubleshooting.md)
4. Contact Render support via dashboard

---

**Last Updated**: 2024
**Deployment Method**: Docker on Render
**Expected Deployment Time**: 5-10 minutes (first deploy), 2-5 minutes (subsequent)
