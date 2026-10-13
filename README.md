# PrefabManager

Install via Unity Package Manager → Add package from git URL:

```
https://github.com/SacraneScrny/PrefabManager.git
```

Type-safe access to prefabs from `Resources` via a generated tree plus a GUID manifest
(no string paths at runtime).

Identity is the asset GUID: rename/move of a prefab keeps serialized `PrefabRef`
valid — only the `guid -> resourcePath` mapping is regenerated.

## Code generation

`Prefabs/Generate` scans prefabs in all `Resources` folders and writes to
`Assets/PrefabManager.Generated/`:
- `Prefabs.cs` — dot access (`Prefabs.Enemies.Goblins.goblin01`),
- `PrefabInterfaces.cs` + `PrefabWrappers.cs` — for `[SerializeReference, SubclassSelector]`,
- `PrefabManifestInstaller.cs` — registers guid→path on startup,
- `PrefabManager.Generated.asmdef` (references `PrefabManager`).

Each prefab is bound to the nearest parent `Resources` folder. An exact duplicate
`resourcePath` from different `Resources` folders keeps the first by `fullPath` with
a warning.

Folder/prefab names are sanitized to C# identifiers (`-`/space/`.` → `_`, keywords →
`_*`, leading digit → `_...`); collisions after sanitizing (leaf vs leaf, leaf vs
folder, interface, wrapper class) are uniquified with `_1`, `_2` plus a warning.

`Prefabs/Validate` re-scans and reports duplicate paths/guids.
An `AssetPostprocessor` auto-regenerates the mapping (without refresh) on
`.prefab` add/delete/move/rename outside `Assets/PrefabManager.Generated/`.
Generated files are only rewritten when content changes.

## Runtime API

```csharp
PrefabRef r = Prefabs.Enemies.Goblins.goblin01;
if (!r.IsValid) return; // PrefabRef.None has empty Guid
if (!r.IsRegistered()) return; // guid missing from the generated manifest

// Get prefab asset without instantiate (cached Resources.Load):
GameObject prefab = r.Load();
var agentPrefab = r.Load<NavMeshAgent>(); // where T : Component
// NB: Load<T> returns the component on the prefab ASSET — do not mutate it.

// Silent check, no warning on failure:
if (r.TryLoad(out var asset)) { /* ... */ }

// Instantiate (null + warning on failure):
var go = r.Instantiate(pos, rot);
var agent = r.Instantiate<NavMeshAgent>(pos, rot);
var child = r.Instantiate(parent);
var child2 = r.Instantiate(parent, worldPositionStays: false);
var agentChild = r.Instantiate<NavMeshAgent>(parent);
```

The same `Load` / `TryLoad` / `Instantiate` methods exist on `IPrefabRef` wrappers
(plus `IsValid` / `IsRegistered()`).

`PrefabManifest.Register` is public — it is called by the generated installer from its own assembly.
It copies the dictionary; `null` clears it. `PrefabManifest.TryGetPath(guid, out path)`
resolves the current path for debugging/tools, `Count` reports manifest size.
Set `PrefabManifest.WarningsEnabled = false` to silence `[Prefabs]` warnings
(e.g. when probing optional prefabs — or use `TryLoad` for a single silent check).

## Reskin / substitution (no code changes at call sites)

`PrefabRef` identity is untouched (same guid, same hash). The override is resolved
inside `PrefabManifest`/`PrefabRegistry`, so `Load`/`Instantiate` on `PrefabRef`
and `IPrefabRef` pick it up automatically:

```csharp
// One prefab:
PrefabManifest.SetOverride(Prefabs.Enemies.Goblins.goblin01, Prefabs.Enemies.Goblins.goblin01_halloween);

// Whole group/skin at once (one cache invalidation):
PrefabManifest.SetOverrides(new Dictionary<PrefabRef, PrefabRef>
{
    [Prefabs.Enemies.Goblins.goblin01] = Prefabs.Enemies.Goblins.goblin01_halloween,
});

// Revert:
PrefabManifest.ClearOverride(Prefabs.Enemies.Goblins.goblin01);
PrefabManifest.ClearAllOverrides();
```

Notes: overrides are runtime-only (not serialized), chains resolve transitively
with a cycle guard, `Register` does not clear them, any change clears the load cache.

### Override maps (editor reskin assets)

Create via `Create → Prefabs → Override Map`: a list of Original → Replacement rows
(pick prefabs by drag-drop — the drawer stores only the guid, both sides should live
in `Resources`). Each row renders as a single line `[x] original -> replacement`
(no foldout, no `Element N`); the checkbox excludes the row without deleting it,
a `!` mark with tooltip flags a missing asset or a prefab outside `Resources`.
Maps anywhere under a `Resources` folder auto-apply
at startup (`BeforeSplashScreen`, confirmed by a `[Prefabs] Applied N override maps ...`
console line) — no manual step, cache invalidation is automatic.
When maps are found, previous overrides are cleared first, so unchecked
or deleted rows reliably turn off (also across play sessions without domain reload);
maps elsewhere are applied manually. The map inspector warns when auto-apply
won't pick it up:

```csharp
myMap.Apply();   // applies checked rows
myMap.Revert();  // releases only slots this map still owns
```

Several maps: applied sorted by `priority`, then name. Same original → different
replacement overwrites with a `[Prefabs] Override conflict ...` warning (higher
priority wins); re-applying the same value is silent. Test helper:
`Prefabs/Overrides/Clear All Overrides`.

**Dependencies:** none. **Editor:** the `PrefabManager.Editor` generator.
