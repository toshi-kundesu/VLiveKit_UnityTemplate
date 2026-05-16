# VLiveKit MCP Setup

This template includes Unity AI Assistant so Unity MCP can be enabled as soon as the project resolves packages in Unity.

## Unity Setup

1. Open this repository root in Unity Hub with Unity `6000.4.6f1` or a compatible Unity 6 editor.
2. Let Unity resolve packages. The manifest includes `com.unity.ai.assistant`.
3. Open `Edit > Project Settings > AI > Unity MCP`.
4. Confirm `Unity Bridge` is `Running`. If it is stopped, press `Start`.
5. Keep Unity open while connecting an external MCP client.

Unity installs the relay here on Windows after the package imports:

```text
%USERPROFILE%\.unity\relay\relay_win.exe
```

Unity MCP discovery files live here:

```text
%USERPROFILE%\.unity\mcp\connections\*.json
```

If more than one Unity Editor is open, copy the `project_path` from the matching discovery file and pass it to the relay with `--project-path`.

## Codex CLI

Run the helper from the repository root:

```powershell
.\Tools\MCP\Configure-CodexUnityMcp.ps1
```

Or pass an explicit project path:

```powershell
.\Tools\MCP\Configure-CodexUnityMcp.ps1 -ProjectPath "E:\share_ssd\Unity\GitHub\VLiveKit_UnityTemplate"
```

The script runs the equivalent of:

```powershell
codex mcp add unity-mcp -- "%USERPROFILE%\.unity\relay\relay_win.exe" --mcp --project-path "<project-path>"
```

Check the result:

```powershell
codex mcp list
```

## Cursor / Claude-Style JSON

Use `.mcp.example.json` as a starting point for clients that read an `mcpServers` JSON block. Replace `YOUR_USER` and `PROJECT_PATH`, or use the Unity MCP settings page's `Integrations` section to configure supported clients automatically.

## First Connection Approval

External MCP clients require approval in Unity:

1. Start the MCP client.
2. In Unity, open `Edit > Project Settings > AI > Unity MCP`.
3. Accept the pending client connection.

Previously approved clients should reconnect automatically.

## Smoke Test

Ask the client:

```text
Read the Unity console messages and summarize any warnings or errors.
```

Useful low-risk Unity MCP tools include console read, active scene read, hierarchy read, and undoable GameObject creation. Avoid saving dirty scenes unless the user explicitly asks.