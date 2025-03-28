using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class VaseObstacle : Obstacle
{
    [Header("Vase Specific Settings")]
    [SerializeField] private Sprite halfDamagedSprite; // sprite for 1 damage taken
    [SerializeField] private Color destructionFlashColor = new Color(0.2f, 0.6f, 0.9f, 1f);

    // track which match groups have already damaged this vase in a single turn
    private HashSet<int> matchGroupsAppliedDamage = new HashSet<int>();

    protected override void Awake()
    {
        // set vase-specific defaults
        maxHealth = 2; // vase requires 2 damage to be destroyed

        // call base Awake for common initialization
        base.Awake();
    }

    public override void Initialize(GridManager gridManagerRef, LevelController levelControllerRef)
    {
        base.Initialize(gridManagerRef, levelControllerRef);



    }

    // vase obstacles take damage from matches in adjacent cells
    // but only one damage per match group
    public bool CheckForAdjacentMatch(Vector2Int matchPosition, int matchGroupId)
    {
        // if this match group has already damaged this vase, ignore
        if (matchGroupsAppliedDamage.Contains(matchGroupId))
            return false;

        Vector2Int myPosition = GetGridPosition();

        // check if the match is adjacent (horizontally or vertically)
        bool isAdjacent =
            (Mathf.Abs(matchPosition.x - myPosition.x) == 1 && matchPosition.y == myPosition.y) || // horizontally adjacent
            (Mathf.Abs(matchPosition.y - myPosition.y) == 1 && matchPosition.x == myPosition.x);   // Vertically adjacent

        // if adjacent, take damage and record this match group
        if (isAdjacent)
        {
            matchGroupsAppliedDamage.Add(matchGroupId);
            return TakeDamageFromMatch();
        }

        return false;
    }

    // vase takes damage from matches
    public override bool TakeDamageFromMatch()
    {
        if (canInteract)
        {
            Debug.Log("Vase taking damage from match");
            return TakeDamage(1);
        }
        return false;
    }

    // vase takes damage from rockets
    public override bool TakeDamageFromRocket()
    {
        if (canInteract)
        {
            Debug.Log("Vase taking damage from rocket");
            return TakeDamage(1);
        }
        return false;
    }

    // override to provide vase-specific visual damage feedback
    protected override void OnDamageTaken(bool willBeDestroyed = false)
    {
        // if there's a half-damaged sprite and health is 1, show it
        if (halfDamagedSprite != null && currentHealth == 1 && spriteRenderer != null)
        {
            spriteRenderer.sprite = halfDamagedSprite;
        }

        // call base for standard damage animation
        base.OnDamageTaken(willBeDestroyed);
    }

    // vase specific damage animation with crack effect
    protected override IEnumerator PlayDamageAnimation(bool skipParticles = false)
    {
        isAnimating = true;

        // use the AnimationManager for animation
        AnimationManager.Instance.AnimateObstacleDamage(this, !skipParticles);

        // wait for animation to complete
        yield return new WaitForSeconds(0.3f);

        isAnimating = false;
    }

    // vase destruction with shatter effect
    protected override IEnumerator PlayDestructionAnimation()
    {
        isAnimating = true;

        // use the AnimationManager for destruction animation
        AnimationManager.Instance.AnimateObstacleDestruction(this);

        // wait for animation to complete
        yield return new WaitForSeconds(0.5f);

        // Ensure it's destroyed
        Destroy(gameObject);
    }

    // Clear match group tracking at the start of a new turn
    public void ClearMatchGroupTracking()
    {
        matchGroupsAppliedDamage.Clear();
    }
}