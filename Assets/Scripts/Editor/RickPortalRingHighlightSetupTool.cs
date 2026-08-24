#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Safely configures ONLY the Particle System behaviour of the selected RingHighlight.
/// IMPORTANT: this tool deliberately preserves the user's existing hierarchy, Transform,
/// Renderer render mode, Renderer mesh, Renderer material, sorting settings, and local asset setup.
/// </summary>
public static class RickPortalRingHighlightSetupTool
{
    [MenuItem("Tower Defense/VFX/Setup Selected Ring Highlight")]
    public static void SetupSelectedRingHighlight()
    {
        GameObject go = Selection.activeGameObject;
        if (go == null)
        {
            EditorUtility.DisplayDialog("Ring Highlight", "Select your existing RingHighlight GameObject first.", "OK");
            return;
        }

        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        if (ps == null)
        {
            EditorUtility.DisplayDialog(
                "Ring Highlight",
                "The selected object has no ParticleSystem. Nothing was changed so your existing structure stays untouched.",
                "OK");
            return;
        }

        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        Mesh preservedMesh = renderer != null ? renderer.mesh : null;
        Material preservedMaterial = renderer != null ? renderer.sharedMaterial : null;
        ParticleSystemRenderMode preservedRenderMode = renderer != null ? renderer.renderMode : ParticleSystemRenderMode.Billboard;
        Vector3 preservedLocalPosition = go.transform.localPosition;
        Quaternion preservedLocalRotation = go.transform.localRotation;
        Vector3 preservedLocalScale = go.transform.localScale;

        ConfigureParticleSystemOnly(ps);

        // Explicitly restore the user's existing setup. The tool must never choose a mesh/material for them.
        go.transform.localPosition = preservedLocalPosition;
        go.transform.localRotation = preservedLocalRotation;
        go.transform.localScale = preservedLocalScale;

        if (renderer != null)
        {
            renderer.renderMode = preservedRenderMode;
            renderer.sharedMaterial = preservedMaterial;
            if (preservedRenderMode == ParticleSystemRenderMode.Mesh)
                renderer.mesh = preservedMesh;
            EditorUtility.SetDirty(renderer);
        }

        EditorUtility.SetDirty(ps);
        EditorUtility.SetDirty(go);
        SceneView.RepaintAll();

        string rendererInfo = renderer == null
            ? "No ParticleSystemRenderer was found."
            : "Preserved Renderer: " + renderer.renderMode +
              "\nMaterial: " + (preservedMaterial != null ? preservedMaterial.name : "<unchanged / none>") +
              "\nMesh: " + (preservedMesh != null ? preservedMesh.name : "<unchanged / none>");

        EditorUtility.DisplayDialog(
            "RingHighlight Particle Settings Updated",
            "Only Particle System timing/emission was changed.\n\n" +
            "NOT changed:\n- Hierarchy\n- Transform\n- Renderer mode\n- Mesh\n- Material\n- Your Blender / Material Maker / Krita assets\n\n" +
            rendererInfo,
            "OK");
    }

    private static void ConfigureParticleSystemOnly(ParticleSystem ps)
    {
        Undo.RecordObject(ps, "Configure Ring Highlight Particle System");

        bool wasPlaying = ps.isPlaying;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.duration = 1f;
        main.startDelay = 0f;
        main.startLifetime = 999f;
        main.startSpeed = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 1;

        // Preserve the user's Start Size / Rotation / Color because those control the visual scale/orientation.

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.rateOverDistance = 0f;
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, 1, 1, 0, 0.01f)
        });

        // The reference RingHighlight is one stationary rendered particle.
        // Disable Shape so its large scene gizmo does not affect spawning.
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = false;

        // Do NOT alter Renderer, mesh, material, Transform, SizeOverLifetime,
        // ColorOverLifetime, RotationOverLifetime, Noise, or other authored modules.

        if (!Application.isPlaying || wasPlaying)
            ps.Play(true);
    }
}
#endif
