using UnityEngine;
using System.Collections;

public class TrailEffectFade : MonoBehaviour {

    // Use this for initialization
    public Renderer toGetColor;
    public Transform startPoint;
    public Vector3 endPoint;
    public float rateOfDecay;
    public float speed;
    float currentA;

    void Start() {
        toGetColor = GetComponent<Renderer>();

    }
    public void ObjectActive(Vector3 destination) {
        currentA = 1;
        endPoint = destination;
        gameObject.SetActive(true);
    }

    void Update() {
        if (currentA > 0) {
            transform.position = Vector3.MoveTowards(transform.position, endPoint, speed);

            if (transform.position == endPoint) {
                currentA -= rateOfDecay;
                toGetColor.material.color = new Color(1, 1, 1, currentA);
            }
        } else {
            transform.position = startPoint.position;
            gameObject.SetActive(false);
        }

    }
}
