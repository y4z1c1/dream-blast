using System.Collections;
using UnityEngine;

// rocket creator is a simple class that creates rockets in the grid.
public static class RocketCreator
{
    // get rocket prefab with error handling
    private static GameObject GetRocketPrefab()
    {
        return Resources.Load<GameObject>("Prefabs/Rocket");
    }

    public static void CreateRocket(Vector2Int position, GridManager gridManager)
    {
        // randomly decide if horizontal or vertical
        bool isHorizontal = Random.value > 0.5f;

        // get reference to the rocket prefab with better error handling
        GameObject rocketPrefab = GetRocketPrefab();

        if (rocketPrefab == null)
        {
            Debug.LogError("Failed to load rocket prefab. Check Resources folder structure.");
            return;
        }

        // get the cell at the position
        GridCell cell = gridManager.GetCell(position.x, position.y);
        if (cell == null)
        {
            Debug.LogError($"Cannot create rocket: No cell at position {position}");
            return;
        }

        // get the world position for the rocket
        Vector3 rocketPosition = cell.transform.position;

        // ensure cell is clear before placing the rocket
        if (!cell.IsEmpty())
        {
            GridItem existingItem = cell.GetItem();
            if (existingItem != null)
            {
                // check if existing item is not a cube being combined
                if (!(existingItem is Cube))
                {
                    Debug.LogWarning($"Creating rocket at position {position}, but cell already contains {existingItem.GetType().Name}");
                }

                // clear the cell
                cell.ClearItem();

                // destroy the existing item
                GameObject.Destroy(existingItem.gameObject);
            }
        }

        // create the rocket
        GameObject rocketObj = GameObject.Instantiate(rocketPrefab, rocketPosition, Quaternion.identity);

        // get the rocket component
        Rocket rocket = rocketObj.GetComponent<Rocket>();
        if (rocket == null)
        {
            Debug.LogError("Instantiated rocket prefab does not contain a Rocket component!");
            GameObject.Destroy(rocketObj);
            return;
        }

        // set the direction
        rocket.SetDirection(isHorizontal ? Rocket.RocketDirection.Horizontal : Rocket.RocketDirection.Vertical);

        // apply the correct sprite based on direction
        rocket.ApplyDirectionSprite();

        // find the level controller
        LevelController levelController = GameObject.FindFirstObjectByType<LevelController>();

        // initialize the rocket
        rocket.Initialize(position.x, position.y, gridManager, levelController);

        // set the rocket in the cell
        cell.SetItem(rocket);

        Debug.Log($"Created {(isHorizontal ? "horizontal" : "vertical")} rocket at {position}");

    }
}