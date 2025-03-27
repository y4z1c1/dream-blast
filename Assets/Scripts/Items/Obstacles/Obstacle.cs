using UnityEngine;
using System.Collections;

public abstract class Obstacle : GridItem
{
    // health/durability of the obstacle
    [SerializeField] protected int maxHealth = 1;
    protected int currentHealth;

    // visual feedback
    [SerializeField] protected SpriteRenderer spriteRenderer;
    [SerializeField] protected Sprite damagedSprite;

    // references
    protected new GridManager gridManager;
    protected LevelController levelController;

    protected bool isAnimating = false;

    protected virtual void Awake()
    {
        // initialize health
        currentHealth = maxHealth;

        // get reference to sprite renderer if not assigned
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // initialize the obstacle with grid reference
    public virtual void Initialize(GridManager gridManagerRef, LevelController levelControllerRef)
    {
        gridManager = gridManagerRef;
        levelController = levelControllerRef;

        // register with level controller
        if (levelController != null)
        {
            levelController.RegisterObstacle(this);
        }
    }

    // take damage from a match
    public virtual bool TakeDamageFromMatch()
    {
        // default implementation - take one damage point
        return TakeDamage(1);
    }

    // take damage from a rocket
    public virtual bool TakeDamageFromRocket()
    {
        // default implementation - take one damage point
        return TakeDamage(1);
    }

    // general damage method
    protected virtual bool TakeDamage(int damageAmount)
    {
        // skip if already animating
        if (isAnimating)
            return false;

        currentHealth -= damageAmount;

        // check if destroyed
        bool willBeDestroyed = currentHealth <= 0;

        // visual feedback - skip particles if about to be destroyed
        OnDamageTaken(willBeDestroyed);

        // if destroyed, handle it
        if (willBeDestroyed)
        {
            Destroy();
            return true; // was destroyed
        }

        return false; // Still alive
    }

    // visual feedback when damaged
    protected virtual void OnDamageTaken(bool willBeDestroyed = false)
    {
        // if there's a damaged sprite and health is below max, show it
        if (damagedSprite != null && currentHealth < maxHealth && spriteRenderer != null)
        {
            spriteRenderer.sprite = damagedSprite;
        }

        // play damage animation
        StartCoroutine(PlayDamageAnimation(willBeDestroyed));
    }

    // play damage animation
    protected virtual IEnumerator PlayDamageAnimation(bool skipParticles = false)
    {
        isAnimating = true;

        // use the animation manager to play the damage animation
        AnimationManager.Instance.AnimateObstacleDamage(this, !skipParticles);

        // wait a bit for the animation to complete
        yield return new WaitForSeconds(0.3f);

        isAnimating = false;
    }

    // destroy the obstacle
    protected virtual void Destroy()
    {
        // unregister from level controller
        if (levelController != null)
        {
            levelController.UnregisterObstacle(this);
        }

        // notify grid cell that this obstacle is gone
        Vector2Int pos = GetGridPosition();
        GridCell cell = gridManager.GetCell(pos.x, pos.y);
        if (cell != null)
        {
            cell.ClearItem();
        }

        // play destruction animation
        StartCoroutine(PlayDestructionAnimation());
    }

    // play destruction animation
    protected virtual IEnumerator PlayDestructionAnimation()
    {
        isAnimating = true;

        // use the animation manager to play the destruction animation
        AnimationManager.Instance.AnimateObstacleDestruction(this);

        // wait for the animation to complete
        yield return new WaitForSeconds(0.5f);

        // destroy the game object after animation completes
        Destroy(gameObject);
    }

    // override the OnTap method from GridItem
    public override void OnTap()
    {
        // obstacles don't do anything when tapped directly
    }

    // get the current health
    public int GetHealth()
    {
        return currentHealth;
    }

    // check if this obstacle is still alive
    public bool IsAlive()
    {
        return currentHealth > 0;
    }
}