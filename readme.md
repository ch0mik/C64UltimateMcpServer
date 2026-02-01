# C64 Ultimate MCP Server

**English** | [**Polski**](#-c64-ultimate-mcp-server-polski)

> ⚙️ **This project was built 100% using AI Agent prompts** - Every line of code, configuration, and documentation was generated through conversational prompts to GitHub Copilot Agent.

Comprehensive **Model Context Protocol (MCP)** server for the **Commodore 64 Ultimate** device with **45+ tools** (45 device operations) and **23 embedded documentation resources**. Built in C# with enterprise-grade architecture, type safety, and comprehensive API coverage.

**License:** MIT License (see [LICENSE](LICENSE) file)

## 🎥 Demo Video

[![C64 Ultimate MCP Server Demo](https://img.youtube.com/vi/1q80quIIkV0/0.jpg)](https://www.youtube.com/watch?v=1q80quIIkV0?cc_load_policy=1&cc_lang_pref=en)

## ⚡ Key Features

- **Complete Ultimate 64 API Support**: All 45+ tools from the 1541U REST API
- **BASIC to PRG Compilation**: Real-time compilation of BASIC V2 source code to executable PRG files
- **MCP Resources**: 23 embedded documentation resources (BASIC specs, Assembly guides, Kernal API, memory maps, graphics, sound, I/O, drive & printer specs)
- **Separated Client Library**: Reusable `C64UltimateClient` NuGet package with clean async API
- **Clean Architecture**: Client → Service → MCP wrapper pattern with proper DI
- **HTTP/SSE Transport**: Remote multi-client support with session management
- **Streaming Support**: Video, audio, and debug streaming capabilities
- **Binary File Support**: Upload PRG files via base64, file path, or URL
- **ASP.NET Core 8.0/10.0**: Modern, high-performance web framework
- **Enterprise Configuration**: appsettings.json with environment overrides
- **Type Safety**: C# static typing with full null-safety and nullable reference types
- **Structured Logging**: Separate loggers for client and service layers
- **Production-Ready**: Zero errors/warnings, comprehensive error handling
- **Agent Integration**: Works with Continue, Copilot, Cody, Cursor, Claude Desktop

## 🚀 Quick Start

```bash
# 1. Build
dotnet build

# 2. Configure (optional, defaults to http://192.168.0.120)
export Ultimate__BaseUrl=http://your-c64-ultimate-ip:port

# 3. Run
dotnet run

# 4. Server starts on http://localhost:8080
```

## 📦 Installation

### Requirements
- **.NET 10.0 SDK** - [Download](https://dotnet.microsoft.com/download)
- **C64 Ultimate device** - On your network
- **Curl or HTTP client** - For testing
- **Docker** (optional) - [Download](https://www.docker.com/products/docker-desktop)

### Setup

```bash
git clone https://github.com/ch0mik/C64UltimateMcpServer.git
cd C64UltimateMcpServer
dotnet build
dotnet run
```

## ⚙️ Configuration

### appsettings.json
```json
{
  "Ultimate": {
    "BaseUrl": "http://192.168.0.120"
  }
}
```

### Environment Variable
```bash
Ultimate__BaseUrl=http://192.168.0.120
```

### Docker
```yaml
environment:
  - Ultimate__BaseUrl=http://192.168.0.120
```

## 🤖 Agent Integration

### Continue IDE (Recommended)

1. **Install Continue** - https://continue.dev
2. **Configure** `.continue/config.json`:
```json
{
  "models": [
    {
      "title": "Claude",
      "provider": "openai",
      "model": "claude-3-5-sonnet"
    }
  ],
  "mcp_servers": {
    "c64-ultimate": {
      "url": "http://localhost:8080"
    }
  }
}
```

3. **Use in Chat**:
```
@C64 reboot the device
@C64 load and run /games/loderunner.prg
@C64 what config categories are available?
```

### VS Code Chat (GitHub Copilot)

1. **Install** - GitHub Copilot Chat extension
2. **Configure** `.vscode/settings.json`:
```json
{
  "github.copilot.chat.mcpServers": [
    {
      "name": "C64 Ultimate",
      "type": "streamable-http",
      "url": "http://localhost:8080"
    }
  ]
}
```

3. **Use**:
```
@C64 reboot device
@C64 mount disk /games/disk1.d64
```

### Cody (Sourcegraph)

```bash
cody mcp add c64-ultimate http://localhost:8080
```

Then in Cody:
```
Ask Cody: Use C64 Ultimate MCP to reboot the device
Ask Cody: List all available config categories
```

### Cursor IDE

1. **Configure** `.cursor/mcp.json`:
```json
{
  "servers": [
    {
      "name": "C64 Ultimate",
      "type": "http",
      "url": "http://localhost:8080"
    }
  ]
}
```

2. **Use slash commands**:
```
/c64 reboot device
/c64 load program
```

## 🛠️ Tools (45 Total)

### Machine Control (5)
- **ultimate_reset_machine** - Soft reset C64 (preserves configuration)
- **ultimate_reboot_device** - Full device restart (reinitializes cartridge)
- **ultimate_pause_machine** - Pause machine execution
- **ultimate_resume_machine** - Resume machine execution
- **ultimate_power_off** - Power off C64

### Program Management (6)
- **ultimate_generate_basic_prg** - Generate PRG from BASIC source code with token conversion
- **ultimate_load_program** - Load PRG program from filesystem
- **ultimate_run_program** - Load and execute PRG from disk
- **ultimate_run_prg_binary** - Execute program from binary data, URL or file
- **ultimate_run_cartridge** - Load and run cartridge
- **ultimate_menu_button** - Simulate menu button press

### Memory Management (5)
- **ultimate_read_memory** - Read C64 RAM memory contents
- **ultimate_write_memory** - Write to RAM memory
- **ultimate_write_memory_binary** - Write binary data to memory
- **ultimate_get_debug_register** - Read debug register
- **ultimate_set_debug_register** - Write to debug register

### Audio Playback (5)
- **ultimate_play_sid** - Play SID file with optional song number
- **ultimate_play_sid_binary** - Play SID from binary data, URL or file
- **ultimate_play_mod** - Play Amiga MOD file
- **ultimate_start_stream** - Start audio/video/debug stream
- **ultimate_stop_stream** - Stop active stream

### Drive Management (7)
- **ultimate_get_drives** - Get information about all disk drives
- **ultimate_mount_disk** - Mount disk image from filesystem or binary data
- **ultimate_unmount_disk** - Unmount disk image
- **ultimate_reset_drive** - Reset selected disk drive
- **ultimate_set_drive_mode** - Change drive operating mode
- **ultimate_turn_drive_on** - Power on disk drive
- **ultimate_turn_drive_off** - Power off disk drive

### Disk Creation (4)
- **ultimate_create_d64** - Create 1541 disk image (D64)
- **ultimate_create_d71** - Create 1571 disk image (D71)
- **ultimate_create_d81** - Create 1581 disk image (D81)
- **ultimate_create_dnp** - Create Native Partition disk image (DNP)

### ROM Management (1)
- **ultimate_load_drive_rom** - Load custom ROM to disk drive

### File Management (1)
- **ultimate_get_file_info** - Get file information

### Configuration Management (6)
- **ultimate_get_config_categories** - Get list of available configuration categories
- **ultimate_get_config_category** - Get all settings in a category
- **ultimate_get_config_item** - Get single configuration value
- **ultimate_set_config_item** - Set configuration value
- **ultimate_bulk_config_update** - Update multiple settings at once
- **ultimate_load_config** - Load configuration from flash

Note: `ultimate_save_config` and `ultimate_reset_config` are also available for configuration management

### Connection Management (2)
- **ultimate_get_connection** - Get current connection settings
- **ultimate_set_connection** - Change device hostname/port

### System Information (1)
- **ultimate_version** - Get C64 Ultimate API version


## �️ Summary (45 Total Tools)

- **Machine Control**: 5 tools
- **Program Management**: 6 tools
- **Memory Management**: 5 tools
- **Audio Playback**: 5 tools
- **Drive Management**: 7 tools
- **Disk Creation**: 4 tools
- **ROM Management**: 1 tool
- **File Management**: 1 tool
- **Configuration Management**: 6 tools
- **Connection Management**: 2 tools
- **System Information**: 1 tool

## �📚 Resources (23 Total)

Embedded documentation accessible via MCP Inspector at `http://localhost:8000`:

### BASIC Resources (7)
- **c64://basic/spec** - BASIC V2 Language Specification
- **c64://basic/pitfalls** - BASIC Pitfalls and Common Gotchas
- **c64://basic/examples/hello-world** - Hello World Example
- **c64://basic/examples/joystick** - Joystick Input Example
- **c64://basic/examples/bounce** - Bounce Animation Example
- **c64://basic/examples/wave** - Wave Animation Example
- **c64://basic/api** - BASIC Callable API Reference

### Assembly Resources (1)
- **c64://assembly/spec** - 6510 Assembly Language Specification

### Memory Resources (3)
- **c64://memory/map** - C64 Memory Map (0x0000-0xFFFF)
- **c64://memory/kernal** - Kernal Memory Map (ROM $E000-$FFFF)
- **c64://memory/low** - Low Memory Map (0x0000-0x03FF)

### Graphics Resources (4)
- **c64://graphics/vic** - VIC-II Chip Specification
- **c64://graphics/charset** - Character Set Reference (ASCII/PETSCII)
- **c64://graphics/color-palette** - C64 16-Color Palette Reference
- **c64://graphics/style-guide** - Graphics Programming Style Guide

### Sound Resources (4)
- **c64://sound/sid** - SID Chip (6581/8580) Specification
- **c64://sound/programming** - SID Programming Guide
- **c64://sound/file-format** - SID File Format Documentation
- **c64://sound/effects** - Sound Effects Programming Tips

### I/O Resources (2)
- **c64://io/cia** - CIA Chip Specification
- **c64://io/spec** - I/O Port Specification

### Drive Resources (1)
- **c64://drive/spec** - 1541/1571/1581 Disk Drive Specification

### Printer Resources (6)
- **c64://printer/commodore-spec** - Commodore Printer Specification
- **c64://printer/epson-spec** - Epson Printer Specification
- **c64://printer/commodore-bitmap** - Commodore Printer Bitmap Font
- **c64://printer/epson-bitmap** - Epson Printer Bitmap Font
- **c64://printer/graphics-mode** - Graphics Mode Programming Guide
- **c64://printer/troubleshooting** - Printer Troubleshooting Guide

### API Resources (2)
- **c64://api/basic** - BASIC V2 Callable API Reference
- **c64://api/kernal** - Kernal Callable API Reference (23 routines)

**Access Resources via MCP Inspector:**
1. Open http://localhost:8000 (MCP Inspector)
2. Click "Resources" tab
3. Select any resource to view its full content
4. Copy code examples and specifications directly

## 📝 Examples

### HTTP/cURL

All HTTP examples are available in `examples_http/` folder. Key examples:

**Reboot Device** - See [mcp_reboot_device.http](examples_http/mcp_reboot_device.http):
```bash
curl -X POST http://localhost:8080/ \
  -H "Content-Type: application/json" \
  -H "Accept: application/json, text/event-stream" \
  -d '{
    "jsonrpc": "2.0",
    "id": 1,
    "method": "tools/call",
    "params": {
      "name": "ultimate_reboot_device",
      "arguments": {}
    }
  }'
```

**Mount Disk** - See [mcp_mount_disk.http](examples_http/mcp_mount_disk.http)

**Run PRG Binary** - See [TEST_RUN_PRG_BINARY.http](examples_http/TEST_RUN_PRG_BINARY.http)

**Play SID File** - See [TEST_PLAY_SID_BINARY.http](examples_http/TEST_PLAY_SID_BINARY.http)

All 45+ tools have corresponding `.http` files in `examples_http/` for testing

### Continue IDE

```
@C64 Create a BASIC program that prints "HELLO WORLD" and run it
@C64 Mount the disk image games.d64 on drive 8
@C64 Reset the C64 and show machine info
```

**BASIC Program Example** - See [mcp_generate_basic_prg.http](examples_http/mcp_generate_basic_prg.http) to compile BASIC source code directly to executable PRG files and create your own BASIC PRG binaries.

## 🧪 Testing

See `examples_http/` folder for complete test examples:

**Test Basic Connection**:
```bash
# HTTP files: test_basic_connection.http
curl http://localhost:8080/health
# Response: "C64 Ultimate MCP Server is running"
```

**List Tools** - See [mcp_list_tools.http](examples_http/mcp_list_tools.http)

**Get Version** - See [mcp_get_version.http](examples_http/mcp_get_version.http)

**Test SID Binary Upload** - See [TEST_PLAY_SID_BINARY.http](examples_http/TEST_PLAY_SID_BINARY.http)

**Test PRG Binary Upload** - See [TEST_RUN_PRG_BINARY.http](examples_http/TEST_RUN_PRG_BINARY.http)

All files in `examples_http/mcp_*.http` correspond to available tools.

## 🐳 Docker Deployment

### About MCP Inspector
The **MCP Inspector** is a web-based tool for testing and debugging MCP servers. It provides:
- Interactive tool exploration
- Real-time parameter testing
- Request/response inspection
- WebSocket monitoring

It automatically connects to the MCP server and displays all available tools with detailed information.

### Option 1: Docker Compose with Build
```bash
docker compose up --build
```
Builds the image locally and starts both MCP server and inspector.

**Access:**
- **MCP Server**: http://localhost:8080 - Main MCP Server endpoint
- **MCP Inspector**: http://localhost:8000 - Web UI for testing tools

**Services:**
- `mcp-server` - C64 Ultimate MCP Server (handles all tool requests)
- `inspector` - MCP Inspector UI (connects to mcp-server at http://mcp-server:8080)

### Option 2: Docker Compose Simple
```bash
docker compose up
```
Starts pre-built containers (same as Option 1 but without rebuilding image).

**Access:**
- **MCP Server**: http://localhost:8080
- **MCP Inspector**: http://localhost:8000

### Option 3: Docker Run (Pre-built Image)
```bash
docker run -p 8080:8080 \
  -e ASPNETCORE_URLS=http://+:8080 \
  -e Ultimate__BaseUrl=http://192.168.0.120 \
  -d sq7mru/c64ultimatemcpserver:latest
```

**Access:**
- **MCP Server**: http://localhost:8080

**Note:** This option runs only the MCP Server. To also run MCP Inspector, either use Option 1 or 2, or run a separate inspector container.

### Custom docker-compose.yml
```yaml
services:
  mcp-server:
    image: sq7mru/c64ultimatemcpserver:latest
    ports:
      - "8080:8080"
    healthcheck:
      test: ["CMD", "sh", "-c", "ps x| grep dotnet | grep C64UltimateMcpServer"]
      interval: 10s
      timeout: 5s
      retries: 10
      start_period: 10s
    environment:
      - Ultimate__BaseUrl=http://192.168.0.120
      - ASPNETCORE_URLS=http://+:8080
      - ASPNETCORE_ENVIRONMENT=Production
    networks:
      - c64-network

  mcp-inspector:
    image: mcpuse/inspector:latest
    container_name: inspector
    command:
      - /bin/sh
      - -c
      - |
        npx @mcp-use/inspector \
          --port 8000 \
        --url http://mcp-server:8080 
    ports:
      - "8000:8000"
    depends_on:
      mcp-server:
          condition: service_healthy
    networks:
      - c64-network

networks:
  c64-network:
    driver: bridge
```

### Environment Variables
- `Ultimate__BaseUrl` - C64 Ultimate device URL (default: http://192.168.0.120)
- `ASPNETCORE_URLS` - Server binding (default: http://+:8080)
- `ASPNETCORE_ENVIRONMENT` - Environment mode (Production/Development)

## 📚 Architecture

```
┌─────────────────────────────────────────────┐
│         AI Agents                           │
│  (Continue/Copilot/Cody/Cursor)             │
└──────────────────┬──────────────────────────┘
                   │ HTTP/SSE
┌──────────────────▼──────────────────────────┐
│      MCP Server (localhost:8080)            │
│  - HTTP Transport Layer                     │
│  - Session Management                       │
│  - Tool Routing                             │
└──────────────────┬──────────────────────────┘
                   │ REST API
┌──────────────────▼──────────────────────────┐
│   C64 Ultimate Device                       │
│   (192.168.0.120:80)                        │
│  - V1 REST API                              │
│  - SID/MOD Playback                         │
│  - Drive Control                            │
│  - Memory Access                            │
└─────────────────────────────────────────────┘
```

## ✅ Status: Production Ready

- ✅ All 53+ tools implemented (45 device operations + 8 learning guides)
- ✅ 23 embedded documentation resources
- ✅ BASIC to PRG on-the-fly compilation
- ✅ HTTP/SSE transport
- ✅ Agent integration (Continue, Copilot, Cody, Cursor)
- ✅ Docker containerization with MCP Inspector
- ✅ Zero errors/warnings
- ✅ Comprehensive documentation
- ✅ Configuration management
- ✅ Session handling

## 📦 External Resources

**External Resources and Learning Materials:**  
The embedded MCP Resources and C64 Learning Guide tools are based on documentation from the **[c64bridge](https://github.com/chrisgleissner/c64bridge/)** project by Chris Gleissner.

**23 Embedded External Resources** include:
- BASIC V2 Specification and Examples
- 6510 Assembly Language Guide
- C64 Memory Maps
- VIC-II, SID, CIA Specifications
- Kernal API Reference (23 callable routines)
- And more...

These external resources have been adapted and integrated as MCP Resources and Learning Guide tools for this server.

## 🔗 Links

- **C64 Ultimate**: https://ultimate64.com/
- **1541u API**: https://github.com/GideonZ/1541u-documentation
- **c64bridge**: https://github.com/chrisgleissner/c64bridge/
- **RetroC64**: https://retroc64.github.io/
- **C64U (cybersorcerer)**: https://github.com/cybersorcerer/c64u/
- **C64U MCP Server**: https://github.com/xphileby/c64u-mcp-server
- **Ultimate64 MCP**: https://github.com/Martijn-DevRev/Ultimate64MCP
- **MCP Spec**: https://modelcontextprotocol.io/
- **Continue IDE**: https://continue.dev
- **GitHub Copilot**: https://github.com/features/copilot
- **Cody**: https://cody.dev
- **Cursor**: https://cursor.sh

## 📄 License

MIT

---

# 🇵🇱 C64 Ultimate MCP Server (Polski)

> ⚙️ **Ten projekt został stworzony 100% za pomocą promptów do Agenta AI** - Każda linia kodu, konfiguracji i dokumentacji została wygenerowana poprzez konwersacyjne prompty do GitHub Copilot Agent.

Kompleksowy serwer **Model Context Protocol (MCP)** dla urządzenia **Commodore 64 Ultimate** z **45+ narzędziami** (45 operacji device) i **23 zasobami dokumentacyjnymi**. Zbudowany w C# z architekturą klasy enterprise, bezpieczeństwem typów i kompleksowym pokryciem API.

## 🎥 Wideo Demo

[![C64 Ultimate MCP Server Demo](https://img.youtube.com/vi/1q80quIIkV0/0.jpg)](https://www.youtube.com/watch?v=1q80quIIkV0?cc_load_policy=1&cc_lang_pref=pl)

## 📋 Spis Treści

- **.NET 10.0 SDK** - [Pobierz](https://dotnet.microsoft.com/download)
- **Urządzenie C64 Ultimate** - W sieci lokalnej
- **cURL lub klient HTTP** - Do testowania
- **Docker** (opcjonalnie) - [Pobierz](https://www.docker.com/products/docker-desktop)

## Instalacja (PL)

```bash
# 1. Klonuj repozytorium
git clone https://github.com/ch0mik/C64UltimateMcpServer.git
cd C64UltimateMcpServer

# 2. Zbuduj projekt
dotnet build

# 3. Uruchom serwer
dotnet run

# Serwer dostępny na http://localhost:8080
```

### Docker

```bash
docker-compose up
# Serwer: http://localhost:8080
# Inspektor: http://localhost:6274
```

## Konfiguracja (PL)

### appsettings.json
```json
{
  "Ultimate": {
    "BaseUrl": "http://192.168.0.120"
  }
}
```

### Zmienna środowiska
```bash
Ultimate__BaseUrl=http://192.168.0.120
```

## 🛠️ Narzędzia (45 razem)

### Kontrola Maszyny (5)
- **ultimate_reset_machine** - Miękki reset C64 (zachowuje konfigurację)
- **ultimate_reboot_device** - Pełny restart urządzenia (reinicjalizacja kartridża)
- **ultimate_pause_machine** - Wstrzymanie pracy maszyny
- **ultimate_resume_machine** - Wznowienie pracy maszyny
- **ultimate_power_off** - Wyłączenie C64

### Zarządzanie Programami (6)
- **ultimate_generate_basic_prg** - Generowanie PRG z kodu BASIC z konwersją do tokenów
- **ultimate_load_program** - Ładowanie programu PRG z systemu plików
- **ultimate_run_program** - Uruchomienie programu PRG z dysku
- **ultimate_run_prg_binary** - Uruchomienie programu z danych binarnych, URL lub pliku
- **ultimate_run_cartridge** - Uruchomienie kartridża
- **ultimate_menu_button** - Symulacja naciśnięcia przycisku menu

### Zarządzanie Pamięcią (5)
- **ultimate_read_memory** - Odczyt zawartości pamięci RAM
- **ultimate_write_memory** - Zapis do pamięci RAM
- **ultimate_write_memory_binary** - Zapis danych binarnych do pamięci
- **ultimate_get_debug_register** - Odczyt rejestru debugowania
- **ultimate_set_debug_register** - Zapis do rejestru debugowania

### Odtwarzanie Audio (5)
- **ultimate_play_sid** - Odtwarzanie pliku SID z wskazanym numerem piosenki
- **ultimate_play_sid_binary** - Odtwarzanie SID z danych binarnych, URL lub pliku
- **ultimate_play_mod** - Odtwarzanie pliku MOD
- **ultimate_start_stream** - Uruchomienie strumienia audio/wideo/debug
- **ultimate_stop_stream** - Zatrzymanie aktywnego strumienia

### Zarządzanie Stacjami Dysków (7)
- **ultimate_get_drives** - Pobranie informacji o wszystkich stacjach dysków
- **ultimate_mount_disk** - Zamontowanie obrazu dysku z systemu plików lub danych binarnych
- **ultimate_unmount_disk** - Odmontowanie obrazu dysku
- **ultimate_reset_drive** - Reset wybranej stacji dysków
- **ultimate_set_drive_mode** - Zmiana trybu pracy stacji
- **ultimate_turn_drive_on** - Włączenie stacji dysków
- **ultimate_turn_drive_off** - Wyłączenie stacji dysków

### Tworzenie Dysków (4)
- **ultimate_create_d64** - Tworzenie obrazu dysku 1541 (D64)
- **ultimate_create_d71** - Tworzenie obrazu dysku 1571 (D71)
- **ultimate_create_d81** - Tworzenie obrazu dysku 1581 (D81)
- **ultimate_create_dnp** - Tworzenie obrazu dysku Native Partition (DNP)

### Zarządzanie ROM-ami (1)
- **ultimate_load_drive_rom** - Załadowanie niestandardowego ROM-u do stacji dysków

### Zarządzanie Plikami (1)
- **ultimate_get_file_info** - Pobranie informacji o pliku

### Zarządzanie Konfiguracją (6)
- **ultimate_get_config_categories** - Pobranie listy dostępnych kategorii konfiguracyjnych
- **ultimate_get_config_category** - Pobranie ustawień całej kategorii
- **ultimate_get_config_item** - Pobranie wartości pojedynczego ustawienia
- **ultimate_set_config_item** - Zmiana wartości ustawienia
- **ultimate_bulk_config_update** - Masowa zmiana wielu ustawień jednocześnie
- **ultimate_load_config** - Załadowanie konfiguracji

Uwaga: `ultimate_save_config` i `ultimate_reset_config` są również dostępne do zarządzania konfiguracją

### Zarządzanie Połączeniem (2)
- **ultimate_get_connection** - Pobranie aktualnych ustawień połączenia
- **ultimate_set_connection** - Zmiana hosta/portu do nawiązania połączenia

### Informacje Systemowe (1)
- **ultimate_version** - Pobranie wersji C64 Ultimate

## 📚 Zasoby (23 razem)

Wbudowana dokumentacja dostępna w MCP Inspector pod adresem `http://localhost:8000`:

### Zasoby BASIC (7)
- **c64://basic/spec** - Specyfikacja języka BASIC V2
- **c64://basic/pitfalls** - Typowe błędy i pułapki w BASIC
- **c64://basic/examples/hello-world** - Przykład Hello World
- **c64://basic/examples/joystick** - Przykład obsługi joysticka
- **c64://basic/examples/bounce** - Przykład animacji odbijającej się piłki
- **c64://basic/examples/wave** - Przykład animacji fali
- **c64://basic/api** - Referencja API BASIC

### Zasoby Asemblera (1)
- **c64://assembly/spec** - Specyfikacja języka asemblera 6510

### Zasoby Pamięci (3)
- **c64://memory/map** - Mapa pamięci C64 (0x0000-0xFFFF)
- **c64://memory/kernal** - Mapa pamięci Kernal (ROM $E000-$FFFF)
- **c64://memory/low** - Mapa pamięci niskiej (0x0000-0x03FF)

### Zasoby Grafiki (4)
- **c64://graphics/vic** - Specyfikacja układu VIC-II
- **c64://graphics/charset** - Referencja zestawu znaków (ASCII/PETSCII)
- **c64://graphics/color-palette** - Paleta 16 kolorów C64
- **c64://graphics/style-guide** - Przewodnik stylu programowania grafiki

### Zasoby Dźwięku (4)
- **c64://sound/sid** - Specyfikacja układu SID (6581/8580)
- **c64://sound/programming** - Przewodnik programowania SID
- **c64://sound/file-format** - Dokumentacja formatu pliku SID
- **c64://sound/effects** - Wskazówki do programowania efektów dźwiękowych

### Zasoby I/O (2)
- **c64://io/cia** - Specyfikacja układu CIA
- **c64://io/spec** - Specyfikacja portów I/O

### Zasoby Stacji Dysków (1)
- **c64://drive/spec** - Specyfikacja stacji dysków 1541/1571/1581

### Zasoby Drukarki (6)
- **c64://printer/commodore-spec** - Specyfikacja drukarki Commodore
- **c64://printer/epson-spec** - Specyfikacja drukarki Epson
- **c64://printer/commodore-bitmap** - Czcionka bitmapowa drukarki Commodore
- **c64://printer/epson-bitmap** - Czcionka bitmapowa drukarki Epson
- **c64://printer/graphics-mode** - Przewodnik trybu graficznego
- **c64://printer/troubleshooting** - Rozwiązywanie problemów z drukarką

### Zasoby API (2)
- **c64://api/basic** - Referencja API BASIC V2
- **c64://api/kernal** - Referencja Kernal API (23 procedury)

**Dostęp do zasobów przez MCP Inspector:**
1. Otwórz http://localhost:8000 (MCP Inspector)
2. Kliknij zakładkę "Resources"
3. Wybierz dowolny zasób, aby zobaczyć jego pełną zawartość
4. Skopiuj przykłady kodu i specyfikacje bezpośrednio

## 🤖 Agenci AI (PL)

### Continue IDE (Polecane)

1. **Instalacja** - https://continue.dev
2. **Konfiguracja** `.continue/config.json`:
```json
{
  "models": [
    {
      "title": "Claude",
      "provider": "openai",
      "model": "claude-3-5-sonnet"
    }
  ],
  "mcp_servers": {
    "c64-ultimate": {
      "url": "http://localhost:8080"
    }
  }
}
```

3. **Użycie w Chacie**:
```
@C64 zrestartuj urządzenie
@C64 załaduj i uruchom /games/loderunner.prg
@C64 jakie kategorie konfiguracji są dostępne?
```

### VS Code Chat (GitHub Copilot)

1. **Instalacja** - Rozszerzenie GitHub Copilot Chat
2. **Konfiguracja** `.vscode/settings.json`:
```json
{
  "github.copilot.chat.mcpServers": [
    {
      "name": "C64 Ultimate",
      "type": "streamable-http",
      "url": "http://localhost:8080"
    }
  ]
}
```

3. **Polecenia**:
```
@C64 zrestartuj maszynę
@C64 montuj dysk /games/disk1.d64
@C64 pokaż informacje o urządzeniu
```

### Cody (Sourcegraph)

```bash
cody mcp add c64-ultimate http://localhost:8080
```

W Cody:
```
Ask Cody: Use C64 Ultimate to reboot the device
Ask Cody: Pokaz dostepne kategorie konfiguracji
```

### Cursor IDE

1. **Konfiguracja** `.cursor/mcp.json`:
```json
{
  "servers": [
    {
      "name": "C64 Ultimate",
      "type": "http",
      "url": "http://localhost:8080"
    }
  ]
}
```

2. **Polecenia slash**:
```
/c64 zrestartuj urządzenie
/c64 załaduj program
```

## Przykłady (PL)

### cURL

Wszystkie przykłady HTTP dostępne w folderze `examples_http/`. Kluczowe przykłady:

**Zrestartuj urządzenie** - Zobacz [mcp_reboot_device.http](examples_http/mcp_reboot_device.http):
```bash
curl -X POST http://localhost:8080/ \
  -H "Content-Type: application/json" \
  -H "Accept: application/json, text/event-stream" \
  -d '{
    "jsonrpc": "2.0",
    "id": 1,
    "method": "tools/call",
    "params": {
      "name": "ultimate_reboot_device",
      "arguments": {}
    }
  }'
```

**Montuj dysk** - Zobacz [mcp_mount_disk.http](examples_http/mcp_mount_disk.http)

**Uruchom program PRG** - Zobacz [TEST_RUN_PRG_BINARY.http](examples_http/TEST_RUN_PRG_BINARY.http)

**Odtwarzaj muzykę SID** - Zobacz [TEST_PLAY_SID_BINARY.http](examples_http/TEST_PLAY_SID_BINARY.http)

Wszystkie 45+ narzędzi mają odpowiadające im pliki `.http` w folderze `examples_http/` do testowania

### Continue IDE

```
@C64 Create a BASIC program that prints "HELLO WORLD" and run it
@C64 Montuj obraz dysku games.d64 na napęd 8
@C64 Zrestartuj C64 i pokaż informacje o maszynie
```

**Przykład programu BASIC** - Zobacz [mcp_generate_basic_prg.http](examples_http/mcp_generate_basic_prg.http) - zawiera Hello World ze wskazówkami jak tworzyć własne binarne programy BASIC (.prg).

## 🧪 Testowanie (PL)

Wszystkie przykłady testowe znajdują się w folderze `examples_http/`:

**Testuj Połączenie**:
```bash
# Pliki HTTP: test_basic_connection.http
curl http://localhost:8080/health
# Odpowiedź: "C64 Ultimate MCP Server is running"
```

**Lista Narzędzi** - Zobacz [mcp_list_tools.http](examples_http/mcp_list_tools.http)

**Wersja** - Zobacz [mcp_get_version.http](examples_http/mcp_get_version.http)

**Test Wgrywania SID** - Zobacz [TEST_PLAY_SID_BINARY.http](examples_http/TEST_PLAY_SID_BINARY.http)

**Test Wgrywania PRG** - Zobacz [TEST_RUN_PRG_BINARY.http](examples_http/TEST_RUN_PRG_BINARY.http)

Wszystkie pliki w `examples_http/mcp_*.http` odpowiadają dostępnym narzędziom.

## 🐳 Wdrażanie Docker (PL)

### O MCP Inspector
**MCP Inspector** to narzędzie webowe do testowania i debugowania serwerów MCP. Zapewnia:
- Interaktywne przeglądanie narzędzi
- Testowanie parametrów w czasie rzeczywistym
- Inspekcję żądań/odpowiedzi
- Monitorowanie WebSocket

Automatycznie łączy się z serwerem MCP i wyświetla wszystkie dostępne narzędzia z szczegółowymi informacjami.

### Opcja 1: Docker Compose z Budowaniem
```bash
docker compose up --build
```
Buduje obraz lokalnie i uruchamia zarówno serwer MCP jak i inspector.

**Dostęp:**
- **Serwer MCP**: http://localhost:8080 - Główny punkt końcowy MCP Server
- **MCP Inspector**: http://localhost:8000 - Interfejs webowy do testowania narzędzi

**Usługi:**
- `mcp-server` - Serwer C64 Ultimate MCP (obsługuje wszystkie żądania narzędzi)
- `inspector` - Interfejs MCP Inspector (łączy się z mcp-server pod adresem http://mcp-server:8080)

### Opcja 2: Docker Compose Prosta
```bash
docker compose up
```
Uruchamia wstępnie zbudowane kontenery (to samo co Opcja 1, ale bez przebudowywania obrazu).

**Dostęp:**
- **Serwer MCP**: http://localhost:8080
- **MCP Inspector**: http://localhost:8000

### Opcja 3: Docker Run (Wstępnie Zbudowany Obraz)
```bash
docker run -p 8080:8080 \
  -e ASPNETCORE_URLS=http://+:8080 \
  -e Ultimate__BaseUrl=http://192.168.0.120 \
  -d sq7mru/c64ultimatemcpserver:latest
```

**Dostęp:**
- **Serwer MCP**: http://localhost:8080

**Uwaga:** Ta opcja uruchamia tylko Serwer MCP. Aby uruchomić również MCP Inspector, użyj Opcji 1 lub 2, lub uruchom oddzielny kontener inspectora.

### Niestandardowy docker-compose.yml
```yaml
services:
  mcp-server:
    image: sq7mru/c64ultimatemcpserver:latest
    ports:
      - "8080:8080"
    healthcheck:
      test: ["CMD", "sh", "-c", "ps x| grep dotnet | grep C64UltimateMcpServer"]
      interval: 10s
      timeout: 5s
      retries: 10
      start_period: 10s
    environment:
      - Ultimate__BaseUrl=http://192.168.0.120
      - ASPNETCORE_URLS=http://+:8080
      - ASPNETCORE_ENVIRONMENT=Production
    networks:
      - c64-network

  mcp-inspector:
    image: mcpuse/inspector:latest
    container_name: inspector
    command:
      - /bin/sh
      - -c
      - |
        npx @mcp-use/inspector \
          --port 8000 \
        --url http://mcp-server:8080 
    ports:
      - "8000:8000"
    depends_on:
      mcp-server:
          condition: service_healthy
    networks:
      - c64-network

networks:
  c64-network:
    driver: bridge
```

### Zmienne Środowiska
- `Ultimate__BaseUrl` - URL urządzenia C64 Ultimate (domyślnie: http://192.168.0.120)
- `ASPNETCORE_URLS` - Wiązanie serwera (domyślnie: http://+:8080)
- `ASPNETCORE_ENVIRONMENT` - Tryb środowiska (Production/Development)

## 📚 Architektura (PL)

```
┌─────────────────────────────────────────────┐
│         Agenci AI                           │
│  (Continue/Copilot/Cody/Cursor)             │
└──────────────────┬──────────────────────────┘
                   │ HTTP/SSE
┌──────────────────▼──────────────────────────┐
│      Serwer MCP (localhost:8080)            │
│  - Warstwa Transportu HTTP                  │
│  - Zarządzanie Sesją                        │
│  - Routing Narzędzi                         │
└──────────────────┬──────────────────────────┘
                   │ REST API
┌──────────────────▼──────────────────────────┐
│   Urządzenie C64 Ultimate                   │
│   (192.168.0.120:80)                        │
│  - V1 REST API                              │
│  - Odtwarzanie SID/MOD                      │
│  - Kontrola Stacji Dysków                   │
│  - Dostęp do Pamięci                        │
└─────────────────────────────────────────────┘
```

## ✅ Status: Gotowy do Produkcji (PL)

- ✅ Wszystkie 53+ narzędzia zaimplementowane (45 operacji device + 8 przewodników)
- ✅ 23 wbudowane zasoby dokumentacyjne
- ✅ Kompilacja BASIC do PRG w locie
- ✅ Transport HTTP/SSE
- ✅ Integracja z agentami AI (Continue, Copilot, Cody, Cursor)
- ✅ Konteneryzacja Docker z MCP Inspector
- ✅ Brak błędów i ostrzeżeń
- ✅ Pełna dokumentacja
- ✅ Zarządzanie konfiguracją
- ✅ Obsługa sesji

## Rozwiązywanie Problemów (PL)

### Serwer nie odpowiada
```bash
# Sprawdzenie statusu
curl http://localhost:8080/health

# Restart
taskkill /F /IM dotnet.exe  # Windows
killall dotnet              # macOS/Linux
dotnet run
```

### Nie mogę połączyć się z C64 Ultimate
```bash
# Sprawdzenie dostępności
ping 192.168.0.120

# Aktualizacja konfiguracji
export Ultimate__BaseUrl=http://192.168.0.120
dotnet run
```

### Agent nie widzi narzędzi
```bash
# Sprawdzenie listy narzędzi
curl -X POST http://localhost:8080/ \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":1,"method":"tools/list"}'
```

## 🎉 Status: Gotowy do Produkcji

- ✅ Wszystkie 45+ narzędzia zaimplementowane
- ✅ Kompilacja BASIC do PRG w locie
- ✅ Transport HTTP/SSE
- ✅ Integracja z agentami AI
- ✅ Konteneryzacja Docker
- ✅ Brak błędów i ostrzeżeń
- ✅ Pełna dokumentacja
- ✅ Zarządzanie konfiguracją
- ✅ Obsługa sesji

## 📦 Zasoby Zewnętrzne

**Zasoby i Materiały Edukacyjne:**  
Wbudowane MCP Resources i narzędzia przewodników C64 są oparte na dokumentacji z projektu **[c64bridge](https://github.com/chrisgleissner/c64bridge/)** autorstwa Chrisa Gleissnera.

**23 Wbudowane Zasoby Zewnętrzne** zawierają:
- Specyfikację BASIC V2 i przykłady
- Przewodnik języka asemblera 6510
- Mapy pamięci C64
- Specyfikacje VIC-II, SID, CIA
- Referencję Kernal API (23 procedury do wywoływania)
- I wiele więcej...

Zasoby te zostały zaadaptowane i zintegrowane jako MCP Resources i narzędzia przewodników dla tego serwera.

## 🔗 Linki

- **C64 Ultimate**: https://ultimate64.com/
- **1541u API**: https://github.com/GideonZ/1541u-documentation
- **c64bridge**: https://github.com/chrisgleissner/c64bridge/
- **RetroC64**: https://retroc64.github.io/
- **C64U (cybersorcerer)**: https://github.com/cybersorcerer/c64u/
- **C64U MCP Server**: https://github.com/xphileby/c64u-mcp-server
- **Ultimate64 MCP**: https://github.com/Martijn-DevRev/Ultimate64MCP
- **MCP**: https://modelcontextprotocol.io/
- **Continue IDE**: https://continue.dev
- **GitHub Copilot**: https://github.com/features/copilot
- **Cody**: https://cody.dev
- **Cursor**: https://cursor.sh

## 📄 Licencja

MIT


