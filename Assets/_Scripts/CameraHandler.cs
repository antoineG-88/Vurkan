using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraHandler : MonoBehaviour
{
    public float movementTargetLerpRatio;
    public VurkanController vurkan;
    public float distanceBehind;
    public float upOffset;

    public Transform target;

    void Update()
    {
    }

    private void FixedUpdate()
    {
        UpdateCameraPos();
    }

    private void UpdateCameraPos()
    {
        target.position = vurkan.transform.position + (vurkan.stanceRefRotation * Vector3.forward) * -distanceBehind
            + ((vurkan.stanceRefRotation * Quaternion.AngleAxis(-90, Vector3.right)) * Vector3.forward) * upOffset;

        target.rotation = vurkan.stanceRefRotation;

        transform.position = Vector3.Lerp(transform.position, target.position, movementTargetLerpRatio * Time.deltaTime);
        transform.rotation = Quaternion.Lerp(transform.rotation, target.rotation, movementTargetLerpRatio * Time.deltaTime);
    }
}
