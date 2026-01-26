# Publishing C64UltimateClient to NuGet

A reusable .NET client library for the Commodore 64 Ultimate REST API.

**Current Version:** 1.0.0  
**Target Frameworks:** net8.0, net10.0  
**License:** MIT License

## Prerequisites

1. **.NET SDK 8.0 or higher** installed
2. **A NuGet.org account** created at https://www.nuget.org/users/account/LogOn
3. **NuGet API key** from https://www.nuget.org/account/apikeys

## Package Information

The C64UltimateClient package provides:

- **40+ async methods** for Commodore 64 Ultimate control
- **Zero external dependencies** (except Logging.Abstractions)
- **Full type safety** with nullable reference types
- **Structured logging support** via ILogger<Ultimate64Client>
- **Comprehensive error handling** with UltimateClientException
- **Complete API coverage** from official 1541U documentation

## Building the Package

### Option 1: Using dotnet CLI

```bash
cd C64UltimateClient
dotnet pack -c Release
```

This will create a `.nupkg` file in `C64UltimateClient/bin/Release/`

### Option 2: Using Visual Studio

1. Right-click the C64UltimateClient project
2. Select "Pack"
3. Navigate to `bin/Release/` to find the .nupkg file

## Publishing to NuGet

### Using dotnet CLI

```bash
# Set your API key (one-time)
dotnet nuget add source https://api.nuget.org/v3/index.json --name nuget.org

# Push the package
dotnet nuget push C64UltimateClient/bin/Release/C64UltimateClient.1.0.0.nupkg \
  --api-key YOUR_NUGET_API_KEY \
  --source https://api.nuget.org/v3/index.json
```

### Using NuGet.exe

```bash
nuget push C64UltimateClient/bin/Release/C64UltimateClient.1.0.0.nupkg \
  YOUR_NUGET_API_KEY \
  -Source https://api.nuget.org/v3/index.json
```

## Installation by Users

Once published, users can install the package using:

```bash
dotnet add package C64UltimateClient
```

Or in Package Manager Console:

```powershell
Install-Package C64UltimateClient
```

## Versioning

Update the version in `C64UltimateClient.csproj`:

```xml
<Version>1.1.0</Version>
```

Then increment when publishing new versions.

## Package Contents

The published NuGet package includes:
- Compiled assemblies for .NET 8.0 and .NET 10.0
- XML documentation comments
- README.md
- LICENSE file

## Verification

After publishing, verify the package on NuGet.org:
https://www.nuget.org/packages/C64UltimateClient

It typically takes a few minutes for the package to appear and be searchable.

## Local Testing

To test locally before publishing:

```bash
dotnet add package C64UltimateClient --source ./C64UltimateClient/bin/Release/
```

Or add to local NuGet source:

```bash
dotnet nuget add source C:/path/to/C64UltimateClient/bin/Release/ --name local
```
