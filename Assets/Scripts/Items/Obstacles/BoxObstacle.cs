using UnityEngine;
using System.Collections;

// box obstacle is a class that represents a box obstacle in the grid.
public class BoxObstacle : Obstacle
{
    [Header("Box Specific Settings")]
    [SerializeField] private Color destructionFlashColor = new Color(0.8f, 0.6f, 0.3f, 1f);

    protected override void Awake()
    {
        maxHealth = 1; // box has 1 health

        base.Awake();
    }

    // box obstacles take damage from matches in adjacent cells
    public bool CheckForAdjacentMatch(Vector2Int matchPosition)
    {
        Vector2Int myPosition = GetGridPosition();

        // check if the match is adjacent (horizontally or vertically)
        bool isAdjacent =
            (Mathf.Abs(matchPosition.x - myPosition.x) == 1 && matchPosition.y == myPosition.y) || // horizontally adjacent
            (Mathf.Abs(matchPosition.y - myPosition.y) == 1 && matchPosition.x == myPosition.x);   // Vertically adjacent

        // if adjacent, take damage
        if (isAdjacent)
        {
            return TakeDamageFromMatch();
        }

        return false;
    }

    // override to provide box-specific behavior for match damage
    public override bool TakeDamageFromMatch()
    {
        // boxes take full damage from matches
        return TakeDamage(1);
    }

    // override to provide box-specific behavior for rocket damage
    public override bool TakeDamageFromRocket()
    {
        // boxes take full damage from rockets
        return TakeDamage(1);
    }

    // box damage animation - override to customize appearance
    protected override void OnDamageTaken(bool willBeDestroyed = false)
    {
        // call base method to set the damaged sprite
        base.OnDamageTaken(willBeDestroyed);

    }
}