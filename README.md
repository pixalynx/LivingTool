# LivingTool

A command-line tool for unpacking and analyzing Guardians Crusade game files. LivingTool helps you extract and explore data from the game binary files.

[![Publish LivingTool Release](https://github.com/pixalynx/LivingTool/actions/workflows/release.yml/badge.svg)](https://github.com/pixalynx/LivingTool/actions/workflows/release.yml)

## Features

- Extract game data from binary files
- Process LOC sectors and executable files
- Read and analyze game data structures

## Installation

### Pre-built Binaries

Download the latest release for your platform from the [Releases](https://github.com/pixalynx/LivingTool/releases) page:

- Windows: `livingtool-win-x64.zip`
- macOS (Intel): `livingtool-osx-x64.zip`
- macOS (Apple Silicon): `livingtool-osx-arm64.zip`
- Linux: `livingtool-linux-x64.zip`

Extract the zip file and run the executable.

### Build from Source

Requirements:

- .NET 9.0 SDK or later
- Node.js 18+ (for the desktop app frontend)
- Rust toolchain (for Tauri desktop builds)

```bash
# Clone the repository
git clone https://github.com/user/LivingTool.git
cd LivingTool

# Build the project
dotnet build

# Run the application
dotnet run --project src/LivingTool.Console

# Run the desktop app
cd src/LivingTool.Desktop
npm install
npm run tauri dev
```

## Usage

LivingTool supports the following commands:

### Run Command

Unpacks game binary files:

```bash
livingtool run --file gc.bin --output-directory output
```

Options:

- `-f, --file` - The file to unpack (default: gc.bin)
- `-o, --output-directory` - The output directory for unpacked files (default: output)
- `-l, --loc-sectors-file` - The LOC sectors file (default: locsectors.bin)
- `-e, --executable` - The executable file (default: SLUS_008.11)

### Read Command

Reads and displays information from game files:

```bash
livingtool read --file [filename]
```

### NPC Decode Command

Decodes an NPC BIN file and returns structured output (JSON by default):

```bash
livingtool npc --file output/NPC/NPC07.BIN
```

Optional output mode:

- `--format json` (default)
- `--format text`

## Project Structure

- `LivingTool.Console` - Command-line interface
- `LivingTool.Core` - Core functionality and services
- `LivingTool.Core.Tests` - Unit tests
- `LivingTool.Desktop` - Tauri desktop UI (React + TypeScript + TailwindCSS) that runs `LivingTool.Console` commands

## Documentation

- High-level ISO extraction flow: `docs/ISO_UNPACKING_HIGH_LEVEL.md`
- Simplified extraction explanation: `docs/ISO_UNPACKING_FOR_DUMMIES.md`
- NPC BIN parsing details: `docs/NPC_BIN_UNPACKING.md`

## Development

To contribute to LivingTool:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

- Guardians Crusade game and its developers
- .NET Core team
- [Spectre.Console](https://spectreconsole.net/) library for creating beautiful console applications
