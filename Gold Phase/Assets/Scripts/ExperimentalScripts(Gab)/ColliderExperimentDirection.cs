using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColliderExperimentDirection : MonoBehaviour {

    public Transform startingPoint;
    public Transform target;
    public Collider[] colliders;
    // Use this for initialization
    void Start() {

    }

    // Update is called once per frame
    void Update() {
        if (CheckCollider(colliders,  startingPoint.position - target.position))
            Debug.DrawLine(startingPoint.position, target.position, Color.black);
    }

    bool CheckCollider(Collider[] colliders, Vector3 givenVector) {
        Vector3 gradient = Mathf.Abs(givenVector.x) >= Mathf.Abs(givenVector.z) ? givenVector / Mathf.Abs(givenVector.x) : givenVector / Mathf.Abs(givenVector.z);
        Debug.Log(gradient.x + gradient.z);

        foreach (Collider collider in colliders) {

        }
        return true;
    }

    float CalculateGradient(Vector3 difference) {
        if (difference.x == 0)
            return 0;
        if (difference.z == 0)
            return 1;

        return difference.x / Mathf.Abs(difference.z);
    }
}
