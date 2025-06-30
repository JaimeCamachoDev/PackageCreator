using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text.RegularExpressions;

public class PackageCreatorWindow : EditorWindow
{
    private string description = "Descripción del paquete";

    [MenuItem("Tools/JaimeCamachoDev/Crear Nuevo Package")]
    public static void OpenWindow()
    {
        GetWindow<PackageCreatorWindow>("Crear Package");
    }

    [MenuItem("Assets/JaimeCamachoDev/Crear Nuevo Package")]
    public static void ContextCreatePackage()
    {
        OpenWindow();
    }

    private void OnGUI()
    {
        GUILayout.Label("Crear nuevo Package", EditorStyles.boldLabel);

        description = EditorGUILayout.TextField("Descripción del paquete", description);

        if (GUILayout.Button("Crear Package desde nombre del proyecto"))
        {
            CreatePackage();
        }
    }

    private void CreatePackage()
    {
        // Obtener nombre real del proyecto desde la ruta raíz
        string projectPath = Directory.GetParent(Application.dataPath).FullName;
        string projectName = Path.GetFileName(projectPath);
        string projectId = Regex.Replace(projectName.ToLower(), @"[^a-z0-9]", "");

        string basePath = $"Packages/com.jaimecamacho.{projectId}";
        string templatePath = "Packages/com.jaimecamacho.packagecreator/Templates/package.json.template";

        if (!File.Exists(templatePath))
        {
            EditorUtility.DisplayDialog("Error", $"No se encontró el archivo '{templatePath}'", "OK");
            return;
        }

        string templateText = File.ReadAllText(templatePath);

        string finalJson = templateText
            .Replace("__PROJECT_NAME__", projectName)
            .Replace("__projectid__", projectId)
            .Replace("__DESCRIPTION__", description);

        Directory.CreateDirectory(basePath);
        File.WriteAllText(Path.Combine(basePath, "package.json"), finalJson);
        AssetDatabase.Refresh();

        Debug.Log($"✅ Paquete creado correctamente en: {basePath}");
    }

}
