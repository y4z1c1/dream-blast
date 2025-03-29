using UnityEngine;
using System.IO;
using System.Linq;

// level parser is a class that parses the level data from a JSON file.
public class LevelParser : MonoBehaviour
{
    // path to the levels folder within Resources
    [SerializeField] private string levelsPath = "Levels";

    // load a specific level by number
    public LevelData LoadLevel(int levelNumber)
    {
        // format the file name (e.g., "level_01.json")
        string fileName = string.Format("level_{0:D2}", levelNumber);

        // load the JSON file from Resources folder
        TextAsset levelFile = Resources.Load<TextAsset>(Path.Combine(levelsPath, fileName));

        if (levelFile == null)
        {
            Debug.LogError($"Level file not found: {fileName}");
            return null;
        }

        // parse JSON into LevelData object
        LevelData levelData = JsonUtility.FromJson<LevelData>(levelFile.text);

        if (levelData == null)
        {
            Debug.LogError($"Failed to parse level data from: {fileName}");
            return null;
        }

        Debug.Log($"Loaded level {levelNumber}: {levelData.grid_width}x{levelData.grid_height}, {levelData.move_count} moves");
        return levelData;
    }

    // get the total number of available levels
    public int GetTotalLevelCount()
    {
        // dynamically count level files in resources folder
        TextAsset[] levelFiles = Resources.LoadAll<TextAsset>(levelsPath)
            .Where(asset => asset.name.StartsWith("level_") && asset.name.Contains(".json"))
            .ToArray();

        // log the count for debugging
        Debug.Log($"found {levelFiles.Length} level files in {levelsPath}");

        return levelFiles.Length > 0 ? levelFiles.Length : 10; // fallback to 10 if no files found
    }
}