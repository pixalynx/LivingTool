import { invoke } from "@tauri-apps/api/core";
import { getCurrentWindow } from "@tauri-apps/api/window";
import { useEffect, useMemo, useState } from "react";

type CommandKind = "run" | "read" | "npc";

type InvokeRequest = {
  command: CommandKind;
  args: string[];
  workingDirectory: string | null;
};

type InvokeResponse = {
  commandLine: string;
  stdout: string;
  stderr: string;
  combinedOutput: string;
  exitCode: number;
  success: boolean;
  projectPath: string;
  workingDirectory: string;
};

const descriptions: Record<CommandKind, string> = {
  run: "Unpack game assets using gc.bin, loc sectors, and output folder options.",
  read: "Read and print structure details for a supplied file.",
  npc: "Decode NPC BIN names and dialogues from a selected NPC file.",
};

const normalizePath = (value: string): string => value.trim().replace(/\\/g, "/");

const splitArgs = (value: string): string[] =>
  value
    .split("\n")
    .map((part) => part.trim())
    .filter((part) => part.length > 0);

const toParentDirectory = (path: string): string => {
  const normalized = normalizePath(path);
  const index = normalized.lastIndexOf("/");
  return index > 0 ? normalized.slice(0, index) : normalized;
};

const quoteArg = (value: string): string =>
  /[\s"']/u.test(value) ? `"${value.replace(/"/g, '\\"')}"` : value;

function App() {
  const [command, setCommand] = useState<CommandKind>("run");
  const [workingDirectory, setWorkingDirectory] = useState("");
  const [additionalArgs, setAdditionalArgs] = useState("");

  const [runFile, setRunFile] = useState("gc.bin");
  const [runOutputDir, setRunOutputDir] = useState("output");
  const [runLocSectors, setRunLocSectors] = useState("locsectors.bin");
  const [runExecutable, setRunExecutable] = useState("SLUS_008.11");

  const [readFile, setReadFile] = useState("");
  const [npcFile, setNpcFile] = useState("");

  const [dropMessage, setDropMessage] = useState("Drop files anywhere in this window.");
  const [droppedFiles, setDroppedFiles] = useState<string[]>([]);

  const [isRunning, setIsRunning] = useState(false);
  const [result, setResult] = useState<InvokeResponse | null>(null);
  const [invocationError, setInvocationError] = useState("");

  useEffect(() => {
    let unlisten: (() => void) | undefined;

    void getCurrentWindow()
      .onDragDropEvent((event) => {
        if (event.payload.type === "drop") {
          const normalized = event.payload.paths.map(normalizePath);
          setDroppedFiles((current) => {
            const merged = [...normalized, ...current];
            return Array.from(new Set(merged)).slice(0, 64);
          });
          setDropMessage(`Captured ${normalized.length} dropped path(s).`);
        } else if (event.payload.type === "leave") {
          setDropMessage("Drop files anywhere in this window.");
        }
      })
      .then((dispose) => {
        unlisten = dispose;
      })
      .catch((error: unknown) => {
        setDropMessage(`File drop listener failed: ${String(error)}`);
      });

    return () => {
      unlisten?.();
    };
  }, []);

  const commandArgs = useMemo(() => {
    const args: string[] = [];

    if (command === "run") {
      if (runFile.trim()) {
        args.push("-f", normalizePath(runFile));
      }
      if (runOutputDir.trim()) {
        args.push("-o", normalizePath(runOutputDir));
      }
      if (runLocSectors.trim()) {
        args.push("-l", normalizePath(runLocSectors));
      }
      if (runExecutable.trim()) {
        args.push("-e", normalizePath(runExecutable));
      }
    }

    if (command === "read" && readFile.trim()) {
      args.push("-f", normalizePath(readFile));
    }

    if (command === "npc" && npcFile.trim()) {
      args.push("-f", normalizePath(npcFile));
    }

    args.push(...splitArgs(additionalArgs));

    return args;
  }, [additionalArgs, command, npcFile, readFile, runExecutable, runFile, runLocSectors, runOutputDir]);

  const commandPreview = useMemo(() => {
    const base = [
      "dotnet",
      "run",
      "--project",
      "src/LivingTool.Console/LivingTool.Console.csproj",
      "--",
      command,
      ...commandArgs,
    ];
    return base.map(quoteArg).join(" ");
  }, [command, commandArgs]);

  const validationError = useMemo(() => {
    if (command === "read" && !readFile.trim()) {
      return "read requires a file path.";
    }

    if (command === "npc" && !npcFile.trim()) {
      return "npc requires an NPC BIN file path.";
    }

    return "";
  }, [command, npcFile, readFile]);

  const applyDroppedPath = (path: string, target: "main" | "loc" | "exe" | "output" | "read" | "npc" | "work") => {
    if (target === "main") {
      setRunFile(path);
      return;
    }
    if (target === "loc") {
      setRunLocSectors(path);
      return;
    }
    if (target === "exe") {
      setRunExecutable(path);
      return;
    }
    if (target === "output") {
      setRunOutputDir(toParentDirectory(path));
      return;
    }
    if (target === "read") {
      setReadFile(path);
      return;
    }
    if (target === "npc") {
      setNpcFile(path);
      return;
    }
    setWorkingDirectory(toParentDirectory(path));
  };

  const removeDroppedPath = (path: string) => {
    setDroppedFiles((current) => current.filter((entry) => entry !== path));
  };

  const runCommand = async () => {
    if (validationError) {
      setInvocationError(validationError);
      return;
    }

    setInvocationError("");
    setResult(null);
    setIsRunning(true);

    try {
      const request: InvokeRequest = {
        command,
        args: commandArgs,
        workingDirectory: workingDirectory.trim() ? normalizePath(workingDirectory) : null,
      };

      const output = await invoke<InvokeResponse>("invoke_livingtool_command", { request });
      setResult(output);
    } catch (error: unknown) {
      setInvocationError(String(error));
    } finally {
      setIsRunning(false);
    }
  };

  return (
    <main className="mx-auto flex min-h-screen w-full max-w-6xl flex-col gap-6 px-6 py-8 text-ink md:px-10">
      <header className="rounded-2xl border border-ink/10 bg-white/65 p-6 shadow-panel backdrop-blur">
        <p className="font-mono text-xs uppercase tracking-[0.24em] text-rust">LivingTool Desktop</p>
        <h1 className="mt-2 text-3xl font-bold tracking-tight md:text-4xl">Tauri + React Command Workbench</h1>
        <p className="mt-3 max-w-3xl text-sm text-ink/80">
          Execute `LivingTool.Console` commands directly from the desktop app and route dropped file paths into your
          command arguments.
        </p>
      </header>

      <section className="grid gap-6 lg:grid-cols-[1.2fr_0.8fr]">
        <article className="rounded-2xl border border-ink/10 bg-white/75 p-6 shadow-panel backdrop-blur">
          <div className="grid gap-4 md:grid-cols-2">
            <label className="flex flex-col gap-2 text-sm font-medium">
              Command
              <select
                value={command}
                onChange={(event) => setCommand(event.target.value as CommandKind)}
                className="rounded-xl border border-ink/20 bg-white px-3 py-2 font-mono text-sm outline-none ring-rust/50 transition focus:ring-2"
              >
                <option value="run">run</option>
                <option value="read">read</option>
                <option value="npc">npc</option>
              </select>
            </label>

            <label className="flex flex-col gap-2 text-sm font-medium">
              Working directory (optional)
              <input
                value={workingDirectory}
                onChange={(event) => setWorkingDirectory(event.target.value)}
                placeholder="/absolute/path/to/workdir"
                className="rounded-xl border border-ink/20 bg-white px-3 py-2 font-mono text-sm outline-none ring-rust/50 transition focus:ring-2"
              />
            </label>
          </div>

          <p className="mt-3 text-sm text-ink/75">{descriptions[command]}</p>

          {command === "run" && (
            <div className="mt-5 grid gap-4 md:grid-cols-2">
              <label className="flex flex-col gap-2 text-sm font-medium">
                Main file (`-f`)
                <input
                  value={runFile}
                  onChange={(event) => setRunFile(event.target.value)}
                  className="rounded-xl border border-ink/20 bg-white px-3 py-2 font-mono text-sm outline-none ring-rust/50 transition focus:ring-2"
                />
              </label>

              <label className="flex flex-col gap-2 text-sm font-medium">
                Output directory (`-o`)
                <input
                  value={runOutputDir}
                  onChange={(event) => setRunOutputDir(event.target.value)}
                  className="rounded-xl border border-ink/20 bg-white px-3 py-2 font-mono text-sm outline-none ring-rust/50 transition focus:ring-2"
                />
              </label>

              <label className="flex flex-col gap-2 text-sm font-medium">
                Loc sectors file (`-l`)
                <input
                  value={runLocSectors}
                  onChange={(event) => setRunLocSectors(event.target.value)}
                  className="rounded-xl border border-ink/20 bg-white px-3 py-2 font-mono text-sm outline-none ring-rust/50 transition focus:ring-2"
                />
              </label>

              <label className="flex flex-col gap-2 text-sm font-medium">
                Executable (`-e`)
                <input
                  value={runExecutable}
                  onChange={(event) => setRunExecutable(event.target.value)}
                  className="rounded-xl border border-ink/20 bg-white px-3 py-2 font-mono text-sm outline-none ring-rust/50 transition focus:ring-2"
                />
              </label>
            </div>
          )}

          {command === "read" && (
            <label className="mt-5 flex flex-col gap-2 text-sm font-medium">
              File path (`-f`)
              <input
                value={readFile}
                onChange={(event) => setReadFile(event.target.value)}
                className="rounded-xl border border-ink/20 bg-white px-3 py-2 font-mono text-sm outline-none ring-rust/50 transition focus:ring-2"
              />
            </label>
          )}

          {command === "npc" && (
            <label className="mt-5 flex flex-col gap-2 text-sm font-medium">
              NPC BIN file (`-f`)
              <input
                value={npcFile}
                onChange={(event) => setNpcFile(event.target.value)}
                className="rounded-xl border border-ink/20 bg-white px-3 py-2 font-mono text-sm outline-none ring-rust/50 transition focus:ring-2"
              />
            </label>
          )}

          <label className="mt-5 flex flex-col gap-2 text-sm font-medium">
            Additional args (one argument per line)
            <textarea
              value={additionalArgs}
              onChange={(event) => setAdditionalArgs(event.target.value)}
              rows={4}
              placeholder="--flag\nvalue"
              className="rounded-xl border border-ink/20 bg-white px-3 py-2 font-mono text-sm outline-none ring-rust/50 transition focus:ring-2"
            />
          </label>

          <div className="mt-5 rounded-xl border border-teal/30 bg-teal/10 px-3 py-2 font-mono text-xs text-ink/85">
            {commandPreview}
          </div>

          {validationError && <p className="mt-3 text-sm font-medium text-rust">{validationError}</p>}
          {invocationError && <p className="mt-3 text-sm font-medium text-rust">{invocationError}</p>}

          <button
            type="button"
            onClick={() => {
              void runCommand();
            }}
            disabled={isRunning}
            className="mt-5 inline-flex items-center rounded-xl bg-rust px-5 py-2 text-sm font-semibold text-white transition hover:bg-rust/90 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {isRunning ? "Running..." : "Run Command"}
          </button>
        </article>

        <article className="rounded-2xl border border-ink/10 bg-white/75 p-6 shadow-panel backdrop-blur">
          <div className="flex items-center justify-between gap-3">
            <h2 className="text-xl font-semibold">Dropped Paths</h2>
            <button
              type="button"
              onClick={() => setDroppedFiles([])}
              className="rounded-lg border border-ink/20 px-3 py-1 text-xs font-semibold uppercase tracking-wide text-ink/70 transition hover:bg-ink/5"
            >
              Clear
            </button>
          </div>

          <p className="mt-2 text-sm text-ink/75">{dropMessage}</p>

          <div className="mt-4 max-h-[420px] space-y-3 overflow-auto pr-1">
            {droppedFiles.length === 0 && (
              <p className="rounded-xl border border-dashed border-ink/20 bg-white/60 px-3 py-6 text-center text-sm text-ink/60">
                Drop files from Finder or Explorer to capture absolute paths.
              </p>
            )}

            {droppedFiles.map((path) => (
              <div key={path} className="rounded-xl border border-ink/10 bg-white p-3">
                <p className="break-all font-mono text-xs text-ink/80">{path}</p>

                <div className="mt-3 flex flex-wrap gap-2">
                  {command === "run" && (
                    <>
                      <button
                        type="button"
                        onClick={() => applyDroppedPath(path, "main")}
                        className="rounded-md bg-ink/10 px-2 py-1 text-xs font-semibold text-ink/80 hover:bg-ink/20"
                      >
                        Set main
                      </button>
                      <button
                        type="button"
                        onClick={() => applyDroppedPath(path, "loc")}
                        className="rounded-md bg-ink/10 px-2 py-1 text-xs font-semibold text-ink/80 hover:bg-ink/20"
                      >
                        Set loc sectors
                      </button>
                      <button
                        type="button"
                        onClick={() => applyDroppedPath(path, "exe")}
                        className="rounded-md bg-ink/10 px-2 py-1 text-xs font-semibold text-ink/80 hover:bg-ink/20"
                      >
                        Set executable
                      </button>
                      <button
                        type="button"
                        onClick={() => applyDroppedPath(path, "output")}
                        className="rounded-md bg-ink/10 px-2 py-1 text-xs font-semibold text-ink/80 hover:bg-ink/20"
                      >
                        Set output folder
                      </button>
                    </>
                  )}

                  {command === "read" && (
                    <button
                      type="button"
                      onClick={() => applyDroppedPath(path, "read")}
                      className="rounded-md bg-ink/10 px-2 py-1 text-xs font-semibold text-ink/80 hover:bg-ink/20"
                    >
                      Set read file
                    </button>
                  )}

                  {command === "npc" && (
                    <button
                      type="button"
                      onClick={() => applyDroppedPath(path, "npc")}
                      className="rounded-md bg-ink/10 px-2 py-1 text-xs font-semibold text-ink/80 hover:bg-ink/20"
                    >
                      Set NPC file
                    </button>
                  )}

                  <button
                    type="button"
                    onClick={() => applyDroppedPath(path, "work")}
                    className="rounded-md bg-teal/20 px-2 py-1 text-xs font-semibold text-teal-900 hover:bg-teal/30"
                  >
                    Set workdir
                  </button>

                  <button
                    type="button"
                    onClick={() => removeDroppedPath(path)}
                    className="rounded-md bg-rust/10 px-2 py-1 text-xs font-semibold text-rust hover:bg-rust/20"
                  >
                    Remove
                  </button>
                </div>
              </div>
            ))}
          </div>
        </article>
      </section>

      <section className="rounded-2xl border border-ink/10 bg-[#1d2327] p-5 text-[#e9f0f4] shadow-panel">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <h2 className="text-lg font-semibold">Command Output</h2>
          {result && (
            <span
              className={`rounded-full px-3 py-1 font-mono text-xs ${
                result.success ? "bg-teal/30 text-teal-100" : "bg-rust/30 text-red-100"
              }`}
            >
              exit {result.exitCode}
            </span>
          )}
        </div>

        {result && (
          <p className="mt-3 break-all font-mono text-xs text-[#bdd2de]">{result.commandLine}</p>
        )}

        <pre className="mt-4 max-h-[360px] overflow-auto rounded-xl border border-white/10 bg-[#13181b] p-4 font-mono text-xs leading-6 text-[#dcf3ff]">
          {result?.combinedOutput || "Run a command to see output here."}
        </pre>

        {result?.stderr && (
          <pre className="mt-3 max-h-[180px] overflow-auto rounded-xl border border-rust/35 bg-[#2b1411] p-4 font-mono text-xs leading-6 text-[#ffd9cf]">
            {result.stderr}
          </pre>
        )}
      </section>
    </main>
  );
}

export default App;
