using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class AuditUnusedAssets
{
    [Serializable] public class Report { public string[] candidates; public string[] retained; public long bytes; }
    public static void Run()
    {
        var paths = AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/") && File.Exists(p)).ToArray();
        var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string p in paths)
        {
            string ext = Path.GetExtension(p).ToLowerInvariant();
            if (p.StartsWith("Assets/Scenes/") || p.Contains("/Resources/") || p.Contains("/StreamingAssets/") ||
                new[] { ".cs", ".dll", ".asmdef", ".asmref", ".asset", ".inputactions", ".shader", ".shadergraph", ".shadersubgraph", ".compute", ".gltf", ".bin", ".mtl" }.Contains(ext)) roots.Add(p);
            if (ext == ".cs")
            {
                foreach (Match m in Regex.Matches(File.ReadAllText(p), "Assets/[^\"\\r\\n]+"))
                    if (File.Exists(m.Value)) roots.Add(m.Value);
            }
        }
        foreach (string settings in Directory.GetFiles("ProjectSettings"))
            foreach (Match m in Regex.Matches(File.ReadAllText(settings), "guid: ([a-fA-F0-9]{32})"))
            {
                string path = AssetDatabase.GUIDToAssetPath(m.Groups[1].Value);
                if (!string.IsNullOrEmpty(path)) roots.Add(path);
            }
        var used = new HashSet<string>(AssetDatabase.GetDependencies(roots.ToArray(), true), StringComparer.OrdinalIgnoreCase);
        string[] removable = { ".prefab", ".mat", ".fbx", ".obj", ".png", ".jpg", ".jpeg", ".tga", ".psd", ".wav", ".mp3", ".ogg", ".unity" };
        var candidates = paths.Where(p => !used.Contains(p) && removable.Contains(Path.GetExtension(p).ToLowerInvariant())).OrderBy(p => p).ToArray();
        var report = new Report { candidates = candidates, retained = used.OrderBy(p => p).ToArray(), bytes = candidates.Sum(p => new FileInfo(p).Length) };
        File.WriteAllText("UnusedAssetsReport.json", JsonUtility.ToJson(report, true));
        Debug.Log("AUDIT COMPLETE: " + candidates.Length + " unused candidates, " + report.bytes + " bytes");
    }
}
