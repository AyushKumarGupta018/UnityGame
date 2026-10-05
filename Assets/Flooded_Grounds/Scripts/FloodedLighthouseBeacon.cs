using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class FloodedLighthouseBeacon : MonoBehaviour
{
    [Header("Beacon Light Assembly")]
    public Transform rotatingHead;
    public Light beaconSpotlight;
    public float rotationSpeed = 35f;
    public bool isActivated = false;

    [Header("Interaction")]
    public float interactionDistance = 4.0f;
    public int requiredCells = 2;

    [Header("FX")]
    public ParticleSystem activationFlare;
    public AudioSource beaconAudio;

    private Transform playerTransform;

    void Start()
    {
        var player = FindAnyObjectByType<FloodedPlayerController>();
        if (player != null) playerTransform = player.transform;

        if (beaconSpotlight != null)
        {
            beaconSpotlight.enabled = isActivated;
        }
    }

    void Update()
    {
        if (isActivated)
        {
            if (rotatingHead != null)
            {
                rotatingHead.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
            }
            return;
        }

        if (playerTransform == null)
        {
            var p = FindAnyObjectByType<FloodedPlayerController>();
            if (p != null) playerTransform = p.transform;
            return;
        }

        float dist = GetDistanceToPlayer();
        if (dist <= interactionDistance)
        {
            var mgr = FindAnyObjectByType<BeaconGameManager>();
            if (mgr != null && mgr.collectedCellsCount >= requiredCells)
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
                    ActivateBeacon(mgr);
                }
            }
        }
    }

    public void ActivateBeacon(BeaconGameManager mgr)
    {
        isActivated = true;

        if (beaconSpotlight != null)
        {
            beaconSpotlight.enabled = true;
            beaconSpotlight.intensity = 15f;
            beaconSpotlight.range = 500f;
            beaconSpotlight.spotAngle = 35f;
            beaconSpotlight.color = new Color(1.0f, 0.95f, 0.8f);
        }

        if (activationFlare != null)
        {
            activationFlare.Play();
        }

        if (beaconAudio != null)
        {
            beaconAudio.Play();
        }

        var pole = GetComponent<FloodedBeaconPole>();
        if (pole == null) pole = GetComponentInChildren<FloodedBeaconPole>();
        if (pole != null)
        {
            pole.PowerUp();
        }

        if (mgr != null)
        {
            mgr.OnBeaconActivated();
        }
    }

    public float GetDistanceToPlayer()
    {
        if (playerTransform == null) return 9999f;
        Vector2 beaconFlat = new Vector2(transform.position.x, transform.position.z);
        Vector2 playerFlat = new Vector2(playerTransform.position.x, playerTransform.position.z);
        float flatDist = Vector2.Distance(beaconFlat, playerFlat);
        float heightDiff = Mathf.Abs(transform.position.y - playerTransform.position.y);
        return (heightDiff <= 4.0f) ? flatDist : Vector3.Distance(transform.position, playerTransform.position);
    }
}
