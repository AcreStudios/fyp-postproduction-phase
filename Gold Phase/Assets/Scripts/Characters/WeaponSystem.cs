using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class WeaponSystem : MonoBehaviour
{
	// Components
	private Animator anim;
	private PlayerInputManager playerInput;
	private ThirdPersonCamera tpCam;
	private CombatUI combatUI;
	private Animator crosshairAnim;
	private PlayerController playerController;
	private SoundManager soundManager;

	[Header("Debug")]
	public bool DebugAim = false;
	public bool DebugWeaponRay = true;
	public bool DebugAimRay = true;
	public bool DebugShootRay = true;

	[Header("Weapon Setup")]
	public Transform BulletSpawnPoint;
	public GameObject MuzzleFlash;
	public Transform MuzzleSpawnPoint;

	[Header("Weapon Settings")]
	public float Damage = 33f;
	public float HeadshotMultiplier = 3f;
	public float LimbshotMultiplier = .5f;
	public float AttackRate = .3f;
	public float AttackRange = 100f;
	public bool AutoReload = false;
	public float ReloadDuration = 2f;
	public bool UseBulletSpread = true;
	public float BulletSpreadAmount = .02f;
	public LayerMask BulletLayer;

	private RaycastHit hit;
	private bool aiming;
	private bool shootCooldown;
	private bool reloading;
	private bool empty;
	private bool playAimSound;

	[Header("Ammo Settings")]
	public int CurrentAmmo = 12;
	public int MaxAmmo = 12;

	[Header("Model Settings")]
	public bool DebugSpine = false;
	public Transform DebugSpineTrans;
	public float SpineOffsetX = -12f;
	public float SpineOffsetY = -6f;
	public float AimSpineOffsetZ = 10f;


	void Awake() 
	{
		// Cache
		anim = GetComponent<Animator>();
		playerController = GetComponent<PlayerController>();
	}
	
	void Start() 
	{
		// Get manager
		playerInput = PlayerInputManager.GetInstance();
		tpCam = ThirdPersonCamera.GetInstance();
		combatUI = CombatUI.GetInstance();
		soundManager = SoundManager.GetInstance();
	}
	
	void Update() 
	{
		UpdateWeaponLogic();
	}

	void LateUpdate() 
	{
		if(aiming && DebugSpine)
			RotateSpine();
	}

	private void UpdateWeaponLogic() 
	{
		// Aim
		aiming = tpCam.GetAimState() || DebugAim;
		if(aiming)
			Aim();

		if(!aiming)
			playAimSound = false;

		anim.SetBool("aim", aiming);
		combatUI.ToggleUI((aiming) ? true : false);

		// Reload
		if(playerInput.reloadKey)
			Reload();
	}

	private void Aim() 
	{
		// Debug
		if(BulletSpawnPoint && aiming && (DebugAim || DebugWeaponRay))
		{
			Transform bPoint = BulletSpawnPoint;
			Debug.DrawRay(bPoint.position, bPoint.forward * AttackRange, Color.red);
		}

		// Raycast forward
		Transform mainCamTrans = tpCam.camTrans;
		Vector3 start = mainCamTrans.position;
		Vector3 dir = mainCamTrans.forward;

		if(DebugAimRay)
			Debug.DrawRay(start, dir * AttackRange, Color.blue);

		if(Physics.Raycast(start, dir, out hit, AttackRange, BulletLayer))
		{
			// Shoot
			if(playerInput.LMB)
				Shoot(start, dir, hit);
		}

		// Sounds
		if(!playAimSound)
		{
			playAimSound = true;

			soundManager.PlaySoundOnce(BulletSpawnPoint.position, soundManager.sounds.aimClip, 2f, 1f);
		}

		// Rotate model
		RotateSpine();
	}

	private IEnumerator FinishEmptyFireSound() 
	{
		soundManager.PlaySoundOnce(BulletSpawnPoint.position, soundManager.sounds.emptyClip, 2f, 1f);
		yield return new WaitForSeconds(AttackRate);
		empty = false;
	}

	private void Shoot(Vector3 start, Vector3 dir, RaycastHit hit) // Shoot bullet 
	{
		if(shootCooldown || !BulletSpawnPoint)
			return;

		// Auto reload?
		if(AutoReload && CurrentAmmo == 0)
		{
			Reload();
			if(!empty)
			{
				empty = true;
				StartCoroutine(FinishEmptyFireSound());
			}
			return;
		}

		if(reloading) return;

		// Bullet spread
		dir += (Vector3)UnityEngine.Random.insideUnitCircle * BulletSpreadAmount;

		if(DebugShootRay)
			Debug.DrawRay(start, dir * AttackRange, Color.green, 2f);

		if(Physics.Raycast(start, dir, out hit, AttackRange, BulletLayer))
		{
			EnemyHealth hp = hit.transform.GetComponent<EnemyHealth>();
			if(hp && hp.isActiveAndEnabled)
			{
				if(hit.collider is SphereCollider)
					hp.ReceiveDamage(Damage * HeadshotMultiplier);
				else if(hit.collider is CapsuleCollider)
					hp.ReceiveDamage(Damage * LimbshotMultiplier);
				else if(hit.collider is BoxCollider)
					hp.ReceiveDamage(Damage);
				else
					print("Cant shoot enemy body part! Remove " + hit.collider);

				combatUI.TriggerHitEnemy();
			}
			else
				Debug.Log("Did not hit an enemy. Hit " + hit.transform.name + " instead!");

			AIFunctions ai = hit.transform.GetComponent<AIFunctions>();
			if(ai && ai.isActiveAndEnabled)
				ai.DamageRecieved();

			BulletImpactLogic(hit);
		}

		// Muzzle flash
		if(MuzzleFlash)
		{
			Vector3 bSpawnPos = MuzzleSpawnPoint.position;
			GameObject mFlash = Instantiate(MuzzleFlash, bSpawnPos, Quaternion.identity);
			Transform mFlashTrans = mFlash.transform;
			mFlashTrans.SetParent(MuzzleSpawnPoint);
			Destroy(mFlash, .5f);
		}

		// Sounds
		soundManager.PlaySoundOnce(BulletSpawnPoint.position, soundManager.sounds.fireClip, 2f, .5f, true, .8f, 1.1f);

		// Shoot cooldown
		StartCoroutine(FinishShooting());

		// Update ammo
		CurrentAmmo--;

		if(combatUI)
			combatUI.UpdateAmmobar(CurrentAmmo);

		// Animate
		anim.SetTrigger("shoot");
	}

	private IEnumerator FinishShooting () // Loads next bullet; Cooldown interval between bullets
	{
		shootCooldown = true;
		yield return new WaitForSeconds(AttackRate);
		shootCooldown = false;
	}

	private void BulletImpactLogic(RaycastHit hit) // Logic when a bullet hits something 
	{
		if(soundManager.BulletImpacts.Length == 0) return;
		
		foreach(BulletImpactType bType in soundManager.BulletImpacts)
		{
			foreach(Material mat in bType.Mats)
			{
				Renderer rend = hit.collider.GetComponent<Renderer>();

				if(rend)
				{
					if(rend.material.mainTexture == mat.mainTexture)
					{
						// Decal prefabs
						if(bType.ImpactPrefab)
						{
							Vector3 hitPoint = hit.point;
							Quaternion lookRot = Quaternion.LookRotation(hit.normal);
							GameObject decal = Instantiate(bType.ImpactPrefab, hitPoint, lookRot);
							Transform decalTrans = decal.transform;
							Transform hitTrans = hit.transform;
							decalTrans.SetParent(hitTrans);
							Destroy(decal, 20f);
						}

						// Sound
						soundManager.PlaySoundOnce(hit.point, bType.ImpactSounds[UnityEngine.Random.Range(0, bType.ImpactSounds.Length)]);
					}
				}

				
			}
		}
	}

	private void Reload() 
	{
		if(reloading || CurrentAmmo == MaxAmmo) return;

		// Play reload sound
		//if(soundManager)
		//{
		//	if(activeWeapon.soundSettings.reloadSound && activeWeapon.soundSettings.audioSrc)
		//	{
		//		soundManager.PlaySound(activeWeapon.soundSettings.audioSrc,
		//			activeWeapon.soundSettings.reloadSound,
		//			true,
		//			activeWeapon.soundSettings.pitchMin,
		//			activeWeapon.soundSettings.pitchMax);
		//	}
		//}

		// Animate
		anim.SetTrigger("reload");

		StartCoroutine(FinishReloading());
	}

	public void PlayReloadSound()
	{
		soundManager.PlaySoundOnce(BulletSpawnPoint.position, soundManager.sounds.reloadClip, 2f, 1f);
	}

	private IEnumerator FinishReloading() 
	{
		reloading = true;
		yield return new WaitForSeconds(ReloadDuration);
		LoadAmmo();
		reloading = false;
	}

	private void LoadAmmo() 
	{
		CurrentAmmo = MaxAmmo;

		if(combatUI)
			combatUI.UpdateAmmobar(CurrentAmmo);
	}

	private void RotateSpine() // Makes the character spine face center 
	{
		Vector3 newRot = new Vector3(tpCam.camTrans.parent.localPosition.x + SpineOffsetX, tpCam.camTrans.parent.localPosition.y + SpineOffsetY, -tpCam.newY + ((playerController.inCover) ? AimSpineOffsetZ : 0f));

		DebugSpineTrans.Rotate(newRot);
	}
}
