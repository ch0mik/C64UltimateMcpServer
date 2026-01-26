# C64UltimateClient - Commodore 64 Ultimate REST API Client

A comprehensive, production-ready .NET client library for the **Commodore 64 Ultimate** REST API (1541U). Clean, async-first design with full API coverage and enterprise-grade error handling.

**License:** MIT License

## Features

✅ **Complete API Coverage** - All 40+ endpoints from 1541U documentation  
✅ **Async/Await Support** - Modern async/await with proper cancellation token support  
✅ **Type-Safe** - Full C# type safety with nullable reference types enabled  
✅ **Structured Logging** - Optional ILogger<Ultimate64Client> integration  
✅ **Error Handling** - UltimateClientException with API error details  
✅ **Multiple .NET Versions** - Targets net8.0 and net10.0  
✅ **Zero Dependencies** - Only uses System.* and Microsoft.Extensions.Logging.Abstractions  
✅ **NuGet Ready** - Fully packaged for public distribution  

## Installation

```bash
dotnet add package C64UltimateClient
```

## Quick Start

```csharp
using C64UltimateClient;
using Microsoft.Extensions.Logging;

// Create client (with optional logger)
var client = new Ultimate64Client(
    baseUrl: "http://192.168.0.120",
    logger: null, // or provide ILogger<Ultimate64Client>
    timeout: TimeSpan.FromSeconds(30)
);

// Get version
var version = await client.GetVersionAsync();
Console.WriteLine($"API Version: {version.Version}");
foreach (var error in version.Errors ?? [])
{
    Console.WriteLine($"Error: {error}");
}

// Get drive information
var drives = await client.GetDrivesInfoAsync();

// Mount a disk image
await client.MountDiskAsync("a", "/path/to/image.d64");

// Play a SID file
await client.PlaySidAsync("/Music/song.sid", songNumber: 1);

// Run a program
await client.RunProgramAsync("/Games/game.prg");

// Write to memory (address, hex data string)
await client.WriteMemoryAsync("D020", "05 04");

// Read from memory (address, length)
var memory = await client.ReadMemoryAsync("D000", 256);

// Error handling
try
{
    await client.PlaySidAsync("/Music/invalid.sid");
}
catch (UltimateClientException ex)
{
    Console.WriteLine($"API Error: {ex.Message}");
    if (ex.ApiErrors?.Count > 0)
    {
        foreach (var error in ex.ApiErrors)
            Console.WriteLine($"  - {error}");
    }
}
```

## API Categories

### About
- `GetVersionAsync()` - Get API version

### Machine Control
- `ResetMachineAsync()` - Soft reset
- `RebootMachineAsync()` - Full reboot
- `PauseMachineAsync()` - Pause CPU
- `ResumeMachineAsync()` - Resume CPU
- `PowerOffMachineAsync()` - Power off (U64 only)

### Memory Operations
- `ReadMemoryAsync(address, length)` - Read from C64 memory via DMA
- `WriteMemoryAsync(address, data)` - Write to C64 memory via DMA
- `GetDebugRegisterAsync()` - Read debug register (U64 only)
- `SetDebugRegisterAsync(value)` - Write debug register (U64 only)

### Audio Playback
- `PlaySidAsync(file, songNumber)` - Play SID file
- `PlaySidBinaryAsync(data, songLengths?)` - Play SID from binary
- `PlayModAsync(file)` - Play MOD file
- `PlayModBinaryAsync(data)` - Play MOD from binary

### Program & Cartridge Execution
- `LoadProgramAsync(file)` - Load PRG into memory
- `RunProgramAsync(file)` - Load and run PRG
- `RunProgramBinaryAsync(data)` - Run PRG from binary
- `RunCartridgeAsync(file)` - Run CRT cartridge

### Configuration Management
- `GetConfigCategoriesAsync()` - List all config categories
- `GetConfigCategoryAsync(category)` - Get category items
- `GetConfigItemAsync(category, item)` - Get specific item value
- `SetConfigItemAsync(category, item, value)` - Set configuration
- `BulkConfigUpdateAsync(config)` - Update multiple settings
- `SaveConfigAsync()` - Save to flash
- `LoadConfigAsync()` - Load from flash
- `ResetConfigToDefaultAsync()` - Factory reset

### Drive Management
- `GetDrivesInfoAsync()` - Get all drive information
- `MountDiskAsync(drive, file, type?, mode?)` - Mount disk image
- `MountDiskBinaryAsync(drive, data, type?, mode?)` - Mount disk from binary
- `UnmountDiskAsync(drive)` - Unmount disk
- `ResetDriveAsync(drive)` - Reset drive
- `SetDriveModeAsync(drive, mode)` - Change drive type (1541/1571/1581)
- `EnableDriveAsync(drive)` - Turn drive on
- `DisableDriveAsync(drive)` - Turn drive off
- `LoadDriveRomAsync(drive, file)` - Load custom drive ROM
- `LoadDriveRomBinaryAsync(drive, data)` - Load ROM from binary

### Data Streams (U64 only)
- `StartStreamAsync(stream, ipAddress, port?)` - Start video/audio/debug stream
- `StopStreamAsync(stream)` - Stop stream

### File Operations
- `CreateD64Async(path, tracks?, diskName?)` - Create D64 disk image
- `CreateD71Async(path, diskName?)` - Create D71 disk image
- `CreateD81Async(path, diskName?)` - Create D81 disk image
- `CreateDnpAsync(path, tracks, diskName?)` - Create DNP disk image
- `GetFileInfoAsync(path)` - Get file information

## Error Handling

```csharp
try
{
    await client.PlaySidAsync("/Music/song.sid");
}
catch (UltimateClientException ex)
{
    Console.WriteLine($"API Error: {ex.Message}");
    if (ex.ApiErrors?.Any() == true)
    {
        foreach (var error in ex.ApiErrors)
            Console.WriteLine($"  - {error}");
    }
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"Connection Error: {ex.Message}");
}
```

## Configuration

```csharp
var client = new Ultimate64Client(
    baseUrl: "http://192.168.0.120",
    timeout: TimeSpan.FromSeconds(30)
);
```

## Logging

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var services = new ServiceCollection();
services.AddLogging(config => config.AddConsole());

var client = new Ultimate64Client(
    baseUrl: "http://192.168.0.120",
    services.BuildServiceProvider().GetRequiredService<ILogger<Ultimate64Client>>()
);
```

## License

MIT License - See LICENSE file
