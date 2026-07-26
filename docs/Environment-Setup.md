# KromicStore Environment Configuration Guide

Complete reference for configuring KromicStore API with environment variables.

## Overview

KromicStore uses environment variables for all configuration values to support deployment across different environments (Development, Staging, Production) without code changes. All sensitive values must be provided via environment variables in production.

## Environment Files

### Development (`.env.development`)
Safe test values for local development. Contains placeholder credentials for third-party services.

### Staging (`.env.staging`)
Template for staging environment. Uses `${VARIABLE}` syntax for secrets injected from secure storage.

### Production (`.env.production`)
Template for production deployment. All secrets must be injected from Render Secrets or equivalent.

## Configuration Validation

On startup, the application validates:
- All required environment variables are present
- String values are not empty or whitespace
- Numeric values are within valid ranges
- Encryption keys are at least 32 characters
- JWT secrets are at least 32 characters

If validation fails, the application logs missing/invalid variables and exits with error code 1.

## Required Environment Variables

### Core Infrastructure

| Variable | Type | Min Length | Example | Description |
|----------|------|-----------|---------|-------------|
| `DATABASE_URL` | string | - | `postgresql://user:pass@host:5432/db` | PostgreSQL connection string |
| `REDIS_URL` | string | - | `localhost:6379` | Redis server address |
| `JWT_SECRET` | string | 32 | (generated) | JWT token signing secret |
| `SECURITY_ENCRYPTION_KEY` | string | 32 | (generated) | Data encryption key |

### API Configuration

| Variable | Type | Default | Example | Description |
|----------|------|---------|---------|-------------|
| `API_BASE_URL` | string | - | `https://api.example.com` | Public API URL |
| `FRONTEND_BASE_URL` | string | - | `https://app.example.com` | Frontend application URL |
| `SWAGGER_ENABLED` | bool | `true` | `false` (production) | Enable/disable Swagger UI |
| `CORS_ALLOWED_ORIGINS` | string | - | `https://app.example.com` | Comma-separated CORS origins |

### Authentication

| Variable | Type | Default | Example | Description |
|----------|------|---------|---------|-------------|
| `JWT_AUTHORITY` | string | `https://auth.example.com` | `https://auth.kronmicstore.com` | JWT issuer URL |
| `JWT_AUDIENCE` | string | `kromic-store-api` | - | JWT audience claim |
| `JWT_EXPIRATION_MINUTES` | int | `60` | `120` | Access token lifetime |
| `REFRESH_TOKEN_EXPIRATION_DAYS` | int | `7` | `30` | Refresh token lifetime |

### Password Policy

| Variable | Type | Default | Description |
|----------|------|---------|-------------|
| `PASSWORD_MIN_LENGTH` | int | `8` | Minimum password length |
| `PASSWORD_REQUIRE_UPPERCASE` | bool | `true` | Require uppercase letters |
| `PASSWORD_REQUIRE_NUMBERS` | bool | `true` | Require numeric digits |
| `PASSWORD_REQUIRE_SPECIAL` | bool | `true` | Require special characters |

### Database Connection Pooling

| Variable | Type | Default | Range | Description |
|----------|------|---------|-------|-------------|
| `DB_CONNECTION_POOL_MIN` | int | `5` | 1-100 | Minimum pool connections |
| `DB_CONNECTION_POOL_MAX` | int | `25` | 5-1000 | Maximum pool connections |
| `DB_CONNECTION_TIMEOUT_SECONDS` | int | `30` | 5-300 | Connection acquisition timeout |
| `DB_IDLE_TIMEOUT_SECONDS` | int | `300` | 60-3600 | Connection idle timeout |
| `DB_MAX_AGE_SECONDS` | int | `1800` | 300-7200 | Max connection lifetime |

### Caching (Redis)

| Variable | Type | Default | Description |
|----------|------|---------|-------------|
| `REDIS_URL` | string | - | Redis server address |
| `REDIS_PASSWORD` | string | `` | Redis password (if needed) |
| `REDIS_DB` | int | `0` | Redis database number |
| `REDIS_TIMEOUT_MS` | int | `5000` | Redis operation timeout |
| `CACHE_TTL_PRODUCTS_MINUTES` | int | `60` | Product cache TTL |
| `CACHE_TTL_ORDERS_MINUTES` | int | `5` | Order cache TTL |
| `CACHE_TTL_CONFIG_MINUTES` | int | `30` | Configuration cache TTL |

### Rate Limiting

| Variable | Type | Default | Description |
|----------|------|---------|-------------|
| `RATE_LIMIT_ENABLED` | bool | `true` | Enable rate limiting |
| `RATE_LIMIT_REQUESTS_PER_MINUTE` | int | `100` | Default rate limit (dev: 1000) |
| `RATE_LIMIT_BY_PLAN` | json | (see file) | Per-plan limits |
| `RATE_LIMIT_CACHE_KEY_PREFIX` | string | `rate_limit` | Redis key prefix |

### External Services

| Variable | Type | Default | Range | Description |
|----------|------|---------|-------|-------------|
| `EXTERNAL_SERVICE_TIMEOUT_SECONDS` | int | `30` | 5-300 | External service timeout |
| `EXTERNAL_SERVICE_MAX_RETRIES` | int | `4` | 1-10 | Max retry attempts |
| `EXTERNAL_SERVICE_RETRY_DELAYS_MS` | json | `[100,1000,10000,30000]` | - | Retry delay schedule |
| `CIRCUIT_BREAKER_FAILURE_THRESHOLD` | int | `5` | 1-20 | Circuit breaker threshold |
| `CIRCUIT_BREAKER_TIMEOUT_SECONDS` | int | `30` | 10-300 | Circuit breaker timeout |



### Razorpay Payment Gateway

| Variable | Type | Required | Description |
|----------|------|----------|-------------|
| `RAZORPAY_KEY_ID` | string | Yes | Razorpay API key (rzp_live_* for prod) |
| `RAZORPAY_KEY_SECRET` | string | Yes | Razorpay API secret |
| `RAZORPAY_WEBHOOK_SECRET` | string | Yes | Razorpay webhook signing secret |
| `RAZORPAY_TIMEOUT_SECONDS` | int | No | Default: 30 seconds |
| `RAZORPAY_RETRY_ENABLED` | bool | No | Default: true |
| `RAZORPAY_CIRCUIT_BREAKER_THRESHOLD` | int | No | Default: 5 failures |

### Google OAuth

| Variable | Type | Required | Description |
|----------|------|----------|-------------|
| `GOOGLE_CLIENT_ID` | string | Yes | OAuth client ID |
| `GOOGLE_CLIENT_SECRET` | string | Yes | OAuth client secret |
| `GOOGLE_REDIRECT_URI` | string | No | Callback URL (auto-constructed) |
| `GOOGLE_TOKEN_ENDPOINT` | string | No | Default: `https://oauth2.googleapis.com/token` |
| `GOOGLE_USER_INFO_ENDPOINT` | string | No | Default: `https://www.googleapis.com/oauth2/v2/userinfo` |

### Cloudinary Media Service

| Variable | Type | Required | Description |
|----------|------|----------|-------------|
| `CLOUDINARY_CLOUD_NAME` | string | Yes | Cloudinary cloud name |
| `CLOUDINARY_API_KEY` | string | Yes | Cloudinary API key |
| `CLOUDINARY_API_SECRET` | string | Yes | Cloudinary API secret |
| `CLOUDINARY_BASE_URL` | string | No | Default: `https://api.cloudinary.com` |
| `CLOUDINARY_FOLDER_PATH` | string | No | Default: `kromic-store` |
| `CLOUDINARY_QUALITY` | string | No | Default: `auto` |
| `CLOUDINARY_MAX_FILE_SIZE_MB` | int | No | Default: 100 MB |

### Brevo Email Service

| Variable | Type | Required | Description |
|----------|------|----------|-------------|
| `BREVO_API_KEY` | string | Yes | Brevo API key |
| `BREVO_SENDER_EMAIL` | string | No | Default: `noreply@example.com` |
| `BREVO_SENDER_NAME` | string | No | Default: `KromicStore` |
| `BREVO_BASE_URL` | string | No | Default: `https://api.brevo.com` |
| `BREVO_API_VERSION` | string | No | Default: `v3` |
| `BREVO_WELCOME_EMAIL_TEMPLATE_ID` | int | No | Default: `1` |
| `BREVO_ORDER_CONFIRMATION_TEMPLATE_ID` | int | No | Default: `2` |
| `BREVO_SHIPMENT_NOTIFICATION_TEMPLATE_ID` | int | No | Default: `3` |
| `BREVO_PAYMENT_FAILURE_TEMPLATE_ID` | int | No | Default: `4` |

### Hangfire Background Jobs

| Variable | Type | Default | Description |
|----------|------|---------|-------------|
| `HANGFIRE_ENABLED` | bool | `true` | Enable background jobs |
| `HANGFIRE_WORKER_COUNT` | int | CPU cores | Number of worker threads |
| `HANGFIRE_QUEUES` | string | `default,webhooks,scheduled` | Job queues |
| `HANGFIRE_SUCCESS_JOB_EXPIRY_MINUTES` | int | `60` | Keep successful jobs for X minutes |
| `HANGFIRE_FAILED_JOB_EXPIRY_DAYS` | int | `7` | Keep failed jobs for X days |

### Application Settings

| Variable | Type | Default | Description |
|----------|------|---------|-------------|
| `ASPNETCORE_ENVIRONMENT` | string | `Production` | Environment name |
| `ASPNETCORE_URLS` | string | `http://+:8080` | Server listen URLs |
| `APPLICATION_INSTANCE_ID` | string | (UUID) | Unique instance identifier |
| `MAX_UPLOAD_SIZE_MB` | int | `100` | Maximum file upload size |
| `SESSION_TIMEOUT_MINUTES` | int | `30` | User session timeout |
| `PAGINATION_DEFAULT_PAGE_SIZE` | int | `20` | Default page size |
| `PAGINATION_MAX_PAGE_SIZE` | int | `100` | Maximum page size |

### Tenant Settings

| Variable | Type | Default | Description |
|----------|------|---------|-------------|
| `TENANT_TRIAL_DURATION_DAYS` | int | `14` | Trial period duration |
| `TENANT_MAX_USERS_STARTER` | int | `5` | Starter plan user limit |
| `TENANT_MAX_USERS_PROFESSIONAL` | int | `50` | Professional plan user limit |
| `TENANT_MAX_PRODUCTS_STARTER` | int | `100` | Starter plan product limit |
| `TENANT_MAX_PRODUCTS_PROFESSIONAL` | int | `1000` | Professional plan product limit |
| `TENANT_MAX_API_CALLS_STARTER` | int | `10000` | Starter plan API call limit |
| `TENANT_MAX_API_CALLS_PROFESSIONAL` | int | `100000` | Professional plan API call limit |

### Subscription Plans

| Variable | Type | Default | Description |
|----------|------|---------|-------------|
| `SUBSCRIPTION_PLAN_STARTER_PRICE` | decimal | `9.99` | Starter plan price |
| `SUBSCRIPTION_PLAN_PROFESSIONAL_PRICE` | decimal | `29.99` | Professional plan price |
| `SUBSCRIPTION_PLAN_ENTERPRISE_PRICE` | decimal | `99.99` | Enterprise plan price |
| `SUBSCRIPTION_PLAN_CURRENCY` | string | `USD` | Billing currency |

### Logging

| Variable | Type | Default | Values | Description |
|----------|------|---------|--------|-------------|
| `LOG_LEVEL` | string | `Information` | Debug, Information, Warning, Error, Fatal | Minimum log level |
| `LOG_OUTPUT_FORMAT` | string | `json` | json, text | Log output format |
| `LOG_FILE_PATH` | string | `/var/log/app.log` | - | Log file path |
| `LOG_FILE_SIZE_MB` | int | `100` | - | Max log file size |
| `LOG_FILES_TO_KEEP` | int | `10` | - | Retained log files |
| `CORRELATION_ID_ENABLED` | bool | `true` | - | Enable correlation IDs |

### Monitoring

| Variable | Type | Default | Description |
|----------|------|---------|-------------|
| `MONITORING_ENABLED` | bool | `false` | Enable Application Insights |
| `MONITORING_INSTRUMENTATION_KEY` | string | `` | App Insights key |
| `MONITORING_LOG_LEVEL` | string | `Information` | Monitoring log level |
| `MONITORING_RESPONSE_TIME_THRESHOLD_MS` | int | `500` | Slow request threshold |
| `MONITORING_DB_QUERY_THRESHOLD_MS` | int | `100` | Slow query threshold |

### Security

| Variable | Type | Default | Description |
|----------|------|---------|-------------|
| `SECURITY_REQUIRE_HTTPS` | bool | `true` (prod) | Require HTTPS in prod |
| `CORS_ALLOW_CREDENTIALS` | bool | `true` | Allow credentials in CORS |
| `CORS_ALLOW_ANY_ORIGIN` | bool | `false` | Allow any origin (dev only) |

## Setup Instructions

### Local Development

1. Copy `.env.development` to `.env.local`:
   ```bash
   cp .env.development .env.local
   ```

2. Update test credentials:
   ```bash
   RAZORPAY_KEY_ID=rzp_test_xxxxx
   GOOGLE_CLIENT_ID=your-client-id
   CLOUDINARY_CLOUD_NAME=your-cloud
   BREVO_API_KEY=your-api-key
   ```

3. Start PostgreSQL and Redis:
   ```bash
   docker-compose up -d
   ```

4. Run the application:
   ```bash
   dotnet run --project src/KromicStore.API
   ```

### Staging Deployment

1. Configure environment variables in Render dashboard:
   - Copy all variables from `.env.staging`
   - Replace `${VARIABLE}` placeholders with actual values from secrets manager
   - Never commit actual secrets

2. Deploy to Render:
   ```bash
   git push origin main
   ```

### Production Deployment

1. Generate secure secrets:
   ```bash
   # Generate JWT secret (32+ chars)
   openssl rand -base64 32

   # Generate encryption key (32+ chars)
   openssl rand -base64 32
   ```

2. Store in Render Secrets (or equivalent):
   - Never use `.env.production` with actual secrets in repository
   - Configure secrets via Render dashboard or environment variables
   - Rotate secrets regularly (6-12 months)

3. Verify required secrets are set:
   ```bash
   # Check before deployment
   - DATABASE_URL
   - JWT_SECRET (length >= 32)
   - SECURITY_ENCRYPTION_KEY (length >= 32)
   - RAZORPAY_KEY_ID
   - RAZORPAY_KEY_SECRET
   - GOOGLE_CLIENT_ID
   - GOOGLE_CLIENT_SECRET
   - CLOUDINARY_CLOUD_NAME
   - CLOUDINARY_API_KEY
   - CLOUDINARY_API_SECRET
   - BREVO_API_KEY
   - REDIS_URL
   ```

## Configuration Validation Rules

### Ranges

- `JWT_EXPIRATION_MINUTES`: 1 to 10,080 (1 minute to 7 days)
- `REFRESH_TOKEN_EXPIRATION_DAYS`: 1 to 365
- `PASSWORD_MIN_LENGTH`: 6 to 128
- `DB_CONNECTION_POOL_MIN`: 1 to 100
- `DB_CONNECTION_POOL_MAX`: 5 to 1000
- `CACHE_TTL_*`: 1 to 1,440 minutes (1 minute to 24 hours)
- `EXTERNAL_SERVICE_TIMEOUT_SECONDS`: 5 to 300
- `RAZORPAY_TIMEOUT_SECONDS`: 5 to 300

### Format Validation

- URLs must be valid URI format (starts with http:// or https://)
- Connection strings must include host, port, database
- API keys must be non-empty strings
- Template IDs must be positive integers

### Secrets Security

- JWT_SECRET: minimum 32 characters
- SECURITY_ENCRYPTION_KEY: minimum 32 characters
- API Keys: use generated values, never use defaults
- Webhook secrets: use service-provided values

## Troubleshooting

### Configuration Not Loading

**Symptom**: Application exits with configuration validation error

**Solution**:
1. Check environment variables are set: `printenv | grep VARIABLE`
2. Verify variable values are not empty or whitespace
3. Check string length requirements (JWT_SECRET, ENCRYPTION_KEY)
4. Review detailed error message for specific missing variables

### Connection String Issues

**Symptom**: Database connection fails on startup

**Solution**:
1. Verify PostgreSQL is running
2. Test connection manually: `psql DATABASE_URL`
3. Check connection pool settings match PostgreSQL limits
4. Verify maximum connections: `SHOW max_connections` in psql

### Redis Connection Issues

**Symptom**: Cache operations timeout or fail

**Solution**:
1. Verify Redis is running: `redis-cli ping`
2. Test connection: `redis-cli -h $REDIS_HOST -p $REDIS_PORT ping`
3. Check network connectivity and firewall rules
4. Increase REDIS_TIMEOUT_MS if network is slow

### Third-Party Service Failures

**Symptom**: API calls to Razorpay, Google, Cloudinary, or Brevo fail

**Solution**:
1. Verify API keys are correct and active
2. Check service endpoints are accessible
3. Review timeout settings (EXTERNAL_SERVICE_TIMEOUT_SECONDS)
4. Check circuit breaker status (may be open after repeated failures)
5. Review application logs for specific error messages

## Security Checklist

- [ ] Never commit `.env.production` with actual secrets
- [ ] Rotate secrets at least annually
- [ ] Use strong, randomly-generated encryption keys
- [ ] Use HTTPS in production (SECURITY_REQUIRE_HTTPS=true)
- [ ] Limit CORS origins to known frontend URLs
- [ ] Keep external service API keys in secure vault
- [ ] Monitor failed authentication attempts (PASSWORD_MIN_LENGTH and complexity)
- [ ] Review and log configuration changes
- [ ] Test secrets before deploying to production

