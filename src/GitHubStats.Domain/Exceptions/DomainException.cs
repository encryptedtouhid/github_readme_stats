namespace GitHubStats.Domain.Exceptions;

/// <summary>
/// Base exception for domain-specific errors.
/// </summary>
public class DomainException : Exception
{
    public string ErrorCode { get; }

    public DomainException(string message, string errorCode = "DOMAIN_ERROR")
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public DomainException(string message, string errorCode, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}

/// <summary>
/// Exception thrown when a required parameter is missing.
/// </summary>
public class MissingParameterException : DomainException
{
    public IReadOnlyList<string> MissingParameters { get; }

    public MissingParameterException(IReadOnlyList<string> parameters)
        : base($"Missing required parameter(s): {string.Join(", ", parameters)}", "MISSING_PARAM")
    {
        MissingParameters = parameters;
    }
}

/// <summary>
/// Exception thrown when a user is not found.
/// </summary>
public class UserNotFoundException : DomainException
{
    public string Username { get; }

    public UserNotFoundException(string username)
        : base($"User '{username}' not found", "USER_NOT_FOUND")
    {
        Username = username;
    }
}

/// <summary>
/// Exception thrown when a repository is not found.
/// </summary>
public class RepositoryNotFoundException : DomainException
{
    public string Username { get; }
    public string Repository { get; }

    public RepositoryNotFoundException(string username, string repository)
        : base($"Repository '{username}/{repository}' not found", "REPO_NOT_FOUND")
    {
        Username = username;
        Repository = repository;
    }
}

/// <summary>
/// Exception thrown when a gist is not found.
/// </summary>
public class GistNotFoundException : DomainException
{
    public string GistId { get; }

    public GistNotFoundException(string gistId)
        : base($"Gist '{gistId}' not found", "GIST_NOT_FOUND")
    {
        GistId = gistId;
    }
}

/// <summary>
/// Exception thrown when rate limited by GitHub API.
/// </summary>
public class RateLimitException : DomainException
{
    public RateLimitException()
        : base("GitHub API rate limit exceeded. Please try again later.", "RATE_LIMITED")
    {
    }
}

/// <summary>
/// Exception thrown when access is denied (blacklisted or not whitelisted).
/// </summary>
public class AccessDeniedException : DomainException
{
    public AccessDeniedException(string reason)
        : base(reason, "ACCESS_DENIED")
    {
    }
}
