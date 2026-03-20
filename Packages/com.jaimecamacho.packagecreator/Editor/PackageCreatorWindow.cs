using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public class PackageCreatorWindow : EditorWindow
{
    private const string CreatorPackageName = "com.jaimecamacho.packagecreator";
    private const string TemplatesRoot = "Packages/com.jaimecamacho.packagecreator/Templates";

    private string packageId = string.Empty;
    private string displayName = string.Empty;
    private string description = "Unity package description";
    private string authorName = "Jaime Camacho";
    private string companyPrefix = "com.jaimecamacho";
    private string version = "1.0.0";
    private string unityVersion = "6000.0";
    private string unityRelease = "0f1";
    private bool includeReleaseScripts = true;
    private bool includeSamplesFolder = true;
    private Vector2 scroll;

    [MenuItem("Tools/JaimeCamachoDev/Package Creator")]
    public static void OpenWindow()
    {
        GetWindow<PackageCreatorWindow>("Package Creator");
    }

    [MenuItem("Assets/JaimeCamachoDev/Package Creator")]
    public static void OpenFromAssets()
    {
        OpenWindow();
    }

    private void OnEnable()
    {
        var projectName = Path.GetFileName(Directory.GetParent(Application.dataPath)?.FullName ?? "UnityProject");
        var normalizedProjectName = Regex.Replace(projectName, @"[^A-Za-z0-9]+", " ").Trim();

        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = normalizedProjectName;
        }

        if (string.IsNullOrWhiteSpace(packageId))
        {
            packageId = NormalizeIdSegment(projectName);
        }
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.LabelField("UPM Package Creator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Create a Unity package scaffold and optional release scripts for prepare, sign and publish.",
            MessageType.Info);

        displayName = EditorGUILayout.TextField("Display Name", displayName);
        packageId = EditorGUILayout.TextField("Package Id", packageId);
        description = EditorGUILayout.TextField("Description", description);
        authorName = EditorGUILayout.TextField("Author", authorName);
        companyPrefix = EditorGUILayout.TextField("Company Prefix", companyPrefix);
        version = EditorGUILayout.TextField("Version", version);
        unityVersion = EditorGUILayout.TextField("Unity", unityVersion);
        unityRelease = EditorGUILayout.TextField("Unity Release", unityRelease);

        EditorGUILayout.Space(8);
        includeReleaseScripts = EditorGUILayout.ToggleLeft("Generate release scripts in project root", includeReleaseScripts);
        includeSamplesFolder = EditorGUILayout.ToggleLeft("Create Samples~ folder", includeSamplesFolder);

        var packageName = GetPackageName();
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Result package", packageName);
        EditorGUILayout.LabelField("Target path", $"Packages/{packageName}");

        EditorGUILayout.Space(12);
        GUI.enabled = !string.IsNullOrWhiteSpace(packageId) && !string.IsNullOrWhiteSpace(displayName);
        if (GUILayout.Button("Create Package"))
        {
            CreatePackage();
        }

        GUI.enabled = true;
        EditorGUILayout.EndScrollView();
    }

    private void CreatePackage()
    {
        try
        {
            var normalizedId = NormalizeIdSegment(packageId);
            if (string.IsNullOrWhiteSpace(normalizedId))
            {
                throw new InvalidOperationException("Package Id cannot be empty.");
            }

            if (!Regex.IsMatch(companyPrefix, @"^[a-z0-9\-]+(\.[a-z0-9\-]+)+$"))
            {
                throw new InvalidOperationException("Company Prefix must be a reverse domain like com.company.");
            }

            if (!Regex.IsMatch(version, @"^\d+\.\d+\.\d+(-[0-9A-Za-z\.-]+)?$"))
            {
                throw new InvalidOperationException("Version must be valid semver.");
            }

            var packageName = $"{companyPrefix}.{normalizedId}";
            if (string.Equals(packageName, CreatorPackageName, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "The generated package name collides with the Package Creator package. " +
                    "Use a different Package Id, for example 'mytool', 'core', 'notes-runtime' or another project-specific name.");
            }

            var packagePath = Path.Combine("Packages", packageName);
            if (Directory.Exists(packagePath))
            {
                throw new InvalidOperationException($"The package folder already exists: {packagePath}");
            }

            Directory.CreateDirectory(packagePath);
            Directory.CreateDirectory(Path.Combine(packagePath, "Runtime"));
            Directory.CreateDirectory(Path.Combine(packagePath, "Editor"));
            Directory.CreateDirectory(Path.Combine(packagePath, "Documentation~"));

            if (includeSamplesFolder)
            {
                Directory.CreateDirectory(Path.Combine(packagePath, "Samples~"));
            }

            WriteTemplate("package.json.template", Path.Combine(packagePath, "package.json"), packageName, normalizedId);
            WriteTemplate("README.md.template", Path.Combine(packagePath, "README.md"), packageName, normalizedId);
            WriteTemplate("CHANGELOG.md.template", Path.Combine(packagePath, "CHANGELOG.md"), packageName, normalizedId);
            WriteTemplate("LICENSE.template", Path.Combine(packagePath, "LICENSE"), packageName, normalizedId);
            WriteTemplate("Documentation.md.template", Path.Combine(packagePath, "Documentation~", "index.md"), packageName, normalizedId);
            WriteTemplate("Runtime.asmdef.template", Path.Combine(packagePath, "Runtime", $"{packageName}.Runtime.asmdef"), packageName, normalizedId);
            WriteTemplate("Editor.asmdef.template", Path.Combine(packagePath, "Editor", $"{packageName}.Editor.asmdef"), packageName, normalizedId);

            if (includeReleaseScripts)
            {
                GenerateReleaseScripts(packageName, normalizedId);
            }

            AssetDatabase.Refresh();
            ShowNextSteps(packagePath);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog("Package Creator", exception.Message, "OK");
        }
    }

    private void GenerateReleaseScripts(string packageName, string normalizedId)
    {
        Directory.CreateDirectory("scripts");

        WriteTemplate("Prepare-UpmRelease.ps1.template", Path.Combine("scripts", "Prepare-UpmRelease.ps1"), packageName, normalizedId);
        WriteTemplate("Sign-UpmPackage.ps1.template", Path.Combine("scripts", "Sign-UpmPackage.ps1"), packageName, normalizedId);
        WriteTemplate("Publish-UpmPackage.ps1.template", Path.Combine("scripts", "Publish-UpmPackage.ps1"), packageName, normalizedId);
        WriteTemplate("Publish-SignedTarball.ps1.template", Path.Combine("scripts", "Publish-SignedTarball.ps1"), packageName, normalizedId);
        WriteTemplate("Release-UpmPackage.ps1.template", Path.Combine("scripts", "Release-UpmPackage.ps1"), packageName, normalizedId);
        WriteTemplate("Test-UpmPackage.ps1.template", Path.Combine("scripts", "Test-UpmPackage.ps1"), packageName, normalizedId);
        WriteTemplate("Release-UpmPackage.bat.template", "Release-UpmPackage.bat", packageName, normalizedId);
        WriteTemplate("Release-UpmPackage-ManualSign.bat.template", "Release-UpmPackage-ManualSign.bat", packageName, normalizedId);
        WriteTemplate("Publish-SignedTarball.bat.template", "Publish-SignedTarball.bat", packageName, normalizedId);

        if (!File.Exists("README.md"))
        {
            File.WriteAllText("README.md", $"# {displayName}{Environment.NewLine}{Environment.NewLine}This repository contains the Unity package `{packageName}`.{Environment.NewLine}");
        }

        if (!File.Exists("CHANGELOG.md"))
        {
            File.WriteAllText(
                "CHANGELOG.md",
                $"# Changelog{Environment.NewLine}{Environment.NewLine}All notable changes to this project will be documented in this file.{Environment.NewLine}{Environment.NewLine}The format is based on Keep a Changelog and this project follows Semantic Versioning.{Environment.NewLine}{Environment.NewLine}## [{version}] - {DateTime.UtcNow:yyyy-MM-dd}{Environment.NewLine}{Environment.NewLine}- Initial package scaffold created with Package Creator.{Environment.NewLine}");
        }

        if (!File.Exists("LICENSE"))
        {
            File.WriteAllText("LICENSE", GetTemplateContent("LICENSE.template", packageName, normalizedId));
        }
    }

    private void WriteTemplate(string templateName, string outputPath, string packageName, string normalizedId)
    {
        File.WriteAllText(outputPath, GetTemplateContent(templateName, packageName, normalizedId));
    }

    private void ShowNextSteps(string packagePath)
    {
        var openFiles = EditorUtility.DisplayDialogComplex(
            "Package Created",
            $"Package created successfully at {packagePath}\n\nNext step:\n1. Review README.md\n2. Review CHANGELOG.md\n3. Add your runtime/editor code\n4. Run the release scripts when you want to publish",
            "Open README + CHANGELOG",
            "Close",
            "Show Package Folder");

        switch (openFiles)
        {
            case 0:
                OpenFileIfExists("README.md");
                OpenFileIfExists("CHANGELOG.md");
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(packagePath);
                EditorGUIUtility.PingObject(Selection.activeObject);
                PackageReleaseManagerWindow.OpenWindowForPackage(Path.GetFileName(packagePath));
                break;
            case 2:
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(packagePath);
                EditorGUIUtility.PingObject(Selection.activeObject);
                PackageReleaseManagerWindow.OpenWindowForPackage(Path.GetFileName(packagePath));
                break;
        }
    }

    private static void OpenFileIfExists(string assetPath)
    {
        var fullPath = Path.GetFullPath(assetPath);
        if (File.Exists(fullPath))
        {
            InternalEditorUtility.OpenFileAtLineExternal(fullPath, 1);
        }
    }

    private string GetTemplateContent(string templateName, string packageName, string normalizedId)
    {
        var templatePath = Path.Combine(TemplatesRoot, templateName);
        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException($"Missing template: {templatePath}");
        }

        return File.ReadAllText(templatePath)
            .Replace("__DISPLAY_NAME__", displayName)
            .Replace("__PACKAGE_NAME__", packageName)
            .Replace("__PACKAGE_ID__", normalizedId)
            .Replace("__DESCRIPTION__", description)
            .Replace("__AUTHOR__", authorName)
            .Replace("__VERSION__", version)
            .Replace("__UNITY_VERSION__", unityVersion)
            .Replace("__UNITY_RELEASE__", unityRelease)
            .Replace("__YEAR__", DateTime.UtcNow.Year.ToString())
            .Replace("__PACKAGE_PATH__", $"Packages/{packageName}");
    }

    private string GetPackageName()
    {
        var normalizedId = NormalizeIdSegment(packageId);
        return string.IsNullOrWhiteSpace(normalizedId) ? companyPrefix : $"{companyPrefix}.{normalizedId}";
    }

    private static string NormalizeIdSegment(string value)
    {
        var lowered = value.Trim().ToLowerInvariant();
        lowered = Regex.Replace(lowered, @"[^a-z0-9\-]+", "-");
        lowered = Regex.Replace(lowered, @"-+", "-");
        return lowered.Trim('-');
    }
}
