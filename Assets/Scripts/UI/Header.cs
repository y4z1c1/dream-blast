using UnityEngine;

// header is a class that represents the header of the game.
public class Header : MonoBehaviour
{
    private AnimationManager animationManager;
    private bool hasAnimated = false;

    // start is called once before the first execution of update after the monobehaviour is created
    void Start()
    {
        // get animation manager reference
        animationManager = AnimationManager.Instance;

        // perform appearance animation
        AnimateAppearance();
    }

    private void OnEnable()
    {
        // if this isn't the first time (component was disabled and re-enabled)
        if (hasAnimated && animationManager != null)
        {
            // reset position for new animation
            AnimateAppearance();
        }
    }

    private void AnimateAppearance()
    {
        // use animation manager if available
        if (animationManager.AreUIAnimationsEnabled())
        {
            // animate header from top
            animationManager.AnimateHeaderAppearance(
                transform,       // header transform
                1f,            // duration
                10f,              // offset from top
                () => { hasAnimated = true; }  // on complete callback
            );
        }
        else
        {
            // fallback if animation manager not available
            Debug.LogWarning("AnimationManager not found for header animation");
            hasAnimated = true;
        }
    }
}
