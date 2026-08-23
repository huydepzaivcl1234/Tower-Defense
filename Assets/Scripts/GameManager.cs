using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Tracks gold, lives and run end state.</summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Starting Values")]
    public int startingGold = 200;
    public int startingLives = 20;

    public int CurrentGold { get; private set; }
    public int CurrentLives { get; private set; }
    public bool IsGameOver { get; private set; }
    public bool HasWon { get; private set; }

    public static event System.Action<int> OnGoldChanged;
    public static event System.Action<int> OnLivesChanged;
    public static event System.Action OnGameOver;
    public static event System.Action OnGameWon;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        CurrentGold = startingGold;
        CurrentLives = startingLives;
    }

    private void Start()
    {
        OnGoldChanged?.Invoke(CurrentGold);
        OnLivesChanged?.Invoke(CurrentLives);
    }

    public bool SpendGold(int amount)
    {
        if (amount > CurrentGold) return false;
        CurrentGold -= amount;
        OnGoldChanged?.Invoke(CurrentGold);
        return true;
    }

    /// <summary>
    /// Adds earned gold and returns the actual amount granted after relic bonuses.
    /// Set applyRelicBonus=false for refunds/selling so bonus-gold relics cannot multiply refunds.
    /// </summary>
    public int AddGold(int amount, bool applyRelicBonus = true)
    {
        int granted = amount;
        if (applyRelicBonus && RelicManager.Instance != null)
            granted = RelicManager.Instance.ApplyGoldGain(amount);

        CurrentGold += granted;
        OnGoldChanged?.Invoke(CurrentGold);
        return granted;
    }

    /// <summary>Adds lives during the current run (used by permanent run relics).</summary>
    public void AddLives(int amount)
    {
        if (amount <= 0 || IsGameOver) return;
        CurrentLives += amount;
        OnLivesChanged?.Invoke(CurrentLives);
    }

    public void LoseLives(int amount)
    {
        if (IsGameOver) return;
        CurrentLives = Mathf.Max(0, CurrentLives - amount);
        OnLivesChanged?.Invoke(CurrentLives);
        if (CurrentLives <= 0) TriggerGameOver();
    }

    public void HandleAllWavesCleared()
    {
        if (IsGameOver) return;
        HasWon = true;
        IsGameOver = true;
        OnGameWon?.Invoke();
        Time.timeScale = 0f;
    }

    private void TriggerGameOver()
    {
        if (IsGameOver) return;
        IsGameOver = true;
        OnGameOver?.Invoke();
        Time.timeScale = 0f;
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
