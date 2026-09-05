using System;
using System.Collections.Generic;

/// <summary>
/// Persistent meta-progression/profile data. Per-run Gold/Lives remain outside this payload.
/// Keep future shop/unlock/profile fields here so persistent state stays under one save architecture.
/// </summary>
[Serializable]
public class PlayerProfileData
{
    public int saveVersion = 2;

    // Player identity. Avatar stores an index into the designer-authored avatar list on PlayerProfilePanel.
    public string playerName = "Player";
    public int avatarIndex = 0;

    // Persistent currency / lifetime stats.
    public int diamonds = 0;
    public double totalPlaySeconds = 0d;
    public long totalEnemiesKilled = 0L;

    // Permanent Shop upgrades. Missing fields in older JSON saves safely default to zero.
    public int diamondDropChanceUpgradeLevel = 0;

    // Sequential Story progression. Level 1 is always available on new and older saves.
    public int highestUnlockedStoryLevel = 1;

    // Reserved for shop/progression systems.
    public List<string> purchasedShopItemIds = new List<string>();
    public List<string> unlockedContentIds = new List<string>();

    public void Sanitize(int maxDiamonds, int maxPlayerNameLength = 24)
    {
        diamonds = Math.Max(0, Math.Min(diamonds, Math.Max(0, maxDiamonds)));
        totalPlaySeconds = Math.Max(0d, totalPlaySeconds);
        totalEnemiesKilled = Math.Max(0L, totalEnemiesKilled);
        avatarIndex = Math.Max(0, avatarIndex);
        diamondDropChanceUpgradeLevel = Math.Max(0, Math.Min(diamondDropChanceUpgradeLevel, 10));
        highestUnlockedStoryLevel = Math.Max(1, highestUnlockedStoryLevel);

        if (string.IsNullOrWhiteSpace(playerName))
            playerName = "Player";

        playerName = playerName.Trim();
        int safeNameLength = Math.Max(1, maxPlayerNameLength);
        if (playerName.Length > safeNameLength)
            playerName = playerName.Substring(0, safeNameLength);

        if (purchasedShopItemIds == null) purchasedShopItemIds = new List<string>();
        if (unlockedContentIds == null) unlockedContentIds = new List<string>();
    }
}
