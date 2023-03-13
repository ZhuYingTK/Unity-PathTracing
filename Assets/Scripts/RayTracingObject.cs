using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class RayTracingObject : MonoBehaviour
{
    public Vector3 albedo = Vector3.zero;   
    public Vector3 specular = Vector3.one * 0.65f; 
    public float  smoothness = 0.9f;
    public Vector3 emission = Vector3.zero;
    public float opacity;
    public float refractivity;

    private void Update()
    {
        if (this.transform.hasChanged)
        {
            ObjectTracingManager.RefreshObjects();
            this.transform.hasChanged = false;
        }
    }

    private void OnEnable()
    {
        ObjectTracingManager.RegisterObject(this);
    }

    private void OnDisable()
    {
        ObjectTracingManager.UnregisterObject(this);
    }
}
