using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class pinchZoom : MonoBehaviour {

    public float perspectiveZoomSpeed = .5f;
    public float orthoZoomSpeed = .5f;

    private void Start()
    {
        GetComponent<Camera>().orthographicSize = 20;
    }

    // Update is called once per frame
    void Update () {
		if(Input.touchCount == 2)
        {
            //print("hi");
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

            float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

            float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;

            if (GetComponent<Camera>().orthographic)
            {
                float newCameraPosition = GetComponent<Camera>().orthographicSize + deltaMagnitudeDiff * orthoZoomSpeed;

                newCameraPosition = Mathf.Max(newCameraPosition, 5f);
                newCameraPosition = Mathf.Min(newCameraPosition, 200f);

                GetComponent<Camera>().orthographicSize = newCameraPosition;
            } else
            {
                //we're not using FoV camera at this time
                /*
                // Otherwise change the field of view based on the change in distance between the touches.
                GetComponent<Camera>().fieldOfView += deltaMagnitudeDiff * perspectiveZoomSpeed;

                // Clamp the field of view to make sure it's between 0 and 180.
                GetComponent<Camera>().fieldOfView = Mathf.Clamp(GetComponent<Camera>().fieldOfView, 0.1f, 179.9f);
                */
            }
        }
	}
}
