#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Configures ONLY the RingHighlight layer of the user's hand-built Rick-style portal.
/// It does not create or modify the rest of the portal hierarchy.
/// Uses the user's existing RMP_ring material and, when found, an existing custom portal/circle mesh.
/// </summary>
public static class RickPortalRingHighlightSetupTool
{
    [MenuItem("Tower Defense/VFX/Setup Selected Ring Highlight")]
    public static void SetupSelectedRingHighlight()
    {
        GameObject go = Selection.activeGameObject;
        if (go == null)
        {
            EditorUtility.DisplayDialog("Ring Highlight", "Select your RingHighlight GameObject in the Hierarchy first.", "OK");
            return;
        }

        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        if (ps == null)
            ps = Undo.AddComponent<ParticleSystem>(go);

        Undo.RecordObject(go.transform, "Setup Ring Highlight Transform");
        go.transform.localScale = Vector3.one;

        ConfigureParticleSystem(ps);

        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        Material ringMaterial = FindRingMaterial();
        if (ringMaterial != null)
        {
            Undo.RecordObject(renderer, "Assign Ring Highlight Material");
            renderer.sharedMaterial = ringMaterial;
        }

        Mesh portalMesh = FindPortalMesh();
        bool usingMesh = portalMesh != null;
        if (usingMesh)
        {
            Undo.RecordObject(renderer, "Assign Ring Highlight Mesh");
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = portalMesh;
            renderer.alignment = ParticleSystemRenderSpace.Local;
        }
        else
        {
            // Safe fallback: still lets the material be previewed while the user assigns the Blender mesh.
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.Local;
        }

        renderer.sortingOrder = 1;
        renderer.minParticleSize = 0f;
        renderer.maxParticleSize = 3f;

        EditorUtility.SetDirty(ps);
        EditorUtility.SetDirty(renderer);
        EditorUtility.SetDirty(go);
        SceneView.RepaintAll();

        string matMessage = ringMaterial != null
            ? $"Material: {ringMaterial.name}"
            : "RMP_ring material was not found automatically. Drag your RMP_ring material into Particle System > Renderer > Material.";

        string meshMessage = usingMesh
            ? $"Mesh: {portalMesh.name}"
            : "No custom portal/circle mesh was found automatically. In Particle System > Renderer choose Render Mode = Mesh and assign the flat ring/portal mesh you exported from Blender.";

        EditorUtility.DisplayDialog(
            "Ring Highlight Configured",
            "RingHighlight is now configured as ONE stationary particle.\n\n" +
            "Important: Shape is disabled. The giant orange ellipse you were seeing was the Shape gizmo, not the rendered ring.\n\n" +
            matMessage + "\n" + meshMessage + "\n\n" +
            "If the ring is still too large/small, adjust ONLY Main > Start Size first (recommended 0.8-1.5), not the GameObject scale.",
            "OK");
    }

    private static void ConfigureParticleSystem(ParticleSystem ps)
    {
        Undo.RecordObject(ps, "Configure Ring Highlight Particle System");

        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.duration = 1f;
        main.startDelay = 0f;
        main.startLifetime = 999f;
        main.startSpeed = 0f;
        main.startSize3D = false;
        main.startSize = 1f;
        main.startRotation = 0f;
        main.startColor = Color.white;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 1;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.rateOverDistance = 0f;
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, 1, 1, 0, 0.01f)
        });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = false;

        ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
        velocity.enabled = false;

        ParticleSystem.NoiseModule noise = ps.noise;
        noise.enabled = false;

        ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
        size.enabled = false;

        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = false;

        ParticleSystem.RotationOverLifetimeModule rotation = ps.rotationOverLifetime;
        rotation.enabled = false;

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.Play(true);
    }

    private static Material FindRingMaterial()
    {
        string[] guids = AssetDatabase.FindAssets("t:Material");
        Material best = null;
        int bestScore = int.MinValue;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            string n = mat.name.ToLowerInvariant();
            int score = 0;
            if (n == "rmp_ring") score += 100;
            if (n.Contains("rmp") && n.Contains("ring")) score += 80;
            if (n.Contains("ring")) score += 20;
            if (n.Contains("highlight")) score += 10;

            if (score > bestScore)
            {
                bestScore = score;
                best = mat;
            }
        }

        return bestScore >= 20 ? best : null;
    }

    private static Mesh FindPortalMesh()
    {
        string[] guids = AssetDatabase.FindAssets("t:Mesh");
        Mesh best = null;
        int bestScore = int.MinValue;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (Mesh mesh in assets.OfType<Mesh>())
            {
                string n = mesh.name.ToLowerInvariant();
                int score = 0;
                if (n.Contains("rick") && n.Contains("portal")) score += 100;
                if (n.Contains("portal") && n.Contains("circle")) score += 90;
                if (n.Contains("circle02")) score += 80;
                if (n.Contains("circle01")) score += 70;
                if (n.Contains("portal")) score += 50;
                if (n.Contains("circle")) score += 25;
                if (n.Contains("ring")) score += 20;

                if (score > bestScore)
                {
                    bestScore = score;
                    best = mesh;
                }
            }
        }

        return bestScore >= 25 ? best : null;
    }
}
#endif
