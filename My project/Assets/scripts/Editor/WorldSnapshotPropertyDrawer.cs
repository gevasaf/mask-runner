using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom property drawer for WorldSnapshot that shows a dropdown menu of available worlds
/// </summary>
[CustomPropertyDrawer(typeof(AudioMixerSnapshotManager.WorldSnapshot))]
public class WorldSnapshotPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        // Get the worldIndex property
        SerializedProperty worldIndexProp = property.FindPropertyRelative("worldIndex");
        SerializedProperty snapshotProp = property.FindPropertyRelative("snapshot");
        
        if (worldIndexProp == null || snapshotProp == null)
        {
            EditorGUI.LabelField(position, label.text, "WorldSnapshot properties not found");
            EditorGUI.EndProperty();
            return;
        }
        
        // Find GameManager to get world list
        GameManager gameManager = FindGameManager();
        string[] worldNames = GetWorldNames(gameManager);
        
        // Calculate rects
        Rect labelRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        Rect dropdownRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + 2, position.width, EditorGUIUtility.singleLineHeight);
        Rect snapshotRect = new Rect(position.x, position.y + (EditorGUIUtility.singleLineHeight + 2) * 2, position.width, EditorGUIUtility.singleLineHeight);
        
        // Draw foldout label
        property.isExpanded = EditorGUI.Foldout(labelRect, property.isExpanded, label, true);
        
        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            
            // Draw world dropdown
            int currentIndex = worldIndexProp.intValue;
            // Clamp index to valid range
            if (currentIndex < 0 || currentIndex >= worldNames.Length)
            {
                currentIndex = 0;
            }
            int newIndex = EditorGUI.Popup(dropdownRect, "World", currentIndex, worldNames);
            
            if (newIndex != currentIndex && newIndex >= 0 && newIndex < worldNames.Length)
            {
                worldIndexProp.intValue = newIndex;
            }
            
            // Draw snapshot field
            EditorGUI.PropertyField(snapshotRect, snapshotProp, new GUIContent("Snapshot"));
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUI.EndProperty();
    }
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
        {
            return EditorGUIUtility.singleLineHeight;
        }
        
        // Height for: label + dropdown + snapshot field + spacing
        return EditorGUIUtility.singleLineHeight * 3 + 4;
    }
    
    /// <summary>
    /// Finds GameManager in the scene or project
    /// </summary>
    private GameManager FindGameManager()
    {
        // Try to find in active scene first
        GameManager gm = Object.FindObjectOfType<GameManager>();
        if (gm != null)
        {
            return gm;
        }
        
        // Try to find in all loaded scenes
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
            if (scene.isLoaded)
            {
                foreach (var rootObj in scene.GetRootGameObjects())
                {
                    gm = rootObj.GetComponentInChildren<GameManager>();
                    if (gm != null)
                    {
                        return gm;
                    }
                }
            }
        }
        
        // Try to find in prefabs or scene assets
        string[] guids = AssetDatabase.FindAssets("t:GameObject");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                gm = prefab.GetComponent<GameManager>();
                if (gm == null)
                {
                    gm = prefab.GetComponentInChildren<GameManager>();
                }
                if (gm != null)
                {
                    return gm;
                }
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Gets array of world names from GameManager
    /// </summary>
    private string[] GetWorldNames(GameManager gameManager)
    {
        if (gameManager == null)
        {
            return new string[] { "No GameManager found" };
        }
        
        // Access worlds array - it's public in GameManager
        WorldData[] worlds = gameManager.worlds;
        
        if (worlds == null || worlds.Length == 0)
        {
            return new string[] { "No worlds configured" };
        }
        
        string[] names = new string[worlds.Length];
        for (int i = 0; i < worlds.Length; i++)
        {
            if (worlds[i] != null)
            {
                names[i] = $"{i}: {worlds[i].displayName}";
            }
            else
            {
                names[i] = $"{i}: (null)";
            }
        }
        
        return names;
    }
}
