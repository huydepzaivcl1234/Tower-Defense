using UnityEngine;

/// <summary>
/// Lightweight procedural firing animation for towers.
/// No animation clips are required: recoil, impact punch, idle mechanical motion,
/// muzzle particles and a short muzzle light are generated at runtime.
/// This component is visual-only and does not change tower stats or projectile logic.
/// </summary>
[DisallowMultipleComponent]
public class TowerFireAnimator : MonoBehaviour
{
    public enum FireStyle
    {
        Light,
        Crossbow,
        Cannon,
        HeavyCannon,
        Mortar,
        Flame,
        Energy
    }

    [Header("Style")]
    public FireStyle style = FireStyle.Light;

    [Header("Motion")]
    [Tooltip("Optional override. If empty, Tower.turretHead is animated.")]
    public Transform animatedPart;
    [Min(0f)] public float recoilDistance = 0.10f;
    [Min(0.01f)] public float recoilReturnTime = 0.09f;
    [Range(0f, 0.25f)] public float scalePunch = 0.045f;
    public bool idleMotion = true;
    [Range(0f, 0.05f)] public float idleAmplitude = 0.008f;
    [Range(0.1f, 5f)] public float idleSpeed = 1.35f;

    [Header("Muzzle FX")]
    public bool createMuzzleFx = true;
    public Color muzzleColor = new Color(1f, 0.55f, 0.12f, 1f);
    [Min(0f)] public float muzzleLightIntensity = 3.2f;
    [Min(0.01f)] public float muzzleLightDuration = 0.055f;
    [Range(2, 30)] public int particleBurst = 8;
    [Min(0.01f)] public float particleSize = 0.12f;

    private Tower tower;
    private Transform boundPart;
    private Vector3 baseLocalPosition;
    private Vector3 baseLocalScale;
    private float recoil;
    private float recoilVelocity;
    private float punch;
    private float punchVelocity;
    private float lightTimer;
    private float idlePhase;

    private ParticleSystem muzzleParticles;
    private Light muzzleLight;

    private void Awake()
    {
        tower = GetComponent<Tower>();
        idlePhase = Random.Range(0f, Mathf.PI * 2f);
        Rebind();
    }

    private void OnEnable()
    {
        Rebind();
    }

    /// <summary>Call after a tower changes visual phase so the new TurretHead / FirePoint is used.</summary>
    public void Rebind()
    {
        if (tower == null) tower = GetComponent<Tower>();
        Transform next = animatedPart != null ? animatedPart : (tower != null ? tower.turretHead : null);
        if (next == null) return;

        if (boundPart != next)
        {
            RestoreBoundPart();
            boundPart = next;
            baseLocalPosition = boundPart.localPosition;
            baseLocalScale = boundPart.localScale;
        }

        EnsureMuzzleFx();
    }

    public void PlayFire()
    {
        Rebind();
        ApplyStyleDefaultsIfNeeded();

        recoil = Mathf.Max(recoil, recoilDistance);
        punch = Mathf.Max(punch, 1f);

        if (muzzleParticles != null)
        {
            muzzleParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            muzzleParticles.Emit(particleBurst);
        }

        if (muzzleLight != null)
        {
            muzzleLight.enabled = true;
            muzzleLight.intensity = muzzleLightIntensity;
            lightTimer = muzzleLightDuration;
        }
    }

    private void LateUpdate()
    {
        if (boundPart == null)
        {
            Rebind();
            if (boundPart == null) return;
        }

        recoil = Mathf.SmoothDamp(recoil, 0f, ref recoilVelocity, recoilReturnTime);
        punch = Mathf.SmoothDamp(punch, 0f, ref punchVelocity, recoilReturnTime * 0.75f);

        Vector3 localIdle = Vector3.zero;
        if (idleMotion)
        {
            float wave = Mathf.Sin(Time.time * idleSpeed * Mathf.PI * 2f + idlePhase);
            localIdle.y = wave * idleAmplitude;
        }

        Transform parent = boundPart.parent;
        Vector3 baseWorld = parent != null
            ? parent.TransformPoint(baseLocalPosition + localIdle)
            : baseLocalPosition + localIdle;
        boundPart.position = baseWorld - boundPart.forward * recoil;

        float side = 1f + punch * scalePunch;
        float length = 1f - punch * scalePunch * 0.70f;
        boundPart.localScale = Vector3.Scale(baseLocalScale, new Vector3(side, side, length));

        if (muzzleLight != null && muzzleLight.enabled)
        {
            lightTimer -= Time.deltaTime;
            if (lightTimer <= 0f)
            {
                muzzleLight.enabled = false;
                muzzleLight.intensity = 0f;
            }
            else
            {
                muzzleLight.intensity = muzzleLightIntensity * Mathf.Clamp01(lightTimer / muzzleLightDuration);
            }
        }
    }

    private void OnDisable()
    {
        RestoreBoundPart();
        if (muzzleLight != null) muzzleLight.enabled = false;
    }

    private void RestoreBoundPart()
    {
        if (boundPart == null) return;
        boundPart.localPosition = baseLocalPosition;
        boundPart.localScale = baseLocalScale;
    }

    private void EnsureMuzzleFx()
    {
        if (!createMuzzleFx || tower == null || tower.firePoint == null) return;

        Transform fp = tower.firePoint;
        if (muzzleParticles != null && muzzleParticles.transform.parent == fp) return;

        if (muzzleParticles != null)
        {
            muzzleParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            Destroy(muzzleParticles.gameObject);
            muzzleParticles = null;
            muzzleLight = null;
        }

        GameObject fx = new GameObject("RuntimeMuzzleFX");
        fx.transform.SetParent(fp, false);
        fx.transform.localPosition = Vector3.zero;
        fx.transform.localRotation = Quaternion.identity;

        muzzleParticles = fx.AddComponent<ParticleSystem>();

        // A newly-added ParticleSystem can already be considered playing for this frame.
        // Stop and clear it BEFORE changing duration/module settings to avoid Unity's runtime warning.
        muzzleParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = muzzleParticles.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 0.12f;
        main.startLifetime = 0.09f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(particleSize * 0.45f, particleSize);
        main.startColor = muzzleColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 48;

        var emission = muzzleParticles.emission;
        emission.enabled = false;

        var shape = muzzleParticles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = style == FireStyle.Flame ? 18f : 9f;
        shape.radius = 0.025f;
        shape.length = 0.08f;

        var colorOverLifetime = muzzleParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(muzzleColor, 0f), new GradientColorKey(muzzleColor, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = g;

        ParticleSystemRenderer renderer = fx.GetComponent<ParticleSystemRenderer>();
        Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (particleShader == null) particleShader = Shader.Find("Particles/Standard Unlit");
        if (particleShader != null)
        {
            Material m = new Material(particleShader);
            m.name = "RuntimeMuzzleMaterial";
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", muzzleColor);
            if (m.HasProperty("_Color")) m.SetColor("_Color", muzzleColor);
            renderer.material = m;
        }

        muzzleLight = fx.AddComponent<Light>();
        muzzleLight.type = LightType.Point;
        muzzleLight.range = style == FireStyle.HeavyCannon ? 3.2f : 2.0f;
        muzzleLight.color = muzzleColor;
        muzzleLight.shadows = LightShadows.None;
        muzzleLight.enabled = false;
    }

    private void ApplyStyleDefaultsIfNeeded()
    {
        switch (style)
        {
            case FireStyle.Crossbow:
            case FireStyle.Cannon:
            case FireStyle.HeavyCannon:
            case FireStyle.Mortar:
            case FireStyle.Flame:
            case FireStyle.Energy:
            case FireStyle.Light:
                break;
        }
    }
}
