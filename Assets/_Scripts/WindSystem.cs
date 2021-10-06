using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindSystem : MonoBehaviour
{
    public Transform windDefaultEndDirectedSpeed;
    public static Vector3 defaultWindDirectedSpeed;

    private void Start()
    {
        defaultWindDirectedSpeed = windDefaultEndDirectedSpeed.position - transform.position;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, windDefaultEndDirectedSpeed.position);
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(windDefaultEndDirectedSpeed.position, 0.1f);
    }
}
