using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterController))]
public class FloodedPlayerController : MonoBehaviour
{
    [Header("Movement Speeds")]
    public float walkSpeed = 5.0f;
    public float sprintSpeed = 8.5f;
    public float waterWadeSpeed = 3.2f;
    public float jumpHeight = 1.6f;
    public float gravity = -18.0f;

    [Header("Water Settings")]
    public float waterLevel = 16.5f;
    public bool isInWater = false;

    [Header("Camera & View")]
    public Transform cameraFollowTarget;
    public Camera playerCamera;
    public float mouseSensitivity = 2.2f;
    public float minPitch = -40f;
    public float maxPitch = 70f;
    public float thirdPersonDistance = 3.8f;
    public Vector3 thirdPersonOffset = new Vector3(0.35f, 1.6f, 0f);
    public bool isFirstPerson = false;

    [Header("Audio & Effects")]
    public AudioSource audioSource;
    public AudioClip groundStepSound;
    public AudioClip waterStepSound;
    public GameObject waterSplashPrefab;

    private CharacterController controller;
    private Animator animator;
    private Vector3 velocity;
    private float currentPitch = 15f;
    private float currentYaw = 0f;
    private float targetSpeed;
    private float currentSpeed;
    private float stepCycle = 0f;
    private float nextStep = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();

        if (cameraFollowTarget == null)
        {
            var targetObj = new GameObject("CameraFollowTarget");
            targetObj.transform.SetParent(transform);
            targetObj.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            cameraFollowTarget = targetObj.transform;
        }

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                var camObj = new GameObject("PlayerCamera");
                playerCamera = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
            }
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f; // 2D for player feedback
        }

        currentYaw = transform.eulerAngles.y;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleInputAndRotation();
        HandleMovementAndPhysics();
        UpdateCameraPosition();
        UpdateAnimator();
    }

    void HandleInputAndRotation()
    {
        float mouseX = 0f;
        float mouseY = 0f;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();
            mouseX = delta.x * 0.08f * mouseSensitivity;
            mouseY = delta.y * 0.08f * mouseSensitivity;

            if (Mouse.current.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        if (Keyboard.current != null)
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (Keyboard.current.vKey.wasPressedThisFrame)
            {
                isFirstPerson = !isFirstPerson;
            }
        }
#else
        mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        if (Input.GetKeyDown(KeyCode.V))
        {
            isFirstPerson = !isFirstPerson;
        }
#endif

        currentYaw += mouseX;
        currentPitch = Mathf.Clamp(currentPitch - mouseY, minPitch, maxPitch);
        transform.rotation = Quaternion.Euler(0f, currentYaw, 0f);
    }

    void HandleMovementAndPhysics()
    {
        float h = 0f;
        float v = 0f;
        bool isSprinting = false;
        bool jumpPressed = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            var kb = Keyboard.current;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) h += 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) h -= 1f;

            isSprinting = kb.leftShiftKey.isPressed;
            jumpPressed = kb.spaceKey.wasPressedThisFrame;
        }
#else
        h = Input.GetAxisRaw("Horizontal");
        v = Input.GetAxisRaw("Vertical");
        isSprinting = Input.GetKey(KeyCode.LeftShift);
        jumpPressed = Input.GetKeyDown(KeyCode.Space);
#endif

        Vector3 moveInput = new Vector3(h, 0f, v).normalized;

        // Water detection
        bool wasInWater = isInWater;
        isInWater = transform.position.y < waterLevel;

        if (isInWater && !wasInWater)
        {
            SpawnSplashEffect();
        }

        // Determine speed based on ground vs water
        if (isInWater)
        {
            targetSpeed = (moveInput.magnitude > 0.1f) ? waterWadeSpeed : 0f;
        }
        else
        {
            if (moveInput.magnitude > 0.1f)
            {
                targetSpeed = isSprinting ? sprintSpeed : walkSpeed;
            }
            else
            {
                targetSpeed = 0f;
            }
        }

        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 10f);

        // Direction in camera/player forward
        Vector3 moveDir = transform.forward * moveInput.z + transform.right * moveInput.x;
        controller.Move(moveDir * currentSpeed * Time.deltaTime);

        // Vertical physics & jumping
        if (controller.isGrounded)
        {
            if (velocity.y < 0f)
            {
                velocity.y = -2f; // Keep grounded firmly
            }

            if (jumpPressed && !isInWater)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                if (animator != null) animator.SetTrigger("Jump");
            }
            else if (jumpPressed && isInWater)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * 0.5f * -2f * gravity);
                if (animator != null) animator.SetTrigger("Jump");
                SpawnSplashEffect();
            }
        }
        else
        {
            if (isInWater && velocity.y < -3f)
            {
                velocity.y = Mathf.Lerp(velocity.y, -3f, Time.deltaTime * 6f); // Water buoyancy drag
            }
            else
            {
                velocity.y += gravity * Time.deltaTime;
            }
        }

        controller.Move(velocity * Time.deltaTime);

        // Footstep cycle
        if (controller.isGrounded && moveInput.magnitude > 0.1f)
        {
            float speedFactor = (currentSpeed / walkSpeed);
            stepCycle += speedFactor * Time.deltaTime;
            if (stepCycle > nextStep)
            {
                nextStep = stepCycle + (isInWater ? 0.6f : 0.45f);
                PlayFootstepSound();
            }
        }
    }

    void UpdateCameraPosition()
    {
        if (playerCamera == null) return;

        Quaternion camRot = Quaternion.Euler(currentPitch, currentYaw, 0f);

        if (isFirstPerson)
        {
            Vector3 eyePos = transform.position + new Vector3(0f, 1.65f, 0f) + transform.forward * 0.2f;
            playerCamera.transform.position = eyePos;
            playerCamera.transform.rotation = camRot;
        }
        else
        {
            Vector3 focusPos = transform.position + thirdPersonOffset;
            Vector3 desiredPos = focusPos - (camRot * Vector3.forward * thirdPersonDistance);

            // Raycast for camera collision against terrain/structures
            RaycastHit hit;
            if (Physics.SphereCast(focusPos, 0.25f, (desiredPos - focusPos).normalized, out hit, thirdPersonDistance, ~LayerMask.GetMask("Ignore Raycast")))
            {
                desiredPos = focusPos + (desiredPos - focusPos).normalized * (hit.distance - 0.1f);
            }

            playerCamera.transform.position = desiredPos;
            playerCamera.transform.rotation = camRot;
        }
    }

    void UpdateAnimator()
    {
        if (animator == null) return;

        float normSpeed = currentSpeed / sprintSpeed;
        animator.SetFloat("Speed", normSpeed);
        animator.SetBool("IsGrounded", controller.isGrounded);
        animator.SetBool("IsInWater", isInWater);
    }

    void PlayFootstepSound()
    {
        if (audioSource == null) return;
        AudioClip clip = isInWater ? waterStepSound : groundStepSound;
        if (clip != null)
        {
            audioSource.pitch = Random.Range(0.85f, 1.15f);
            audioSource.PlayOneShot(clip, isInWater ? 0.45f : 0.32f);
        }
    }

    void SpawnSplashEffect()
    {
        if (waterSplashPrefab != null)
        {
            Vector3 splashPos = new Vector3(transform.position.x, waterLevel + 0.05f, transform.position.z);
            Instantiate(waterSplashPrefab, splashPos, Quaternion.identity);
        }
    }
}
