using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMove : MonoBehaviour
{

    public float speed = 0.1f;
    public Transform target;
    public AudioSource transition;

    public void setAnchor(Transform newAnchor)
    {
        transition.Play();
        target = newAnchor;
    }

    public void Quit()
    {
        Application.Quit();
        Debug.Log("Application quit");
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = Vector3.Lerp(transform.position, target.position, speed);
    }

}
