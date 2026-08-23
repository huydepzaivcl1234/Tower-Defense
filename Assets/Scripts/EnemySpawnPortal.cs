using UnityEngine;

/// <summary>
/// Procedural liquid dimensional portal used as the visual source of enemy spawns.
/// The look is generated entirely by shader + particles in Unity; no portal texture is required.
/// </summary>
public class EnemySpawnPortal : MonoBehaviour
{
    [Header("Portal Shape")]
    [Min(0.5f)] public float radius = 2.4f;
    [Range(0.8f, 1.8f)] public float verticalScale = 1.28f;
    [Range(-0.2f, 0.2f)] public float visualDepthOffset = 0f;

    [Header("Liquid Swirl")]
    [Min(0f)] public float swirlSpeed = 1.2f;
    [Range(1f, 10f)] public float swirlStrength = 5.5f;
    [Range(0f, 0.2f)] public float edgeWobble = 0.075f;
    [Range(0.1f, 8f)] public float emissionStrength = 2.2f;

    [Header("Reference Green Style")]
    [Tooltip("Keeps the portal palette close to the bright green liquid-vortex reference. Disable this to customize the four colors below.")]
    public bool useReferenceGreenPreset = true;
    [ColorUsage(true, true)] public Color outerColor = new Color(0.34f, 4.8f, 0.015f, 1f);
    [ColorUsage(true, true)] public Color midColor = new Color(0.025f, 1.65f, 0.01f, 1f);
    [ColorUsage(true, true)] public Color darkCenterColor = new Color(0.002f, 0.12f, 0.006f, 1f);
    [ColorUsage(true, true)] public Color highlightColor = new Color(2.8f, 7.0f, 0.65f, 1f);

    [Header("Glow Light")]
    [Min(0f)] public float lightIntensity = 4.5f;
    [Min(0.1f)] public float lightRange = 8f;

    [Header("Edge Specks")]
    [Range(0, 100)] public int particlesPerSecond = 20;
    [Min(0.1f)] public float particleLifetime = 0.65f;
    [Min(0.01f)] public float particleSize = 0.075f;

    private const string GeneratedRootName = "GeneratedPortalVisual";
    private Renderer portalRenderer;
    private Material portalMaterial;
    private Material particleMaterial;
    private Light portalLight;
    private ParticleSystem portalParticles;

    private void Awake()
    {
        ApplyReferencePresetIfEnabled();
        BuildIfNeeded();
        ApplyVisualSettings();
    }

    private void OnEnable()
    {
        BuildIfNeeded();
        ApplyVisualSettings();
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
        if (!Application.isPlaying)
            ApplyVisualSettings();
    }

    [ContextMenu("Apply Green Liquid Reference Preset")]
    public void ApplyGreenReferencePreset()
    {
        useReferenceGreenPreset = true;
        ApplyReferencePresetIfEnabled();
        ApplyVisualSettings();
    }

    [ContextMenu("Rebuild Portal Visual")]
    public void Rebuild()
    {
        ClearGeneratedChildren();
        portalRenderer = null;
        portalLight = null;
        portalParticles = null;
        BuildIfNeeded();
        ApplyVisualSettings();
    }

    private void ApplyReferencePresetIfEnabled()
    {
        if (!useReferenceGreenPreset) return;

        outerColor = new Color(0.34f, 4.8f, 0.015f, 1f);
        midColor = new Color(0.025f, 1.65f, 0.01f, 1f);
        darkCenterColor = new Color(0.002f, 0.12f, 0.006f, 1f);
        highlightColor = new Color(2.8f, 7.0f, 0.65f, 1f);
    }

    private void BuildIfNeeded()
    {
        Transform existing = transform.Find(GeneratedRootName);
        if (existing != null)
        {
            if (portalRenderer == null)
                portalRenderer = existing.GetComponentInChildren<MeshRenderer>(true);
            if (portalParticles == null)
                portalParticles = existing.GetComponentInChildren<ParticleSystem>(true);
            if (portalLight == null)
                portalLight = existing.GetComponentInChildren<Light>(true);

            if (portalRenderer != null)
            {
                portalMaterial = portalRenderer.material;
                return;
            }
        }

        if (existing != null)
        {
            if (Application.isPlaying) Destroy(existing.gameObject);
            else DestroyImmediate(existing.gameObject);
        }

        GameObject visual = new GameObject(GeneratedRootName);
        visual.transform.SetParent(transform, false);
        visual.transform.localPosition = new Vector3(0f, 0f, visualDepthOffset);

        CreatePortalSurface(visual.transform);
        CreateParticles(visual.transform);
        CreateLight(visual.transform);
    }

    private void CreatePortalSurface(Transform parent)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "LiquidSwirlSurface";
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = new Vector3(radius * 2f, radius * 2f * verticalScale, 1f);

        Collider col = go.GetComponent<Collider>();
        if (col != null)
        {
            if (Application.isPlaying) Destroy(col);
            else DestroyImmediate(col);
        }

        portalRenderer = go.GetComponent<MeshRenderer>();
        Shader shader = Shader.Find("TowerDefense/EnemySpawnPortalSwirl");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        portalMaterial = new Material(shader) { name = "Runtime_LiquidSpawnPortal" };
        portalRenderer.sharedMaterial = portalMaterial;
    }

    private void CreateParticles(Transform parent)
    {
        GameObject go = new GameObject("PortalEdgeSpecks");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;

        portalParticles = go.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = portalParticles.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = particleLifetime;
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.04f, 0.16f);
        main.startSize = new ParticleSystem.MinMaxCurve(particleSize * 0.55f, particleSize * 1.45f);
        main.startColor = new ParticleSystem.MinMaxGradient(Color.white, new Color(0.72f, 1f, 0.15f, 1f));
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        ParticleSystem.EmissionModule emission = portalParticles.emission;
        emission.rateOverTime = particlesPerSecond;

        ParticleSystem.ShapeModule shape = portalParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius * 0.92f;
        shape.radiusThickness = 0.15f;
        shape.scale = new Vector3(1f, verticalScale, 1f);

        ParticleSystem.VelocityOverLifetimeModule velocity = portalParticles.velocityOverLifetime;
        velocity.enabled = true;
        // Unity requires all orbital axes to use the same MinMaxCurve mode.
        velocity.orbitalX = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.orbitalY = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.orbitalZ = new ParticleSystem.MinMaxCurve(-0.55f, 0.55f);
        velocity.radial = new ParticleSystem.MinMaxCurve(-0.05f, 0.04f);

        ParticleSystemRenderer renderer = portalParticles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (particleShader == null) particleShader = Shader.Find("Particles/Standard Unlit");
        if (particleShader == null) particleShader = Shader.Find("Sprites/Default");

        particleMaterial = new Material(particleShader) { name = "Runtime_PortalSpecks" };
        if (particleMaterial.HasProperty("_BaseColor")) particleMaterial.SetColor("_BaseColor", Color.white);
        if (particleMaterial.HasProperty("_Color")) particleMaterial.SetColor("_Color", Color.white);
        renderer.sharedMaterial = particleMaterial;
    }

    private void CreateLight(Transform parent)
    {
        GameObject go = new GameObject("PortalLight");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, 0f, 0.5f);

        portalLight = go.AddComponent<Light>();
        portalLight.type = LightType.Point;
        portalLight.color = new Color(0.22f, 1f, 0.025f);
        portalLight.range = lightRange;
        portalLight.intensity = lightIntensity;
        portalLight.shadows = LightShadows.None;
    }

    private void ApplyVisualSettings()
    {
        Transform generated = transform.Find(GeneratedRootName);
        if (generated != null)
            generated.localPosition = new Vector3(0f, 0f, visualDepthOffset);

        if (portalRenderer != null)
            portalRenderer.transform.localScale = new Vector3(radius * 2f, radius * 2f * verticalScale, 1f);

        if (portalMaterial != null)
        {
            SetColorIfPresent(portalMaterial, "_OuterColor", outerColor);
            SetColorIfPresent(portalMaterial, "_MidColor", midColor);
            SetColorIfPresent(portalMaterial, "_DarkColor", darkCenterColor);
            SetColorIfPresent(portalMaterial, "_HighlightColor", highlightColor);
            SetFloatIfPresent(portalMaterial, "_Speed", swirlSpeed);
            SetFloatIfPresent(portalMaterial, "_SwirlStrength", swirlStrength);
            SetFloatIfPresent(portalMaterial, "_EdgeWobble", edgeWobble);
            SetFloatIfPresent(portalMaterial, "_EmissionStrength", emissionStrength);

            // Fallback when the custom shader has not imported yet.
            SetColorIfPresent(portalMaterial, "_BaseColor", midColor);
            SetColorIfPresent(portalMaterial, "_Color", midColor);
        }

        if (portalParticles != null)
        {
            ParticleSystem.MainModule main = portalParticles.main;
            main.startLifetime = particleLifetime;
            main.startSize = new ParticleSystem.MinMaxCurve(particleSize * 0.55f, particleSize * 1.45f);

            ParticleSystem.EmissionModule emission = portalParticles.emission;
            emission.rateOverTime = particlesPerSecond;

            ParticleSystem.ShapeModule shape = portalParticles.shape;
            shape.radius = radius * 0.92f;
            shape.scale = new Vector3(1f, verticalScale, 1f);
        }

        if (portalLight != null)
        {
            portalLight.color = Color.Lerp(new Color(0.10f, 0.75f, 0.01f), Color.green, 0.45f);
            portalLight.range = lightRange;
            portalLight.intensity = lightIntensity;
        }
    }

    private static void SetColorIfPresent(Material mat, string property, Color value)
    {
        if (mat != null && mat.HasProperty(property)) mat.SetColor(property, value);
    }

    private static void SetFloatIfPresent(Material mat, string property, float value)
    {
        if (mat != null && mat.HasProperty(property)) mat.SetFloat(property, value);
    }

    private void ClearGeneratedChildren()
    {
        Transform child = transform.Find(GeneratedRootName);
        if (child == null) return;
        if (Application.isPlaying) Destroy(child.gameObject);
        else DestroyImmediate(child.gameObject);
    }
}
