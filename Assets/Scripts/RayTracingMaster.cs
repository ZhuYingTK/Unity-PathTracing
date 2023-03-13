using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;
public class RayTracingMaster : MonoBehaviour
{
    public static uint _currentSample = 0;
    
    public ComputeShader RayTracingShader;
    private RenderTexture _target;
    private RenderTexture _converged;//高精度缓冲
    private Camera _camera;
    public Texture SkyboxTexture;
    public int Seed;
    [Range(0f, 5f)] public float HDRIntensity = 1;

    //定向光
    public Light DirectionalLight;

    //anti-Aliasing抗锯齿
    private Material _addMaterial;

    //所有的球体
    private ComputeBuffer _sphereBuffer;

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        SetShaderParameters();
        Render(dest);
        ObjectTracingManager.RebuildMeshObjectBuffer();
    }

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    private void OnEnable()
    {
        _currentSample = 0;
        _sphereBuffer = new CreateSpheres( Seed).Create();
    }

    private void OnDisable()
    {
        if (_sphereBuffer != null)
            _sphereBuffer.Release();
    }

    private void Update()
    {
        if (transform.hasChanged)
        {
            //重新开始抗锯齿
            _currentSample = 0;
            transform.hasChanged = false;
        }
        if(DirectionalLight.transform.hasChanged)
        {
            //重新开始抗锯齿
            _currentSample = 0;
            DirectionalLight.transform.hasChanged = false;
        }
    }

    private void Render(RenderTexture dest)
    {
        //初始化纹理信息
        InitRenderTexture();
        RenderTexture _test = new RenderTexture(Screen.width/4, Screen.height/4, 0, 
            RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        //填充数据
        RayTracingShader.SetTexture(0,"Result",_test);
        //设置渲染线程
        int threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);
        RayTracingShader.Dispatch(0,threadGroupsX,threadGroupsY,1);
        
        //抗锯齿
        if (_addMaterial == null)
            _addMaterial = new Material(Shader.Find("Hidden/AddShader"));
        _addMaterial.SetFloat("_Sample",_currentSample);
        Graphics.Blit(_test,_converged,_addMaterial);
        Graphics.Blit(_converged,dest);
        _currentSample++;
    }
    
    /// <summary>
    /// 向Shader传递参数
    /// </summary>
    /// </summary>
    private void SetShaderParameters()
    {
        //相机参数
        RayTracingShader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        RayTracingShader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);
        
        //天空球
        RayTracingShader.SetTexture(0,"_SkyBoxTexture", SkyboxTexture);
        
        //抗锯齿抖动
        RayTracingShader.SetVector("_PixelOffset",new Vector2(Random.value,Random.value));
        
        //定向光
        Vector3 l = DirectionalLight.transform.forward;
        RayTracingShader.SetVector("_DirectionalLight",
            new Vector4(l.x,l.y,l.z,DirectionalLight.intensity));
        
        //设置Mesh
        SetComputeBuffer("_Spheres", _sphereBuffer);
        SetComputeBuffer("_MeshObjects", ObjectTracingManager._meshObjectBuffer);
        SetComputeBuffer("_Vertices", ObjectTracingManager._vertexBuffer);
        SetComputeBuffer("_Indices", ObjectTracingManager._indexBuffer);
        
        //传入随机值
        RayTracingShader.SetFloat("_Seed",Random.value);
        
        RayTracingShader.SetFloat("_HDRIntensity",HDRIntensity);
    }
    
    /// <summary>
    /// 初始化目标纹理信息
    /// </summary>
    private void InitRenderTexture()
    {
        if (_target == null || _target.width != Screen.width || _target.height != Screen.height)
        {
            //如果已经有target了,就释放
            if (_target != null)
            {
                _target.Release();
                _converged.Release();
            }
            
            _target = new RenderTexture(Screen.width, Screen.height, 0, 
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            _converged = new RenderTexture(Screen.width, Screen.height, 0, 
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            
            _target.enableRandomWrite = true;
            _converged.enableRandomWrite = true;
            _target.Create();
            _converged.Create();
        }
    }
    
    //判断null并传递给GPU
    private void SetComputeBuffer(string name, ComputeBuffer buffer)
    {
        if (buffer != null)
        {
            RayTracingShader.SetBuffer(0, name, buffer);
        }
    }
}
