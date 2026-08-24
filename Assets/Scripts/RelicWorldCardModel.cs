using UnityEngine;

/// <summary>
/// Builds a lightweight 3D relic card directly in Unity using primitive meshes.
/// No texture/image is required. The card uses rarity color for glow/frame accents.
/// </summary>
public static class RelicWorldCardModel
{
    public static GameObject Create(Vector3 position, RelicRarity rarity, bool bossReward)
    {
        GameObject root = new GameObject(bossReward ? "BossRelicCardDrop" : "RelicCardDrop");
        root.transform.position = position;
        root.transform.rotation = Quaternion.Euler(12f, 18f, 0f);
        root.transform.localScale = Vector3.one * (bossReward ? 1.15f : 0.95f);

        BoxCollider rootCollider = root.AddComponent<BoxCollider>();
        rootCollider.center = Vector3.zero;
        rootCollider.size = new Vector3(1.05f, 1.42f, 0.22f);

        Color rarityColor = RelicManager.GetRarityColor(rarity);
        Color darkBody = new Color(0.035f, 0.055f, 0.085f, 1f);
        Color gold = new Color(1f, 0.62f, 0.12f, 1f);

        CreateBox(root.transform, "CardBody", new Vector3(0f, 0f, 0f), new Vector3(0.95f, 1.28f, 0.14f), darkBody, false);
        CreateBox(root.transform, "InnerPanel", new Vector3(0f, 0f, -0.085f), new Vector3(0.76f, 1.06f, 0.035f), new Color(0.055f, 0.09f, 0.14f, 1f), false);

        // Raised frame pieces make the pickup read as a physical card rather than a flat billboard.
        CreateBox(root.transform, "FrameTop", new Vector3(0f, 0.61f, -0.10f), new Vector3(0.92f, 0.08f, 0.055f), rarityColor, true);
        CreateBox(root.transform, "FrameBottom", new Vector3(0f, -0.61f, -0.10f), new Vector3(0.92f, 0.08f, 0.055f), rarityColor, true);
        CreateBox(root.transform, "FrameLeft", new Vector3(-0.44f, 0f, -0.10f), new Vector3(0.08f, 1.16f, 0.055f), rarityColor, true);
        CreateBox(root.transform, "FrameRight", new Vector3(0.44f, 0f, -0.10f), new Vector3(0.08f, 1.16f, 0.055f), rarityColor, true);

        // Corner caps for a collectible-card silhouette.
        CreateBox(root.transform, "CornerTL", new Vector3(-0.38f, 0.55f, -0.13f), new Vector3(0.16f, 0.16f, 0.08f), gold, true);
        CreateBox(root.transform, "CornerTR", new Vector3(0.38f, 0.55f, -0.13f), new Vector3(0.16f, 0.16f, 0.08f), gold, true);
        CreateBox(root.transform, "CornerBL", new Vector3(-0.38f, -0.55f, -0.13f), new Vector3(0.16f, 0.16f, 0.08f), gold, true);
        CreateBox(root.transform, "CornerBR", new Vector3(0.38f, -0.55f, -0.13f), new Vector3(0.16f, 0.16f, 0.08f), gold, true);

        // Center relic crystal made from a rotated cube so it reads clearly from the game camera.
        GameObject crystal = CreateBox(root.transform, "RelicCore", new Vector3(0f, 0f, -0.18f), new Vector3(0.34f, 0.34f, 0.11f), rarityColor, true);
        crystal.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

        GameObject halo = CreateCylinder(root.transform, "CoreHalo", new Vector3(0f, 0f, -0.15f), new Vector3(0.52f, 0.035f, 0.52f), gold, true);
        halo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        return root;
    }

    private static GameObject CreateBox(Transform parent, string name, Vector3 localPos, Vector3 localScale, Color color, bool emission)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = localScale;
        RemoveCollider(go);
        ApplyMaterial(go.GetComponent<Renderer>(), color, emission);
        return go;
    }

    private static GameObject CreateCylinder(Transform parent, string name, Vector3 localPos, Vector3 localScale, Color color, bool emission)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = localScale;
        RemoveCollider(go);
        ApplyMaterial(go.GetComponent<Renderer>(), color, emission);
        return go;
    }

    private static void RemoveCollider(GameObject go)
    {
        Collider col = go.GetComponent<Collider>();
        if (col != null)
        {
            if (Application.isPlaying) Object.Destroy(col);
            else Object.DestroyImmediate(col);
        }
    }

    private static void ApplyMaterial(Renderer renderer, Color color, bool emission)
    {
        if (renderer == null) return;

        Material mat = renderer.material;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        else if (mat.HasProperty("_Color")) mat.color = color;

        if (emission && mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 1.8f);
        }
    }
}
