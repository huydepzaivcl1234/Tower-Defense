using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Tracks gold, lives and run end state.</summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Starting Values")]
    public int startingGold = 200;
    public int startingLives = 20;

    [Header("Win Reward")]
    [Tooltip("Persistent Diamonds granted when all waves are cleared. 0 disables the reward.")]
    [Min(0)] public int winDiamondReward = 20;
    public bool countWinDiamondsInRunTotal = true;
    public bool showWinDiamondToast = false;

    public int CurrentGold { get; private set; }
    public int CurrentLives { get; private set; }
    public bool IsGameOver { get; private set; }
    public bool HasWon { get; private set; }
    public int LastWinDiamondRewardGranted { get; private set; }

    public static event System.Action<int> OnGoldChanged;
    public static event System.Action<int> OnLivesChanged;
    public static event System.Action<int> OnGoldSpent;
    public static event System.Action OnGameOver;
    public static event System.Action OnGameWon;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        CurrentGold = startingGold;
        CurrentLives = startingLives;
        LastWinDiamondRewardGranted = 0;
    }

    private void Start()
    {
        PlayerProfileManager.Instance?.BeginRun();
        OnGoldChanged?.Invoke(CurrentGold);
        OnLivesChanged?.Invoke(CurrentLives);
    }

    public bool SpendGold(int amount)
    {
        if (amount <= 0) return true;
        if (amount > CurrentGold) return false;
        CurrentGold -= amount;
        OnGoldChanged?.Invoke(CurrentGold);
        OnGoldSpent?.Invoke(amount);
        return true;
    }

    public int AddGold(int amount, bool applyRelicBonus = true)
    {
        int granted = amount;
        if (applyRelicBonus && RelicManager.Instance != null)
            granted = RelicManager.Instance.ApplyGoldGain(amount);

        CurrentGold += granted;
        OnGoldChanged?.Invoke(CurrentGold);
        return granted;
    }

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

        LastWinDiamondRewardGranted = 0;
        if (winDiamondReward > 0 && PlayerProfileManager.Instance != null)
        {
            LastWinDiamondRewardGranted = PlayerProfileManager.Instance.AddDiamonds(
                winDiamondReward,
                countWinDiamondsInRunTotal,
                showWinDiamondToast);
        }

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
