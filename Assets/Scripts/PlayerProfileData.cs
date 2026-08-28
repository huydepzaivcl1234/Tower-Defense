using System;
using System.Collections.Generic;

/// <summary>
/// Persistent meta-progression data. This is intentionally separate from per-run state such as Gold/Lives.
/// Add future shop/unlock/profile fields here so all persistent data stays in one save payload.
/// </summary>
[Serializable]
public class PlayerProfileData
{
    public int saveVersion = 1;
    public int diamonds = 0;

    // Reserved for the future shop/progression system. Keeping these in the profile now avoids
    // needing a second save architecture later.
    public List<string> purchasedShopItemIds = new List<string>();
    public List<string> unlockedContentIds = new List<string>();

    public void Sanitize(int maxDiamonds)
    {
        diamonds = Math.Max(0, Math.Min(diamonds, Math.Max(0, maxDiamonds)));
        if (purchasedShopItemIds == null) purchasedShopItemIds = new List<string>();
        if (unlockedContentIds == null) unlockedContentIds = new List<string>();
    }
}
