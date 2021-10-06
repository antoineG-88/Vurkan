using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class VurkanController : MonoBehaviour
{
    [Header("Control settings")]
    public float maxStabilizedPitch;
    public float maxStabilizedRoll;
    public float pitchStanceLerpRatio;
    public float rollStanceLerpRatio;
    public float maxFreePitchSpeed;
    public float maxFreeRollSpeed;
    public float minStanceToFreeMove;
    public float yawSpeedByRollAndPitch;
    public bool yawByRollOnly;
    public bool turnWhileFreeMove;
    public float autoRestabilisationMaxSpeed;
    public float autoRestabilisationLerpRatio;
    [Header("Turning settings")]
    public float pitchSpeedByForwardVelocity;
    public float rollSpeedByForwardVelocity;
    public float yawSpeedByForwardVelocity;
    [Space]
    [Header("Movement settings")]
    public float gravityScale;
    public float generalDragPowerCoefficient;
    public float retractedDragRatio;
    [Header("Wings resistance")]
    public bool useWingThrust;
    public float wingResistanceOrientationPower;
    public float wingResistanceForcePowerMultiplier;
    public float wingMaxResistanceForce;
    public float wingDragForceRatio;
    public float wingUpwardDragForceRatio;
    [Header("Thrust")]
    public float thrustByWingsDownwardConversion;
    public float thrustByWingsUpwardConversion;
    [Range(0f, 1f)] public float liftByThrustRatioOnForceAbsorbed;
    [Header("Lift")]
    public bool useLift;
    public float maxLift;
    public float liftPower;
    public float liftCoefficient;
    public float forwardDragCoefficient;
    public float baseLiftByAirfoil;
    public bool absoluteUpwardLift;
    [Header("Lateral drag")]
    public bool useLateralDrag;
    public float lateralOrientationPower;
    public float lateralDragForceRatio;
    public float lateralVelocityForwardConversionRatio;
    [Header("Flap")]
    public float flapTime;
    public float baseFlapForce;
    [Range(0f, 1f)] public float flapLiftThrustRatio;
    public float startLessEffectiveVelocityToFlap;
    public float startLeastEffectiveVelocityToFlap;
    public AnimationCurve flapForceRatioByRelativeVelocity;
    public AnimationCurve flapForceRatioByFlapTime;
    [Space]
    [Header("Graphics")]
    public Animator animator;

    [Space]
    [Header("Debug & Test")]
    public bool correctedStabilization;
    public Text debug1Text;
    public Text debug2Text;
    public Text debug3Text;
    public Text debug4Text;
    public Image fillBar1;
    public Image fillBar2;

    private float rollStanceState;
    private float pitchStanceState;
    private float currentRollInput;
    private float currentPitchInput;

    private Rigidbody rb;
    private Vector3 relativeVelocity; //The velocity taking wind in account
    private Vector3 forward;
    private Vector3 up;
    private Vector3 right;

    private float rollAngle;
    private float pitchAngle;
    [HideInInspector] public Quaternion stanceRefRotation;
    private float currentYawSpeed;
    private float currentPitchSpeed;
    private float currentRollSpeed;
    private bool isStabilized;
    private bool stabilizeFlag;
    private bool isFlapping;
    private bool hasWingsRetracted;
    private bool retractFlag;
    private bool hasAskToFlap;
    private bool isRestabilizing;

    private float wingResistanceCurrentAbsorbedRatio;
    private float wingAbsorbedForce;
    private float frontAbsorbedForce;
    private float forwardSpeed;
    private float lateralSpeed;
    private float downwardSpeed;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        stanceRefRotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward, Vector3.up));
        StartCoroutine(Flap());
    }

    private void Update()
    {
        debug1Text.text = "Pitch stance : " + Mathf.Round(pitchStanceState * 10) + "  |  Roll stance : " + Mathf.Round(rollStanceState * 10);
        debug2Text.text = "Relative velocity : " + relativeVelocity.magnitude;
        debug3Text.text = "Front absorbed : " + frontAbsorbedForce;
        debug4Text.text = "Lift : " + liftForce.magnitude; 
        fillBar1.fillAmount = Mathf.Abs(Mathf.Cos(Vector3.Angle(relativeVelocity, -up) * Mathf.Deg2Rad));
        fillBar2.fillAmount = Mathf.Sin(Vector3.SignedAngle(relativeVelocity, forward, -right) * Mathf.Deg2Rad);

        UpdateInputs();
        UpdateVurkanStance();
        UpdateRotation();
    }

    private void FixedUpdate()
    {
        forwardSpeed = Mathf.Cos(Vector3.Angle(relativeVelocity, forward) * Mathf.Deg2Rad) * relativeVelocity.magnitude;
        lateralSpeed = Mathf.Cos(Vector3.Angle(relativeVelocity, right) * Mathf.Deg2Rad) * relativeVelocity.magnitude;
        downwardSpeed = Mathf.Cos(Vector3.Angle(relativeVelocity, -up) * Mathf.Deg2Rad) * relativeVelocity.magnitude;
        UpdateVurkanMovements();
    }

    private void UpdateInputs()
    {
        forward = transform.forward;
        up = transform.up;
        right = transform.right;

        currentRollInput = Input.GetAxisRaw("LeftStickH");
        currentPitchInput = Input.GetAxisRaw("LeftStickV");
        //currentPitchInput = -1;

        isStabilized = Input.GetAxisRaw("LeftTrigger") < 0.5f;

        if(Input.GetButtonDown("AButton") && !hasWingsRetracted)
        {
            hasAskToFlap = true;
        }

        if(hasAskToFlap && !isFlapping)
        {
            hasAskToFlap = false;
            StartCoroutine(Flap());
        }

        if (Input.GetAxisRaw("RightTrigger") == 1 && !retractFlag)
        {
            retractFlag = true;
            RetractWings();
        }

        if (Input.GetAxisRaw("RightTrigger") != 1 && retractFlag)
        {
            retractFlag = false;
            OpenWings();
        }

        if (Input.GetButtonDown("StartButton"))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    private Quaternion targetStabilizedRotation;
    private void UpdateVurkanStance()
    {
        rollStanceState = Mathf.Lerp(rollStanceState, currentRollInput, rollStanceLerpRatio * Time.deltaTime);
        pitchStanceState = Mathf.Lerp(pitchStanceState, currentPitchInput, pitchStanceLerpRatio * Time.deltaTime);

        if(isStabilized)
        {
            if(!stabilizeFlag)
            {
                isRestabilizing = true;
                stabilizeFlag = true;
                targetStabilizedRotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(forward, Vector3.up));// a changer et controller
            }

            if (isRestabilizing)
            {
                if (Mathf.Abs(stanceRefRotation.eulerAngles.x) >= 1 || Mathf.Abs(stanceRefRotation.eulerAngles.z) >= 1)
                {
                    stanceRefRotation = Quaternion.RotateTowards(stanceRefRotation, Quaternion.Lerp(stanceRefRotation, targetStabilizedRotation, autoRestabilisationLerpRatio * Time.deltaTime), autoRestabilisationMaxSpeed * Time.deltaTime);
                }
                else
                {
                    stanceRefRotation = targetStabilizedRotation;
                    isRestabilizing = false;
                }
            }
        }
        else
        {
            stabilizeFlag = false;

            if(rollStanceState != 0 && Mathf.Abs(currentRollInput) >= minStanceToFreeMove)
            {
                //currentRollSpeed = Mathf.Lerp(-maxFreeRollSpeed, maxFreeRollSpeed, (rollStanceState + 1) / 2);

                currentRollSpeed = Mathf.Lerp(-Mathf.Clamp(rollSpeedByForwardVelocity * forwardSpeed, -maxFreeRollSpeed, maxFreeRollSpeed), Mathf.Clamp(rollSpeedByForwardVelocity * forwardSpeed, -maxFreeRollSpeed, maxFreeRollSpeed), (rollStanceState + 1) / 2);

                stanceRefRotation *= Quaternion.AngleAxis(currentRollSpeed * Time.deltaTime, Vector3.forward);
            }


            if (pitchStanceState != 0 && Mathf.Abs(currentPitchInput) >= minStanceToFreeMove)
            {
                //currentPitchSpeed = Mathf.Lerp(-maxFreePitchSpeed, maxFreePitchSpeed, (pitchStanceState + 1) / 2);

                currentPitchSpeed = Mathf.Lerp(-Mathf.Clamp(pitchSpeedByForwardVelocity * forwardSpeed, -maxFreePitchSpeed, maxFreePitchSpeed), Mathf.Clamp(pitchSpeedByForwardVelocity * forwardSpeed, -maxFreePitchSpeed, maxFreePitchSpeed), (pitchStanceState + 1) / 2);

                stanceRefRotation *= Quaternion.AngleAxis(currentPitchSpeed * Time.deltaTime, Vector3.right);
            }
        }

        if((turnWhileFreeMove && !isStabilized) || isStabilized)
        {
            currentYawSpeed = rollStanceState * (yawByRollOnly ? -1 : pitchStanceState * (pitchAngle < 0 ? 1 : 0)) * yawSpeedByRollAndPitch * yawSpeedByForwardVelocity * forwardSpeed;
        }
        else
        {
            currentYawSpeed = 0;
        }

        stanceRefRotation *= Quaternion.AngleAxis(currentYawSpeed * Time.deltaTime, Vector3.up);
    }

    private void RetractWings()
    {
        hasWingsRetracted = true;
        hasAskToFlap = false;
        animator.SetBool("Retracted", true);
    }

    private void OpenWings()
    {
        hasWingsRetracted = false;
        animator.SetBool("Retracted", false);
    }

    private Vector3 velocity;
    private Vector3 addedVelocity;
    private Vector3 gravityForce;
    private Vector3 dragForce;
    private Vector3 wingResistanceForce;
    private Vector3 thrustForce;
    private Vector3 liftForce;
    private void UpdateVurkanMovements()
    {
        forward = transform.forward;
        up = transform.up;
        right = transform.right;

        velocity = rb.velocity;
        relativeVelocity = rb.velocity - WindSystem.defaultWindDirectedSpeed;
        addedVelocity = Vector3.zero;
        thrustForce = Vector3.zero;
        wingResistanceForce = Vector3.zero;
        dragForce = Vector3.zero;
        liftForce = Vector3.zero;

        gravityForce = Vector3.down * gravityScale;
        addedVelocity += gravityForce;
        relativeVelocity += addedVelocity;

        wingResistanceCurrentAbsorbedRatio = 0;
        if (relativeVelocity.magnitude != 0)
        {
            if (!hasWingsRetracted)
            {
                if (useWingThrust)
                    UpdateDownWardVelocityConversion();

                if (useLift)
                    UpdateForwardVelocityConversion();

                if (useLateralDrag)
                    UpdateLateralVelocityConversion();
            }

            dragForce = -relativeVelocity.normalized * Mathf.Pow(relativeVelocity.magnitude, 2) * Mathf.Pow(10, generalDragPowerCoefficient) * (hasWingsRetracted ? retractedDragRatio : 1);

            addedVelocity += thrustForce;
            addedVelocity += liftForce;
            addedVelocity += lateralDragForce;
            addedVelocity += dragForce;
            relativeVelocity += addedVelocity;
        }

        rb.velocity = velocity + addedVelocity;

        rb.angularVelocity = Vector3.zero;
    }

    private void UpdateDownWardVelocityConversion()
    {
        if (relativeVelocity.magnitude > 0.1f)
        {
            wingResistanceCurrentAbsorbedRatio = Mathf.Pow(Mathf.Abs(Mathf.Cos(Vector3.Angle(relativeVelocity, -up) * Mathf.Deg2Rad)), wingResistanceOrientationPower);
        }
        else
        {
            wingResistanceCurrentAbsorbedRatio = 0;
        }

        wingAbsorbedForce = wingResistanceCurrentAbsorbedRatio * Mathf.Pow(relativeVelocity.magnitude, 2) * Mathf.Pow(10, wingResistanceForcePowerMultiplier);
        wingAbsorbedForce = Mathf.Clamp(wingAbsorbedForce, -wingMaxResistanceForce, wingMaxResistanceForce);

        thrustForce = (1 - liftByThrustRatioOnForceAbsorbed) * wingAbsorbedForce * forward * (Vector3.Angle(relativeVelocity, -up) <= 90 ? thrustByWingsDownwardConversion : thrustByWingsUpwardConversion);
        liftForce = liftByThrustRatioOnForceAbsorbed * wingAbsorbedForce * up * (Vector3.Angle(relativeVelocity, -up) <= 90 ? 1 : 0);
        wingResistanceForce = wingAbsorbedForce * -relativeVelocity.normalized * (Vector3.Angle(relativeVelocity, -up) <= 90 ? wingDragForceRatio : wingUpwardDragForceRatio);

        thrustForce += wingResistanceForce;
    }

    private float lateralVelocityRatio;
    private Vector3 lateralDragForce;
    private void UpdateLateralVelocityConversion()
    {
        if (relativeVelocity.magnitude > 0.1f)
        {
            lateralVelocityRatio = Mathf.Pow(Mathf.Abs(Mathf.Cos(Vector3.Angle(relativeVelocity, right) * Mathf.Deg2Rad)), lateralOrientationPower);
        }
        else
        {
            lateralVelocityRatio = 0;
        }

        thrustForce += lateralVelocityRatio * Mathf.Pow(relativeVelocity.magnitude, 2) * forward * lateralVelocityForwardConversionRatio;

        lateralDragForce = lateralVelocityRatio * (Vector3.Angle(relativeVelocity, right) <= 90 ? -right : right) * lateralDragForceRatio;
    }

    private float forwardVelocityRatio;
    private float liftMagnitude;
    private void UpdateForwardVelocityConversion()
    {
        if (relativeVelocity.magnitude > 0.1f)
        {
            forwardVelocityRatio = Mathf.Pow(Mathf.Sin(Vector3.SignedAngle(relativeVelocity, forward, -right) * Mathf.Deg2Rad), liftPower);
        }
        else
        {
            forwardVelocityRatio = 0;
        }


        liftMagnitude = Mathf.Clamp(((forwardVelocityRatio * liftCoefficient) + baseLiftByAirfoil) * Mathf.Pow(relativeVelocity.magnitude, 2), 0, maxLift);

        liftForce = (liftMagnitude * (absoluteUpwardLift ? Vector3.up : up)) + (liftMagnitude * forwardDragCoefficient * -relativeVelocity.normalized);
    }

    private void UpdateRotation()
    {
        rollAngle = Mathf.Lerp(-maxStabilizedRoll, maxStabilizedRoll, (rollStanceState + 1) / 2);
        pitchAngle = Mathf.Lerp(-maxStabilizedPitch, maxStabilizedPitch, (pitchStanceState + 1) / 2);

        if (!correctedStabilization)
        {
            transform.rotation = stanceRefRotation * Quaternion.AngleAxis(rollAngle, Vector3.forward) * Quaternion.AngleAxis(pitchAngle, Vector3.right);
        }
        else
        {
            if (pitchAngle < 0)
            {
                transform.rotation = stanceRefRotation * Quaternion.AngleAxis(rollAngle, Vector3.forward) * Quaternion.AngleAxis(pitchAngle, Vector3.right);
            }
            else
            {
                transform.rotation = stanceRefRotation * Quaternion.AngleAxis(pitchAngle, Vector3.right) * Quaternion.AngleAxis(rollAngle, Vector3.forward);
            }
        }
    }

    private IEnumerator Flap()
    {
        animator.SetTrigger("Flap");
        isFlapping = true;
        float timer = 0;
        float baseFlapForwardMagnitude = baseFlapForce * flapForceRatioByRelativeVelocity.Evaluate((forwardSpeed - startLessEffectiveVelocityToFlap) / startLeastEffectiveVelocityToFlap);
        float baseflapLiftMagnitude = baseFlapForce;
        float flapForwardForce;
        float flapLiftForce;
        while(timer < flapTime || hasWingsRetracted)
        {
            flapForwardForce = baseFlapForwardMagnitude * flapForceRatioByFlapTime.Evaluate(timer / flapTime);
            flapLiftForce = baseflapLiftMagnitude * flapForceRatioByFlapTime.Evaluate(timer / flapTime);
            rb.velocity += (up * flapLiftThrustRatio * flapLiftForce) + (forward * (flapForwardForce * (1 - flapLiftThrustRatio)));

            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        isFlapping = false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + thrustForce);

        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(transform.position, transform.position + liftForce);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + lateralDragForce);
    }
}
