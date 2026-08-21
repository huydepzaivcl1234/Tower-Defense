#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Adds and tunes TowerFireAnimator on the generated modern-classic tower prefabs.
/// Visual-only: does not change TowerData stats, projectile prefabs, costs or gameplay effects.
/// Safe to run repeatedly.
/// </summary>
public static class TowerFireAnimationSetup
{
    private const string Root = "Assets/TowerPrefabs/GeneratedModernClassic";

    [MenuItem("Tower Defense/Models/Setup Tower Fire Animations")]
    public static void Setup()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { Root });
        int changed = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root == null) continue;

            Tower tower = root.GetComponent<Tower>();
            if (tower == null)
            {
                PrefabUtility.UnloadPrefabContents(root);
                continue;
            }

            TowerFireAnimator fx = root.GetComponent<TowerFireAnimator>();
            if (fx == null) fx = root.AddComponent<TowerFireAnimator>();

            Configure(path.ToLowerInvariant(), fx);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
            changed++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog(
            "Tower Fire Animation",
            "Đã setup hiệu ứng bắn cho " + changed + " tower prefab.\n\n" +
            "Hiệu ứng gồm recoil, punch, idle mechanical motion, muzzle flash particles và muzzle light.\n" +
            "Không thay đổi Damage / Attack Speed / Range / Cost.",
            "OK");
    }

    private static void Configure(string path, TowerFireAnimator fx)
    {
        fx.animatedPart = null;
        fx.idleMotion = true;
        fx.createMuzzleFx = true;

        if (path.Contains("big cannon"))
        {
            fx.style = TowerFireAnimator.FireStyle.HeavyCannon;
            fx.recoilDistance = 0.24f;
            fx.recoilReturnTime = 0.16f;
            fx.scalePunch = 0.085f;
            fx.idleAmplitude = 0.004f;
            fx.particleBurst = 16;
            fx.particleSize = 0.22f;
            fx.muzzleLightIntensity = 6.5f;
            fx.muzzleLightDuration = 0.09f;
            fx.muzzleColor = new Color(1f, 0.42f, 0.08f, 1f);
        }
        else if (path.Contains("cannon") || path.Contains("canon"))
        {
            fx.style = TowerFireAnimator.FireStyle.Cannon;
            fx.recoilDistance = 0.16f;
            fx.recoilReturnTime = 0.12f;
            fx.scalePunch = 0.065f;
            fx.idleAmplitude = 0.005f;
            fx.particleBurst = 12;
            fx.particleSize = 0.17f;
            fx.muzzleLightIntensity = 5f;
            fx.muzzleLightDuration = 0.07f;
            fx.muzzleColor = new Color(1f, 0.48f, 0.08f, 1f);
        }
        else if (path.Contains("bomb"))
        {
            fx.style = TowerFireAnimator.FireStyle.Mortar;
            fx.recoilDistance = 0.18f;
            fx.recoilReturnTime = 0.15f;
            fx.scalePunch = 0.075f;
            fx.idleAmplitude = 0.006f;
            fx.particleBurst = 14;
            fx.particleSize = 0.20f;
            fx.muzzleLightIntensity = 5.5f;
            fx.muzzleLightDuration = 0.08f;
            fx.muzzleColor = new Color(1f, 0.36f, 0.04f, 1f);
        }
        else if (path.Contains("burning"))
        {
            fx.style = TowerFireAnimator.FireStyle.Flame;
            fx.recoilDistance = 0.045f;
            fx.recoilReturnTime = 0.07f;
            fx.scalePunch = 0.025f;
            fx.idleAmplitude = 0.010f;
            fx.idleSpeed = 1.8f;
            fx.particleBurst = 18;
            fx.particleSize = 0.14f;
            fx.muzzleLightIntensity = 4.5f;
            fx.muzzleLightDuration = 0.06f;
            fx.muzzleColor = new Color(1f, 0.28f, 0.02f, 1f);
        }
        else if (path.Contains("ultimate"))
        {
            fx.style = TowerFireAnimator.FireStyle.Energy;
            fx.recoilDistance = 0.12f;
            fx.recoilReturnTime = 0.14f;
            fx.scalePunch = 0.070f;
            fx.idleAmplitude = 0.010f;
            fx.idleSpeed = 1.1f;
            fx.particleBurst = 18;
            fx.particleSize = 0.20f;
            fx.muzzleLightIntensity = 7f;
            fx.muzzleLightDuration = 0.10f;
            fx.muzzleColor = new Color(0.05f, 0.80f, 1f, 1f);
        }
        else if (path.Contains("xbow"))
        {
            fx.style = TowerFireAnimator.FireStyle.Crossbow;
            fx.recoilDistance = 0.085f;
            fx.recoilReturnTime = 0.075f;
            fx.scalePunch = 0.035f;
            fx.idleAmplitude = 0.006f;
            fx.particleBurst = 6;
            fx.particleSize = 0.09f;
            fx.muzzleLightIntensity = 2.4f;
            fx.muzzleLightDuration = 0.04f;
            fx.muzzleColor = new Color(0.10f, 0.85f, 1f, 1f);
        }
        else
        {
            // Archer and other light projectile towers.
            fx.style = TowerFireAnimator.FireStyle.Light;
            fx.recoilDistance = 0.065f;
            fx.recoilReturnTime = 0.065f;
            fx.scalePunch = 0.030f;
            fx.idleAmplitude = 0.006f;
            fx.particleBurst = 5;
            fx.particleSize = 0.075f;
            fx.muzzleLightIntensity = 2f;
            fx.muzzleLightDuration = 0.035f;
            fx.muzzleColor = new Color(0.10f, 0.85f, 1f, 1f);
        }
    }
}
#endif
