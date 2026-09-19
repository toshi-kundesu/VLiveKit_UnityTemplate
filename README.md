# VLiveKit Unity Template

Unity starter project for VLiveKit development with the VLiveKit package set checked in as Git submodules under `Packages/`.

## Create a project from this template

```powershell
git clone --recurse-submodules https://github.com/toshi-kundesu/VLiveKit_UnityTemplate.git MyVLiveKitProject
cd MyVLiveKitProject
git remote rename origin template
git remote add origin <your-project-repository-url>
git push -u origin main
```

The Unity samples in `VLiveKit_TestAssetsContainer` use shortened paths under
`Unity/Demos` so a normal Windows clone does not need `core.longpaths=true`.
The submodule's CI checks the tracked path budget and performs a Windows checkout
with long-path support disabled. Keep the project root reasonably short, such as
`E:\VLiveKit\_UnityTemplate`, since its length counts toward Windows' path limit
and other submodules have their own directory layouts.

## Sample guide

The VLiveKit sample guide opens when the project starts. You can reopen it from
`toshi/VLiveKit/Samples` and disable automatic opening for this project in the
window. The camera-unit and LED/LTCGI scenes are featured entry points. Scene
changes ask whether to save your current work first. The searchable package
catalog also lists other VLiveKit scenes, including unimported samples and
scenes outside each submodule's exported package, with links to their locations.

Lens Filters test scenes use a dark stage with a lion sculpture, material spheres,
and fixed exposure. Auto Cycle is enabled by default; select a filter from the
on-screen list to pause on it, or resume cycling with the Auto Cycle button. The
template registers the installed HDRP filters; after adding filters to another
project, use `toshi/VLiveKit/LensFilters/Register Installed HDRP Filters` to add
missing registrations without changing the existing filter order.

## Pull template updates into an existing project

Keep the original template repo as a remote named `template`:

```powershell
git fetch template
git merge template/main
git submodule update --init --recursive
```

Resolve Unity project setting or package manifest conflicts deliberately. VLiveKit package updates should usually be taken as submodule pointer updates or npm/UPM version updates, while project settings should be reviewed before accepting.

`Packages/packages-lock.json` is intentionally not committed in the template. Open the project once in the target Unity version and commit the regenerated lock file in the derived project if you want exact dependency pinning there.

## MCP Setup

Unity MCP is included through `com.unity.ai.assistant`. Open the project once in Unity, then follow `Docs/MCP_SETUP.md` or use `Tools/MCP/Configure-CodexUnityMcp.ps1` to register the Unity relay with Codex CLI.

## Included submodules

- `Packages/VLiveKit`
- `Packages/VLiveKit_camera`
- `Packages/VLiveKit_LiveLensFilters`
- `Packages/VLiveKit_LiveToon`
- `Packages/VLiveKit_VideoRack`
- `Packages/VLiveKit_TestAssetsContainer`
- `Packages/VLiveKit_ArtNetLink`
- `Packages/VLiveKit_LEDVision`
- `Packages/VLiveKit_PerformerAct`
- `Packages/VLiveKit_StageBuilder`
- `Packages/VLiveKit_StageEffect`
- `Packages/VLiveKit_ThirdPartyUtilities`
- `Packages/Memo`

`VLiveKit_ThirdPartyUtilities` is private/local-only and requires repository access. It is included here as a sandbox dependency, not as an npm-published package.
