using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour {
    public float panSpeed = 1f;
    public float panBorderThickness = 5f;

    // Update is called once per frame
    void Update () {
        Vector3 position = transform.position;

        if (Input.GetKey ("up")) {
            position.y += panSpeed * Time.deltaTime;
        }

        if (Input.GetKey ("down")) {
            position.y -= panSpeed * Time.deltaTime;
        }

        if (Input.GetKey ("left")) {
            position.x -= panSpeed * Time.deltaTime;
        }

        if (Input.GetKey ("right")) {
            position.x += panSpeed * Time.deltaTime;
        }

        transform.position = position;
    }
}