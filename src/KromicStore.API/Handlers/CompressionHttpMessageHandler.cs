using System.Net.Http.Headers;

namespace KromicStore.API.Handlers;

/// <summary>
/// HTTP message handler that configures automatic request/response compression
/// Reduces bandwidth usage for external service communications
/// </summary>
public class CompressionHttpMessageHandler : DelegatingHandler
{
    private const int MinCompressionSizeBytes = 1024;  // Only compress if > 1KB

    public CompressionHttpMessageHandler()
    {
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        // Add compression support headers to request
        // Indicates to the server that we support gzip and deflate compression
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip", 1.0));
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate", 0.9));
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("br", 0.8));  // Brotli if supported

        // Send the request
        var response = await base.SendAsync(request, cancellationToken);

        // Handle response decompression automatically (handled by HttpClient by default)
        return response;
    }
}
