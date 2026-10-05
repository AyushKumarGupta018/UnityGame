using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class FloodedExperienceSetup
{
    [MenuItem("FloodedGrounds/Build Complete Experience")]
    public static void BuildExperience()
    {
        EditorUtility.DisplayProgressBar("Flooded Grounds Setup", "Assembling player, mini-game & cinematic tour...", 0.1f);

        try
        {
            // 1. Clean existing setup objects if any
            string[] toClean = new string[] { "Player_Hero", "GameManager", "CinematicTourManager", "PowerCell_A", "PowerCell_B", "Lighthouse_Generator" };
            foreach (var name in toClean)
            {
                var oldObj = GameObject.Find(name);
                if (oldObj != null) Object.DestroyImmediate(oldObj);
            }

            // Disable old FpsController if present
            var oldFps = GameObject.Find("FpsController");
            if (oldFps != null) oldFps.SetActive(false);

            // 2. Setup Player Character
            EditorUtility.DisplayProgressBar("Flooded Grounds Setup", "Configuring animated player character...", 0.3f);
            var playerObj = SetupPlayer();

            // 3. Setup Power Cells in Flooded Village
            EditorUtility.DisplayProgressBar("Flooded Grounds Setup", "Placing power cells in submerged village...", 0.5f);
            SetupPowerCells();

            // 4. Setup Lighthouse Emergency Generator & Rotating Beacon
            EditorUtility.DisplayProgressBar("Flooded Grounds Setup", "Building lighthouse emergency beacon...", 0.7f);
            SetupLighthouseBeacon();

            // 5. Setup Game Manager & Cinematic Tour
            EditorUtility.DisplayProgressBar("Flooded Grounds Setup", "Initializing game manager & cinematic tour...", 0.9f);
            var mgrObj = new GameObject("GameManager");
            mgrObj.AddComponent<BeaconGameManager>();

            var tourObj = new GameObject("CinematicTourManager");
            tourObj.AddComponent<FloodedCinematicTour>();

            // 6. Save Scene
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            Debug.Log("<color=green>[FloodedExperienceSetup] Complete experience successfully built and saved!</color>");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private static GameObject SetupPlayer()
    {
        var playerObj = new GameObject("Player_Hero");
        playerObj.transform.position = new Vector3(568f, 24.5f, 462f); // Bridge overlooking town
        playerObj.transform.rotation = Quaternion.Euler(0f, -45f, 0f);

        // Character Controller
        var cc = playerObj.AddComponent<CharacterController>();
        cc.center = new Vector3(0f, 0.95f, 0f);
        cc.radius = 0.35f;
        cc.height = 1.9f;
        cc.stepOffset = 0.4f;

        // Visual Model
        string modelPath = "Assets/Kevin Iglesias/Human Animations/Models/HumanM_Model.fbx";
        var modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (modelPrefab != null)
        {
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, playerObj.transform);
            visual.name = "VisualModel";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;

            // Bind Animator Controller
            string ctrlPath = "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Basic Motions/AnimatorControllers/HumanBasicMotionsScene.controller";
            var animCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ctrlPath);
            var animator = visual.GetComponent<Animator>();
            if (animator != null && animCtrl != null)
            {
                animator.runtimeAnimatorController = animCtrl;
            }
        }
        else
        {
            // Fallback capsule visual
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "VisualModel";
            capsule.transform.SetParent(playerObj.transform, false);
            capsule.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            var col = capsule.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
        }

        // Camera Follow Target
        var camTarget = new GameObject("CameraFollowTarget");
        camTarget.transform.SetParent(playerObj.transform, false);
        camTarget.transform.localPosition = new Vector3(0f, 1.6f, 0f);

        // Player Camera
        var camObj = new GameObject("PlayerThirdPersonCamera");
        var cam = camObj.AddComponent<Camera>();
        cam.tag = "MainCamera";
        camObj.AddComponent<AudioListener>();
        var uac = camObj.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        uac.renderPostProcessing = true;

        // Player Controller Script
        var controller = playerObj.AddComponent<FloodedPlayerController>();
        controller.cameraFollowTarget = camTarget.transform;
        controller.playerCamera = cam;
        controller.waterLevel = 16.5f;

        return playerObj;
    }

    private static void SetupPowerCells()
    {
        // Cell 1: Porch of submerged villa (Amber Core)
        CreatePowerCell("PowerCell_A", new Vector3(562f, 22.8f, 452f), new Color(1.0f, 0.75f, 0.2f));

        // Cell 2: Inside flooded brick house ruins (Cyan Core)
        CreatePowerCell("PowerCell_B", new Vector3(535f, 23.0f, 480f), new Color(0.2f, 0.9f, 1.0f));
    }

    private static void CreatePowerCell(string name, Vector3 position, Color glowColor)
    {
        var cellObj = new GameObject(name);
        cellObj.transform.position = position;

        // Metal base cylinder
        var baseMesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseMesh.name = "MetalCasing";
        baseMesh.transform.SetParent(cellObj.transform, false);
        baseMesh.transform.localScale = new Vector3(0.45f, 0.25f, 0.45f);
        var col1 = baseMesh.GetComponent<Collider>();
        if (col1 != null) Object.DestroyImmediate(col1);

        // Glowing core sphere
        var core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        core.name = "GlowingCore";
        core.transform.SetParent(cellObj.transform, false);
        core.transform.localPosition = new Vector3(0f, 0.35f, 0f);
        core.transform.localScale = new Vector3(0.38f, 0.45f, 0.38f);
        var col2 = core.GetComponent<Collider>();
        if (col2 != null) Object.DestroyImmediate(col2);

        // Core Emission Material
        var coreMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        coreMat.SetColor("_BaseColor", glowColor);
        coreMat.SetColor("_EmissionColor", glowColor * 2.5f);
        coreMat.EnableKeyword("_EMISSION");
        core.GetComponent<Renderer>().sharedMaterial = coreMat;

        // Light
        var lightObj = new GameObject("BeaconLight");
        lightObj.transform.SetParent(cellObj.transform, false);
        lightObj.transform.localPosition = new Vector3(0f, 0.4f, 0f);
        var l = lightObj.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = glowColor;
        l.range = 8.0f;
        l.intensity = 2.5f;

        // Floating particles
        var psObj = new GameObject("AuraParticles");
        psObj.transform.SetParent(cellObj.transform, false);
        psObj.transform.localPosition = new Vector3(0f, 0.35f, 0f);
        var ps = psObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 1.2f;
        main.startSpeed = 0.4f;
        main.startSize = 0.12f;
        main.startColor = new Color(glowColor.r, glowColor.g, glowColor.b, 0.8f);
        main.maxParticles = 25;
        var em = ps.emission;
        em.rateOverTime = 12f;

        // Script
        var script = cellObj.AddComponent<FloodedPowerCell>();
        script.cellName = (name == "PowerCell_A") ? "Alpha Cell" : "Beta Cell";
        script.beaconLight = l;
        script.auraParticles = ps;
    }

    private static void SetupLighthouseBeacon()
    {
        var beaconObj = new GameObject("Lighthouse_Generator");
        beaconObj.transform.position = new Vector3(138.3f, 33.5f, 615.7f); // Top lantern room of Lighthouse

        // Generator Console Base
        var consoleBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
        consoleBase.name = "GeneratorConsole";
        consoleBase.transform.SetParent(beaconObj.transform, false);
        consoleBase.transform.localScale = new Vector3(1.4f, 1.1f, 0.8f);
        var col = consoleBase.GetComponent<Collider>();
        if (col != null) Object.DestroyImmediate(col);

        // Rotating Searchlight Head
        var headObj = new GameObject("RotatingHead");
        headObj.transform.SetParent(beaconObj.transform, false);
        headObj.transform.localPosition = new Vector3(0f, 1.2f, 0f);

        // Searchlight Spot
        var spotObj = new GameObject("BeaconSpotlight");
        spotObj.transform.SetParent(headObj.transform, false);
        spotObj.transform.localRotation = Quaternion.Euler(8f, 0f, 0f);
        var spot = spotObj.AddComponent<Light>();
        spot.type = LightType.Spot;
        spot.color = new Color(1.0f, 0.95f, 0.82f);
        spot.range = 600f;
        spot.spotAngle = 32f;
        spot.intensity = 18f;
        spot.shadows = LightShadows.Soft;
        spot.enabled = false;

        // Rotating Beacon Glass Lens
        var lens = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        lens.name = "LampLens";
        lens.transform.SetParent(headObj.transform, false);
        lens.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        lens.transform.localScale = new Vector3(0.5f, 0.15f, 0.5f);
        var lensMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        lensMat.SetColor("_BaseColor", new Color(1.0f, 0.95f, 0.8f));
        lensMat.SetColor("_EmissionColor", new Color(1.0f, 0.95f, 0.8f) * 3f);
        lensMat.EnableKeyword("_EMISSION");
        lens.GetComponent<Renderer>().sharedMaterial = lensMat;

        // Rescue Flare Particles
        var flareObj = new GameObject("RescueFlareParticles");
        flareObj.transform.SetParent(beaconObj.transform, false);
        flareObj.transform.localPosition = new Vector3(0f, 2.5f, 0f);
        var flarePs = flareObj.AddComponent<ParticleSystem>();
        var fMain = flarePs.main;
        fMain.startLifetime = 3.5f;
        fMain.startSpeed = 15f;
        fMain.startSize = 0.8f;
        fMain.startColor = new Color(1f, 0.3f, 0.1f, 0.9f);
        fMain.maxParticles = 60;
        fMain.playOnAwake = false;
        var fShape = flarePs.shape;
        fShape.shapeType = ParticleSystemShapeType.Cone;
        fShape.angle = 12f;

        // Script
        var script = beaconObj.AddComponent<FloodedLighthouseBeacon>();
        script.rotatingHead = headObj.transform;
        script.beaconSpotlight = spot;
        script.activationFlare = flarePs;
    }
}
