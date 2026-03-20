using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public class PackageReleaseManagerWindow : EditorWindow
{
    private const string CreatorPackageName = "com.jaimecamacho.packagecreator";
    private const string UnityPathPrefKey = "JaimeCamachoDev.PackageCreator.UnityPath";
    private const string OutputDirectoryPrefKey = "JaimeCamachoDev.PackageCreator.OutputDirectory";

    private string[] packageNames = Array.Empty<string>();
    private int selectedPackageIndex;
    private string version = string.Empty;
    private string unityPath = string.Empty;
    private string outputDirectory = "ReleaseArtifacts";
    private string changelogLine1 = string.Empty;
    private string changelogLine2 = string.Empty;
    private string changelogLine3 = string.Empty;
    private Vector2 scroll;

    [MenuItem("Tools/JaimeCamachoDev/Package Release Manager")]
    public static void OpenWindow()
    {
        GetWindow<PackageReleaseManagerWindow>("Package Release");
    }

    public static void OpenWindowForPackage(string packageName)
    {
        var window = GetWindow<PackageReleaseManagerWindow>("Package Release");
        window.RefreshPackages(packageName);
        window.Focus();
    }

    private void OnEnable()
    {
        unityPath = EditorPrefs.GetString(UnityPathPrefKey, string.Empty);
        outputDirectory = EditorPrefs.GetString(OutputDirectoryPrefKey, "ReleaseArtifacts");
        RefreshPackages(null);
    }

    private void OnFocus()
    {
        RefreshPackages(GetSelectedPackageName());
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.LabelField("UPM Package Release Manager", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Use this window to review release files, prepare the package, sign the tgz and publish it to npm.",
            MessageType.Info);

        if (packageNames.Length == 0)
        {
            EditorGUILayout.HelpBox("No generated packages were found under Packages/.", MessageType.Warning);
            if (GUILayout.Button("Refresh"))
            {
                RefreshPackages(null);
            }

            EditorGUILayout.EndScrollView();
            return;
        }

        selectedPackageIndex = EditorGUILayout.Popup("Package", selectedPackageIndex, packageNames);

        var packageName = packageNames[selectedPackageIndex];
        var packagePath = $"Packages/{packageName}";
        var packageJsonPath = Path.Combine(packagePath, "package.json");
        var packageVersion = ReadPackageVersion(packageJsonPath);
        if (string.IsNullOrWhiteSpace(version))
        {
            version = packageVersion;
        }

        EditorGUILayout.LabelField("Package Path", packagePath);
        version = EditorGUILayout.TextField("Release Version", version);
        unityPath = EditorGUILayout.TextField("Unity Path", unityPath);
        outputDirectory = EditorGUILayout.TextField("Output Directory", outputDirectory);

        EditorPrefs.SetString(UnityPathPrefKey, unityPath);
        EditorPrefs.SetString(OutputDirectoryPrefKey, outputDirectory);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Changelog Notes", EditorStyles.boldLabel);
        changelogLine1 = EditorGUILayout.TextField("Note 1", changelogLine1);
        changelogLine2 = EditorGUILayout.TextField("Note 2", changelogLine2);
        changelogLine3 = EditorGUILayout.TextField("Note 3", changelogLine3);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("1. Review Files", EditorStyles.boldLabel);
        if (GUILayout.Button("Open README.md"))
        {
            OpenExternalFile("README.md");
        }

        if (GUILayout.Button("Open CHANGELOG.md"))
        {
            OpenExternalFile("CHANGELOG.md");
        }

        if (GUILayout.Button("Ping Package Folder"))
        {
            PingAsset(packagePath);
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("2. Prepare Metadata", EditorStyles.boldLabel);
        if (GUILayout.Button("Prepare Release"))
        {
            RunPowerShellScript("scripts/Prepare-UpmRelease.ps1",
                $"-Version {Quote(version)} -PackagePath {Quote(packagePath)}{BuildChangelogNotesArguments()} -CreateChangelogEntry");
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("3. Sign Package", EditorStyles.boldLabel);
        if (GUILayout.Button("Sign in Unity (Automatic)"))
        {
            if (string.IsNullOrWhiteSpace(unityPath))
            {
                EditorUtility.DisplayDialog("Package Release", "Unity Path is required for automatic signing.", "OK");
            }
            else
            {
                RunPowerShellScript("scripts/Sign-UpmPackage.ps1",
                    $"-UnityPath {Quote(unityPath)} -PackagePath {Quote(packagePath)} -OutputDirectory {Quote(outputDirectory)}");
            }
        }

        if (GUILayout.Button("Manual Sign Instructions"))
        {
            RunPowerShellScript("scripts/Release-UpmPackage.ps1",
                $"-Version {Quote(version)} -PackagePath {Quote(packagePath)} -OutputDirectory {Quote(outputDirectory)} -ManualSign -SkipPrepare -SkipPublish");
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("4. Publish", EditorStyles.boldLabel);
        if (GUILayout.Button("Publish Signed Tarball"))
        {
            RunPowerShellScript("scripts/Publish-SignedTarball.ps1",
                $"-Version {Quote(version)} -PackagePath {Quote(packagePath)} -OutputDirectory {Quote(outputDirectory)} -LoginIfNeeded");
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("All-In-One", EditorStyles.boldLabel);
        if (GUILayout.Button("Prepare + Manual Sign + Publish"))
        {
            RunPowerShellScript("scripts/Release-UpmPackage.ps1",
                $"-Version {Quote(version)} -PackagePath {Quote(packagePath)} -OutputDirectory {Quote(outputDirectory)} -ManualSign{BuildChangelogNotesArguments()}");
        }

        if (GUILayout.Button("Prepare + Auto Sign + Publish"))
        {
            if (string.IsNullOrWhiteSpace(unityPath))
            {
                EditorUtility.DisplayDialog("Package Release", "Unity Path is required for automatic signing.", "OK");
            }
            else
            {
                RunPowerShellScript("scripts/Release-UpmPackage.ps1",
                    $"-Version {Quote(version)} -UnityPath {Quote(unityPath)} -PackagePath {Quote(packagePath)} -OutputDirectory {Quote(outputDirectory)}{BuildChangelogNotesArguments()}");
            }
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "Git tag and GitHub release are still separate. Recommended after npm publish: git add ., git commit -m \"Release x.y.z\", git tag vx.y.z, git push origin main, git push origin vx.y.z.",
            MessageType.None);

        EditorGUILayout.EndScrollView();
    }

    private void RefreshPackages(string preferredPackageName)
    {
        packageNames = Directory
            .GetDirectories("Packages", "com.*", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Where(name => !string.Equals(name, CreatorPackageName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name)
            .ToArray();

        if (packageNames.Length == 0)
        {
            selectedPackageIndex = 0;
            version = string.Empty;
            return;
        }

        if (!string.IsNullOrWhiteSpace(preferredPackageName))
        {
            var preferredIndex = Array.IndexOf(packageNames, preferredPackageName);
            if (preferredIndex >= 0)
            {
                selectedPackageIndex = preferredIndex;
            }
        }
        else
        {
            selectedPackageIndex = Mathf.Clamp(selectedPackageIndex, 0, packageNames.Length - 1);
        }

        var packageJsonPath = Path.Combine("Packages", packageNames[selectedPackageIndex], "package.json");
        version = ReadPackageVersion(packageJsonPath);
    }

    private string GetSelectedPackageName()
    {
        if (packageNames.Length == 0 || selectedPackageIndex < 0 || selectedPackageIndex >= packageNames.Length)
        {
            return null;
        }

        return packageNames[selectedPackageIndex];
    }

    private static string ReadPackageVersion(string packageJsonPath)
    {
        if (!File.Exists(packageJsonPath))
        {
            return string.Empty;
        }

        var package = JsonUtility.FromJson<PackageJsonVersionOnly>(File.ReadAllText(packageJsonPath));
        return package?.version ?? string.Empty;
    }

    private string BuildChangelogNotesArguments()
    {
        var notes = new[] { changelogLine1, changelogLine2, changelogLine3 }
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => Quote(line.Trim()))
            .ToArray();

        if (notes.Length == 0)
        {
            return string.Empty;
        }

        return $" -ChangelogNotes {string.Join(" ", notes)}";
    }

    private static void RunPowerShellScript(string relativeScriptPath, string arguments)
    {
        var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrWhiteSpace(projectRoot))
        {
            EditorUtility.DisplayDialog("Package Release", "Failed to resolve the Unity project root.", "OK");
            return;
        }

        var scriptPath = Path.Combine(projectRoot, relativeScriptPath);
        if (!File.Exists(scriptPath))
        {
            EditorUtility.DisplayDialog("Package Release", $"Missing script: {scriptPath}", "OK");
            return;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoExit -ExecutionPolicy Bypass -File {Quote(scriptPath)} {arguments}",
            WorkingDirectory = projectRoot,
            UseShellExecute = true
        };

        Process.Start(startInfo);
    }

    private static string Quote(string value)
    {
        return $"\"{value.Replace("\"", "\\\"")}\"";
    }

    private static void OpenExternalFile(string relativePath)
    {
        var fullPath = Path.Combine(Directory.GetParent(Application.dataPath)?.FullName ?? ".", relativePath);
        if (File.Exists(fullPath))
        {
            InternalEditorUtility.OpenFileAtLineExternal(fullPath, 1);
        }
        else
        {
            EditorUtility.DisplayDialog("Package Release", $"File not found: {fullPath}", "OK");
        }
    }

    private static void PingAsset(string assetPath)
    {
        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
        if (asset != null)
        {
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }
    }

    [Serializable]
    private class PackageJsonVersionOnly
    {
        public string version;
    }
}
