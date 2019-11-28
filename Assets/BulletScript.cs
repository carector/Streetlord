using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletScript : MonoBehaviour
{
    public Rigidbody rb;
    public AudioClip explode;
    AudioSource audio;
    
    // Start is called before the first frame update
    void Start()
    {
        audio = GameObject.Find("Turret").GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
                
    }

    private void OnTriggerEnter(Collider other)
    {
        //audio.PlayOneShot(explode);
        Destroy(this.gameObject);
    }
}
