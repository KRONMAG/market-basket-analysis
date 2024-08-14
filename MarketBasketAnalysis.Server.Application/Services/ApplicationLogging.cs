using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using InvalidOperationException = System.InvalidOperationException;

namespace MarketBasketAnalysis.Server.Application.Services;

public static class ApplicationLogging
{
    public static ILoggerFactory? LoggerFactory { get; set; }

    public static ILogger CreateLogger<T>()
    {
        ThrowIfLoggerFactoryIsNull();

        return LoggerFactory.CreateLogger<T>();
    }

    public static ILogger CreateLogger(string categoryName)
    {
        ThrowIfLoggerFactoryIsNull();

        return LoggerFactory.CreateLogger(categoryName);
    }

    [MemberNotNull(nameof(LoggerFactory))]
    private static void ThrowIfLoggerFactoryIsNull()
    {
        if (LoggerFactory == null)
            throw new InvalidOperationException("Failed to create logger: logger factory is not initialized.");
    }
}
