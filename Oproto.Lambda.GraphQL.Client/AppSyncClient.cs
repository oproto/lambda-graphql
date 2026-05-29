using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Amazon.Runtime;
using Amazon.Runtime.Credentials;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Oproto.Lambda.GraphQL.Client.Serialization;

namespace Oproto.Lambda.GraphQL.Client;

public sealed class AppSyncClient : IDisposable
{
    private const string ServiceName = "appsync";

    private readonly AppSyncClientOptions _options;
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    private readonly ILogger<AppSyncClient> _logger;
    private readonly string _region;

    public AppSyncClient(AppSyncClientOptions options)
        : this(options, null)
    {
    }

    public AppSyncClient(AppSyncClientOptions options, ILogger<AppSyncClient>? logger)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.Endpoint))
            throw new ArgumentException("Endpoint must not be null or empty.", nameof(options));

        _options = options;
        _logger = logger ?? NullLogger<AppSyncClient>.Instance;
        _region = options.Region
                  ?? Environment.GetEnvironmentVariable("AWS_REGION")
                  ?? Environment.GetEnvironmentVariable("AWS_DEFAULT_REGION")
                  ?? "us-east-1";

        if (options.HttpClient is not null)
        {
            _httpClient = options.HttpClient;
            _ownsHttpClient = false;
        }
        else
        {
            _httpClient = new HttpClient();
            _ownsHttpClient = true;
        }
    }

    /// <summary>
    /// Applies authentication headers to the request based on the configured auth mode.
    /// Returns a failed AppSyncResponse if authentication cannot be applied, or null on success.
    /// </summary>
    internal AppSyncResponse<TResult>? ApplyAuthentication<TResult>(HttpRequestMessage request, byte[] bodyBytes)
    {
        return _options.AuthMode switch
        {
            AuthMode.Iam => ApplyIamAuthentication<TResult>(request, bodyBytes),
            AuthMode.ApiKey => ApplyApiKeyAuthentication<TResult>(request),
            _ => new AppSyncResponse<TResult>
            {
                Exception = new InvalidOperationException($"Unsupported auth mode: {_options.AuthMode}")
            }
        };
    }

    private AppSyncResponse<TResult>? ApplyApiKeyAuthentication<TResult>(HttpRequestMessage request)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return new AppSyncResponse<TResult>
            {
                Exception = new InvalidOperationException(
                    "ApiKey must be provided when AuthMode is ApiKey.")
            };
        }

        request.Headers.TryAddWithoutValidation("x-api-key", _options.ApiKey);
        return null;
    }

    private AppSyncResponse<TResult>? ApplyIamAuthentication<TResult>(HttpRequestMessage request, byte[] bodyBytes)
    {
        ImmutableCredentials credentials;
        try
        {
            var awsCredentials = DefaultAWSCredentialsIdentityResolver.GetCredentials();
            credentials = awsCredentials.GetCredentials();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve AWS credentials for SigV4 signing");
            return new AppSyncResponse<TResult>
            {
                Exception = new InvalidOperationException(
                    "Unable to resolve AWS credentials for IAM authentication. " +
                    "Ensure credentials are available via environment variables, IAM role, or instance profile.",
                    ex)
            };
        }

        try
        {
            SignRequest(request, bodyBytes, credentials);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sign request with SigV4");
            return new AppSyncResponse<TResult>
            {
                Exception = new InvalidOperationException("Failed to sign request with SigV4.", ex)
            };
        }
    }

    /// <summary>
    /// Signs an HttpRequestMessage using AWS SigV4 with the appsync service name.
    /// </summary>
    internal void SignRequest(HttpRequestMessage request, byte[] bodyBytes, ImmutableCredentials credentials)
    {
        var uri = request.RequestUri!;
        var signedAt = DateTime.UtcNow;
        var dateStamp = signedAt.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var amzDate = signedAt.ToString("yyyyMMddTHHmmssZ", CultureInfo.InvariantCulture);

        // Build the host header value
        var host = uri.Host;
        if (!uri.IsDefaultPort)
            host += ":" + uri.Port;

        // Compute payload hash
        var payloadHash = HexEncode(SHA256.HashData(bodyBytes));

        // Build canonical headers - must include content-type, host, x-amz-date
        // and optionally x-amz-security-token, sorted by lowercase header name
        var canonicalHeaders = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["content-type"] = "application/json",
            ["host"] = host,
            ["x-amz-date"] = amzDate
        };

        if (!string.IsNullOrEmpty(credentials.Token))
        {
            canonicalHeaders["x-amz-security-token"] = credentials.Token;
        }

        var signedHeadersList = string.Join(";", canonicalHeaders.Keys);

        var canonicalHeadersString = new StringBuilder();
        foreach (var header in canonicalHeaders)
        {
            canonicalHeadersString.Append(header.Key);
            canonicalHeadersString.Append(':');
            canonicalHeadersString.Append(header.Value.Trim());
            canonicalHeadersString.Append('\n');
        }

        // Build canonical request
        var canonicalRequest = string.Join("\n",
            "POST",
            uri.AbsolutePath,
            "", // no query string
            canonicalHeadersString.ToString(),
            signedHeadersList,
            payloadHash);

        // Build string to sign
        var scope = $"{dateStamp}/{_region}/{ServiceName}/aws4_request";
        var stringToSign = string.Join("\n",
            "AWS4-HMAC-SHA256",
            amzDate,
            scope,
            HexEncode(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalRequest))));

        // Derive signing key
        var signingKey = DeriveSigningKey(credentials.SecretKey, dateStamp, _region, ServiceName);

        // Compute signature
        var signature = HexEncode(HMACSHA256.HashData(signingKey, Encoding.UTF8.GetBytes(stringToSign)));

        // Build authorization header
        var authorization = $"AWS4-HMAC-SHA256 Credential={credentials.AccessKey}/{scope}, SignedHeaders={signedHeadersList}, Signature={signature}";

        // Apply headers to the request
        request.Headers.TryAddWithoutValidation("Authorization", authorization);
        request.Headers.TryAddWithoutValidation("X-Amz-Date", amzDate);

        if (!string.IsNullOrEmpty(credentials.Token))
        {
            request.Headers.TryAddWithoutValidation("X-Amz-Security-Token", credentials.Token);
        }
    }

    private static byte[] DeriveSigningKey(string secretKey, string dateStamp, string region, string service)
    {
        var kDate = HMACSHA256.HashData(Encoding.UTF8.GetBytes("AWS4" + secretKey), Encoding.UTF8.GetBytes(dateStamp));
        var kRegion = HMACSHA256.HashData(kDate, Encoding.UTF8.GetBytes(region));
        var kService = HMACSHA256.HashData(kRegion, Encoding.UTF8.GetBytes(service));
        return HMACSHA256.HashData(kService, Encoding.UTF8.GetBytes("aws4_request"));
    }

    private static string HexEncode(byte[] data)
    {
#if NET9_0_OR_GREATER
        return Convert.ToHexStringLower(data);
#else
        return Convert.ToHexString(data).ToLowerInvariant();
#endif
    }

    /// <summary>
    /// Sends a GraphQL operation to the configured AppSync endpoint.
    /// </summary>
    public async Task<AppSyncResponse<TResult>> SendAsync<TResult>(
        string query,
        object? variables = null,
        CancellationToken cancellationToken = default)
    {
        // Build request body
        var requestBody = new GraphQLRequestBody
        {
            Query = query,
            Variables = variables
        };

        // Serialize — variables use caller context if provided, else default camelCase
        byte[] bodyBytes;
        try
        {
            bodyBytes = SerializeRequestBody(requestBody);
        }
        catch (Exception ex)
        {
            return new AppSyncResponse<TResult> { Exception = ex };
        }

        // Build HTTP request
        var request = new HttpRequestMessage(HttpMethod.Post, _options.Endpoint)
        {
            Content = new ByteArrayContent(bodyBytes)
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        // Apply authentication
        var authError = ApplyAuthentication<TResult>(request, bodyBytes);
        if (authError is not null)
            return authError;

        // Execute with retry
        HttpResponseMessage response;
        try
        {
            response = await ExecuteWithRetryAsync(
                () => _httpClient.SendAsync(request, cancellationToken),
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
        {
            return new AppSyncResponse<TResult> { Exception = ex };
        }
        catch (Exception ex)
        {
            return new AppSyncResponse<TResult> { Exception = ex };
        }

        // Read response body
        var statusCode = (int)response.StatusCode;
        string rawBody;
        try
        {
            rawBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new AppSyncResponse<TResult> { StatusCode = statusCode, Exception = ex };
        }

        // Non-2xx — return error response
        if (statusCode < 200 || statusCode >= 300)
        {
            return new AppSyncResponse<TResult>
            {
                StatusCode = statusCode,
                RawBody = rawBody
            };
        }

        // Deserialize response envelope
        return DeserializeResponse<TResult>(rawBody, statusCode);
    }

    /// <summary>
    /// Sends a GraphQL mutation. Convenience wrapper around <see cref="SendAsync{TResult}"/>.
    /// </summary>
    public Task<AppSyncResponse<TResult>> MutateAsync<TResult>(
        string mutation,
        object? variables = null,
        CancellationToken cancellationToken = default)
        => SendAsync<TResult>(mutation, variables, cancellationToken);

    /// <summary>
    /// Sends a GraphQL query. Convenience wrapper around <see cref="SendAsync{TResult}"/>.
    /// </summary>
    public Task<AppSyncResponse<TResult>> QueryAsync<TResult>(
        string query,
        object? variables = null,
        CancellationToken cancellationToken = default)
        => SendAsync<TResult>(query, variables, cancellationToken);

    private byte[] SerializeRequestBody(GraphQLRequestBody requestBody)
    {
        // If variables are present and caller supplied a JsonSerializerContext,
        // serialize variables separately with the caller's context, then embed as raw JSON.
        if (requestBody.Variables is not null && _options.JsonSerializerContext is not null)
        {
            var variablesJson = JsonSerializer.Serialize(
                requestBody.Variables,
                requestBody.Variables.GetType(),
                _options.JsonSerializerContext);

            // Build the JSON manually: {"query":"...","variables":<raw>}
            var queryJson = JsonSerializer.Serialize(requestBody.Query);
            return Encoding.UTF8.GetBytes($"{{\"query\":{queryJson},\"variables\":{variablesJson}}}");
        }

        if (requestBody.Variables is not null)
        {
            // No caller context — use default camelCase serialization for variables
            var variablesJson = JsonSerializer.Serialize(
                requestBody.Variables,
                requestBody.Variables.GetType(),
                DefaultSerializerOptions);

            var queryJson = JsonSerializer.Serialize(requestBody.Query);

            if (requestBody.Variables is null)
                return Encoding.UTF8.GetBytes($"{{\"query\":{queryJson}}}");

            return Encoding.UTF8.GetBytes($"{{\"query\":{queryJson},\"variables\":{variablesJson}}}");
        }

        // No variables — use the internal source-generated context
        return JsonSerializer.SerializeToUtf8Bytes(
            requestBody,
            AppSyncClientJsonContext.Default.GraphQLRequestBody);
    }

    private AppSyncResponse<TResult> DeserializeResponse<TResult>(string rawBody, int statusCode)
    {
        AppSyncResponseEnvelope envelope;
        try
        {
            envelope = JsonSerializer.Deserialize(
                rawBody,
                AppSyncClientJsonContext.Default.AppSyncResponseEnvelope)!;
        }
        catch (JsonException)
        {
            return new AppSyncResponse<TResult>
            {
                StatusCode = statusCode,
                RawBody = rawBody
            };
        }

        var result = new AppSyncResponse<TResult>
        {
            StatusCode = statusCode,
            Errors = envelope.Errors
        };

        // Deserialize the data field if present
        if (envelope.Data is { ValueKind: not JsonValueKind.Null and not JsonValueKind.Undefined } dataElement)
        {
            try
            {
                if (_options.JsonSerializerContext is not null)
                {
                    result.Data = (TResult?)JsonSerializer.Deserialize(
                        dataElement.GetRawText(),
                        typeof(TResult),
                        _options.JsonSerializerContext);
                }
                else
                {
                    result.Data = JsonSerializer.Deserialize<TResult>(
                        dataElement.GetRawText(),
                        DefaultSerializerOptions);
                }
            }
            catch (JsonException)
            {
                result.RawBody = rawBody;
            }
        }

        return result;
    }

    private static readonly JsonSerializerOptions DefaultSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }

    /// <summary>
    /// Calculates the delay for a retry attempt using exponential backoff with ±50% jitter.
    /// Base delay: 100ms × 2^attempt.
    /// </summary>
    internal static TimeSpan CalculateDelay(int attempt)
    {
        var baseDelay = TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt));
        var jitter = Random.Shared.NextDouble() * baseDelay.TotalMilliseconds;
        return baseDelay + TimeSpan.FromMilliseconds(jitter - baseDelay.TotalMilliseconds / 2);
    }

    /// <summary>
    /// Executes an HTTP operation with retry logic for transient failures.
    /// Retries on HTTP 5xx, TaskCanceledException (timeout), and HttpRequestException.
    /// Does not retry on HTTP 4xx or when the CancellationToken is cancelled.
    /// </summary>
    internal async Task<HttpResponseMessage> ExecuteWithRetryAsync(
        Func<Task<HttpResponseMessage>> operation,
        CancellationToken cancellationToken)
    {
        var maxAttempts = _options.MaxRetries + 1; // first attempt + retries
        HttpResponseMessage? lastResponse = null;
        Exception? lastException = null;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                lastResponse?.Dispose();
                lastResponse = null;

                var response = await operation().ConfigureAwait(false);
                var statusCode = (int)response.StatusCode;

                // 2xx or 4xx — not retryable
                if (statusCode < 500)
                    return response;

                // 5xx — retryable
                lastResponse = response;
                lastException = null;

                if (attempt < maxAttempts - 1)
                {
                    var delay = CalculateDelay(attempt);
                    _logger.LogWarning(
                        "Request failed with HTTP {StatusCode}. Retrying attempt {Attempt}/{MaxRetries} after {DelayMs}ms",
                        statusCode, attempt + 1, _options.MaxRetries, (int)delay.TotalMilliseconds);
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                // Request timeout (not caller cancellation) — retryable
                lastException = ex;

                if (attempt < maxAttempts - 1)
                {
                    var delay = CalculateDelay(attempt);
                    _logger.LogWarning(
                        ex,
                        "Request timed out. Retrying attempt {Attempt}/{MaxRetries} after {DelayMs}ms",
                        attempt + 1, _options.MaxRetries, (int)delay.TotalMilliseconds);
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (HttpRequestException ex)
            {
                // Network error — retryable
                lastException = ex;

                if (attempt < maxAttempts - 1)
                {
                    var delay = CalculateDelay(attempt);
                    _logger.LogWarning(
                        ex,
                        "Request failed with network error: {Message}. Retrying attempt {Attempt}/{MaxRetries} after {DelayMs}ms",
                        ex.Message, attempt + 1, _options.MaxRetries, (int)delay.TotalMilliseconds);
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        // All retries exhausted — return last response or throw last exception
        if (lastResponse is not null)
            return lastResponse;

        throw lastException!;
    }

}
