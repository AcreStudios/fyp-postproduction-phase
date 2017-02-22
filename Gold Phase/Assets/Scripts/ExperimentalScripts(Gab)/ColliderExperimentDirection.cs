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
        if (CheckCollider(colliders, target.position, startingPoint.position))
            Debug.DrawLine(startingPoint.position, target.position, Color.black);
    }

    bool CheckCollider(Collider[] colliders, Vector3 targetPos, Vector3 startPos) {
        float targetGradient = CalculateGradient(startPos - targetPos);
        foreach (Collider collider in colliders) {
            Vector3 temp = Vector3.zero;

            temp = collider.bounds.center - target.position;

            temp.x = (temp.x / Mathf.Abs(temp.x)) * collider.bounds.extents.x;
            temp.z = (temp.z / Mathf.Abs(temp.z)) * collider.bounds.extents.z;

            temp.x *= -1;
            Debug.DrawLine(startPos, collider.bounds.center + temp, Color.red);
            Debug.DrawLine(startPos, collider.bounds.center - temp, Color.red);
            Debug.Log(targetGradient);
            Debug.Log(CalculateGradient(startPos - collider.bounds.center + temp));
            Debug.Log(CalculateGradient(startPos - collider.bounds.center - temp));

            if (CalculateGradient(startPos - collider.bounds.center + temp) >= targetGradient && targetGradient >= CalculateGradient(startPos - collider.bounds.center - temp))
                return false;

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
