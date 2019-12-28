using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour {
    public float panSpeed = 0.6f;
    public float panBorderThickness = 5f;

    public float zoomSpeed = 10.0f;
    public float targetCameraSize;
    public float smoothSpeed = 2.0f;
    public float minZoomSize = 1.0f;
    public float maxZoomSize = 10.0f;

    Camera camera;
    public float halfHeight;
    public float halfWidth;

    private Map map;

    void Awake () {
        map = FindObjectOfType<Map> ();
        camera = GetComponent<Camera> ();
    }

    void Start () {
        targetCameraSize = Camera.main.orthographicSize;

        halfHeight = camera.orthographicSize;
        halfWidth = camera.aspect * halfHeight;
    }

    // Update is called once per frame
    void Update () {
        Vector3 position = transform.position;

        float boardHeight = map.height;
        float boardWidht = map.width;

        float panDistance = panSpeed * Time.deltaTime;

        if (Input.GetKey ("up") ||
            (!Application.isEditor && Screen.fullScreen && Input.mousePosition.y > Screen.height - panBorderThickness)) {

            if (transform.position.y + halfHeight + (panDistance) <= boardHeight) {
                position.y += panDistance;
            }

        }

        if (Input.GetKey ("down") ||
            (!Application.isEditor && Screen.fullScreen && Input.mousePosition.y < panBorderThickness)) {

            if (transform.position.y - halfHeight - (panDistance) >= -2) {
                position.y -= panDistance;
            }
        }

        if (Input.GetKey ("left") ||
            (!Application.isEditor && Screen.fullScreen && Input.mousePosition.x < panBorderThickness)) {

            if (transform.position.x - halfWidth - (panDistance) >= -2) {
                position.x -= panDistance;
            }

        }

        if (Input.GetKey ("right") ||
            (!Application.isEditor && Screen.fullScreen && Input.mousePosition.x > Screen.width - panBorderThickness)) {

            if (transform.position.x + halfWidth + (panDistance) <= boardWidht) {
                position.x += panDistance;
            }
        }

        float scroll = Input.GetAxis ("Mouse ScrollWheel");
        if (scroll != 0.0f) {
            targetCameraSize -= scroll * zoomSpeed;
            targetCameraSize = Mathf.Clamp (targetCameraSize, minZoomSize, maxZoomSize);
        }

        camera.orthographicSize = Mathf.MoveTowards (Camera.main.orthographicSize, targetCameraSize, smoothSpeed * Time.deltaTime);

        transform.position = position;

        halfHeight = camera.orthographicSize;
        halfWidth = camera.aspect * halfHeight;
    }
}