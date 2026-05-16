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

If `VLiveKit_TestAssetsContainer` fails to checkout on Windows because of long Unity sample paths, enable long paths for Git and update the submodules again:

```powershell
git config --global core.longpaths true
git submodule update --init --recursive
```

## Pull template updates into an existing project

Keep the original template repo as a remote named `template`:

```powershell
git fetch template
git merge template/main
git submodule update --init --recursive
```

Resolve Unity project setting or package manifest conflicts deliberately. VLiveKit package updates should usually be taken as submodule pointer updates or npm/UPM version updates, while project settings should be reviewed before accepting.

`Packages/packages-lock.json` is intentionally not committed in the template. Open the project once in the target Unity version and commit the regenerated lock file in the derived project if you want exact dependency pinning there.

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
