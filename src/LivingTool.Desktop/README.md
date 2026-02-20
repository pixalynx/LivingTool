# LivingTool Desktop

Desktop interface for running `LivingTool.Console` commands through a Tauri app with a React + TypeScript + TailwindCSS frontend.

## Features

- Run `run`, `read`, and `npc` commands from `LivingTool.Console`
- See command line preview, stdout/stderr, and exit code in-app
- Drag-and-drop files into the window to quickly populate command arguments
- Optional working directory override per command execution

## Development

From the repository root:

```bash
cd src/LivingTool.Desktop
npm install
npm run tauri dev
```

This app executes:

```bash
dotnet run --project src/LivingTool.Console/LivingTool.Console.csproj -- <command and args>
```

By default, commands run with the repository root as the working directory. You can override this in the UI.

## Build

```bash
cd src/LivingTool.Desktop
npm run tauri build
```
