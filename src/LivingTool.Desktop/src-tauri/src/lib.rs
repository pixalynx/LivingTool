use serde::{Deserialize, Serialize};
use std::path::{Path, PathBuf};
use std::process::Command;

#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
struct ConsoleInvocationRequest {
    command: String,
    args: Vec<String>,
    working_directory: Option<String>,
}

#[derive(Debug, Serialize)]
#[serde(rename_all = "camelCase")]
struct ConsoleInvocationResponse {
    command_line: String,
    stdout: String,
    stderr: String,
    combined_output: String,
    exit_code: i32,
    success: bool,
    project_path: String,
    working_directory: String,
}

fn resolve_console_project_path() -> Result<PathBuf, String> {
    let manifest_dir = Path::new(env!("CARGO_MANIFEST_DIR"));
    let src_dir = manifest_dir
        .parent()
        .and_then(Path::parent)
        .ok_or_else(|| {
            String::from("Unable to resolve source directory from CARGO_MANIFEST_DIR.")
        })?;

    let project_path = src_dir
        .join("LivingTool.Console")
        .join("LivingTool.Console.csproj");

    if !project_path.exists() {
        return Err(format!(
            "LivingTool.Console project not found at {}",
            project_path.display()
        ));
    }

    Ok(project_path)
}

fn resolve_working_directory(
    requested_working_directory: Option<&str>,
    project_path: &Path,
) -> Result<PathBuf, String> {
    if let Some(path) = requested_working_directory {
        let trimmed = path.trim();
        if !trimmed.is_empty() {
            let working_directory = PathBuf::from(trimmed);
            if !working_directory.exists() {
                return Err(format!(
                    "Working directory does not exist: {}",
                    working_directory.display()
                ));
            }

            return Ok(working_directory);
        }
    }

    let fallback = project_path
        .parent()
        .and_then(Path::parent)
        .map(Path::to_path_buf)
        .ok_or_else(|| String::from("Unable to resolve default working directory."))?;

    Ok(fallback)
}

fn quote_arg(value: &str) -> String {
    if value.contains(' ') || value.contains('"') || value.contains('\'') {
        return format!("\"{}\"", value.replace('"', "\\\""));
    }

    value.to_owned()
}

fn format_command_line(project_path: &Path, command: &str, args: &[String]) -> String {
    let mut parts = vec![
        String::from("dotnet"),
        String::from("run"),
        String::from("--project"),
        project_path.display().to_string(),
        String::from("--"),
        command.to_owned(),
    ];

    parts.extend(args.iter().cloned());
    parts
        .into_iter()
        .map(|part| quote_arg(&part))
        .collect::<Vec<String>>()
        .join(" ")
}

#[tauri::command]
fn invoke_livingtool_command(
    request: ConsoleInvocationRequest,
) -> Result<ConsoleInvocationResponse, String> {
    let command = request.command.trim();
    if command.is_empty() {
        return Err(String::from("Command name is required."));
    }

    let project_path = resolve_console_project_path()?;
    let working_directory =
        resolve_working_directory(request.working_directory.as_deref(), &project_path)?;

    let output = Command::new("dotnet")
        .arg("run")
        .arg("--project")
        .arg(&project_path)
        .arg("--")
        .arg(command)
        .args(&request.args)
        .current_dir(&working_directory)
        .env("NO_COLOR", "1")
        .output()
        .map_err(|error| format!("Failed to execute dotnet command: {error}"))?;

    let stdout = String::from_utf8_lossy(&output.stdout).to_string();
    let stderr = String::from_utf8_lossy(&output.stderr).to_string();

    let combined_output = if stderr.is_empty() {
        stdout.clone()
    } else if stdout.is_empty() {
        stderr.clone()
    } else {
        format!("{stdout}\n{stderr}")
    };

    let exit_code = output
        .status
        .code()
        .unwrap_or(if output.status.success() { 0 } else { 1 });

    Ok(ConsoleInvocationResponse {
        command_line: format_command_line(&project_path, command, &request.args),
        stdout,
        stderr,
        combined_output,
        exit_code,
        success: output.status.success(),
        project_path: project_path.display().to_string(),
        working_directory: working_directory.display().to_string(),
    })
}

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        .plugin(tauri_plugin_opener::init())
        .invoke_handler(tauri::generate_handler![invoke_livingtool_command])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
