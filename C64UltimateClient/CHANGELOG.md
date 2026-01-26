# C64UltimateClient - Changelog

## Version 1.0.0 (2024)

### Features
- ✅ Complete API coverage for Commodore 64 Ultimate REST API
- ✅ Async/await support throughout
- ✅ Type-safe .NET client library
- ✅ Support for .NET 8.0 and .NET 10.0
- ✅ Comprehensive error handling with detailed exceptions
- ✅ Built-in ILogger support

### Endpoints Implemented

#### Machine Control
- `ResetMachineAsync()` - Soft reset with configuration preserved
- `RebootMachineAsync()` - Full reboot with cartridge re-initialization
- `PauseMachineAsync()` - Pause CPU (DMA line)
- `ResumeMachineAsync()` - Resume from paused state
- `PowerOffMachineAsync()` - Power off (U64 only)

#### Memory Operations
- `ReadMemoryAsync(address, length)` - DMA read from C64 memory
- `WriteMemoryAsync(address, data)` - DMA write to C64 memory (hex format)
- `WriteMemoryBinaryAsync(address, data)` - DMA write from binary (>128 bytes)
- `GetDebugRegisterAsync()` - Read debug register (U64 only)
- `SetDebugRegisterAsync(value)` - Write debug register (U64 only)

#### Audio Playback
- `PlaySidAsync(file, songNumber?)` - Play SID from filesystem
- `PlaySidBinaryAsync(data, songLengths?)` - Play SID from binary
- `PlayModAsync(file)` - Play MOD from filesystem
- `PlayModBinaryAsync(data)` - Play MOD from binary

#### Program & Cartridge Execution
- `LoadProgramAsync(file)` - Load PRG without running
- `RunProgramAsync(file)` - Load and run PRG
- `RunProgramBinaryAsync(data)` - Run PRG from binary
- `RunCartridgeAsync(file)` - Run cartridge
- `RunCartridgeBinaryAsync(data)` - Run cartridge from binary

#### Configuration Management
- `GetConfigCategoriesAsync()` - List all config categories
- `GetConfigCategoryAsync(category)` - Get category items
- `GetConfigItemAsync(category, item)` - Get specific value
- `SetConfigItemAsync(category, item, value)` - Set single item
- `BulkConfigUpdateAsync(config)` - Update multiple items
- `SaveConfigAsync()` - Save to flash
- `LoadConfigAsync()` - Load from flash
- `ResetConfigToDefaultAsync()` - Factory reset

#### Drive Management
- `GetDrivesInfoAsync()` - Get all drives information
- `MountDiskAsync(drive, file, type?, mode?)` - Mount from filesystem
- `MountDiskBinaryAsync(drive, data, type?, mode?)` - Mount from binary
- `UnmountDiskAsync(drive)` - Unmount disk
- `ResetDriveAsync(drive)` - Reset drive
- `SetDriveModeAsync(drive, mode)` - Change type (1541/1571/1581)
- `EnableDriveAsync(drive)` - Turn on
- `DisableDriveAsync(drive)` - Turn off
- `LoadDriveRomAsync(drive, file)` - Load custom ROM from filesystem
- `LoadDriveRomBinaryAsync(drive, data)` - Load custom ROM from binary

#### Data Streams (U64 only)
- `StartStreamAsync(stream, ipAddress, port?)` - Start video/audio/debug
- `StopStreamAsync(stream)` - Stop stream

#### File Operations
- `GetFileInfoAsync(path)` - Get file information
- `CreateD64Async(path, tracks?, diskName?)` - Create D64 (35/40 tracks)
- `CreateD71Async(path, diskName?)` - Create D71 (70 tracks)
- `CreateD81Async(path, diskName?)` - Create D81 (160 tracks)
- `CreateDnpAsync(path, tracks, diskName?)` - Create DNP (custom tracks)

### Dependencies
- System.Net.Http.Json (included in .NET SDK)
- No external dependencies!

### License
MIT License

### Documentation
See README.md for usage examples and quick start guide.
