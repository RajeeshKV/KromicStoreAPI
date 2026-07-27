// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.Application.Interfaces;

using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Service interface for domain verification operations.
/// </summary>
public interface IDomainVerificationService
{
    /// <summary>
    /// Verifies domain ownership via DNS TXT record lookup.
    /// </summary>
    Task<bool> VerifyDomainOwnershipAsync(string domain, string verificationToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a domain's DNS is properly configured to point to the platform.
    /// </summary>
    Task<bool> CheckDnsConfigurationAsync(string domain, CancellationToken cancellationToken = default);

    /// <summary>
    /// Initiates SSL certificate provisioning for a domain.
    /// </summary>
    Task<string> InitiateSslProvisioningAsync(string domain, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks SSL certificate status for a domain.
    /// </summary>
    Task<string> CheckSslStatusAsync(string domain, CancellationToken cancellationToken = default);
}
