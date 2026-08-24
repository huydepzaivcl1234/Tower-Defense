using UnityEngine;
using DG.Tweening;

/// <summary>
/// Pre-allocates enough DOTween capacity for combat-heavy waves.
/// This avoids DOTween's runtime auto-resize warnings when many MicroBar animations
/// (HP, shield, heal, splash damage) are active at the same time.
/// </summary>
public static class DOTweenRuntimeConfig
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Configure()
    {
        // DOTween default is only 200 tweeners / 50 sequences.
        // MicroBar can create several tweeners per HP/shield animation, so large waves can
        // legitimately exceed that. Pre-allocate once instead of resizing during gameplay.
        DOTween.SetTweensCapacity(2000, 500);
    }
}
