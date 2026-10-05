using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class FloodedBeaconPole : MonoBehaviour
{
    [Header("Power State")]
    [Tooltip("Only starts glowing red and blue after power cells are inserted")]
    public bool isPowered = false;

    [Header("Cycle Settings")]
    [Tooltip("Duration in seconds for each color before switching")]
    public float cycleTime = 1.0f;

    [Header("Materials")]
    public Material unpoweredMaterial;
    public Material redMaterial;
    public Material blueMaterial;

    [Header("Lights & Colors")]
    public Color redLightColor = new Color(1.0f, 0.08f, 0.08f, 1.0f);
    public Color blueLightColor = new Color(0.08f, 0.50f, 1.0f, 1.0f);

    [Header("Renderers & Lights to Cycle")]
    public Renderer[] poleRenderers;
    public Light[] poleLights;

    [Header("Runtime State")]
    public bool isRed = true;
    public float timer = 0f;

    void Start()
    {
        CacheRenderersAndLights();
        ApplyColorState();
    }

    void OnEnable()
    {
        CacheRenderersAndLights();
        ApplyColorState();
    }

    public void CacheRenderersAndLights()
    {
        if (poleRenderers == null || poleRenderers.Length == 0)
        {
            poleRenderers = GetComponentsInChildren<Renderer>(true);
        }
        if (poleLights == null || poleLights.Length == 0)
        {
            poleLights = GetComponentsInChildren<Light>(true);
        }
    }

    void Update()
    {
        if (!isPowered)
        {
            // Unpowered: ensure dormant visuals
            return;
        }

        if (Application.isPlaying)
        {
            timer += Time.deltaTime;
            if (timer >= cycleTime)
            {
                timer -= cycleTime;
                isRed = !isRed;
                ApplyColorState();
            }
        }
        else
        {
#if UNITY_EDITOR
            // In editor mode, if previewing power
            float t = (float)UnityEditor.EditorApplication.timeSinceStartup;
            bool editorRed = (Mathf.FloorToInt(t / cycleTime) % 2 == 0);
            if (editorRed != isRed)
            {
                isRed = editorRed;
                ApplyColorState();
            }
#endif
        }
    }

    public void PowerUp()
    {
        isPowered = true;
        timer = 0f;
        isRed = true;
        ApplyColorState();
    }

    public void ApplyColorState()
    {
        if (!isPowered)
        {
            // Dormant / Unpowered state
            if (poleRenderers != null && unpoweredMaterial != null)
            {
                foreach (var r in poleRenderers)
                {
                    if (r == null) continue;
                    r.SetPropertyBlock(null);
                    r.sharedMaterial = unpoweredMaterial;
                }
            }

            if (poleLights != null)
            {
                foreach (var l in poleLights)
                {
                    if (l != null)
                    {
                        l.enabled = false;
                    }
                }
            }
            return;
        }

        // Powered state: Alternate Red and Blue
        Material activeMat = isRed ? redMaterial : blueMaterial;
        Color activeLight = isRed ? redLightColor : blueLightColor;

        if (poleRenderers != null && activeMat != null)
        {
            foreach (var r in poleRenderers)
            {
                if (r == null) continue;
                r.SetPropertyBlock(null);
                r.sharedMaterial = activeMat;
            }
        }

        if (poleLights != null)
        {
            foreach (var l in poleLights)
            {
                if (l != null)
                {
                    l.enabled = true;
                    l.color = activeLight;
                }
            }
        }
    }

    void OnValidate()
    {
        ApplyColorState();
    }
}
