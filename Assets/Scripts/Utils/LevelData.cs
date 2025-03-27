using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LevelData
{
    public int level_number;
    public int grid_width;
    public int grid_height;
    public int move_count;
    public List<string> grid;

    // helper method to get grid item at specific position
    public string GetGridItem(int x, int y)
    {
        // convert 2D coordinates to 1D index (starting from bottom left)
        int index = y * grid_width + x;

        // check if index is valid
        if (index >= 0 && index < grid.Count)
        {
            return grid[index];
        }

        // return empty for invalid indices
        return "empty";
    }
}