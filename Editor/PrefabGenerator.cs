using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using UnityEditor;

using UnityEngine;

namespace PrefabManager.Editor
{
    public static class PrefabGenerator
    {
        internal const string OutputPath = "Assets/PrefabManager.Generated";

        internal static bool IsGeneratedPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return false;
            return assetPath.StartsWith(OutputPath + "/", StringComparison.Ordinal);
        }

        [MenuItem("Prefabs/Generate")]
        public static void Generate()
        {
            DoGenerate(refresh: true);
        }

        internal static void DoGenerate(bool refresh)
        {
            var entries = CollectEntries();
            Directory.CreateDirectory(OutputPath);
            EnsureAsmdef();

            var (root, dotMap) = BuildTree(entries);
            var ifaceMap = BuildFolderIfaceMap(entries);

            WritePrefabsClass(root);
            WritePrefabInterfaces(ifaceMap);
            WritePrefabWrappers(entries, dotMap, ifaceMap);
            WriteManifestInstaller(entries);

            if (refresh)
                AssetDatabase.Refresh();
            Debug.Log($"[Prefabs] Generated {entries.Count} entries");
        }

        [MenuItem("Prefabs/Validate")]
        public static void Validate()
        {
            var entries = CollectEntries();
            var byPath = new HashSet<string>(StringComparer.Ordinal);
            int dup = 0;
            foreach (var e in entries)
                if (!byPath.Add(e.ResourcePath))
                    dup++;

            var guids = new HashSet<string>(StringComparer.Ordinal);
            int dupGuid = 0;
            foreach (var e in entries)
                if (!guids.Add(e.Guid))
                    dupGuid++;

            Debug.Log($"[Prefabs] Validate: {entries.Count} entries, duplicate paths: {dup}, duplicate guids: {dupGuid}. " +
                      (dup + dupGuid == 0 ? "OK." : "See warnings above."));
        }

        static void EnsureAsmdef()
        {
            var path = Path.Combine(OutputPath, "PrefabManager.Generated.asmdef");
            if (File.Exists(path)) return;

            const string json =
@"{
    ""name"": ""PrefabManager.Generated"",
    ""rootNamespace"": """",
    ""references"": [
        ""PrefabManager""
    ],
    ""includePlatforms"": [],
    ""excludePlatforms"": [],
    ""allowUnsafeCode"": false,
    ""overrideReferences"": false,
    ""precompiledReferences"": [],
    ""autoReferenced"": true,
    ""defineConstraints"": [],
    ""versionDefines"": [],
    ""noEngineReferences"": false
}";
            File.WriteAllText(path, json);
        }

        // Scan ALL Resources folders, bind each prefab to the NEAREST
        // parent Resources folder, dedup by GUID, warn on duplicate resourcePath.
        // Identity is the asset GUID: rename/move keeps PrefabRef valid,
        // only guid->path mapping is regenerated.
        static List<Entry> CollectEntries()
        {
            var resourceRoots = AssetDatabase
                .GetAllAssetPaths()
                .Where(p => AssetDatabase.IsValidFolder(p) &&
                            (p == "Assets/Resources" || p.EndsWith("/Resources", StringComparison.Ordinal)))
                .ToList();

            // Longest first => first match is the nearest parent Resources.
            resourceRoots.Sort((a, b) => b.Length.CompareTo(a.Length));

            var byGuid = new Dictionary<string, Collected>(StringComparer.Ordinal);

            foreach (var guid in AssetDatabase.FindAssets("t:Prefab"))
            {
                var fullPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(fullPath)) continue;
                if (AssetDatabase.IsValidFolder(fullPath)) continue;

                string bestRoot = null;
                foreach (var root in resourceRoots)
                {
                    if (fullPath.StartsWith(root + "/", StringComparison.Ordinal))
                    {
                        bestRoot = root;
                        break;
                    }
                }

                if (bestRoot == null) continue; // not inside any Resources folder

                var relative = fullPath.Substring(bestRoot.Length + 1).Replace('\\', '/');
                var resourcePath = Path.ChangeExtension(relative, null)?.Replace('\\', '/');
                if (string.IsNullOrEmpty(resourcePath)) continue;

                byGuid[guid] = new Collected(guid, fullPath, resourcePath);
            }

            // Same resourcePath in two different Resources folders => Resources.Load ambiguity.
            // Dedup exact (case-sensitive); case-only differences are kept but warned about.
            var dedupByPath = new List<Collected>();
            foreach (var g in byGuid.Values.GroupBy(c => c.ResourcePath, StringComparer.Ordinal))
            {
                if (g.Count() > 1)
                {
                    var ordered = g.OrderBy(c => c.FullPath, StringComparer.Ordinal).ToList();
                    Debug.LogWarning("[Prefabs] Duplicate resourcePath '" + g.Key + "': " +
                                     string.Join(", ", ordered.Select(c => c.FullPath)) +
                                     ". Using '" + ordered[0].FullPath + "'.");
                    dedupByPath.Add(ordered[0]);
                }
                else
                {
                    dedupByPath.Add(g.First());
                }
            }

            foreach (var g in dedupByPath.GroupBy(c => c.ResourcePath, StringComparer.OrdinalIgnoreCase))
            {
                if (g.Count() > 1 && g.Select(c => c.ResourcePath).Distinct(StringComparer.Ordinal).Count() > 1)
                    Debug.LogWarning("[Prefabs] resourcePaths differ only by case '" + g.Key + "': " +
                                     string.Join(", ", g.Select(c => "'" + c.ResourcePath + "' (" + c.FullPath + ")")) +
                                     ". Resources.Load may be ambiguous on case-insensitive platforms.");
            }

            return dedupByPath
                .OrderBy(c => c.ResourcePath, StringComparer.Ordinal)
                .Select(c => new Entry(c.Guid, c.ResourcePath))
                .ToList();
        }

        static void WritePrefabsClass(TreeNode root)
        {
            var sb = new StringBuilder();
            sb.AppendLine("// AUTO-GENERATED — do not edit manually");
            sb.AppendLine("using PrefabManager;");
            sb.AppendLine("using PrefabManager.Entities;");
            sb.AppendLine();
            sb.AppendLine("public static partial class Prefabs");
            sb.AppendLine("{");
            WriteTreeChildren(sb, root, 1);
            sb.AppendLine("}");
            WriteFile("Prefabs.cs", sb);
        }

        static void WriteTreeChildren(StringBuilder sb, TreeNode node, int depth)
        {
            var pad = Indent(depth);
            foreach (var leaf in node.Leaves.OrderBy(l => l.Name, StringComparer.Ordinal))
            {
                sb.AppendLine($"{pad}/// <summary><c>{leaf.Entry.ResourcePath}</c></summary>");
                sb.AppendLine($"{pad}public static readonly PrefabRef {leaf.Name} = new PrefabRef(\"{leaf.Entry.Guid}\");");
            }
            if (node.Leaves.Count > 0 && node.Children.Count > 0) sb.AppendLine();
            foreach (var child in node.Children.Values.OrderBy(c => c.Name, StringComparer.Ordinal))
            {
                sb.AppendLine($"{pad}public static partial class {child.Name}");
                sb.AppendLine($"{pad}{{");
                WriteTreeChildren(sb, child, depth + 1);
                sb.AppendLine($"{pad}}}");
                sb.AppendLine();
            }
        }

        static void WritePrefabInterfaces(Dictionary<string, string> ifaceMap)
        {
            var sb = new StringBuilder();
            sb.AppendLine("// AUTO-GENERATED — do not edit manually");
            sb.AppendLine("using PrefabManager;");
            sb.AppendLine("using PrefabManager.Entities;");
            sb.AppendLine();

            foreach (var folder in ifaceMap.Keys.OrderBy(f => f, StringComparer.Ordinal))
            {
                var iface = ifaceMap[folder];
                var parent = GetParentFolder(folder);
                var parentIface = parent != null ? ifaceMap[parent] : "IPrefabRef";

                sb.AppendLine($"/// <summary>Prefabs inside <c>{folder}/</c></summary>");
                sb.AppendLine($"public interface {iface} : {parentIface} {{ }}");
            }

            WriteFile("PrefabInterfaces.cs", sb);
        }

        static void WritePrefabWrappers(
            List<Entry> entries,
            Dictionary<string, string> dotMap,
            Dictionary<string, string> ifaceMap)
        {
            var sb = new StringBuilder();
            sb.AppendLine("// AUTO-GENERATED — do not edit manually");
            sb.AppendLine("using System;");
            sb.AppendLine("using PrefabManager;");
            sb.AppendLine("using PrefabManager.Entities;");
            sb.AppendLine();

            var usedClassNames = new HashSet<string>(StringComparer.Ordinal);

            foreach (var e in entries.OrderBy(e => e.ResourcePath, StringComparer.Ordinal))
            {
                var parts = e.ResourcePath.Split('/');
                var folderPath = parts.Length > 1 ? string.Join("/", parts.Take(parts.Length - 1)) : null;
                var iface = folderPath != null ? ifaceMap[folderPath] : "IPrefabRef";

                if (!dotMap.TryGetValue(e.ResourcePath, out var dotAccess))
                    dotAccess = ToDotAccessFallback(e.ResourcePath);

                var baseName = "Prefab_" + SanitizeName(e.ResourcePath.Replace('/', '_').Replace('-', '_'));
                var className = MakeUnique(baseName, usedClassNames);
                if (className != baseName)
                    Debug.LogWarning($"[Prefabs] Wrapper class collision for '{e.ResourcePath}', renamed to '{className}'.");

                sb.AppendLine("[Serializable]");
                sb.AppendLine($"public sealed class {className} : {iface}");
                sb.AppendLine("{");
                sb.AppendLine($"    public PrefabRef GetRef() => {dotAccess};");
                sb.AppendLine("}");
                sb.AppendLine();
            }

            WriteFile("PrefabWrappers.cs", sb);
        }

        static void WriteManifestInstaller(List<Entry> entries)
        {
            var sb = new StringBuilder();
            sb.AppendLine("// AUTO-GENERATED — do not edit manually");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using PrefabManager;");
            sb.AppendLine("using PrefabManager.Entities;");
            sb.AppendLine();
            sb.AppendLine("internal static class PrefabManifestInstaller");
            sb.AppendLine("{");
            sb.AppendLine("    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]");
            sb.AppendLine("    static void Install()");
            sb.AppendLine("    {");
            sb.AppendLine("        PrefabManifest.Register(new Dictionary<string, string>");
            sb.AppendLine("        {");
            foreach (var e in entries)
                sb.AppendLine($"            [\"{Escape(e.Guid)}\"] = \"{Escape(e.ResourcePath)}\",");
            sb.AppendLine("        });");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            WriteFile("PrefabManifestInstaller.cs", sb);
        }

        // "_" separator + numeric uniquification on collision.
        static Dictionary<string, string> BuildFolderIfaceMap(List<Entry> entries)
        {
            var folders = CollectFolders(entries)
                .OrderBy(f => f.Count(c => c == '/'))
                .ThenBy(f => f, StringComparer.Ordinal)
                .ToList();

            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            var used = new HashSet<string>(StringComparer.Ordinal);

            foreach (var folder in folders)
            {
                var baseName = FolderToInterface(folder);
                var unique = MakeUnique(baseName, used);
                if (unique != baseName)
                    Debug.LogWarning($"[Prefabs] Interface name collision for folder '{folder}', renamed to '{unique}'.");
                map[folder] = unique;
            }

            return map;
        }

        static string FolderToInterface(string folder)
            => "IPrefabs_" + string.Join("_", folder.Split('/').Select(SanitizeName));

        static string GetParentFolder(string folder)
        {
            var idx = folder.LastIndexOf('/');
            return idx < 0 ? null : folder[..idx];
        }

        static string ToDotAccessFallback(string resourcePath)
            => "Prefabs." + string.Join(".", resourcePath.Split('/').Select(SanitizeName));

        static HashSet<string> CollectFolders(List<Entry> entries)
        {
            var folders = new HashSet<string>(StringComparer.Ordinal);
            foreach (var e in entries)
            {
                var parts = e.ResourcePath.Split('/');
                for (int i = 1; i < parts.Length; i++)
                    folders.Add(string.Join("/", parts.Take(i)));
            }
            return folders;
        }

        // Uniquify leaf vs leaf and leaf vs child node,
        // and merge folders only when raw names match.
        static (TreeNode Root, Dictionary<string, string> DotMap) BuildTree(List<Entry> entries)
        {
            var root = new TreeNode("Prefabs", string.Empty);
            var dotMap = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var e in entries.OrderBy(e => e.ResourcePath, StringComparer.Ordinal))
            {
                var parts = e.ResourcePath.Split('/');
                var node = root;
                var uniquePath = new List<string>();

                for (int i = 0; i < parts.Length - 1; i++)
                {
                    var raw = parts[i];
                    var baseName = SanitizeName(raw);

                    if (node.Children.TryGetValue(baseName, out var existing) && existing.RawName == raw)
                    {
                        node = existing;
                        uniquePath.Add(node.Name);
                        continue;
                    }

                    var unique = baseName;
                    if (node.Children.ContainsKey(unique) || node.Leaves.Any(l => l.Name == unique))
                    {
                        unique = MakeUnique(baseName, new HashSet<string>(node.Children.Keys.Concat(node.Leaves.Select(l => l.Name)), StringComparer.Ordinal));
                        Debug.LogWarning($"[Prefabs] Folder name collision '{raw}' in '{string.Join("/", parts.Take(i))}', renamed to '{unique}'.");
                    }

                    var child = new TreeNode(unique, raw);
                    node.Children[unique] = child;
                    node = child;
                    uniquePath.Add(node.Name);
                }

                var leafBase = SanitizeName(parts[^1]);
                var taken = new HashSet<string>(node.Children.Keys.Concat(node.Leaves.Select(l => l.Name)), StringComparer.Ordinal);
                var leafName = MakeUnique(leafBase, taken);
                if (leafName != leafBase)
                    Debug.LogWarning($"[Prefabs] Prefab name collision '{parts[^1]}' in '{string.Join("/", uniquePath)}', renamed to '{leafName}'.");

                node.Leaves.Add(new Leaf(leafName, e));
                dotMap[e.ResourcePath] = uniquePath.Count > 0
                    ? "Prefabs." + string.Join(".", uniquePath) + "." + leafName
                    : "Prefabs." + leafName;
            }

            return (root, dotMap);
        }

        static string MakeUnique(string baseName, HashSet<string> used)
        {
            if (used.Add(baseName)) return baseName;
            for (int i = 1; ; i++)
            {
                var candidate = $"{baseName}_{i}";
                if (used.Add(candidate)) return candidate;
            }
        }

        // Empty-safe, digit-safe, keyword-safe.
        static string SanitizeName(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "_";
            var sb = new StringBuilder(raw.Length);
            foreach (var c in raw)
                sb.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');
            var s = sb.ToString();
            if (s.Length == 0) return "_";
            if (char.IsDigit(s[0])) s = "_" + s;
            if (s.Length > 0 && Keywords.Contains(s)) s += "_";
            return s;
        }

        static readonly HashSet<string> Keywords = new HashSet<string>(StringComparer.Ordinal)
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char",
            "checked", "class", "const", "continue", "decimal", "default", "delegate",
            "do", "double", "else", "enum", "event", "explicit", "extern", "false",
            "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit",
            "in", "int", "interface", "internal", "is", "lock", "long", "namespace",
            "new", "null", "object", "operator", "out", "override", "params", "private",
            "protected", "public", "readonly", "ref", "return", "sbyte", "sealed",
            "short", "sizeof", "stackalloc", "static", "string", "struct", "switch",
            "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked",
            "unsafe", "ushort", "using", "virtual", "void", "volatile", "while"
        };

        static string Indent(int depth) => new string(' ', depth * 4);

        static string Escape(string s)
            => s != null ? s.Replace("\\", "\\\\").Replace("\"", "\\\"") : string.Empty;

        static void WriteFile(string fileName, StringBuilder sb)
        {
            var path = Path.Combine(OutputPath, fileName);
            var content = sb.ToString();
            if (File.Exists(path) && File.ReadAllText(path, Encoding.UTF8) == content) return;
            File.WriteAllText(path, content, Encoding.UTF8);
        }

        readonly struct Entry
        {
            public readonly string Guid;
            public readonly string ResourcePath;
            public Entry(string g, string p) { Guid = g; ResourcePath = p; }
        }

        readonly struct Collected
        {
            public readonly string Guid;
            public readonly string FullPath;
            public readonly string ResourcePath;
            public Collected(string g, string f, string p) { Guid = g; FullPath = f; ResourcePath = p; }
        }

        class TreeNode
        {
            public string                       Name;
            public string                       RawName;
            public Dictionary<string, TreeNode> Children = new Dictionary<string, TreeNode>(StringComparer.Ordinal);
            public List<Leaf>                   Leaves   = new List<Leaf>();
            public TreeNode(string name, string raw) { Name = name; RawName = raw; }
        }

        readonly struct Leaf
        {
            public readonly string Name;
            public readonly Entry  Entry;
            public Leaf(string name, Entry entry) { Name = name; Entry = entry; }
        }
    }
}
