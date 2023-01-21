using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class CreateSpheres : MonoBehaviour
{
    public Vector2 SphereRadius = new Vector2(5.0f, 30.0f);
    public uint SpheresMax = 1000;
    public float SpherePlacementRadius = 100.0f;
    public ComputeBuffer _sphereBuffer;
    public int SphereSeed;

    public CreateSpheres()
    {
    }
    public CreateSpheres(int Seed)
    {
        this.SphereSeed = Seed;
    }

    struct Sphere
    {
        public Vector3 position;
        public float radius;
        public Vector3 albedo;
        public Vector3 specular;
        public float smoothness;
        public Vector3 emission;
    }

    public ComputeBuffer Create()
    {
        Random.InitState(SphereSeed);
        List<Sphere> spheres = new List<Sphere>();
        for (int i = 0; i < SpheresMax; i++)
        {
            Sphere sphere = new Sphere();
            
            //生成位置和半径
            sphere.radius = SphereRadius.x + Random.value * (SphereRadius.y - SphereRadius.x);
            Vector2 randomPos = Random.insideUnitCircle * SpherePlacementRadius;
            sphere.position = new Vector3(randomPos.x, sphere.radius, randomPos.y);
            
            // 确保不会发生重叠
            foreach (Sphere other in spheres)
            {
                float minDist = sphere.radius + other.radius;
                if (Vector3.SqrMagnitude(sphere.position - other.position) < minDist * minDist)
                    goto SkipSphere;
            }
            
            //生成材质
            Color color = Random.ColorHSV();
            bool metal = Random.value < 0.5f;
            bool emission = Random.value < 0.3f;
            sphere.albedo = metal ? Vector3.zero : new Vector3(color.r, color.g, color.b);
            sphere.specular = metal ? new Vector3(color.r, color.g, color.b) : Vector3.one * 0.04f;
            sphere.smoothness = Random.value;
            sphere.emission = emission ? new Vector3(color.r + 0.7f, color.g + 0.7f, color.b + 0.7f) : Vector3.zero;
            
            spheres.Add(sphere);
            
            SkipSphere:
            continue;
        }
        //每个float4字节,步长为4x14
        _sphereBuffer = new ComputeBuffer(spheres.Count, 56);
        _sphereBuffer.SetData(spheres);
        return _sphereBuffer;
    }

    private static void CreateComputeBuffer<T>(ref ComputeBuffer buffer, List<T> data, int stride)
        where T : struct
    {
        //如果已有缓冲
        if (buffer != null)
        {
            //如果数据无内容、数据与缓冲不一致、当前步长不一致,则重置缓冲
            if (data.Count == 0 || buffer.count != data.Count || buffer.stride != stride)
            {
                buffer.Release();
                buffer = null;
            }
        }
        
        if (data.Count != 0)
        {
            // If the buffer has been released or wasn't there to
            // begin with, create it
            if (buffer == null)
            {
                buffer = new ComputeBuffer(data.Count, stride);
            }
            // Set data on the buffer
            buffer.SetData(data);
        }
    }
}
