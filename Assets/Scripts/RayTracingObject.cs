using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class RayTracingObject : MonoBehaviour
{
    private void OnEnable()
    {
        ObjectTracingManager.RegisterObject(this);
    }

    private void OnDisable()
    {
        ObjectTracingManager.UnregisterObject(this);
    }
}
