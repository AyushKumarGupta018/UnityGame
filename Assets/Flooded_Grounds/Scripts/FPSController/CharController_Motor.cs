using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class CharController_Motor : MonoBehaviour {

	public float speed = 10.0f;
	public float sensitivity = 30.0f;
	public float WaterHeight = 15.5f;
	CharacterController character;
	public GameObject cam;
	float moveFB, moveLR;
	float rotX, rotY;
	public bool webGLRightClickRotation = true;
	float gravity = -9.8f;
	float camPitch = 0f;

	void Start(){
		character = GetComponent<CharacterController> ();
		if (Application.isEditor) {
			webGLRightClickRotation = false;
			sensitivity = sensitivity * 1.5f;
		}

		if (cam == null) {
			var c = GetComponentInChildren<Camera>();
			if (c != null) cam = c.gameObject;
		}

		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}

	void CheckForWaterHeight(){
		if (transform.position.y < WaterHeight) {
			gravity = 0f;			
		} else {
			gravity = -9.8f;
		}
	}

	void Update(){
		moveFB = 0f;
		moveLR = 0f;
		rotX = 0f;
		rotY = 0f;

#if ENABLE_INPUT_SYSTEM
		if (Keyboard.current != null) {
			var kb = Keyboard.current;
			if (kb.wKey.isPressed || kb.upArrowKey.isPressed) moveLR += speed;
			if (kb.sKey.isPressed || kb.downArrowKey.isPressed) moveLR -= speed;
			if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) moveFB += speed;
			if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) moveFB -= speed;
			if (kb.escapeKey.wasPressedThisFrame) {
				Cursor.lockState = CursorLockMode.None;
				Cursor.visible = true;
			}
		}

		if (Mouse.current != null) {
			var mouse = Mouse.current;
			if (mouse.leftButton.wasPressedThisFrame) {
				Cursor.lockState = CursorLockMode.Locked;
				Cursor.visible = false;
			}
			Vector2 delta = mouse.delta.ReadValue();
			rotX = delta.x * 0.15f * sensitivity;
			rotY = delta.y * 0.15f * sensitivity;
		}
#else
		try {
			moveFB = Input.GetAxis ("Horizontal") * speed;
			moveLR = Input.GetAxis ("Vertical") * speed;
			rotX = Input.GetAxis ("Mouse X") * sensitivity;
			rotY = Input.GetAxis ("Mouse Y") * sensitivity;
		} catch { }
#endif

		CheckForWaterHeight ();

		Vector3 movement = new Vector3 (moveFB, gravity, moveLR);

		if (cam != null) {
			CameraRotation (cam, rotX, rotY);
		}

		movement = transform.rotation * movement;
		if (character != null) {
			character.Move (movement * Time.deltaTime);
		}
	}

	void CameraRotation(GameObject targetCam, float rx, float ry){		
		transform.Rotate (0, rx * Time.deltaTime, 0);
		camPitch = Mathf.Clamp(camPitch - ry * Time.deltaTime, -85f, 85f);
		targetCam.transform.localRotation = Quaternion.Euler(camPitch, 0f, 0f);
	}
}
