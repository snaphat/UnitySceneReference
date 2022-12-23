using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "SceneReference", menuName = "ScriptableObjects/SceneReference")]
public class SceneReference : ScriptableObject
{
#if UNITY_EDITOR
    public SceneAsset sceneAsset; // For storing a scene reference in the editor
#endif
    public string cachedName; // For storing a cached scene name
}

#if UNITY_EDITOR
[CustomEditor(typeof(SceneReference))]
public class TestSceneEditor : Editor
{
    SerializedProperty m_asset;
    SerializedProperty m_cachedName;

    // Get serialized object properties (for UI)
    public void OnEnable()
    {
        // Functional properties
        m_asset = serializedObject.FindProperty("sceneAsset");
        m_cachedName = serializedObject.FindProperty("cachedName");
    }

    // Draw inspector GUI
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        {
            using var changeScope = new EditorGUI.ChangeCheckScope();

            // Emit property field in editor for selecting a SceneAsset
            EditorGUILayout.PropertyField(m_asset);
            
            // Get value from serialized SceneAsset field
            var scene = (SceneAsset)m_asset.boxedValue;
            
            // if scene selected then cache name
            if (scene != null) m_cachedName.stringValue = scene.name;

            // apply changes
            if (changeScope.changed) serializedObject.ApplyModifiedProperties();
        }
    }
}

public class SceneReferenceAssetPostprocessor : AssetPostprocessor
{
    // Load all Assets of type T
    public static IEnumerable<(T, string)> FindAssetsByType<T>() where T : Object
    {
        var guids = AssetDatabase.FindAssets($"t:{typeof(T)}");
        foreach (var t in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(t);
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                yield return (asset, path);
            }
        }
    }

    // Hook for any asset changes in project to detect asset name changes to update SceneReferences
    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
    {
        // Find all SceneReference assets
        foreach ((var sceneReference, var path) in FindAssetsByType<SceneReference>())
        {
            // If a SceneAsset has been specified and the cached name of the scene doesn't match, then update it
            if (sceneReference.sceneAsset != null && sceneReference.cachedName != sceneReference.sceneAsset.name)
            {
                // Update cached name of scene and save SceneReference asset
                sceneReference.cachedName = sceneReference.sceneAsset.name;
                EditorUtility.SetDirty(sceneReference);
                AssetDatabase.SaveAssetIfDirty(sceneReference);
                AssetDatabase.Refresh();
            }
            // If a SceneAsset doesn't exist but a cached name exists, this means that previously a scene existed so generate a warning
            else if(sceneReference.sceneAsset == null && sceneReference.cachedName != "")
            {
                Debug.LogWarning(String.Format("Scene missing for SceneReference '<a href=\"{0}>{0}</a>'", path));
            }
        }
    }
}
#endif
