using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HoazoController : MonoBehaviour
{
    [Header("Control settings")]
    public float rollSpeed;
    public float yawSpeedByRoll;
    public float pitchSpeed;
    public float maxStabilizedRoll;
    public float maxStabilizedPitch;
    [Header("Flap settings")]
    public float flapThrustTime;
    public float flapThrustForce;
    public float flapLiftForce;
    [Header("Physics settings")]
    public float gravityScale;
    public float dragByRelativeSpeedRatio;
    public float liftByRelativeSpeedRatio;
    public float thrustByRelativeDownwardSpeedRatio;
    public float downwardDragByRelativeDownwardSpeedRatio;
    [Header("Debug refs")]
    public Text debug1Text;
    public Text debug2Text;
    public Text debug3Text;
    public Text debug4Text;

    private float currentRollInput;
    private float currentPitchInput;
    private float currentPitchAngle;
    private float currentRollAngle;
    private float currentYawAngle;
    private float targetRollAngle;
    private float targetPitchAngle;
    private Rigidbody rb;
    private Quaternion currentRotation;
    private Vector3 velocity;
    private Vector3 relativeVelocity; //The velocity taking wind in account
    private Vector3 forward;
    private Vector3 up;
    private Vector3 right;
    private Vector3 liftForce;
    private Vector3 dragForce;
    private Vector3 passiveThrustForce;
    private Vector3 downwardDragForce;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        currentRotation = transform.rotation;
    }

    private void Update()
    {
        UpdateInput();
        debug1Text.text = "Target roll : " + targetRollAngle + " - Roll move : " + rollMovement + " - Roll : " + currentRollAngle;
        debug2Text.text = "Target pitch : " + targetPitchAngle + " - Pitch : " + currentPitchAngle;
        debug3Text.text = "Quaternion : " + currentRotation;


        UpdateMovement();
        UpdateTilt();
    }

    private void FixedUpdate()
    {
        velocity = rb.velocity;
        //UpdateForces();
        UpdateVelocity();
    }

    private void UpdateInput()
    {
        currentRollInput = Input.GetAxisRaw("LeftStickH");
        currentPitchInput = Input.GetAxisRaw("LeftStickV");
    }

    private Quaternion rollRotation;
    private Quaternion pitchRotation;
    private Quaternion yawRotation;
    private float rollMovement;
    private float pitchMovement;
    private float yawMovement;
    private void UpdateTilt()
    {
        targetRollAngle = Mathf.Lerp(-maxStabilizedRoll, maxStabilizedRoll, (currentRollInput + 1) / 2);
        targetPitchAngle = Mathf.Lerp(-maxStabilizedPitch, maxStabilizedPitch, (currentPitchInput + 1) / 2);

        if(false)
        {
            if (currentRollInput != 0) //> 0 && currentRollAngle < maxStabilizedRoll || currentRollInput < 0 && currentRollAngle > -maxStabilizedRoll)
            {
                rollMovement = currentRollInput * rollSpeed * Time.deltaTime;
                rollRotation = Quaternion.AngleAxis(rollMovement, Vector3.forward);

                currentRotation *= rollRotation;
            }
            else if (currentRollInput == 0 && Input.GetAxis("LeftTrigger") == 0)
            {
                if (Mathf.Abs(currentRollAngle) > rollSpeed * Time.deltaTime * 1.2f)
                {
                    rollMovement = -Mathf.Sign(currentRollAngle) * rollSpeed * Time.deltaTime;
                    rollRotation = Quaternion.AngleAxis(rollMovement, Vector3.forward);

                    currentRotation *= rollRotation;
                }
                else
                {
                    currentRotation = Quaternion.Euler(currentRotation.eulerAngles.x, currentRotation.eulerAngles.y, 0);
                }
            }

            if (currentPitchInput != 0) //> 0 && currentPitchAngle < maxStabilizedPitch || currentPitchInput < 0 && currentPitchAngle > -maxStabilizedPitch)
            {
                pitchMovement = currentPitchInput * rollSpeed * Time.deltaTime;
                pitchRotation = Quaternion.AngleAxis(pitchMovement, Vector3.right);

                currentRotation *= pitchRotation;
            }
            else if (currentPitchInput == 0 && Input.GetAxis("LeftTrigger") == 0)
            {
                if (Mathf.Abs(currentPitchAngle) > pitchSpeed * Time.deltaTime * 1.2f)
                {
                    pitchMovement = -Mathf.Sign(currentPitchAngle) * pitchSpeed * Time.deltaTime;
                    pitchRotation = Quaternion.AngleAxis(pitchMovement, Vector3.right);

                    currentRotation *= pitchRotation;
                }
                else
                {
                    currentRotation = Quaternion.Euler(0, currentRotation.eulerAngles.y, currentRotation.eulerAngles.z);
                }
            }
        }
        else
        {
            if (Mathf.Abs(currentRollAngle - targetRollAngle) > rollSpeed * Time.deltaTime * 10.2f)
            {
                rollMovement = Mathf.Sign(targetRollAngle - currentRollAngle) * rollSpeed * Time.deltaTime;
                rollRotation = Quaternion.AngleAxis(rollMovement, Vector3.forward);

                currentRotation *= rollRotation;
            }

            if (Mathf.Abs(currentPitchAngle - targetPitchAngle) > pitchSpeed * Time.deltaTime * 1.2f)
            {
                pitchMovement = Mathf.Sign(targetPitchAngle - currentPitchAngle) * pitchSpeed * Time.deltaTime;
                pitchRotation = Quaternion.AngleAxis(pitchMovement, Vector3.right);

                currentRotation *= pitchRotation;
            }


            /*if (Mathf.Abs(currentRollAngle) > 0.1f)
            {
                yawMovement = currentRollAngle * yawSpeedByRoll * Time.deltaTime;
                yawRotation = Quaternion.AngleAxis(yawMovement, Vector3.down);
                currentRotation *= yawRotation;
            }*/
        }

        transform.rotation = currentRotation;

        currentYawAngle = Mathf.Atan2(2 * currentRotation.y * currentRotation.w - 2 * currentRotation.x * currentRotation.z, 1 - 2 * currentRotation.y * currentRotation.y - 2 * currentRotation.z * currentRotation.z) * Mathf.Rad2Deg;
        currentPitchAngle = Mathf.Atan2(2 * currentRotation.x * currentRotation.w - 2 * currentRotation.y * currentRotation.z, 1 - 2 * currentRotation.x * currentRotation.x - 2 * currentRotation.z * currentRotation.z) * Mathf.Rad2Deg;
        currentRollAngle = Mathf.Asin(2 * currentRotation.x * currentRotation.y + 2 * currentRotation.z * currentRotation.w) * Mathf.Rad2Deg;

        rb.angularVelocity = Vector3.zero;
    }

    private void UpdateRotation()
    {

    }

    private void UpdateMovement()
    {
        forward = transform.forward;
        up = transform.up;
        right = transform.right;

        if (Input.GetButtonDown("AButton"))
        {
             StartCoroutine(FlapWings());
        }
    }

    private void UpdateForces()
    {
        forward = transform.forward;
        up = transform.up;
        right = transform.right;

        velocity += Vector3.down * gravityScale;

        relativeVelocity = velocity - WindSystem.defaultWindDirectedSpeed;


        liftForce = up * Mathf.Max(Vector3.Dot(relativeVelocity, forward), 0) * liftByRelativeSpeedRatio;
        dragForce = -velocity.normalized * Mathf.Min(relativeVelocity.magnitude * dragByRelativeSpeedRatio, velocity.magnitude);
        passiveThrustForce = forward * Mathf.Max(Vector3.Dot(relativeVelocity, -up), 0) * thrustByRelativeDownwardSpeedRatio;
        downwardDragForce = up * Mathf.Max(Vector3.Dot(relativeVelocity, -up), 0) * downwardDragByRelativeDownwardSpeedRatio;

        velocity += downwardDragForce + passiveThrustForce + dragForce + liftForce;
        rb.velocity = velocity;
    }

    private void UpdateVelocity()
    {
        relativeVelocity = velocity - WindSystem.defaultWindDirectedSpeed;

        //velocity += Vector3.down * gravityScale;

        velocity = velocity.magnitude * forward;

        rb.velocity = velocity;
    }

    private IEnumerator FlapWings()
    {
        Debug.Log("Flap");
        float timer = 0;
        while(timer < flapThrustTime)
        {
            rb.velocity += forward * flapThrustForce;
            rb.velocity += up * flapLiftForce;
            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
    }
}
