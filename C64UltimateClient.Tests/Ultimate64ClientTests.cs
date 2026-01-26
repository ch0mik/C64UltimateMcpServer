using C64UltimateClient;
using System;
using System.Collections.Generic;
using Xunit;

namespace C64UltimateClient.Tests;

/// <summary>
/// Basic integration tests for Ultimate64Client
/// These tests verify the client structure and basic functionality.
/// Note: Real tests would require a running Ultimate device or mock server.
/// </summary>
public class Ultimate64ClientTests
{
    private const string TestBaseUrl = "http://localhost:8080";

    [Fact]
    public void Constructor_ShouldCreateClientWithBaseUrl()
    {
        // Arrange & Act
        using var client = new Ultimate64Client(TestBaseUrl);

        // Assert - Client should be created without throwing
        Assert.NotNull(client);
    }

    [Fact]
    public void Constructor_ShouldCreateClientWithTimeout()
    {
        // Arrange
        var timeout = TimeSpan.FromSeconds(15);

        // Act
        using var client = new Ultimate64Client(TestBaseUrl, timeout: timeout);

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Arrange
        var client = new Ultimate64Client(TestBaseUrl);

        // Act & Assert
        client.Dispose();
        // Should not throw when disposing
    }

    [Fact]
    public void UltimateClientException_WithMessage_ShouldHaveMessage()
    {
        // Arrange & Act
        var exception = new UltimateClientException("Test error message");

        // Assert
        Assert.Equal("Test error message", exception.Message);
        Assert.Null(exception.ApiErrors);
    }

    [Fact]
    public void UltimateClientException_WithApiErrors_ShouldContainErrors()
    {
        // Arrange
        var errors = new List<string> { "Error 1", "Error 2" };

        // Act
        var exception = new UltimateClientException("Test error", errors);

        // Assert
        Assert.Equal("Test error", exception.Message);
        Assert.NotNull(exception.ApiErrors);
        Assert.Equal(2, exception.ApiErrors.Count);
        Assert.Contains("Error 1", exception.ApiErrors);
    }

    [Fact]
    public void UltimateClientException_WithInnerException_ShouldHaveInnerException()
    {
        // Arrange
        var innerEx = new InvalidOperationException("Inner error");

        // Act
        var exception = new UltimateClientException("Outer error", null, innerEx);

        // Assert
        Assert.Equal("Outer error", exception.Message);
        Assert.NotNull(exception.InnerException);
        Assert.Equal("Inner error", exception.InnerException.Message);
    }
}
