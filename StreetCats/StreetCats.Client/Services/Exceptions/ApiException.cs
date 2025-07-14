using System;

using System.Collections.Generic;

namespace StreetCats.Client.Services.Exceptions;

/// <summary>
/// Eccezione personalizzata per errori API
/// </summary>
public class ApiException : Exception
{
    public int StatusCode { get; }
    public List<string> Errors { get; }
    public bool IsRetryable { get; }

    public ApiException(string message, int statusCode = 0, List<string>? errors = null, bool isRetryable = false)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors ?? new List<string>();
        IsRetryable = isRetryable;
    }

    public ApiException(string message, Exception innerException, int statusCode = 0)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        Errors = new List<string>();
        IsRetryable = false;
    }
}