using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarControl : MonoBehaviour
{
    public float acceleration;
    public float drivingStiffness = 2;
    public float brakingStiffness = 5;
    public float steeringAngle;
    public float maxSpeed;
    public bool isHandbraking;
    public bool isSkidding;
    public bool canShoot;
    public Transform turret;
    public Transform bulletInstantiatePos;
    public Transform centerOfMass;
    public int gear;
    public GameObject turretShaft;
    public GameObject bullet;
    public GameObject torpedo;
    public AudioClip shootSfx;
    public List<WheelCollider> throttleWheels;
    public List<WheelCollider> steeringWheels;
    public List<Transform> wheelRims;
    public List<TrailRenderer> skidTrails;

    float strengthCoefficient = 25000;

    AudioSource sfx;
    AudioSource skidAudio;
    AudioSource turretRotateSfx;
    Rigidbody rb;
    Vector3 previousTurretPosition;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = centerOfMass.localPosition;
        turretRotateSfx = bulletInstantiatePos.GetComponent<AudioSource>();
        sfx = GetComponent<AudioSource>();
        previousTurretPosition = bulletInstantiatePos.parent.parent.rotation.eulerAngles;
        skidAudio = GameObject.Find("SkidAudio").GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        // Rotate turret towards mouse
        Vector3 mousePos = Input.mousePosition;
        Vector3 objectPos = Camera.main.WorldToScreenPoint(turret.position);
        mousePos.x = mousePos.x - objectPos.x;
        mousePos.y = mousePos.y - objectPos.y;

        float angle = Mathf.Atan2(mousePos.x, mousePos.y) * Mathf.Rad2Deg;
        turret.rotation = Quaternion.Euler(new Vector3(0, angle, 0));

        
        // Gear pitch is controlled by speed + current gear
        sfx.pitch = 0.5f + (transform.InverseTransformDirection(rb.velocity).z * 0.1f - gear * 0.1f) / gear;

        // Shift gears to adjust engine sound
        if (sfx.pitch >= 1.25f - gear * 0.025f && gear < 5)
            gear++;
        else if (sfx.pitch <= 0.75f && gear > 1)
            gear--;

        // Move our car
        MoveCar();

        if (Input.GetKeyDown(KeyCode.Alpha1))
            canShoot = true;
        if (Input.GetKeyDown(KeyCode.Alpha2))
            canShoot = false;

        // Check if we can shoot, make our turret appear if so
        if (canShoot)
        {
            // Move the turret upwards
            turret.localPosition = Vector3.Lerp(turret.localPosition, new Vector3(turret.localPosition.x, 0.5f, turret.localPosition.z), 0.25f);

            // Turret rotation sound is louder if the turret has rotated more
            turretRotateSfx.volume = Mathf.Clamp(Vector3.Distance(previousTurretPosition, bulletInstantiatePos.parent.parent.rotation.eulerAngles), 0, 0.6f);
            turretRotateSfx.pitch = Mathf.Clamp(Vector3.Distance(previousTurretPosition, bulletInstantiatePos.parent.parent.rotation.eulerAngles) / 5, 0, 1f);

            previousTurretPosition = bulletInstantiatePos.parent.parent.rotation.eulerAngles;

            // Launch bomb if left mouse button is pressed
            if (Input.GetMouseButtonDown(0))
            {
                Rigidbody b = Instantiate(bullet, bulletInstantiatePos.position, bulletInstantiatePos.rotation).GetComponent<Rigidbody>();
                b.velocity = rb.velocity;
                b.AddRelativeForce(new Vector3(0, 0, 1000));
                turret.GetComponent<AudioSource>().PlayOneShot(shootSfx);
                turretShaft.transform.localScale = new Vector3(0.5f, 2.5f, 2.5f);
                turretShaft.transform.localPosition = new Vector3(0.5f, 0, 0);

            }

            // Launch torpedo if right mouse button is pressed
            /*if (Input.GetMouseButtonDown(1))
            {
                Instantiate(torpedo, bulletInstantiatePos.position, bulletInstantiatePos.rotation);
                turret.GetComponent<AudioSource>().PlayOneShot(shootSfx);
                turretShaft.transform.localScale = new Vector3(0.5f, 2.5f, 2.5f);
                turretShaft.transform.localPosition = new Vector3(0.5f, 0, 0);
            }
            */
            turretShaft.transform.localScale = Vector3.Lerp(turretShaft.transform.localScale, Vector3.one, 0.1f);
            turretShaft.transform.localPosition = Vector3.Lerp(turretShaft.transform.localPosition, Vector3.zero, 0.2f);
        }
        else
        {
            // Hide the turret
            if(turretShaft.transform.localPosition.x >= 0.3f)
                turret.localPosition = Vector3.Lerp(turret.localPosition, new Vector3(turret.localPosition.x, 0.18f, turret.localPosition.z), 0.1f);
            turretShaft.transform.localScale = Vector3.Lerp(turretShaft.transform.localScale, new Vector3(0.25f, 1f, 1f), 0.1f);
            turretShaft.transform.localPosition = Vector3.Lerp(turretShaft.transform.localPosition, new Vector3(0.5f, 0, 0), 0.1f);
            turretRotateSfx.volume = Mathf.Lerp(turretRotateSfx.volume, 0, 0.25f);
        }
    }

    bool isGrounded()
    {
        bool grounded = false;

        foreach (WheelCollider wheel in throttleWheels)
        {
            if (wheel.isGrounded)
                grounded = true;
        }
        return grounded;
    }

    // Handles movement, steering, and braking
    void MoveCar()
    {
        // Forward movement
        if (Input.GetAxis("Vertical") != 0)
        {
            float torque = strengthCoefficient * Input.GetAxis("Vertical") * acceleration * Time.deltaTime;

            foreach (WheelCollider wheel in throttleWheels)
            {
                float speed = rb.velocity.magnitude * 1.5f;

                print(speed);

                if (Mathf.Abs(speed) < maxSpeed)
                {
                    wheel.motorTorque = torque;
                    wheel.brakeTorque = 0;
                }
                else
                {

                    if (Mathf.Abs(wheel.rpm) > 250)
                    {
                        wheel.motorTorque = 0;
                        wheel.brakeTorque = strengthCoefficient;
                    }
                }
            }
        }
        else
        {
            foreach (WheelCollider wheel in throttleWheels)
            {
                // Limit RPM
                if (Mathf.Abs(wheel.rpm) > 250)
                {
                    wheel.motorTorque = 0;
                    wheel.brakeTorque = strengthCoefficient;
                }
            }
        }

        // Make our rims spin too
        foreach (Transform t in wheelRims)
        {
            if (!isHandbraking)
                t.Rotate(new Vector3(0, transform.InverseTransformDirection(rb.velocity).z, 0));
        }

        // Steering
        foreach (WheelCollider wheel in steeringWheels)
        {
            // Powerslide if shift is held
            if (Input.GetKey(KeyCode.LeftShift) && transform.InverseTransformDirection(rb.velocity).z >= 7)
            {
                steeringAngle = 45;

                WheelFrictionCurve wff = wheel.forwardFriction;
                WheelFrictionCurve wf = wheel.sidewaysFriction;
                wf.extremumValue = 1f;
                wf.asymptoteValue = 1f;
                wf.stiffness = 2;
                wff.stiffness = 7;
                wheel.sidewaysFriction = wf;

                foreach(WheelCollider wheel2 in throttleWheels)
                    wheel2.forwardFriction = wff;
            }
            else
            {
                steeringAngle = 30;
                WheelFrictionCurve wff = wheel.forwardFriction;
                WheelFrictionCurve wf = wheel.sidewaysFriction;
                wf.extremumValue = 0.5f;
                wf.asymptoteValue = 0.65f;
                wf.stiffness = 7;
                wff.stiffness = 5;
                wheel.sidewaysFriction = wf;

                foreach (WheelCollider wheel2 in throttleWheels)
                    wheel2.forwardFriction = wff;
            }

            wheel.steerAngle = steeringAngle * Input.GetAxis("Horizontal");
            wheel.transform.localEulerAngles = new Vector3(0, steeringAngle * Input.GetAxis("Horizontal"), -90);
        }

        
        // Handbrake if space is pressed
        if (Input.GetKey(KeyCode.LeftControl))
        {
            if (isGrounded())
            {
                isHandbraking = true;

                if (Mathf.Abs(transform.InverseTransformDirection(rb.velocity).z) >= 1)
                    isSkidding = true;
                else
                    isSkidding = false;
            }

            foreach (WheelCollider wheel in throttleWheels)
            {
                WheelFrictionCurve wf = wheel.forwardFriction;
                wf.stiffness = brakingStiffness;
                wheel.forwardFriction = wf;
                wheel.motorTorque = 0;
                wheel.brakeTorque = strengthCoefficient * 15;
            }
        }
        else
        {
            isHandbraking = false;
            foreach (WheelCollider wheel in throttleWheels)
            {
                WheelFrictionCurve wf = wheel.forwardFriction;
                wf.stiffness = drivingStiffness;
                wheel.forwardFriction = wf;
            }
        }

        // Check if we're skidding while turning
        foreach (WheelCollider wheel in steeringWheels)
        {
            WheelHit hit;
            if (wheel.GetGroundHit(out hit))
            {
                if (Mathf.Abs(hit.sidewaysSlip) >= 0.7f && isGrounded())
                {
                    isSkidding = true;
                }
                else if(!isHandbraking)
                {
                    isSkidding = false;
                }
            }

        }

        // Enable our skid effects if we're skidding
        foreach (TrailRenderer t in skidTrails)
            t.emitting = isSkidding;

        // Play skid audio if we're skidding
        if (isSkidding)
            skidAudio.volume = Mathf.Lerp(skidAudio.volume, 0.25f, 0.25f);
        else
            skidAudio.volume = Mathf.Lerp(skidAudio.volume, 0, 0.25f);
    }
}
