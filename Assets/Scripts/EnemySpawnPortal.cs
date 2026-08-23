using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Procedural red dimensional portal used as the visual source of enemy spawns.
/// Built entirely at runtime so no texture asset is required. Intended for URP.
/// </summary>
public class EnemySpawnPortal : MonoBehaviour
{
    [Header("Shape")]
    [Min(0.5f)] public float radius = 2.4f;
    [Range(2, 6)] public int ringCount = 4;
    [Range(24, 96)] public int segments = 64;
    [Range(0f, 0.35f)] public float distortion = 0.12f;
    [Min(0.01f)] public float ringWidth = 0.11f;

    [Header("Motion")]
    public float rotationSpeed = 35f;
    public float pulseSpeed = 2.4f;
    [Range(0f, 0.35f)] public float pulseAmount = 0.10f;

    [Header("Color / Glow")]
    [ColorUsage(true, true)] public Color outerColor = new Color(5.5f, 0.05f, 0.02f, 1f);
    [ColorUsage(true, true)] public Color innerColor = new Color(10f, 0.15f, 0.04f, 1f);
    [Min(0f)] public float lightIntensity = 5f;
    [Min(0.1f)] public float lightRange = 8f;

    [Header("Particles")]
    [Range(0, 120)] public int particlesPerSecond = 32;
    [Min(0.1f)] public float particleLifetime = 0.7f;
    [Min(0.01f)] public float particleSize = 0.09f;

    private readonly List<Transform> ringRoots = new List<Transform>();
    private readonly List<LineRenderer> rings = new List<LineRenderer>();
    private Material ringMaterial;
    private Light portalLight;
    private Transform core;

    private void Awake()
    {
        BuildIfNeeded();
    }

    private void OnEnable()
    {
        BuildIfNeeded();
    }

    private void Update()
    {
        if (rings.Count == 0) return;

        float now = Time.unscaledTime;
        for (int i = 0; i < ringRoots.Count; i++)
        {
            Transform ring = ringRoots[i];
            if (ring == null) continue;
            float dir = (i & 1) == 0 ? 1f : -1f;
            ring.localRotation = Quaternion.Euler(0f, 0f, now * rotationSpeed * dir * (1f + i * 0.22f));

            LineRenderer lr = rings[i];
            if (lr != null)
            {
                float pulse = 1f + Mathf.Sin(now * pulseSpeed + i * 1.37f) * pulseAmount;
                lr.widthMultiplier = ringWidth * pulse * (1f - i * 0.08f);
            }
        }

        if (core != null)
        {
            float s = 1f + Mathf.Sin(now * pulseSpeed * 0.85f) * pulseAmount * 0.45f;
            core.localScale = new Vector3(radius * 1.15f * s, radius * 1.15f * s, 1f);
        }

        if (portalLight != null)
            portalLight.intensity = lightIntensity * (0.88f + 0.12f * Mathf.Sin(now * pulseSpeed));
    }

    [ContextMenu("Rebuild Portal Visual")]
    public void Rebuild()
    {
        ClearGeneratedChildren();
        ringRoots.Clear();
        rings.Clear();
        BuildIfNeeded();
    }

    private void BuildIfNeeded()
    {
        Transform existing = transform.Find("GeneratedPortalVisual");
        if (existing != null && rings.Count > 0) return;
        if (existing != null) Destroy(existing.gameObject);

        GameObject visual = new GameObject("GeneratedPortalVisual");
        visual.transform.SetParent(transform, false);

        EnsureMaterial();
        CreateCore(visual.transform);

        int actualRings = Mathf.Clamp(ringCount, 2, 6);
        for (int i = 0; i < actualRings; i++)
            CreateEnergyRing(visual.transform, i, actualRings);

        CreateParticles(visual.transform);
        CreateLight(visual.transform);
    }

    private void EnsureMaterial()
    {
        if (ringMaterial != null) return;
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        ringMaterial = new Material(shader) { name = "Runtime_RedSpawnPortal" };
        if (ringMaterial.HasProperty("_BaseColor")) ringMaterial.SetColor("_BaseColor", innerColor);
        if (ringMaterial.HasProperty("_Color")) ringMaterial.SetColor("_Color", innerColor);
    }

    private void CreateCore(Transform parent)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "PortalCore";
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = new Vector3(radius * 1.15f, radius * 1.15f, 1f);
        Destroy(go.GetComponent<Collider>());

        Renderer renderer = go.GetComponent<Renderer>();
        Material mat = new Material(ringMaterial) { name = "Runtime_RedPortalCore" };
        Color coreColor = new Color(innerColor.r * 0.16f, innerColor.g * 0.06f, innerColor.b * 0.04f, 1f);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", coreColor);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", coreColor);
        renderer.material = mat;
        core = go.transform;
    }

    private void CreateEnergyRing(Transform parent, int index, int total)
    {
        GameObject root = new GameObject("EnergyRing_" + (index + 1));
        root.transform.SetParent(parent, false);
        root.transform.localPosition = new Vector3(0f, 0f, -0.015f * index);

        LineRenderer lr = root.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.positionCount = Mathf.Max(24, segments);
        lr.material = new Material(ringMaterial);
        lr.textureMode = LineTextureMode.Stretch;
        lr.numCornerVertices = 2;
        lr.numCapVertices = 2;
        lr.alignment = LineAlignment.TransformZ;

        float tIndex = total <= 1 ? 0f : index / (float)(total - 1);
        float baseRadius = radius * Mathf.Lerp(1.03f, 0.58f, tIndex);
        lr.startColor = Color.Lerp(outerColor, innerColor, tIndex);
        lr.endColor = lr.startColor;
        lr.widthMultiplier = ringWidth * (1f - index * 0.08f);

        int count = lr.positionCount;
        float phase = index * 1.731f;
        for (int i = 0; i < count; i++)
        {
            float a = i / (float)count * Mathf.PI * 2f;
            float wobble = 1f
                + Mathf.Sin(a * (5 + index) + phase) * distortion
                + Mathf.Sin(a * 9f - phase * 0.6f) * distortion * 0.35f;
            float r = baseRadius * wobble;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f));
        }

        ringRoots.Add(root.transform);
        rings.Add(lr);
    }

    private void CreateParticles(Transform parent)
    {
        GameObject go = new GameObject("PortalSparks");
        go.transform.SetParent(parent, false);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = particleLifetime;
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.65f);
        main.startSize = new ParticleSystem.MinMaxCurve(particleSize * 0.6f, particleSize * 1.4f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.03f, 0.01f, 1f), new Color(1f, 0.32f, 0.02f, 1f));
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = particlesPerSecond;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius;
        shape.radiusThickness = 0.15f;
        shape.rotation = Vector3.zero;

        ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
        velocity.enabled = true;

        // Unity requires orbital X/Y/Z curves to use the same MinMaxCurve mode.
        // Keep all three in TwoConstants mode: X/Y stay at zero, Z provides the swirl.
        velocity.orbitalX = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.orbitalY = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.orbitalZ = new ParticleSystem.MinMaxCurve(-1.2f, 1.2f);
        velocity.radial = new ParticleSystem.MinMaxCurve(-0.35f, 0.15f);

        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(ringMaterial);
    }

    private void CreateLight(Transform parent)
    {
        GameObject go = new GameObject("PortalLight");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, 0f, 0.45f);
        portalLight = go.AddComponent<Light>();
        portalLight.type = LightType.Point;
        portalLight.color = new Color(1f, 0.035f, 0.01f);
        portalLight.range = lightRange;
        portalLight.intensity = lightIntensity;
        portalLight.shadows = LightShadows.None;
    }

    private void ClearGeneratedChildren()
    {
        Transform child = transform.Find("GeneratedPortalVisual");
        if (child == null) return;
        if (Application.isPlaying) Destroy(child.gameObject);
        else DestroyImmediate(child.gameObject);
    }
}
