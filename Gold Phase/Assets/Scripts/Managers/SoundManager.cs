using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class SoundManager : MonoBehaviour
{
	// Singleton
	public static SoundManager instance;
	public static SoundManager GetInstance()
	{
		return instance;
	}

	public FloorMaterialType[] FloorTypes;
	public BulletImpactType[] BulletImpacts;
	public SoundEffects sounds;

	void Awake() 
	{
		// Implement singleton
		instance = this;
	}

	public void PlaySoundOnce(Vector3 pos, AudioClip aClip, float lifeTime = 2f, float vol = 1f, bool randomPitch = false, float minRandomPitch = 1f, float maxRandomPitch = 1f)
	{
		// Creates a 3D sound at target location
		GameObject os = new GameObject("OneShotAudio_" + aClip.name);
		os.transform.position = pos;
		AudioSource a = os.AddComponent<AudioSource>();
		a.volume = vol;
		a.spatialBlend = 1f;
		a.clip = aClip;
		a.Play();

		// Plays sound on their own audio source
		if(randomPitch)
			a.pitch = UnityEngine.Random.Range(minRandomPitch, maxRandomPitch);

		// Destroy sound after specific duration
		Destroy(os, lifeTime);
	}
}

[Serializable]
public class FloorMaterialType
{
	public string groundName;
	public Material[] mats;
	public AudioClip[] footstepSounds;
}

[Serializable]
public class BulletImpactType
{
	public string ImpactType;
	public GameObject ImpactPrefab;
	public Material[] Mats;
	public AudioClip[] ImpactSounds;
}

[Serializable]
public class SoundEffects
{
	public AudioClip aimClip;
	public AudioClip fireClip;
	public AudioClip reloadClip;
	public AudioClip emptyClip;
}