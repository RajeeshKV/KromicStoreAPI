// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using KromicStore.Application.Interfaces;
using KromicStore.Domain.Entities;
using KromicStore.Domain.Enums;
using KromicStore.Infrastructure.Services;

namespace KromicStore.Tests.Unit.Infrastructure
{
    /// <summary>
    /// Unit tests for TenantConfigurationSeeder to verify default configuration initialization.
    /// </summary>
    public class TenantConfigurationSeederTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<TenantConfigurationSeeder>> _mockLogger;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly TenantConfigurationSeeder _seeder;

        public TenantConfigurationSeederTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<TenantConfigurationSeeder>>();
            _mockConfiguration = new Mock<IConfiguration>();
            
            SetupMockConfiguration();
            
            _seeder = new TenantConfigurationSeeder(_mockUnitOfWork.Object, _mockLogger.Object, _mockConfiguration.Object);
        }

        /// <summary>
        /// Setup mock configuration with default values for external services.
        /// </summary>
        private void SetupMockConfiguration()
        {
            var configValues = new Dictionary<string, string?>
            {
                { "ExternalServices:Brevo:TemplateIds:WelcomeEmail", "1" },
                { "ExternalServices:Brevo:TemplateIds:OrderConfirmation", "2" },
                { "ExternalServices:Brevo:TemplateIds:ShipmentNotification", "3" },
                { "ExternalServices:Brevo:TemplateIds:PaymentFailure", "4" },
                { "ExternalServices:Brevo:SenderEmail", "support@kromicstore.com" },
                { "ExternalServices:Razorpay:Endpoint", "https://api.razorpay.com/v1/" }
            };

            _mockConfiguration
                .Setup(x => x[It.IsAny<string>()])
                .Returns<string>(key => configValues.ContainsKey(key) ? configValues[key] : null);
        }

        [Fact]
        public async Task SeedDefaultConfigurationAsync_WithValidTenantId_CreatesDefaultConfigurations()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var configurations = new List<TenantConfiguration>();
            var auditLogs = new List<ConfigurationAuditLog>();

            _mockUnitOfWork
                .Setup(x => x.TenantConfigurations.AddAsync(It.IsAny<TenantConfiguration>(), CancellationToken.None))
                .Callback<TenantConfiguration, CancellationToken>((config, _) => configurations.Add(config))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork
                .Setup(x => x.ConfigurationAuditLogs.AddAsync(It.IsAny<ConfigurationAuditLog>(), CancellationToken.None))
                .Callback<ConfigurationAuditLog, CancellationToken>((log, _) => auditLogs.Add(log))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork
                .Setup(x => x.SaveChangesAsync(CancellationToken.None))
                .ReturnsAsync(configurations.Count + auditLogs.Count);

            // Act
            await _seeder.SeedDefaultConfigurationAsync(tenantId, "US", CancellationToken.None);

            // Assert
            Assert.NotEmpty(configurations);
            Assert.NotEmpty(auditLogs);
            Assert.Equal(configurations.Count, auditLogs.Count);
            
            // Verify all configurations have correct tenant ID
            Assert.All(configurations, config => Assert.Equal(tenantId, config.TenantId));
            
            // Verify all audit logs have correct tenant ID
            Assert.All(auditLogs, log => Assert.Equal(tenantId, log.TenantId));
        }

        [Fact]
        public async Task SeedDefaultConfigurationAsync_WithNotificationSettings_CreatesEmailNotificationConfigs()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var configurations = new List<TenantConfiguration>();

            _mockUnitOfWork
                .Setup(x => x.TenantConfigurations.AddAsync(It.IsAny<TenantConfiguration>(), CancellationToken.None))
                .Callback<TenantConfiguration, CancellationToken>((config, _) => configurations.Add(config))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork
                .Setup(x => x.ConfigurationAuditLogs.AddAsync(It.IsAny<ConfigurationAuditLog>(), CancellationToken.None))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork
                .Setup(x => x.SaveChangesAsync(CancellationToken.None))
                .ReturnsAsync(configurations.Count);

            // Act
            await _seeder.SeedDefaultConfigurationAsync(tenantId);

            // Assert
            var notificationConfigs = configurations.Where(c => c.ConfigKey.StartsWith("notifications:")).ToList();
            Assert.NotEmpty(notificationConfigs);
            
            // Verify expected notification settings exist
            var emailEnabledConfig = notificationConfigs.FirstOrDefault(c => c.ConfigKey == "notifications:email_enabled");
            Assert.NotNull(emailEnabledConfig);
            Assert.Equal("true", emailEnabledConfig.ConfigValue);
            
            var emailFrequencyConfig = notificationConfigs.FirstOrDefault(c => c.ConfigKey == "notifications:email_frequency");
            Assert.NotNull(emailFrequencyConfig);
            Assert.Equal("immediate", emailFrequencyConfig.ConfigValue);
        }

        [Fact]
        public async Task SeedDefaultConfigurationAsync_WithWebhookSettings_CreatesWebhookConfigs()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var configurations = new List<TenantConfiguration>();

            _mockUnitOfWork
                .Setup(x => x.TenantConfigurations.AddAsync(It.IsAny<TenantConfiguration>(), CancellationToken.None))
                .Callback<TenantConfiguration, CancellationToken>((config, _) => configurations.Add(config))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork
                .Setup(x => x.ConfigurationAuditLogs.AddAsync(It.IsAny<ConfigurationAuditLog>(), CancellationToken.None))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork
                .Setup(x => x.SaveChangesAsync(CancellationToken.None))
                .ReturnsAsync(configurations.Count);

            // Act
            await _seeder.SeedDefaultConfigurationAsync(tenantId);

            // Assert
            var webhookConfigs = configurations.Where(c => c.ConfigKey.StartsWith("webhooks:")).ToList();
            Assert.NotEmpty(webhookConfigs);
            
            var enabledConfig = webhookConfigs.FirstOrDefault(c => c.ConfigKey == "webhooks:enabled");
            Assert.NotNull(enabledConfig);
            Assert.Equal("true", enabledConfig.ConfigValue);
            
            var retryDelaysConfig = webhookConfigs.FirstOrDefault(c => c.ConfigKey == "webhooks:retry_delays_ms");
            Assert.NotNull(retryDelaysConfig);
            Assert.Contains("1000", retryDelaysConfig.ConfigValue); // 1 second
        }

        [Fact]
        public async Task SeedDefaultConfigurationAsync_WithFeatureFlags_CreatesFeatureConfigs()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var configurations = new List<TenantConfiguration>();

            _mockUnitOfWork
                .Setup(x => x.TenantConfigurations.AddAsync(It.IsAny<TenantConfiguration>(), CancellationToken.None))
                .Callback<TenantConfiguration, CancellationToken>((config, _) => configurations.Add(config))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork
                .Setup(x => x.ConfigurationAuditLogs.AddAsync(It.IsAny<ConfigurationAuditLog>(), CancellationToken.None))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork
                .Setup(x => x.SaveChangesAsync(CancellationToken.None))
                .ReturnsAsync(configurations.Count);

            // Act
            await _seeder.SeedDefaultConfigurationAsync(tenantId);

            // Assert
            var featureConfigs = configurations.Where(c => c.ConfigKey.StartsWith("features:")).ToList();
            Assert.NotEmpty(featureConfigs);
            
            // All trial plan features should be enabled
            Assert.NotNull(featureConfigs.FirstOrDefault(c => c.ConfigKey == "features:products_enabled" && c.ConfigValue == "true"));
            Assert.NotNull(featureConfigs.FirstOrDefault(c => c.ConfigKey == "features:orders_enabled" && c.ConfigValue == "true"));
            Assert.NotNull(featureConfigs.FirstOrDefault(c => c.ConfigKey == "features:webhooks_enabled" && c.ConfigValue == "true"));
            
            // Analytics should be disabled for starter plan
            Assert.NotNull(featureConfigs.FirstOrDefault(c => c.ConfigKey == "features:analytics_enabled" && c.ConfigValue == "false"));
        }

        [Fact]
        public async Task SeedDefaultConfigurationAsync_WithCurrencyConfiguration_SetsCurrencyBasedOnCountry()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var configurations = new List<TenantConfiguration>();

            _mockUnitOfWork
                .Setup(x => x.TenantConfigurations.AddAsync(It.IsAny<TenantConfiguration>(), CancellationToken.None))
                .Callback<TenantConfiguration, CancellationToken>((config, _) => configurations.Add(config))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork
                .Setup(x => x.ConfigurationAuditLogs.AddAsync(It.IsAny<ConfigurationAuditLog>(), CancellationToken.None))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork
                .Setup(x => x.SaveChangesAsync(CancellationToken.None))
                .ReturnsAsync(configurations.Count);

            // Act
            await _seeder.SeedDefaultConfigurationAsync(tenantId, "IN");

            // Assert
            var currencyConfig = configurations.FirstOrDefault(c => c.ConfigKey == "currency:default");
            Assert.NotNull(currencyConfig);
            Assert.Equal("INR", currencyConfig.ConfigValue); // India -> INR
        }

        [Fact]
        public async Task SeedDefaultConfigurationAsync_WithTimezoneConfiguration_SetsTimezoneBasedOnCountry()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var configurations = new List<TenantConfiguration>();

            _mockUnitOfWork
                .Setup(x => x.TenantConfigurations.AddAsync(It.IsAny<TenantConfiguration>(), CancellationToken.None))
                .Callback<TenantConfiguration, CancellationToken>((config, _) => configurations.Add(config))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork
                .Setup(x => x.ConfigurationAuditLogs.AddAsync(It.IsAny<ConfigurationAuditLog>(), CancellationToken.None))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork
                .Setup(x => x.SaveChangesAsync(CancellationToken.None))
                .ReturnsAsync(configurations.Count);

            // Act
            await _seeder.SeedDefaultConfigurationAsync(tenantId, "JP");

            // Assert
            var timezoneConfig = configurations.FirstOrDefault(c => c.ConfigKey == "timezone:default");
            Assert.NotNull(timezoneConfig);
            Assert.Equal("Asia/Tokyo", timezoneConfig.ConfigValue); // Japan
        }

        [Fact]
        public async Task SeedDefaultConfigurationAsync_WithDefaultCountry_SetsCurrencyAndTimezoneToDefaults()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var configurations = new List<TenantConfiguration>();

            _mockUnitOfWork
                .Setup(x => x.TenantConfigurations.AddAsync(It.IsAny<TenantConfiguration>(), CancellationToken.None))
                .Callback<TenantConfiguration, CancellationToken>((config, _) => configurations.Add(config))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork
                .Setup(x => x.ConfigurationAuditLogs.AddAsync(It.IsAny<ConfigurationAuditLog>(), CancellationToken.None))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork
                .Setup(x => x.SaveChangesAsync(CancellationToken.None))
                .ReturnsAsync(configurations.Count);

            // Act - no country specified
            await _seeder.SeedDefaultConfigurationAsync(tenantId, null);

            // Assert
            var currencyConfig = configurations.FirstOrDefault(c => c.ConfigKey == "currency:default");
            Assert.NotNull(currencyConfig);
            Assert.Equal("USD", currencyConfig.ConfigValue); // Default to USD

            var timezoneConfig = configurations.FirstOrDefault(c => c.ConfigKey == "timezone:default");
            Assert.NotNull(timezoneConfig);
            Assert.Equal("UTC", timezoneConfig.ConfigValue); // Default to UTC
        }

        [Fact]
        public async Task SeedDefaultConfigurationAsync_CreatesAuditLogWithSystemUser()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var auditLogs = new List<ConfigurationAuditLog>();
            var systemUserId = new Guid("00000000-0000-0000-0000-000000000001");

            _mockUnitOfWork
                .Setup(x => x.TenantConfigurations.AddAsync(It.IsAny<TenantConfiguration>(), CancellationToken.None))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork
                .Setup(x => x.ConfigurationAuditLogs.AddAsync(It.IsAny<ConfigurationAuditLog>(), CancellationToken.None))
                .Callback<ConfigurationAuditLog, CancellationToken>((log, _) => auditLogs.Add(log))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork
                .Setup(x => x.SaveChangesAsync(CancellationToken.None))
                .ReturnsAsync(auditLogs.Count);

            // Act
            await _seeder.SeedDefaultConfigurationAsync(tenantId);

            // Assert
            Assert.NotEmpty(auditLogs);
            Assert.All(auditLogs, log => 
            {
                Assert.Equal(systemUserId, log.ChangedBy);
                Assert.Equal("Initial configuration on tenant registration", log.Reason);
                Assert.Null(log.OldValue); // No old value for initial config
                Assert.NotNull(log.NewValue);
            });
        }

        [Fact]
        public async Task SeedDefaultConfigurationAsync_WithEmptyTenantId_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _seeder.SeedDefaultConfigurationAsync(Guid.Empty));
        }

        [Fact]
        public async Task SeedDefaultConfigurationAsync_WithAllSettingsScope_SetsTenantScope()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var configurations = new List<TenantConfiguration>();

            _mockUnitOfWork
                .Setup(x => x.TenantConfigurations.AddAsync(It.IsAny<TenantConfiguration>(), CancellationToken.None))
                .Callback<TenantConfiguration, CancellationToken>((config, _) => configurations.Add(config))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork
                .Setup(x => x.ConfigurationAuditLogs.AddAsync(It.IsAny<ConfigurationAuditLog>(), CancellationToken.None))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork
                .Setup(x => x.SaveChangesAsync(CancellationToken.None))
                .ReturnsAsync(configurations.Count);

            // Act
            await _seeder.SeedDefaultConfigurationAsync(tenantId);

            // Assert
            Assert.All(configurations, config => Assert.Equal(ConfigScope.Tenant, config.Scope));
        }

        [Fact]
        public async Task SeedDefaultConfigurationAsync_CallsSaveChangesAsync()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var saveChangesCallCount = 0;

            _mockUnitOfWork
                .Setup(x => x.TenantConfigurations.AddAsync(It.IsAny<TenantConfiguration>(), CancellationToken.None))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork
                .Setup(x => x.ConfigurationAuditLogs.AddAsync(It.IsAny<ConfigurationAuditLog>(), CancellationToken.None))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork
                .Setup(x => x.SaveChangesAsync(CancellationToken.None))
                .Callback(() => saveChangesCallCount++)
                .ReturnsAsync(100);

            // Act
            await _seeder.SeedDefaultConfigurationAsync(tenantId);

            // Assert
            Assert.Equal(1, saveChangesCallCount);
            _mockUnitOfWork.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Once);
        }
    }
}
