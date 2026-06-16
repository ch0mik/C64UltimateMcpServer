using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using C64UltimateMcpServer.Resources;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Xunit;

namespace C64UltimateClient.Tests;

public class C64ResourceCatalogTests
{
    [Fact]
    public void Catalog_ShouldContainUniqueUrisAndNonEmptyMetadata()
    {
        var uris = new HashSet<string>();

        foreach (var resource in C64ResourceCatalog.All)
        {
            Assert.True(uris.Add(resource.Uri), $"Duplicate URI: {resource.Uri}");
            Assert.False(string.IsNullOrWhiteSpace(resource.Name));
            Assert.False(string.IsNullOrWhiteSpace(resource.Description));
            Assert.False(string.IsNullOrWhiteSpace(resource.MimeType));
            Assert.NotEmpty(resource.RelativePathSegments);

            var filePath = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                "C64UltimateMcpServer",
                "Resources",
                "data",
                Path.Combine(resource.RelativePathSegments)));

            Assert.True(File.Exists(filePath), $"Missing resource file: {filePath}");

            if (resource.MimeType == "text/markdown")
            {
                var content = File.ReadAllText(filePath);
                Assert.StartsWith("---", content);
            }
        }
    }

    [Fact]
    public void Catalog_Index_ShouldMentionCoreUris()
    {
        var index = C64ResourceCatalog.BuildIndexMarkdown();

        Assert.Contains("c64://basic/spec", index);
        Assert.Contains("c64://resources/index", index);
        Assert.Contains("c64://sound/sid-spec", index);
    }

    [Fact]
    public void ResourceProvider_Methods_ShouldExposeNonEmptyDescriptions()
    {
        var resourceMethods = typeof(C64ResourceProvider)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => method.GetCustomAttribute<McpServerResourceAttribute>() is not null)
            .ToArray();

        Assert.NotEmpty(resourceMethods);

        foreach (var method in resourceMethods)
        {
            var description = method.GetCustomAttribute<DescriptionAttribute>();
            Assert.True(
                description is not null && !string.IsNullOrWhiteSpace(description.Description),
                $"Missing [Description] on resource method: {method.Name}");
        }
    }

    [Fact]
    public void ResourceProvider_UriTemplates_ShouldMatchCatalogEntries()
    {
        var resourceUris = typeof(C64ResourceProvider)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Select(method => method.GetCustomAttribute<McpServerResourceAttribute>())
            .Where(attribute => attribute is not null)
            .Select(attribute => attribute!.UriTemplate)
            .Where(uri => uri != "c64://resources/index" && uri != "c64://resources/{category}/{slug}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Equal(C64ResourceCatalog.All.Count, resourceUris.Count);

        foreach (var resource in C64ResourceCatalog.All)
        {
            Assert.Contains(resource.Uri, resourceUris);
        }
    }

    [Fact]
    public void ResourceProvider_TemplateLookup_ShouldRejectUnknownCatalogEntry()
    {
        var provider = new C64ResourceProvider(NullLogger<C64ResourceProvider>.Instance);
        var exception = Assert.Throws<McpProtocolException>(() => provider.GetCatalogResource("basic", "missing-slug"));

        Assert.Equal(McpErrorCode.InvalidParams, exception.ErrorCode);
        Assert.Contains("Unknown C64 resource", exception.Message);
    }
}
