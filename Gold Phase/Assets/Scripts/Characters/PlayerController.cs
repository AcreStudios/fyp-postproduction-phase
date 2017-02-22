using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
	// Components
	private Transform trans;
	private Animator anim;
	[HideInInspector]
	public CharacterController charController;
	private PlayerInputManager playerInput;
	private ThirdPersonCamera tpCam;

	[Header("Movement Settings")]
	public float WalkSpeed = 2f;
	public float RunSpeed = 5f;
	public float SpeedSmoothTime = .1f;
	private float speedSmoothVelocity;
	private float currentSpeed;
	public float TurnSmoothTime = .2f;
	private float turnSmoothVelocity;
	private Vector3 currentRotation;
	public float gravity = -9.81f;
	private float velocityY;
	//[HideInInspector]
	public Vector3 footstepVelocity;

	// Inputs
	private float horizontal, vertical;
	private bool leftShift;

	// State
	[HideInInspector]
	public bool inCover = false;
	private bool running;


	void Awake() 
	{
		// Cache
		trans = GetComponent<Transform>();
		anim = GetComponent<Animator>();
		charController = GetComponent<CharacterController>();
	}

	void Start() 
	{
		// Get manager
		playerInput = PlayerInputManager.GetInstance();
		tpCam = ThirdPersonCamera.GetInstance();
	}
	
	void Update() 
	{
		MovePlayer();

		footstepVelocity = charController.velocity;
	}

	public bool GetRunState()
	{
		return running;
	}

	public void SetCoverState(bool state)
	{
		inCover = state;
	}

	private void MovePlayer() // Movement logic 
	{
		if(inCover) return;

		Vector3 input = new Vector3(playerInput.horizontal, 0f, playerInput.vertical);
		Vector3 inputDir = input.normalized;

		if(inputDir != Vector3.zero || playerInput.RMB)
		{
			float targetRot = trans.rotation.y + tpCam.camTrans.eulerAngles.y;
			currentRotation = Vector3.up * Mathf.SmoothDampAngle(currentRotation.y, targetRot, ref turnSmoothVelocity, TurnSmoothTime);
			trans.eulerAngles = currentRotation;
		}

		// Get target speed
		bool walkingBackwards = inputDir.z < 0f;
		running = playerInput.leftShift && !walkingBackwards && !(playerInput.RMB);
		float targetSpeed = ((running) ? RunSpeed : WalkSpeed) * inputDir.magnitude;
		currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedSmoothVelocity, SpeedSmoothTime);

		// Add gravity
		if(CheckGrounded())
			velocityY = 0f;
		else
			velocityY += gravity * Time.deltaTime;

		// Move character
		Vector3 forwardDir = trans.forward * inputDir.z + trans.right * inputDir.x;
		Vector3 velocity =  forwardDir * currentSpeed + Vector3.up * velocityY;
		//print("velocity is: " + velocity.magnitude);
		charController.Move(velocity * Time.deltaTime);
		currentSpeed = new Vector2(charController.velocity.x, charController.velocity.z).magnitude;

		// Animate character
		float runAnimPercent = currentSpeed / RunSpeed;
		float walkAnimPercent = currentSpeed / WalkSpeed * .49f;
		float forward = ((running) ? runAnimPercent : walkAnimPercent) * inputDir.z;
		float strafe = ((running) ? runAnimPercent : walkAnimPercent) * inputDir.x;

		anim.SetFloat("forward", forward, SpeedSmoothTime, Time.deltaTime);
		anim.SetFloat((playerInput.RMB) ? "aimstrafe" : "strafe", strafe, SpeedSmoothTime, Time.deltaTime);
	}

	private bool CheckGrounded() // Spherecasting downwards to check ground 
	{
		RaycastHit hit;
		Vector3 origin = charController.center;
		Vector3 dir = Vector3.down;

		if(Physics.SphereCast(origin, charController.radius, dir, out hit, charController.height * .5f))
			return true;
		else
			return false;
	}
}
