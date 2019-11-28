using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControl : MonoBehaviour
{
    public GameObject target;

    Vector3 offset;

    // Start is called before the first frame update
    void Start()
    {
        offset = target.transform.position - transform.position;        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = target.transform.position - offset;
        transform.rotation = Quaternion.LookRotation(target.transform.position - transform.position);
    }
}
