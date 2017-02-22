using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RagdollHandler : MonoBehaviour 
{
	private Collider[] cols;
	private Rigidbody[] rbs;
	private Animator animator;

	private void Awake()
	{
		cols = GetComponentsInChildren<Collider>();
		rbs = GetComponentsInChildren<Rigidbody>();
		animator = transform.parent.GetComponent<Animator>();
	}

	private void Start()
	{
		// Setup
		ToggleRagdoll(true);
	}

	public void BecomeRagdoll()
	{
		if(!animator || cols.Length == 0 || rbs.Length == 0)
			return;

		ToggleRagdoll(false);
	}

	void ToggleRagdoll(bool state) // Toggle colliders and rigidbodies
	{
		animator.enabled = state;

		foreach(Collider col in cols)
		{
			col.isTrigger = state;
		}
		foreach(Rigidbody rb in rbs)
		{
			rb.isKinematic = state;
		}
	}
}
