using System.Collections.Generic;
using System.Collections;
using UnityEngine;

// match group is a data structure that represents a group of cubes that match.
// it contains the cubes that are part of the match, the color of the match, and the position of the first cube in the match.
public class MatchGroup
{
    public List<Cube> MatchedCubes { get; private set; }
    public Cube.CubeColor Color { get; private set; }
    public int MatchLength => MatchedCubes.Count;
    public bool IsRocketMatch => MatchLength >= 4;
    public Vector2Int ClickedPosition { get; set; }

    public MatchGroup(List<Cube> cubes, Cube.CubeColor color)
    {
        MatchedCubes = cubes;
        Color = color;
        ClickedPosition = cubes.Count > 0 ? cubes[0].GetGridPosition() : new Vector2Int(-1, -1);
    }

    public void SetRocketIndicator(bool show)
    {
        // exit early if no cubes
        if (MatchedCubes == null || MatchedCubes.Count == 0)
            return;

        // only allow showing indicators if the group is large enough for a rocket
        // this is a safety check in case this method is called incorrectly
        if (show && !IsRocketMatch)
        {
            Debug.LogWarning($"[MatchGroup] Attempted to show rocket indicator for a group with only {MatchLength} cubes (needs 4+)");
            return;
        }

        // set indicator on all cubes in the group
        foreach (Cube cube in MatchedCubes)
        {
            if (cube != null)
            {
                cube.ShowRocketIndicator(show);
            }
        }
    }


    // process destruction with animation - using combine effect for rocket matches
    public List<Vector2Int> ProcessDestruction(bool createRocket = false)
    {
        // check if any cube in the group cannot interact
        foreach (Cube cube in MatchedCubes)
        {
            if (cube != null && !cube.CanInteract)
            {
                Debug.LogWarning($"[MatchGroup] Cannot process destruction - cube at {cube.GetGridPosition()} cannot interact");
                return new List<Vector2Int>();
            }
        }

        // remember the clicked position for rocket creation
        Vector2Int rocketPosition = ClickedPosition;

        // store all positions where obstacles might be affected
        List<Vector2Int> affectedPositions = new List<Vector2Int>();

        // get the animation controller if available
        AnimationManager animationManager = AnimationManager.Instance;

        // add all match positions to the affected list
        foreach (Cube cube in MatchedCubes)
        {
            Vector2Int pos = cube.GetGridPosition();
            affectedPositions.Add(pos);
        }

        // if this is a rocket match (4+ cubes) and we should create a rocket
        if (IsRocketMatch && createRocket)
        {


            if (animationManager != null)
            {

                animationManager.AnimateRocketCombine(MatchedCubes, rocketPosition);
            }
            else
            {
                // no animation controller, perform direct destruction
                foreach (Cube cube in MatchedCubes)
                {
                    Vector2Int pos = cube.GetGridPosition();
                    GridCell cell = cube.GetGridManager().GetCell(pos.x, pos.y);

                    if (cell != null)
                        cell.ClearItem();

                    GameObject.Destroy(cube.gameObject);
                }

                // create rocket directly
                if (MatchedCubes.Count > 0)
                {
                    GridManager gridManager = MatchedCubes[0].GetGridManager();
                    if (gridManager != null)
                    {
                        RocketCreator.CreateRocket(rocketPosition, gridManager);
                    }
                }
            }
        }
        else if (animationManager != null)
        {
            // regular match - use normal destruction animation
            foreach (Cube cube in MatchedCubes)
            {
                Vector2Int pos = cube.GetGridPosition();
                GridCell cell = cube.GetGridManager().GetCell(pos.x, pos.y);

                // clear the cell first
                if (cell != null)
                    cell.ClearItem();

                // animate the cube destruction
                animationManager.AnimateCubeDestruction(cube);
            }
        }
        else
        {
            // no animation controller, destroy immediately
            foreach (Cube cube in MatchedCubes)
            {
                Vector2Int pos = cube.GetGridPosition();
                GridCell cell = cube.GetGridManager().GetCell(pos.x, pos.y);

                if (cell != null)
                    cell.ClearItem();

                GameObject.Destroy(cube.gameObject);
            }
        }

        // return the affected positions for obstacle checking
        return affectedPositions;
    }

    // mark a position as reserved for a rocket to prevent cubes from falling onto it
    private void ReservePositionForRocket(Vector2Int position)
    {
        if (MatchedCubes.Count == 0)
            return;

        GridManager gridManager = MatchedCubes[0].GetGridManager();
        if (gridManager == null)
            return;

        GridCell cell = gridManager.GetCell(position.x, position.y);
        if (cell == null)
            return;


        if (!cell.IsEmpty())
        {
            GridItem existingItem = cell.GetItem();
            if (existingItem != null && !(existingItem is Cube && MatchedCubes.Contains(existingItem as Cube)))
            {
                cell.ClearItem();
                GameObject.Destroy(existingItem.gameObject);
            }
        }
    }

}