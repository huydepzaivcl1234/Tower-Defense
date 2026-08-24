using UnityEngine;

/// <summary>
/// Shared compact number formatter for gameplay/UI values.
/// Examples: 999, 1K, 1.25K, 2.5M, 3.1B, ...
/// Supports very large float values and falls back to scientific notation beyond the suffix table.
/// </summary>
public static class CompactNumber
{
    private static readonly string[] Suffixes =
    {
        "", "K", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc", "No", "Dc"
    };

    public static string Format(float value, int maxDecimals = 2)
    {
        if (float.IsNaN(value)) return "0";
        if (float.IsPositiveInfinity(value)) return "∞";
        if (float.IsNegativeInfinity(value)) return "-∞";

        float abs = Mathf.Abs(value);
        if (abs < 1000f)
            return FormatPlain(value, maxDecimals);

        int suffixIndex = 0;
        double scaled = value;
        while (System.Math.Abs(scaled) >= 1000d && suffixIndex < Suffixes.Length - 1)
        {
            scaled /= 1000d;
            suffixIndex++;
        }

        if (suffixIndex == Suffixes.Length - 1 && System.Math.Abs(scaled) >= 1000d)
            return value.ToString("0.##E+0");

        string pattern;
        double absScaled = System.Math.Abs(scaled);
        if (absScaled >= 100d || maxDecimals <= 0) pattern = "0";
        else if (absScaled >= 10d || maxDecimals == 1) pattern = "0.#";
        else pattern = "0.##";

        return scaled.ToString(pattern) + Suffixes[suffixIndex];
    }

    public static string Format(int value) => Format((float)value, 2);

    private static string FormatPlain(float value, int maxDecimals)
    {
        if (Mathf.Approximately(value, Mathf.Round(value)))
            return Mathf.RoundToInt(value).ToString();

        if (maxDecimals <= 0) return value.ToString("0");
        if (maxDecimals == 1) return value.ToString("0.#");
        return value.ToString("0.##");
    }
}
