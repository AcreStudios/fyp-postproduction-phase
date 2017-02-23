using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoverSystem : MonoBehaviour
{
	// Components
	private Transform trans;
	private Animator anim;
	private PlayerInputManager playerInput;
	private PlayerController playerController;
	private ThirdPersonCamera tpCam;

	[Header("Debug")]
	public bool DebugCoverHelper = true;
	public bool DebugSearchRay = true;
	public bool DebugCoverRay = true;

	[Header("Getting Into Cover")]
	public float SearchDistance = 3f;
	public float MinCoverLength = .2f;
	public float GetIntoCoverWalkSpeed = 2f;
	public float GetIntoCoverRunSpeed = 4f;

	private bool hasCover;
	private bool initCover;
	private bool initLerp;
	private Vector3 startPos;
	private Vector3 targetPos;
	private float coverLerp;
	private CoverPositions coverPositions;


	[Header("In Cover")]
	public float OffsetFromWall = .6f;
	public float PeekDistance = .25f;
	public LayerMask WallLayer;
	public bool TwoPointValidation = true;
	public float ValidateWallGapDistance = .2f;
	public float CoverMovespeed = 2.5f;
	public float CoverTurnspeed = 5f;
	public float CoverAngleTolerance = 45f;
	public float AimAtSideMoveDistance = 1f;
	public float AimAtSideMovespeed = 2f;

	private Transform coverHelper;
	private GameObject coverCube;
	private Vector3 relativeInput;
	private bool movePositive;
	private int coverState = 0;
	private bool coverEdge;
	private bool wallOnSide;
	private bool aimingAtSide = false;
	private bool lerpingAim;
	private Vector3 originalPos;

	[Header("Exiting Cover")]
	public float ResetCoverCooldown = .5f;

	private bool canSearchForCover = true;
	private bool canLoseCover = false;


	void Awake()
	{
		trans = GetComponent<Transform>();
		anim = GetComponent<Animator>();
		playerController = GetComponent<PlayerController>();
	}
	
	void Start()
	{
		// Get manager
		playerInput = PlayerInputManager.GetInstance();
		tpCam = ThirdPersonCamera.GetInstance();
		
		// Create helper
		coverHelper = new GameObject("Cover Helper").transform;
		coverCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
		coverCube.transform.SetParent(coverHelper);
		coverCube.name = "Debug Cube";
		coverCube.transform.localScale = Vector3.one * .2f;
		coverCube.GetComponent<MeshRenderer>().material.color = Color.red;
		Destroy(coverCube.GetComponent<BoxCollider>());

		// Init cover
		coverPositions = new CoverPositions();
		canSearchForCover = true;
	}
	
	void Update()
	{
		// Debug cover
		coverCube.SetActive(DebugCoverHelper && hasCover);

		// Cover logic
		if(hasCover)
		{
			if(initCover)
				HandleCoverMovement();
			else
			{
				DisableController();
				LerpIntoCover();
			}
		}

		// Handle input temporary
		if(playerInput.coverKey)
		{
			if(!hasCover && canSearchForCover)
				RaycastForCover();
		}

		// Handle aim
		tpCam.SetCoverState(hasCover, coverEdge);
	}

	private void RaycastForCover()
	{
		Vector3 origin = trans.position + Vector3.up * .5f + -trans.forward;
		Vector3 dir = trans.forward;
		RaycastHit hit;

		float searchDist = (playerController.GetRunState() ? SearchDistance * 2f : SearchDistance);

		if(DebugSearchRay)
			Debug.DrawRay(origin, dir * searchDist, Color.cyan, 2f);
		if(Physics.SphereCast(origin, playerController.charController.radius, dir, out hit, searchDist, WallLayer))
		{
			if(hit.collider is BoxCollider)
			{
				// Position helper
				coverHelper.transform.position = PosWithOffset(origin, hit.point);
				coverHelper.transform.rotation = Quaternion.LookRotation(-hit.normal);

				// Check if there is cover

				bool right = CheckCoverValid(coverHelper, true);
				bool left = CheckCoverValid(coverHelper, false);

				if(right && left)
					hasCover = true;
			}
		}
	}

	private bool CheckCoverValid(Transform helper, bool right)
	{
		bool retVal = false;

		Vector3 side = (right) ? helper.right : -helper.right;
		side *= MinCoverLength;
		Vector3 origin = helper.position + side + -helper.forward;
		RaycastHit sideHit;

		// Check first if there is wall on side
		if(DebugCoverRay)
			Debug.DrawRay(origin, side * MinCoverLength, Color.gray, 1f);
		if(Physics.Raycast(origin, side, out sideHit, MinCoverLength, WallLayer))
			return false;
		else
		{
			origin += side;
			origin.z -= OffsetFromWall;
			Vector3 dir = helper.forward;
			RaycastHit wallHit;

			if(DebugCoverRay)
				Debug.DrawRay(origin, dir * SearchDistance, Color.magenta,2f);
			if(Physics.Raycast(origin, dir, out wallHit, SearchDistance, WallLayer))
			{
				if(wallHit.collider is BoxCollider)
				{
					retVal = true;

					if(right)
						coverPositions.posB = PosWithOffset(origin, wallHit.point);
					else
						coverPositions.posA = PosWithOffset(origin, wallHit.point);
				}
			}
			else
				return false;

		}

		return retVal;
	}

	private Vector3 PosWithOffset(Vector3 origin, Vector3 target) // Offset helper from wall vertically
	{
		Vector3 dir = origin - target;
		dir.Normalize();
		Vector3 offset = dir * OffsetFromWall;
		Vector3 retVal = target + offset;

		return retVal;
	}

	private void HandleCoverMovement()
	{
		relativeInput.x = playerInput.horizontal;
		relativeInput.z = playerInput.vertical;

		// If press back or button
		if(canLoseCover && (relativeInput.z < 0f || playerInput.coverKey))
		{
			EnableController();
			return;
		}

		if(relativeInput.x != 0f)
		{
			movePositive = (relativeInput.x > 0f);
			coverState = CheckCoverType();

			// Animate player
			anim.SetBool("mirror", !movePositive);
			anim.SetInteger("coverstate", coverState);

			// Switch camera side
			ThirdPersonCamera.Shoulder shoulder = (movePositive) ? ThirdPersonCamera.Shoulder.LEFT : ThirdPersonCamera.Shoulder.RIGHT;
			tpCam.SwitchShoulder(shoulder);
		}
		else
			movePositive = (tpCam.CurrentShoulder == ThirdPersonCamera.Shoulder.RIGHT) ? true : false;

		bool isCover = MoveSideways(movePositive);
		coverEdge = (coverState == 2) ? true : ((wallOnSide) ? false : !isCover);

		// Aim side
		if(coverState == 1 && coverEdge)
		{
			if(playerInput.RMB)
			{
				if(!lerpingAim && !aimingAtSide)
					StartCoroutine(LerpToSide());
			}
			else
			{
				if(!lerpingAim && aimingAtSide)
					StartCoroutine(LerpBack());
			}
		}


		if(TwoPointValidation)
		{
			if(!isCover)
				isCover = MoveSideways(movePositive, ValidateWallGapDistance);
		}

		Vector3 targetDir = (coverHelper.position - trans.position).normalized;
		targetDir *= Mathf.Abs(relativeInput.x);

		if(!isCover)
		{
			targetDir = Vector3.zero;
			relativeInput.x = 0f;
		}

		playerController.charController.SimpleMove(targetDir * CoverMovespeed);
		Quaternion targetRot = coverHelper.rotation;
		trans.rotation = Quaternion.Slerp(trans.rotation, targetRot, CoverTurnspeed * Time.deltaTime);
		anim.SetFloat("strafe", relativeInput.x, .15f, Time.deltaTime);
	}

	private IEnumerator LerpToSide()
	{
		lerpingAim = true;

		originalPos = trans.position;

		Vector3 targetAimPos = trans.position;
		targetAimPos += trans.right * ((movePositive) ? AimAtSideMoveDistance : -AimAtSideMoveDistance);

		SoundManager soundManager = SoundManager.GetInstance();
		soundManager.PlaySoundOnce(targetAimPos, soundManager.FloorTypes[0].footstepSounds[Random.Range(0, soundManager.FloorTypes[0].footstepSounds.Length)], 2f, .8f, true, .9f, .95f);

		float lerp = 0f;
		while(lerp < 1f)
		{
			lerp += AimAtSideMovespeed * Time.deltaTime;
			trans.position = Vector3.Lerp(trans.position, targetAimPos, lerp);
			yield return null;
		}
		lerp = 1f;
		trans.position = Vector3.Lerp(trans.position, targetAimPos, lerp);

		

		lerpingAim = false;
		aimingAtSide = true;
	}

	private IEnumerator LerpBack()
	{
		lerpingAim = true;

		SoundManager soundManager = SoundManager.GetInstance();
		soundManager.PlaySoundOnce(originalPos, soundManager.FloorTypes[0].footstepSounds[Random.Range(0, soundManager.FloorTypes[0].footstepSounds.Length)], 2f, .4f, true, .9f, .95f);

		float lerp = 0f;
		while(lerp < 1f)
		{
			lerp += AimAtSideMovespeed * Time.deltaTime;
			trans.position = Vector3.Lerp(trans.position, originalPos, lerp);
			yield return null;
		}
		lerp = 1f;
		trans.position = Vector3.Lerp(trans.position, originalPos, lerp);

		lerpingAim = false;
		aimingAtSide = false;
	}

	private void ReturnToSide()
	{
		Vector3 side = (movePositive) ? trans.right : -trans.right;
		Vector3 targetPos = trans.position - (side * AimAtSideMoveDistance);
		//print("targetPos: " + targetPos);
		trans.position = Vector3.Lerp(trans.position, targetPos, 5f * Time.deltaTime);
	}

	private bool MoveSideways(bool right, float offset = 0f)
	{
		bool retVal = false;

		Vector3 side = (right) ? coverHelper.right : -coverHelper.right;
		side *= PeekDistance + offset;
		Vector3 origin = trans.position + side;
		origin += Vector3.up * .5f;
		Vector3 dir = coverHelper.transform.forward;
		RaycastHit hit;

		if(DebugCoverRay)
			Debug.DrawRay(origin, side * PeekDistance, Color.yellow);
		if(Physics.Raycast(origin, side, out hit, PeekDistance, WallLayer))
		{
			wallOnSide = true;
			return false;
		}
		else
		{
			RaycastHit towards;
			origin += side;

			if(DebugCoverRay)
				Debug.DrawRay(origin, dir * (OffsetFromWall + 1f), Color.yellow);
			if(Physics.Raycast(origin, dir, out towards, OffsetFromWall + 1f, WallLayer))
			{
				if(towards.collider is BoxCollider)
				{
					float angle = Vector3.Angle(coverHelper.forward, -towards.normal);

					if(angle < CoverAngleTolerance)
					{
						retVal = true;
						coverHelper.position = PosWithOffset(origin, towards.point);
						coverHelper.rotation = Quaternion.LookRotation(-towards.normal);
					}
				}
			}
			else return false;

			wallOnSide = false;
		}

		return retVal;
	}

	private void LerpIntoCover()
	{
		if(!initLerp)
		{
			initLerp = true;

			startPos = trans.position;
			targetPos = Vector3.Lerp(coverPositions.posA, coverPositions.posB, .5f);
			coverLerp = 0f;

			// Initial facing dir
			coverState = CheckCoverType();
			anim.SetInteger("coverstate", coverState);

			StartCoroutine(SearchCoverAgainDelay(false));
		}

		float movement = (playerController.GetRunState())? GetIntoCoverRunSpeed * Time.deltaTime : GetIntoCoverWalkSpeed * Time.deltaTime;
		coverLerp += movement;

		//float lerpMovement = movement / length;

		if(coverLerp > 1f)
		{
			coverLerp = 1f;
			initCover = true;
			canSearchForCover = false;
		}

		// Move player, clamp y
		Vector3 pos = Vector3.Lerp(startPos, targetPos, coverLerp);
		pos.y = trans.position.y;
		trans.position = pos;

		Quaternion targetRot = Quaternion.LookRotation(coverHelper.transform.forward);
		trans.rotation = Quaternion.Slerp(trans.rotation, targetRot, coverLerp);
	}

	private int CheckCoverType()
	{
		int coverTypeInt = 2;

		Vector3 origin = coverHelper.position + Vector3.up;
		Vector3 dir = coverHelper.forward;
		RaycastHit hit;

		if(Physics.Raycast(origin, dir, out hit, 1f, WallLayer))
			coverTypeInt = (hit.collider is BoxCollider) ? 1 : 2; // 1 is full, 2 is crouch

		if(coverTypeInt == 2)
		{
			playerController.charController.center = new Vector3(0f, .75f, 0f);
			playerController.charController.height = 1.3f;
		}
		else
		{
			playerController.charController.center = new Vector3(0f, 1f, 0f);
			playerController.charController.height = 1.7f;
		}


		return coverTypeInt;
	}

	public void EnableController() // Reset
	{
		initLerp = false;
		initCover = false;
		hasCover = false;
		StartCoroutine(SearchCoverAgainDelay(true));

		coverState = 0;
		anim.SetInteger("coverstate", coverState);
		anim.SetBool("mirror", false);

		playerController.SetCoverState(false);
	}

	private IEnumerator SearchCoverAgainDelay(bool state)
	{
		yield return new WaitForSeconds(ResetCoverCooldown);
		canSearchForCover = state;
		canLoseCover = !state;
	}

	private void DisableController()
	{
		playerController.SetCoverState(true);
	}
}

public class CoverPositions
{
	public Vector3 posA;
	public Vector3 posB;
}
