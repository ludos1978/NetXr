//============= Copyright (c) Reto Spoerri, All rights reserved. ==============
//
// Purpose: 
//
//=============================================================================

// TargetReachable
using UnityEngine;
using UnityEngine.AI;

public class TargetReachable : MonoBehaviour {
    public Transform target;
    private NavMeshHit hit;
    private bool blocked = false;

    void Update()
    {
        blocked = NavMesh.Raycast(transform.position, target.position, out hit, NavMesh.AllAreas);
        Debug.DrawLine(transform.position, target.position, blocked ? Color.red : Color.green);

        if (blocked)
            Debug.DrawRay(hit.position, Vector3.up, Color.red);
    }
}