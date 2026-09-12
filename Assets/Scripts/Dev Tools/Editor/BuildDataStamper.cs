using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System;
using System.IO;
public class BuildDataStamper : IPreprocessBuildWithReport  
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        string resourcesPath = "Assets/Resources";
        if (!Directory.Exists(resourcesPath))
            Directory.CreateDirectory(resourcesPath);

        string assetPath = resourcesPath + "/BuildInfo.asset";

        BuildInfo info = AssetDatabase.LoadAssetAtPath<BuildInfo>(assetPath);
        if (info == null)
        {
            info = ScriptableObject.CreateInstance<BuildInfo>();
            AssetDatabase.CreateAsset(info, assetPath);
        }

        info.buildDate = DateTime.Today.ToShortDateString();

        EditorUtility.SetDirty(info);
        AssetDatabase.SaveAssets();
    }
}
