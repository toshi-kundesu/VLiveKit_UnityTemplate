# VLiveKit Unity Template Project Overview

This document is a compact map for AI assistants and MCP sessions working in projects created from the VLiveKit Unity Template.

## Project

- Unity: 6000.4.6f1
- Render pipeline: HDRP 17.4.0
- Purpose: Start VLiveKit-based Unity projects with the package set checked out as Git submodules.
- Package manager: local `file:` dependencies in `Packages/manifest.json`.
- Unity MCP: enabled through `com.unity.ai.assistant` and configured per user in the Unity Editor / MCP client.

## Repository Shape

- `Packages/VLiveKit`: installer and package-manager UI for `com.toshi.vlivekit`.
- `Packages/VLiveKit_*`: individual VLiveKit package submodules.
- `Packages/Memo`: writing notes and production notes, not a Unity runtime package.
- `Assets/Docs`: short project-local AI/MCP guidance.
- `Docs`: project setup notes for humans and agents.
- `Tools/MCP`: helper scripts for local MCP client setup.

## Package Roles

| Path | Role |
| --- | --- |
| `Packages/VLiveKit` | Installer/catalog/update surfaces. Keep installer-only. |
| `Packages/VLiveKit_LiveToon` | HDRP toon shader, VRM conversion, character look/setup tools. |
| `Packages/VLiveKit_LiveLensFilters` | HDRP custom passes and post-process lens effects. |
| `Packages/VLiveKit_TestAssetsContainer` | Reusable test-scene helpers and sample dependencies. |
| `Packages/VLiveKit_VideoRack` | Video/FFmpeg-related package work. |
| `Packages/VLiveKit_camera` | Camera-related VLiveKit package work. |
| `Packages/VLiveKit_ThirdPartyUtilities` | Private/local third-party utilities only. Do not npm-publish. |

## Scene Editing Through AI/MCP

Use MCP scene tools conservatively:

1. Read the active scene name, path, dirty state, and root hierarchy before changing anything.
2. If the scene is already dirty, avoid creating, loading, or saving scenes unless the user asks.
3. Put AI-created temporary scene objects under clear names such as `VLiveKit_AI_*` or `MCP_Codex_*`.
4. Prefer undoable editor operations and leave scenes unsaved by default.
5. Read Unity Console after changes and fix compile errors before continuing.

## Template Update Pattern

Projects created from this template can keep the template repo as a remote named `template` and periodically merge `template/main`. Treat package changes as submodule pointer updates, and review `ProjectSettings/` / `Packages/manifest.json` conflicts manually.

## Common Gotchas

- Submodules under `Packages/` may need their own commits before the parent project can commit pointer updates.
- `Library/`, `Temp/`, `Logs/`, `UserSettings/`, and generated Unity artifacts are editor output.
- The installer must not overwrite local, submodule, or `Assets/` installs automatically.
- Unity Package Manager registry state, not GitHub `main`, is the source of truth for package update checks.
- `VLiveKit_ThirdPartyUtilities` is private and requires repository access.