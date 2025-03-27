using UnityEngine;
using System.Collections;

public class StoneObstacle : Obstacle
{
    [Header("Stone Specific Settings")]
    [SerializeField] private Color destructionFlashColor = new Color(0.6f, 0.6f, 0.6f, 1f);

    protected override void Awake()
    {
        // set stone-specific defaults
        maxHealth = 1; // stone has 1 health

        // call base Awake for common initialization
        base.Awake();
    }

    // stone obstacles DO NOT take damage from matches in adjacent cells
    public override bool TakeDamageFromMatch()
    {
        // stone ignores damage from matches
        Debug.Log("Stone obstacle ignores damage from matches");
        return false;
    }

    // stone obstacles take damage ONLY from rockets
    public override bool TakeDamageFromRocket()
    {
        Debug.Log("Stone obstacle taking damage from rocket");
        // stone takes full damage from rockets
        return TakeDamage(1);
    }

    // stone has a special damage animation - it shakes when hit
    protected override IEnumerator PlayDamageAnimation(bool skipParticles = false)
    {
        isAnimating = true;

        // get original position
        Vector3 originalPosition = transform.localPosition;

        // use the animation manager for basic effects
        AnimationManager.Instance.AnimateObstacleDamage(this, !skipParticles);

        // wait for animation
        yield return new WaitForSeconds(0.3f);

        // reset position
        transform.localPosition = originalPosition;

        isAnimating = false;
    }

    // customized destruction with stone crumbling effect
    protected override IEnumerator PlayDestructionAnimation()
    {
        isAnimating = true;

        // use AnimationManager with stone-specific particles
        AnimationManager.Instance.AnimateObstacleDestruction(this);

        // wait for animation
        yield return new WaitForSeconds(0.5f);

        // destroy the game object
        Destroy(gameObject);
    }
}