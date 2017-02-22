using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Footsteps : MonoBehaviour
{
	// Component
	private Transform trans;
	private SoundManager soundManager;
	private PlayerController playerController;

	[Header("Debug")]
	public bool DebugFootsteps = false;

	[Header("Floor Settings")]
	public float RaycastDownDistance = 1.3f;
	public LayerMask GroundLayer;

	[Header("Sounds Settings"), Range(0f, 1f)]
	public float MasterVolume = 1f;
	public bool RandomizePitch = true;
	public float MinPitch = .9f;
	public float MaxPitch = 1.1f;
	public float stepsDelay = 0.02f;

	private bool playingOneFootstep;


	void Awake()
	{
		trans = GetComponent<Transform>();
		PlayerController pc = GetComponent<PlayerController>();
		if(pc)
			playerController = pc;
	}
	
	void Start() 
	{
		soundManager = SoundManager.GetInstance();
	}

	public void PlayFootstepSound(float vol) // Raycast down from each foot
	{
		if(playingOneFootstep) return;

		if(playerController && playerController.footstepVelocity.magnitude < .1f) return;

		playingOneFootstep = true;

		RaycastHit hit;
		Vector3 start = trans.position + trans.up;
		Vector3 dir = Vector3.down;

		if(DebugFootsteps)
			Debug.DrawRay(start, dir * RaycastDownDistance, Color.cyan, 2f);
		if(Physics.Raycast(start, dir, out hit, RaycastDownDistance, GroundLayer))
		{
			MeshRenderer mRend = hit.collider.GetComponent<MeshRenderer>();
			if(mRend)
				StartCoroutine(PlayMeshSound(mRend, vol, hit.point));
		}
	}

	private IEnumerator PlayMeshSound(MeshRenderer rend, float vol, Vector3 hitPos) // Compare material to footstep sound
	{
		if(soundManager.FloorTypes.Length > 0) // If we defined a ground type
		{
			foreach(FloorMaterialType floor in soundManager.FloorTypes)
			{
				if(floor.footstepSounds.Length > 0) // If we have footsteps
				{
					foreach(Material mat in floor.mats)
					{
						//print((rend.material.mainTexture == mat.mainTexture));

						if(rend.material.mainTexture == mat.mainTexture) // Compare
							// If we have a sound manager
							soundManager.PlaySoundOnce(hitPos, floor.footstepSounds[UnityEngine.Random.Range(0, floor.footstepSounds.Length)], 2f, vol * MasterVolume, RandomizePitch, MinPitch, MaxPitch);
					}
				}
			}
		}

		yield return new WaitForSeconds(stepsDelay);

		playingOneFootstep = false;
	}
}


