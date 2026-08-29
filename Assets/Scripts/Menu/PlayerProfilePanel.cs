using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Main-menu player profile presentation. All visual references and avatar choices are designer-authored.
/// Persistent values come from PlayerProfileManager; this component owns only UI presentation/editing.
/// </summary>
[DisallowMultipleComponent]
public class PlayerProfilePanel : MonoBehaviour
{
    [Header("References")]
    public Image avatarImage;
    public TMP_InputField playerNameInput;
    public TMP_Text playTimeText;
    public TMP_Text diamondsText;
    public TMP_Text enemiesKilledText;
    public Button previousAvatarButton;
    public Button nextAvatarButton;
    public Button closeButton;

    [Header("Avatar Library")]
    [Tooltip("Designer-authored avatar Sprites. PlayerProfileData stores only the selected index.")]
    public List<Sprite> avatarSprites = new List<Sprite>();
    [Tooltip("If no avatar Sprite exists, keep any Sprite manually authored on Avatar Image instead of clearing it.")]
    public bool preserveManualAvatarWhenLibraryEmpty = true;

    [Header("Text Formatting")]
    public string playTimePrefix = "PLAY TIME  ";
    public string diamondsPrefix = "DIAMONDS  ";
    public string enemiesKilledPrefix = "ENEMIES KILLED  ";
    [Tooltip("Use compact numbers such as 1.2K for Diamonds and kills.")]
    public bool useCompactNumbers = true;
    [Tooltip("Show play time as HH:MM:SS. When disabled, use a compact Xh Ym format.")]
    public bool useClockPlayTimeFormat = true;

    [Header("Behaviour")]
    public bool saveNameOnEndEdit = true;
    public bool wrapAvatarSelection = true;
    [Tooltip("Refresh lifetime time text while the profile page is open. The profile normally opens from main menu, where play time is paused.")]
    [Min(0.1f)] public float visibleRefreshInterval = 0.5f;

    private float nextRefreshAt;

    private void OnEnable()
    {
        PlayerProfileManager.OnProfileLoaded += Refresh;
        PlayerProfileManager.OnProfileIdentityChanged += Refresh;
        PlayerProfileManager.OnProfileStatsChanged += RefreshStats;
        PlayerProfileManager.OnDiamondsChanged += HandleDiamondsChanged;
        PlayerProfileManager.OnProfileReset += Refresh;

        BindButtons();
        Refresh();
        nextRefreshAt = Time.unscaledTime + Mathf.Max(0.1f, visibleRefreshInterval);
    }

    private void OnDisable()
    {
        PlayerProfileManager.OnProfileLoaded -= Refresh;
        PlayerProfileManager.OnProfileIdentityChanged -= Refresh;
        PlayerProfileManager.OnProfileStatsChanged -= RefreshStats;
        PlayerProfileManager.OnDiamondsChanged -= HandleDiamondsChanged;
        PlayerProfileManager.OnProfileReset -= Refresh;

        UnbindButtons();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefreshAt)
            return;

        nextRefreshAt = Time.unscaledTime + Mathf.Max(0.1f, visibleRefreshInterval);
        RefreshStats();
    }

    private void BindButtons()
    {
        if (previousAvatarButton != null)
        {
            previousAvatarButton.onClick.RemoveListener(SelectPreviousAvatar);
            previousAvatarButton.onClick.AddListener(SelectPreviousAvatar);
        }

        if (nextAvatarButton != null)
        {
            nextAvatarButton.onClick.RemoveListener(SelectNextAvatar);
            nextAvatarButton.onClick.AddListener(SelectNextAvatar);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
            closeButton.onClick.AddListener(Close);
        }

        if (playerNameInput != null && saveNameOnEndEdit)
        {
            playerNameInput.onEndEdit.RemoveListener(HandleNameEndEdit);
            playerNameInput.onEndEdit.AddListener(HandleNameEndEdit);
        }
    }

    private void UnbindButtons()
    {
        if (previousAvatarButton != null) previousAvatarButton.onClick.RemoveListener(SelectPreviousAvatar);
        if (nextAvatarButton != null) nextAvatarButton.onClick.RemoveListener(SelectNextAvatar);
        if (closeButton != null) closeButton.onClick.RemoveListener(Close);
        if (playerNameInput != null) playerNameInput.onEndEdit.RemoveListener(HandleNameEndEdit);
    }

    public void Refresh()
    {
        PlayerProfileManager profile = PlayerProfileManager.Instance;
        if (profile == null)
            return;

        if (playerNameInput != null)
            playerNameInput.SetTextWithoutNotify(profile.PlayerName);

        RefreshAvatar(profile.AvatarIndex);
        RefreshStats();
    }

    public void RefreshStats()
    {
        PlayerProfileManager profile = PlayerProfileManager.Instance;
        if (profile == null)
            return;

        if (playTimeText != null)
            playTimeText.text = playTimePrefix + FormatPlayTime(profile.TotalPlaySeconds);

        HandleDiamondsChanged(profile.CurrentDiamonds);

        if (enemiesKilledText != null)
            enemiesKilledText.text = enemiesKilledPrefix + FormatLong(profile.TotalEnemiesKilled);
    }

    private void HandleDiamondsChanged(int value)
    {
        if (diamondsText == null)
            return;

        diamondsText.text = diamondsPrefix + FormatNumber(value);
    }

    public void CommitPlayerName()
    {
        if (playerNameInput == null || PlayerProfileManager.Instance == null)
            return;

        PlayerProfileManager.Instance.SetPlayerName(playerNameInput.text, true);
        playerNameInput.SetTextWithoutNotify(PlayerProfileManager.Instance.PlayerName);
    }

    private void HandleNameEndEdit(string value)
    {
        if (!saveNameOnEndEdit || PlayerProfileManager.Instance == null)
            return;

        PlayerProfileManager.Instance.SetPlayerName(value, true);
        if (playerNameInput != null)
            playerNameInput.SetTextWithoutNotify(PlayerProfileManager.Instance.PlayerName);
    }

    public void SelectPreviousAvatar()
    {
        ChangeAvatar(-1);
    }

    public void SelectNextAvatar()
    {
        ChangeAvatar(1);
    }

    private void ChangeAvatar(int direction)
    {
        PlayerProfileManager profile = PlayerProfileManager.Instance;
        if (profile == null || avatarSprites == null || avatarSprites.Count == 0)
            return;

        int count = avatarSprites.Count;
        int index = Mathf.Clamp(profile.AvatarIndex, 0, count - 1) + direction;

        if (wrapAvatarSelection)
            index = (index % count + count) % count;
        else
            index = Mathf.Clamp(index, 0, count - 1);

        profile.SetAvatarIndex(index, true);
        RefreshAvatar(index);
    }

    private void RefreshAvatar(int index)
    {
        if (avatarImage == null)
            return;

        if (avatarSprites == null || avatarSprites.Count == 0)
        {
            if (!preserveManualAvatarWhenLibraryEmpty)
                avatarImage.sprite = null;
            avatarImage.enabled = avatarImage.sprite != null;
            return;
        }

        int safeIndex = Mathf.Clamp(index, 0, avatarSprites.Count - 1);
        Sprite sprite = avatarSprites[safeIndex];
        if (sprite != null)
            avatarImage.sprite = sprite;

        avatarImage.preserveAspect = true;
        avatarImage.enabled = avatarImage.sprite != null;
    }

    public void Close()
    {
        CommitPlayerName();
        if (MainMenuController.Instance != null)
            MainMenuController.Instance.CloseProfile();
        else
            gameObject.SetActive(false);
    }

    private string FormatNumber(int value)
    {
        return useCompactNumbers ? CompactNumber.Format(Mathf.Max(0, value)) : Mathf.Max(0, value).ToString("N0");
    }

    private string FormatLong(long value)
    {
        long safe = Math.Max(0L, value);
        if (!useCompactNumbers || safe <= int.MaxValue)
            return useCompactNumbers ? CompactNumber.Format((int)Math.Min(safe, int.MaxValue)) : safe.ToString("N0");

        if (safe >= 1_000_000_000L)
            return (safe / 1_000_000_000d).ToString("0.#") + "B";
        if (safe >= 1_000_000L)
            return (safe / 1_000_000d).ToString("0.#") + "M";
        if (safe >= 1_000L)
            return (safe / 1_000d).ToString("0.#") + "K";
        return safe.ToString("N0");
    }

    private string FormatPlayTime(double seconds)
    {
        TimeSpan time = TimeSpan.FromSeconds(Math.Max(0d, seconds));
        if (useClockPlayTimeFormat)
        {
            long totalHours = (long)Math.Floor(time.TotalHours);
            return $"{totalHours:00}:{time.Minutes:00}:{time.Seconds:00}";
        }

        long hours = (long)Math.Floor(time.TotalHours);
        return hours > 0 ? $"{hours}h {time.Minutes}m" : $"{time.Minutes}m {time.Seconds}s";
    }
}
