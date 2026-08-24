using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Layered URP dimensional portal inspired by the technical structure of the reference:
/// dark core + hollow ring + green spiral + eroded bright spiral + edge wave + sparks.
/// The spiral mesh and UVs are generated procedurally in Unity, so no portal image is required.
/// </summary>
public class EnemySpawnPortal : MonoBehaviour
{
    [Header("Portal Shape")]
    [Min(0.5f)] public float radius = 2.4f;
    [Range(0.8f, 1.8f)] public float verticalScale = 1.28f;
    [Range(24, 128)] public int angularSegments = 72;
    [Range(4, 28)] public int radialSegments = 14;
    [Range(0f, 2.5f)] public float meshTwistTurns = 0.72f;
    [Range(0f, 0.8f)] public float centerDepth = 0.28f;
    [Range(-0.2f, 0.2f)] public float visualDepthOffset = 0f;

    [Header("Spiral Motion")]
    [Min(0f)] public float swirlSpeed = 1.0f;
    [Range(1f, 10f)] public float swirlStrength = 5.5f;
    [Range(0f, 0.2f)] public float edgeWobble = 0.075f;

    [Header("Layer Intensity")]
    [Range(0.1f, 10f)] public float darkCoreEmission = 0.85f;
    [Range(0.1f, 10f)] public float ringEmission = 2.2f;
    [Range(0.1f, 10f)] public float greenSpiralEmission = 2.45f;
    [Range(0.1f, 10f)] public float brightSpiralEmission = 4.8f;
    [Range(0.1f, 10f)] public float edgeWaveEmission = 3.2f;

    [Header("Erosion")]
    [Range(0.2f, 10f)] public float greenErosion = 1.7f;
    [Range(0.2f, 10f)] public float brightErosion = 5.4f;
    [Range(0.2f, 8f)] public float maskErosion = 1.25f;

    [Header("Reference Green Palette")]
    public bool useReferenceGreenPreset = true;
    [ColorUsage(true, true)] public Color darkColor = new Color(0.002f, 0.12f, 0.006f, 1f);
    [ColorUsage(true, true)] public Color greenColor = new Color(0.025f, 1.65f, 0.01f, 1f);
    [ColorUsage(true, true)] public Color limeColor = new Color(0.34f, 4.8f, 0.015f, 1f);
    [ColorUsage(true, true)] public Color highlightColor = new Color(2.8f, 7.0f, 0.65f, 1f);

    [Header("Glow Light")]
    [Min(0f)] public float lightIntensity = 4.5f;
    [Min(0.1f)] public float lightRange = 8f;

    [Header("Edge Sparks")]
    [Range(0, 100)] public int particlesPerSecond = 20;
    [Min(0.1f)] public float particleLifetime = 0.65f;
    [Min(0.01f)] public float particleSize = 0.075f;

    private const string GeneratedRootName = "GeneratedPortalVisual";

    private Mesh spiralMesh;
    private readonly List<Material> runtimeMaterials = new List<Material>();
    private readonly List<MeshRenderer> layerRenderers = new List<MeshRenderer>();
    private ParticleSystem portalParticles;
    private Light portalLight;

    private void Awake()
    {
        ApplyReferencePresetIfEnabled();
        BuildIfNeeded();
    }

    private void OnEnable()
    {
        BuildIfNeeded();
    }

    private void Update()
    {
        if (portalLight != null)
        {
            float pulse = 0.92f + Mathf.Sin(Time.unscaledTime * Mathf.Max(0.1f, swirlSpeed) * 2.2f) * 0.08f;
            portalLight.intensity = lightIntensity * pulse;
        }
    }

    private void OnValidate()
    {
        ApplyReferencePresetIfEnabled();
        if (!Application.isPlaying && transform.Find(GeneratedRootName) != null)
            Rebuild();
    }

    [ContextMenu("Apply Green Portal Preset")]
    public void ApplyGreenReferencePreset()
    {
        useReferenceGreenPreset = true;
        ApplyReferencePresetIfEnabled();
        Rebuild();
    }

    [ContextMenu("Rebuild Portal Visual")]
    public void Rebuild()
    {
        ClearGeneratedChildren();
        DestroyRuntimeResources();
        BuildIfNeeded();
    }

    private void ApplyReferencePresetIfEnabled()
    {
        if (!useReferenceGreenPreset) return;
        darkColor = new Color(0.002f, 0.12f, 0.006f, 1f);
        greenColor = new Color(0.025f, 1.65f, 0.01f, 1f);
        limeColor = new Color(0.34f, 4.8f, 0.015f, 1f);
        highlightColor = new Color(2.8f, 7.0f, 0.65f, 1f);
    }

    private void BuildIfNeeded()
    {
        Transform existing = transform.Find(GeneratedRootName);
        if (existing != null && layerRenderers.Count > 0) return;

        if (existing != null)
        {
            if (Application.isPlaying) Destroy(existing.gameObject);
            else DestroyImmediate(existing.gameObject);
        }

        layerRenderers.Clear();
        runtimeMaterials.Clear();

        GameObject root = new GameObject(GeneratedRootName);
        root.transform.SetParent(transform, false);
        root.transform.localPosition = new Vector3(0f, 0f, visualDepthOffset);

        spiralMesh = CreateSpiralMesh();

        Shader shader = Shader.Find("TowerDefense/EnemySpawnPortalSwirl");
        if (shader == null)
        {
            Debug.LogError("EnemySpawnPortal: TowerDefense/EnemySpawnPortalSwirl shader not found.");
            return;
        }

        CreateLayer(root.transform, "DarkBackground", shader, 0, -0.030f, 0.92f,
            darkColor, darkColor, greenColor, 0.85f, 1.0f, darkCoreEmission, new Vector2(0f, -0.10f), -3);

        CreateLayer(root.transform, "OuterRing", shader, 1, -0.020f, 1.04f,
            greenColor, limeColor, highlightColor, 1.4f, 1.0f, ringEmission, new Vector2(0.08f, -0.20f), -2);

        CreateLayer(root.transform, "GreenSpiral", shader, 2, 0.000f, 1.00f,
            greenColor, limeColor, highlightColor, greenErosion, maskErosion, greenSpiralEmission, new Vector2(0.04f, -0.62f), 1);

        CreateLayer(root.transform, "BrightSpiral", shader, 3, -0.010f, 1.012f,
            limeColor, highlightColor, highlightColor, brightErosion, maskErosion, brightSpiralEmission, new Vector2(-0.03f, -1.05f), 2);

        CreateLayer(root.transform, "EdgeWave", shader, 4, -0.018f, 1.055f,
            limeColor, highlightColor, highlightColor, 3.5f, 1.0f, edgeWaveEmission, new Vector2(0.12f, -0.38f), 3);

        CreateParticles(root.transform);
        CreateLight(root.transform);
    }

    private Mesh CreateSpiralMesh()
    {
        int aSeg = Mathf.Clamp(angularSegments, 24, 128);
        int rSeg = Mathf.Clamp(radialSegments, 4, 28);
        int vertsPerRing = aSeg + 1;

        Vector3[] vertices = new Vector3[(rSeg + 1) * vertsPerRing];
        Vector2[] uvs = new Vector2[vertices.Length];
        Color[] colors = new Color[vertices.Length];
        int[] triangles = new int[rSeg * aSeg * 6];

        int vi = 0;
        for (int r = 0; r <= rSeg; r++)
        {
            float radial01 = r / (float)rSeg; // 0 center -> 1 edge
            float inward = 1f - radial01;
            float twist = inward * meshTwistTurns * Mathf.PI * 2f;
            float z = -centerDepth * inward * inward;

            for (int a = 0; a <= aSeg; a++)
            {
                float u = a / (float)aSeg;
                float angle = u * Mathf.PI * 2f + twist;
                float wobble = 1f + Mathf.Sin(angle * 7f) * edgeWobble * radial01 * 0.35f;
                float rr = radial01 * wobble;

                vertices[vi] = new Vector3(Mathf.Cos(angle) * rr, Mathf.Sin(angle) * rr, z);

                // Critical UV layout: outer edge = V 0, center = V 1.
                // Scrolling negative Y therefore reads as motion from edge toward center.
                uvs[vi] = new Vector2(u, inward);
                colors[vi] = Color.white;
                vi++;
            }
        }

        int ti = 0;
        for (int r = 0; r < rSeg; r++)
        {
            int row = r * vertsPerRing;
            int next = (r + 1) * vertsPerRing;
            for (int a = 0; a < aSeg; a++)
            {
                int i0 = row + a;
                int i1 = row + a + 1;
                int i2 = next + a;
                int i3 = next + a + 1;

                triangles[ti++] = i0;
                triangles[ti++] = i2;
                triangles[ti++] = i1;
                triangles[ti++] = i1;
                triangles[ti++] = i2;
                triangles[ti++] = i3;
            }
        }

        Mesh mesh = new Mesh { name = "Runtime_PortalSpiralMesh" };
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.colors = colors;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private void CreateLayer(
        Transform parent,
        string name,
        Shader shader,
        int mode,
        float zOffset,
        float scaleMultiplier,
        Color colorA,
        Color colorB,
        Color highlight,
        float erosion,
        float layerMaskErosion,
        float emission,
        Vector2 scroll,
        int sortingOrder)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, 0f, zOffset);
        go.transform.localScale = new Vector3(radius * scaleMultiplier, radius * verticalScale * scaleMultiplier, 1f);

        MeshFilter filter = go.AddComponent<MeshFilter>();
        filter.sharedMesh = spiralMesh;

        MeshRenderer renderer = go.AddComponent<MeshRenderer>();
        renderer.sortingOrder = sortingOrder;

        Material mat = new Material(shader) { name = "Runtime_" + name };
        mat.SetColor("_ColorA", colorA);
        mat.SetColor("_ColorB", colorB);
        mat.SetColor("_HighlightColor", highlight);
        mat.SetColor("_DarkColor", darkColor);
        mat.SetFloat("_Speed", swirlSpeed * (mode == 3 ? 1.25f : mode == 4 ? 0.75f : 1f));
        mat.SetFloat("_SwirlStrength", swirlStrength + (mode == 3 ? 0.9f : 0f));
        mat.SetFloat("_Erosion", erosion);
        mat.SetFloat("_MaskErosion", layerMaskErosion);
        mat.SetFloat("_EdgeWobble", edgeWobble);
        mat.SetFloat("_EmissionStrength", emission);
        mat.SetFloat("_Alpha", mode == 0 ? 0.96f : mode == 3 ? 0.72f : mode == 4 ? 0.74f : 0.92f);
        mat.SetFloat("_LayerMode", mode);
        mat.SetVector("_Scroll", new Vector4(scroll.x, scroll.y, 0f, 0f));

        renderer.sharedMaterial = mat;
        runtimeMaterials.Add(mat);
        layerRenderers.Add(renderer);
    }

    private void CreateParticles(Transform parent)
    {
        GameObject go = new GameObject("PortalEdgeSparks");
        go.transform.SetParent(parent, false);

        portalParticles = go.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = portalParticles.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(particleLifetime * 0.7f, particleLifetime * 1.25f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.03f, 0.18f);
        main.startSize = new ParticleSystem.MinMaxCurve(particleSize * 0.55f, particleSize * 1.45f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.45f, 1f, 0.02f, 1f), new Color(1f, 1f, 0.55f, 1f));
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        ParticleSystem.EmissionModule emission = portalParticles.emission;
        emission.rateOverTime = particlesPerSecond;

        ParticleSystem.ShapeModule shape = portalParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius * 0.93f;
        shape.radiusThickness = 0.10f;
        shape.scale = new Vector3(1f, verticalScale, 1f);

        ParticleSystem.VelocityOverLifetimeModule velocity = portalParticles.velocityOverLifetime;
        velocity.enabled = true;
        // All orbital axes deliberately use TwoConstants mode to avoid Unity's mode mismatch warning.
        velocity.orbitalX = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.orbitalY = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.orbitalZ = new ParticleSystem.MinMaxCurve(-0.45f, 0.45f);
        velocity.radial = new ParticleSystem.MinMaxCurve(-0.08f, 0.02f);

        ParticleSystemRenderer renderer = portalParticles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = 4;

        Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (particleShader == null) particleShader = Shader.Find("Particles/Standard Unlit");
        if (particleShader != null)
        {
            Material particleMat = new Material(particleShader) { name = "Runtime_PortalSparks" };
            if (particleMat.HasProperty("_BaseColor")) particleMat.SetColor("_BaseColor", highlightColor);
            if (particleMat.HasProperty("_Color")) particleMat.SetColor("_Color", highlightColor);
            renderer.sharedMaterial = particleMat;
            runtimeMaterials.Add(particleMat);
        }
    }

    private void CreateLight(Transform parent)
    {
        GameObject go = new GameObject("PortalLight");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, 0f, 0.45f);

        portalLight = go.AddComponent<Light>();
        portalLight.type = LightType.Point;
        portalLight.color = new Color(0.30f, 1f, 0.035f);
        portalLight.range = lightRange;
        portalLight.intensity = lightIntensity;
        portalLight.shadows = LightShadows.None;
    }

    private void ClearGeneratedChildren()
    {
        Transform child = transform.Find(GeneratedRootName);
        if (child == null) return;
        if (Application.isPlaying) Destroy(child.gameObject);
        else DestroyImmediate(child.gameObject);
    }

    private void DestroyRuntimeResources()
    {
        if (spiralMesh != null)
        {
            if (Application.isPlaying) Destroy(spiralMesh);
            else DestroyImmediate(spiralMesh);
            spiralMesh = null;
        }

        foreach (Material mat in runtimeMaterials)
        {
            if (mat == null) continue;
            if (Application.isPlaying) Destroy(mat);
            else DestroyImmediate(mat);
        }

        runtimeMaterials.Clear();
        layerRenderers.Clear();
        portalParticles = null;
        portalLight = null;
    }

    private void OnDestroy()
    {
        DestroyRuntimeResources();
    }
}
