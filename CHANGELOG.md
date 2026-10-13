# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## [1.1.0] - 2026-10-13

### Added

- Runtime prefab substitution for reskins: `PrefabManifest.SetOverride` /
  `SetOverrides` (single + bulk group/skin), `ClearOverride` / `ClearAllOverrides`,
  `ResolveGuid` / `TryGetOverride` / `Overrides` / `OverrideCount`. `PrefabRef`
  struct unchanged (same guid, hash, serialization); `TryGetPath`, `IsRegistered`,
  `Load` / `TryLoad` / `Instantiate` resolve overrides, load cache invalidates on change.
- Editor reskin assets: `PrefabOverrideMap` SO (`Create → Prefabs → Override Map`,
  guid-only rows, `priority`, `Apply` / `Revert` / `ApplyAll`), auto-apply at startup
  from `Resources/PrefabOverrideMaps/` sorted by priority then name, conflicts overwrite
  with a warning, `Prefabs/Overrides/Apply All Maps` + `Clear All Overrides` menu.
- `PrefabRef` inspector drawer: object field instead of raw guid, warns on missing asset
  or prefab outside `Resources`. Runtime struct unchanged.

## [1.0.0] - 2026-10-13

### Added

- Initial UPM package layout: `Runtime/` + `Editor/`.
- Runtime assembly `PrefabManager` (`Runtime/PrefabManager.asmdef`): `PrefabRef`, `IPrefabRef`, `PrefabManifest`, `PrefabRegistry`, `PrefabRefExtensions`.
- Editor assembly `PrefabManager.Editor` (`Editor/PrefabManager.Editor.asmdef`, Editor-only): `PrefabGenerator` (`Prefabs/Generate`, `Prefabs/Validate`), `PrefabPostprocessor` (auto-regeneration on prefab/folder changes).
- GUID-based identity: rename/move of a prefab keeps serialized `PrefabRef` valid, only the `guid -> resourcePath` mapping is regenerated.
- Runtime API: `Load` / `TryLoad` / `Instantiate` on `PrefabRef` and `IPrefabRef`, `IsRegistered`, `PrefabManifest.TryGetPath` / `Count` / `WarningsEnabled`.
- `package.json` (`com.sackrany.prefabmanager`): Unity 2021.3+, zero dependencies.
- `README`: installation via Unity Package Manager (git URL).
