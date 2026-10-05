using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class FloodedPowerCell : MonoBehaviour
{
    public string cellName = "Power Cell";
    public bool isCollected = false;
    public float interactionDistance = 3.5f;

    [Header("Visuals")]
    public Light beaconLight;
    public ParticleSystem auraParticles;
    public float rotationSpeed = 45f;
    public float bobFrequency = 1.8f;
    public float bobHeight = 0.18f;

    private Vector3 initialPos;
    private Transform playerTransform;

    void Start()
    {
        initialPos = transform.position;
        var player = FindAnyObjectByType<FloodedPlayerController>();
        if (player != null) playerTransform = player.transform;
    }

    void Update()
    {
        if (isCollected) return;

        // Bob and rotate
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        float newY = initialPos.y + Mathf.Sin(Time.time * bobFrequency) * bobHeight;
        transform.position = new Vector3(initialPos.x, newY, initialPos.z);

        if (playerTransform == null)
        {
            var p = FindAnyObjectByType<FloodedPlayerController>();
            if (p != null) playerTransform = p.transform;
            return;
        }

        Vector2 cellFlat = new Vector2(transform.position.x, transform.position.z);
        Vector2 playerFlat = new Vector2(playerTransform.position.x, playerTransform.position.z);
        float flatDist = Vector2.Distance(cellFlat, playerFlat);
        float heightDiff = Mathf.Abs(transform.position.y - playerTransform.position.y);
        if (flatDist <= interactionDistance && heightDiff <= 3.5f)
        {
            bool interactPressed = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                interactPressed = Keyboard.current.eKey.wasPressedThisFrame;
            }
#else
            interactPressed = Input.GetKeyDown(KeyCode.E);
#endif
            if (interactPressed)
            {
                Collect();
            }
        }
    }

    public void Collect()
    {
        isCollected = true;
        if (beaconLight != null) beaconLight.enabled = false;
        if (auraParticles != null) auraParticles.Stop();

        // Notify Game Manager
        var mgr = FindAnyObjectByType<BeaconGameManager>();
        if (mgr != null)
        {
            mgr.OnPowerCellCollected(this);
        }

        // Hide mesh
        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            r.enabled = false;
        }

        Destroy(gameObject, 0.5f);
    }

    public float GetDistanceToPlayer()
    {
        if (playerTransform == null) return 9999f;
        Vector2 cellFlat = new Vector2(transform.position.x, transform.position.z);
        Vector2 playerFlat = new Vector2(playerTransform.position.x, playerTransform.position.z);
        float flatDist = Vector2.Distance(cellFlat, playerFlat);
        float heightDiff = Mathf.Abs(transform.position.y - playerTransform.position.y);
        return (heightDiff <= 3.5f) ? flatDist : Vector3.Distance(transform.position, playerTransform.position);
    }
}
