﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ThirdPersonCamera : MonoBehaviour
{ 
	public static ThirdPersonCamera instance;
	public static ThirdPersonCamera GetInstance()
	{
		return instance;
	}

	// Components and transforms
	private Transform trans;
	private Transform camPivot;
	[HideInInspector]
	public Transform camTrans;
	private PlayerInputManager playerInput;

	[Header("Debug")]
	public CursorLockMode CursorMode = CursorLockMode.Locked;
	public bool AutoTargetPlayer = true;
	public Transform Target;

	[Header("Camera Options")]
	public float MouseSensitivityX = 2.5f;
	public float MouseSensitivityY = 2.5f;
	public float MinAngle = 40f;
	public float MaxAngle = 85f;
	public float CameraPanSpeed = 5f;
	public float CameraMoveSpeed = 5f;
	public float CameraAimSpeed = 50f;
	public float CameraCollideSpeed = 10f;
	public float CameraSmoothing = .012f;
	public bool InvertY = true;

	[Header("Wall Collision Options")]
	public float WallCheckDist = .1f;
	public float HideMeshDistance = .5f;
	public LayerMask WallLayer;
	[HideInInspector]
	public SkinnedMeshRenderer[] meshes;
	//[HideInInspector]
	//public MeshRenderer[] wpnMeshes;

	[Header("Aim Settings")]
	public float DefaultFOV = 60f;
	public float AimingFOV = 30f;
	public float ZoomSpeed = 10f;

	private Camera mainCam;

	[Header("Positioning")]
	public Vector3 DefaultPositionOffset = new Vector3(0f, 0f, -2.5f);
	public Vector3 AimPositionOffset = new Vector3(-2f, 0f, -2.5f);
	public Vector3 CoverPositionOffset = new Vector3(-1.8f, 0f, -3f);
	public float CoverLookAheadDistance = .8f;
	public enum Shoulder { RIGHT, LEFT }
	public Shoulder CurrentShoulder;

	private bool inCover;
	private bool aiming;
	private bool canAim;
	private Vector3 targetPositionOffset;

	// Camera helpers
	private Vector3 velocity;
	[HideInInspector]
	public float newX, newY = 0f;


	void Awake() 
	{
		// Implement singleton
		if(!instance)
			instance = this;
		else
		{
			if(instance != this)
			{
				Destroy(instance.gameObject);
				instance = this;
			}
		}

		// Cache components and transforms
		trans = GetComponent<Transform>();
		camPivot = trans.GetChild(0);
		camTrans = camPivot.GetChild(0);
	}

	void Start() 
	{
		// Get manager
		playerInput = PlayerInputManager.GetInstance();

		ParentAndResetMainCamera();
		CachePlayer();

		// Cache main camera for changing FOV
		mainCam = Camera.main;

		// Cache all meshes to hide when camera is near, AFTER getting target
		meshes = Target.GetComponentsInChildren<SkinnedMeshRenderer>();
		//wpnMeshes = Target.GetComponentsInChildren<MeshRenderer>();

		// Set cursor lock
		Cursor.lockState = CursorMode;

		// Camera default position
		SwitchShoulder(CurrentShoulder);
		targetPositionOffset = DefaultPositionOffset;

		canAim = true;
	}

	void Update() 
	{
		// Update state
		aiming = playerInput.RMB;
		targetPositionOffset = (aiming) ? AimPositionOffset : ((inCover) ? CoverPositionOffset : DefaultPositionOffset);

		if(inCover && !aiming)
		{
			Vector3 lookForward = CoverPositionOffset;
			lookForward.x += playerInput.horizontal * CoverLookAheadDistance;
			targetPositionOffset = lookForward;
		}

		RotateCamera();
		CheckWallCollision();
		CheckMeshDistance();
		ChangeFOV(aiming);
		if(playerInput.MMB)
			SwitchShoulder(CurrentShoulder);
	}

	void LateUpdate() 
	{
		Vector3 targetPos = Target.position;
		FollowTarget(targetPos);
	}

	public bool GetAimState()
	{
		return aiming;
	}

	public void SetCoverState(bool state, bool canOnlyAimAtSides)
	{
		inCover = state;
		if(inCover)
			canAim = canOnlyAimAtSides;
		else
			canAim = true;
		Target.GetComponent<Animator>().SetBool("mirror", (inCover) ? ((CurrentShoulder == Shoulder.RIGHT) ? false : true) : CurrentShoulder == Shoulder.RIGHT);
	}

	public void SwitchShoulder(Shoulder curShoulder) // Change shoulder side 
	{
		switch(curShoulder)
		{
			case Shoulder.LEFT:
				CurrentShoulder = Shoulder.RIGHT;
				Target.GetComponent<Animator>().SetBool("mirror", (inCover) ? false : true);
				AimPositionOffset = new Vector3(Mathf.Abs(AimPositionOffset.x), AimPositionOffset.y, AimPositionOffset.z);
				CoverPositionOffset = new Vector3(Mathf.Abs(CoverPositionOffset.x), CoverPositionOffset.y, CoverPositionOffset.z);
				DefaultPositionOffset = new Vector3(Mathf.Abs(DefaultPositionOffset.x), DefaultPositionOffset.y, DefaultPositionOffset.z);
				break;
			case Shoulder.RIGHT:
				CurrentShoulder = Shoulder.LEFT;
				Target.GetComponent<Animator>().SetBool("mirror", (inCover) ? true : false);
				AimPositionOffset = new Vector3(-Mathf.Abs(AimPositionOffset.x), AimPositionOffset.y, AimPositionOffset.z);
				CoverPositionOffset = new Vector3(-Mathf.Abs(CoverPositionOffset.x), CoverPositionOffset.y, CoverPositionOffset.z);
				DefaultPositionOffset = new Vector3(-Mathf.Abs(DefaultPositionOffset.x), DefaultPositionOffset.y, DefaultPositionOffset.z);
				break;
		}
	}

	private void FollowTarget(Vector3 targetPos) // Follow the target smoothly 
	{
		targetPos = Vector3.SmoothDamp(trans.position, targetPos, ref velocity, CameraSmoothing);
		trans.position = targetPos;
	}

	private void RotateCamera() // Rotate the camera with input 
	{
		// Get mouse movement
		newX += MouseSensitivityX * playerInput.mouseX;
		newY += (InvertY) ? MouseSensitivityY * playerInput.mouseY * -1f : MouseSensitivityY * playerInput.mouseY;

		// Clamping
		newX = Mathf.Repeat(newX, 360f);
		newY = Mathf.Clamp(newY, -Mathf.Abs(MinAngle), MaxAngle);

		// Rotation
		Vector3 eulerAngleAxis = new Vector3(newY, newX);
		Quaternion newRotation = Quaternion.Slerp(camPivot.localRotation, Quaternion.Euler(eulerAngleAxis), CameraPanSpeed * Time.deltaTime);
		camPivot.localRotation = newRotation;
	}

	private void CheckWallCollision() // Spherecast to prevent collision with walls and also switch shoulders 
	{
		// Do spherecast
		RaycastHit hit;
		Vector3 start = camPivot.position;
		Vector3 dir = camTrans.position - camPivot.position;
		float dist = Mathf.Abs(targetPositionOffset.z);
		if(Physics.SphereCast(start, WallCheckDist, dir, out hit, dist, WallLayer))
			RepositionCamera(hit, camPivot.position, dir);
		else
			PositionCamera(targetPositionOffset);
	}

	private void RepositionCamera(RaycastHit hit, Vector3 pivotPos, Vector3 dir) // Moves camera forward when we hit a wall 
	{
		float hitDist = hit.distance;
		Vector3 targetPos = pivotPos + (dir.normalized * hitDist);

		Vector3 newPos = Vector3.Lerp(camTrans.position, targetPos, CameraCollideSpeed * Time.deltaTime);
		camTrans.position = newPos;
	}

	private void PositionCamera(Vector3 camPos) // Position camera's localPosition to a given location 
	{
		float speed = (aiming) ? CameraAimSpeed : CameraMoveSpeed;
		Vector3 newPos = Vector3.Lerp(camTrans.localPosition, camPos, speed * Time.deltaTime);
		camTrans.localPosition = newPos;
	}

	private void CheckMeshDistance() // Hide the meshes if within set distance 
	{
		Transform mainCamTrans = camTrans;
		Vector3 mainCamPos = mainCamTrans.position;
		Vector3 targetPos = Target.position;
		float dist = Vector3.Distance(mainCamPos, (targetPos + Target.up));

		// Check model meshes
		if(meshes.Length > 0)
			for(int i = 0; i < meshes.Length; i++)
				meshes[i].enabled = (dist < HideMeshDistance) ? false : true;

		// Check weapon meshes
		//if(cameraSettings.wpnMeshes.Length > 0)
		//{
		//	for(int i = 0; i < cameraSettings.wpnMeshes.Length; i++)
		//	{
		//		cameraSettings.wpnMeshes[i].enabled = (dist < cameraSettings.hideMeshDistance) ? false : true;
		//	}
		//}
	}

	private void ChangeFOV(bool aim) // CHange camera FOV  
	{
		float targetFOV = (aim) ? AimingFOV : DefaultFOV;

		float newFOV = Mathf.Lerp(mainCam.fieldOfView, targetFOV, ZoomSpeed * Time.deltaTime);
		mainCam.fieldOfView = newFOV;
	}

	private void ParentAndResetMainCamera() // Setup main camera in any scene as TP camera 
	{
		Transform mainCamTrans = Camera.main.transform;
		mainCamTrans.parent = camTrans;
		mainCamTrans.localPosition = Vector3.zero;
		mainCamTrans.localRotation = Quaternion.Euler(Vector3.zero);
	}

	private void CachePlayer() // Finds player and set it as target 
	{
		if(Target || !AutoTargetPlayer)
			return;

		GameObject p = GameObject.FindGameObjectWithTag("Player");
		if(p)
			Target = p.transform;
		else
			Debug.LogError("There is no GameObject tagged as Player in the scene found!");
	}
}
