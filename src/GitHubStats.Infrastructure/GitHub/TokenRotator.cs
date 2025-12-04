using System.Collections.Concurrent;
using GitHubStats.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace GitHubStats.Infrastructure.GitHub;

/// <summary>
/// Manages rotation of GitHub Personal Access Tokens for load distribution and rate limit handling.
/// Thread-safe implementation for high-concurrency scenarios.
/// </summary>
public sealed class TokenRotator
{
    private readonly string[] _tokens;
    private readonly ConcurrentDictionary<int, DateTime> _rateLimitedUntil = new();
    private int _currentIndex;
    private readonly object _lock = new();

    public TokenRotator(IOptions<GitHubOptions> options)
    {
        _tokens = options.Value.PersonalAccessTokens.ToArray();
        if (_tokens.Length == 0)
        {
            throw new InvalidOperationException("At least one GitHub Personal Access Token is required.");
        }
    }

    /// <summary>
    /// Gets the next available token, skipping rate-limited ones.
    /// </summary>
    public string GetNextToken()
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            var attempts = 0;

            while (attempts < _tokens.Length)
            {
                var index = _currentIndex;
                _currentIndex = (_currentIndex + 1) % _tokens.Length;
                attempts++;

                // Check if this token is rate limited
                if (_rateLimitedUntil.TryGetValue(index, out var limitedUntil) && now < limitedUntil)
                {
                    continue;
                }

                return _tokens[index];
            }

            // All tokens are rate limited, return first one anyway
            // The HTTP handler will handle the retry
            return _tokens[0];
        }
    }

    /// <summary>
    /// Marks a token as rate limited until the specified time.
    /// </summary>
    public void MarkRateLimited(string token, DateTime until)
    {
        var index = Array.IndexOf(_tokens, token);
        if (index >= 0)
        {
            _rateLimitedUntil[index] = until;
        }
    }

    /// <summary>
    /// Clears rate limit status for a token.
    /// </summary>
    public void ClearRateLimit(string token)
    {
        var index = Array.IndexOf(_tokens, token);
        if (index >= 0)
        {
            _rateLimitedUntil.TryRemove(index, out _);
        }
    }

    /// <summary>
    /// Gets the total number of available tokens.
    /// </summary>
    public int TokenCount => _tokens.Length;

    /// <summary>
    /// Gets the number of currently rate-limited tokens.
    /// </summary>
    public int RateLimitedCount
    {
        get
        {
            var now = DateTime.UtcNow;
            return _rateLimitedUntil.Count(kvp => kvp.Value > now);
        }
    }
}
