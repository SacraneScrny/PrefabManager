# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## [1.0.0] - 2026-10-13

### Added

- Initial UPM package layout: `Runtime/` + `Editor/`.
- Runtime assembly `PrefabManager` (`Runtime/PrefabManager.asmdef`): `PrefabRef`, `IPrefabRef`, `PrefabManifest`, `PrefabRegistry`, `PrefabRefExtensions`.
- Editor assembly `PrefabManager.Editor` (`Editor/PrefabManager.Editor.asmdef`, Editor-only): `PrefabGenerator` (`Prefabs/Generate`, `Prefabs/Validate`), `PrefabPostprocessor` (auto-regeneration on prefab/folder changes).
- GUID-based identity: rename/move of a prefab keeps serialized `PrefabRef` valid, only the `guid -> resourcePath` mapping is regenerated.
- Runtime API: `Load` / `TryLoad` / `Instantiate` on `PrefabRef` and `IPrefabRef`, `IsRegistered`, `PrefabManifest.TryGetPath` / `Count` / `WarningsEnabled`.
- `package.json` (`com.sackrany.prefabmanager`): Unity 2021.3+, zero dependencies.
- `README`: installation via Unity Package Manager (git URL).
