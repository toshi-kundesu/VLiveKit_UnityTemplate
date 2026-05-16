param(
    [string]$ProjectPath,
    [string]$ServerName = "unity-mcp",
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptDir "..\..")

if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = $repoRoot.Path
}

$relayPath = Join-Path $env:USERPROFILE ".unity\relay\relay_win.exe"
$args = @("mcp", "add", $ServerName, "--", $relayPath, "--mcp", "--project-path", $ProjectPath)
$codex = Get-Command codex -ErrorAction SilentlyContinue

Write-Host "Codex MCP server name: $ServerName"
Write-Host "Unity project path: $ProjectPath"
Write-Host "Unity MCP relay: $relayPath"

if ($DryRun) {
    Write-Host "Dry run command: codex $($args -join ' ')"
    if (-not (Test-Path -LiteralPath $relayPath)) {
        Write-Host "Note: relay is not installed yet. Open Unity once and start Unity MCP first."
    }
    if ($null -eq $codex) {
        Write-Host "Note: Codex CLI is not on PATH yet. Install it with: npm install -g @openai/codex"
    }
    exit 0
}

if (-not (Test-Path -LiteralPath $relayPath)) {
    Write-Host "Unity MCP relay was not found at: $relayPath"
    Write-Host "Open this project in Unity, let com.unity.ai.assistant import, then start Unity MCP from Edit > Project Settings > AI > Unity MCP."
    exit 1
}

if ($null -eq $codex) {
    Write-Host "Codex CLI was not found on PATH. Install it first: npm install -g @openai/codex"
    exit 1
}

Write-Host "Registering Codex MCP server '$ServerName'."
& $codex.Source @args

Write-Host "Done. Open Unity MCP settings and approve the pending client connection if Unity asks."
Write-Host "Verify with: codex mcp list"