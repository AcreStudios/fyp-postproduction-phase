using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleWeaponPickup : MonoBehaviour 
{
    WeaponSystem wpnSystem;


	void OnTriggerEnter(Collider col) 
    {
        if(col.CompareTag("Player")) 
        {
            WeaponSystem ws = col.GetComponent<WeaponSystem>();
            if (ws)
                wpnSystem = ws;
        }
    }

    void OnTriggerStay(Collider col) 
    {
        if (col.CompareTag("Player")) 
        {
            if (wpnSystem) 
            {
                if (Input.GetKeyDown(KeyCode.E)) {
                    wpnSystem.PickUpWeapon();
                    Destroy(gameObject);
                }
                    
            }
        }
    }
}
