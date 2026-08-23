using UnityEngine;

public enum GameAudioType
{
    Music,
    SFX
}

/// <summary>
/// Optional explicit audio category. Add this to an AudioSource when automatic
/// classification (loop = music, non-loop = SFX) is not what you want.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class GameAudioCategory : MonoBehaviour
{
    public GameAudioType type = GameAudioType.SFX;
}
