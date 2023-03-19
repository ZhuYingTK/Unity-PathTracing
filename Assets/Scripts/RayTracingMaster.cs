using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;
public class RayTracingMaster : MonoBehaviour
{
    public static uint _currentSample = 0;
    private bool shouldRefreshSample = true;
    
    public ComputeShader RayTracingShader;
    private RenderTexture _target;
    private RenderTexture _converged;//高精度缓冲
    private Camera _camera;
    public Texture SkyboxTexture;
    public int Seed;
    [Range(0f, 5f)] public float HDRIntensity = 1;
    
    //分块渲染参数
    private readonly int dispatchGroupX = 32;
    private readonly int dispatchGroupY = 32;
    private int dispatchGroupXFull, dispatchGroupYFull;
    private Vector4 dispatchCount;
    private int currentDownSampler = 0;

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
        RefreshRenderTextureData();
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
            RefreshRenderTextureData();
            transform.hasChanged = false;
        }
        if(DirectionalLight.transform.hasChanged)
        {
            //重新开始抗锯齿
            RefreshRenderTextureData();
            DirectionalLight.transform.hasChanged = false;
        }
    }

    private void Render(RenderTexture dest)
    {
        //初始化纹理信息
        InitRenderTexture();
        //填充数据
        RayTracingShader.SetTexture(0,"Result",_target);
        RayTracingShader.Dispatch(0,dispatchGroupX,dispatchGroupY,1);
        
        //抗锯齿
        if (_addMaterial == null)
            _addMaterial = new Material(Shader.Find("Hidden/AddShader"));
        _addMaterial.SetFloat("_Sample",_currentSample);
        Vector4 mask = GetAddMask();
        _addMaterial.SetVector("_Mask",GetAddMask());
        Graphics.Blit(_target,_converged,_addMaterial);
        Graphics.Blit(_converged,dest);
        
        UpdateDispatchCount();
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
        RayTracingShader.SetVector("_PixelOffset",GetPixelOffset());
        
        //定向光
        Vector3 l = DirectionalLight.transform.forward;
        RayTracingShader.SetVector("_DirectionalLight",
            new Vector4(l.x,l.y,l.z,DirectionalLight.intensity));
        
        //设置Mesh
        SetComputeBuffer("_Spheres", _sphereBuffer);
        SetComputeBuffer("_MeshObjects", ObjectTracingManager._meshObjectBuffer);
        SetComputeBuffer("_Vertices", ObjectTracingManager._vertexBuffer);
        SetComputeBuffer("_Indices", ObjectTracingManager._indexBuffer);
        SetComputeBuffer("_MaterialDatas",ObjectTracingManager._materialBuffer);
        
        //传入随机值
        RayTracingShader.SetFloat("_Seed",Random.value);
        
        RayTracingShader.SetFloat("_HDRIntensity",HDRIntensity);
        RayTracingShader.SetFloat("_Sample",_currentSample);
        
        //传入贴图
        SetTextureArray("_AlbedoTextures",ObjectTracingManager.AlbedoTextures);
        SetTextureArray("_NormalTextures",ObjectTracingManager.NormalTextures);
        SetTextureArray("_RoughnessTextures",ObjectTracingManager.RoughnessTextures);
        SetTextureArray("_MetallicTextures",ObjectTracingManager.MetallicTextures);
        SetTextureArray("_EmissionTextures",ObjectTracingManager.EmissionTextures);
    }
    
    /// <summary>
    /// 初始化目标纹理信息
    /// </summary>
    private void InitRenderTexture()
    {
        if (_converged == null || _converged.width != Screen.width || _converged.height != Screen.height)
        {
            //如果已经有target了,就释放
            if (_converged != null)
            {
                _converged.Release();
            }
            
            _converged = new RenderTexture(Screen.width, Screen.height, 0, 
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            
            _converged.enableRandomWrite = true;
            _converged.Create();
            RefreshRenderTextureData();
        }
    }

    public void RefreshRenderTextureData()
    {
        while ((dispatchGroupX * 8) << currentDownSampler <= Screen.width ||
               (dispatchGroupY * 8) << currentDownSampler <= Screen.height)
        {
            currentDownSampler ++;
        }

        _currentSample = 0;
        dispatchCount.x = 0;
        dispatchCount.y = 0;
        RefreshTargetTexture();
        //更新线程组
    }

    /// <summary>
    /// 获得当前目标贴图目标贴图
    /// </summary>
    private void RefreshTargetTexture()
    {
        if (_target != null)
        {
            _target.Release();
        }
        
        _target = new RenderTexture(Screen.width >> currentDownSampler,
            Screen.height >> currentDownSampler,0,
        RenderTextureFormat.ARGBFloat , RenderTextureReadWrite.Linear);
        _target.enableRandomWrite = true;
        _target.Create();
        
        EstimateGroupsData(_target.width,_target.height);
    }

    /// <summary>
    /// 估算线程组
    /// </summary>
    private void EstimateGroupsData(int width,int height)
    {
        //以dispatchGroupX*Y为线程组,以 8x8 为单组线程数
        dispatchCount = new Vector4(
            0f, 0f,
            Mathf.Ceil(width / (float) (dispatchGroupX * 8)),
            Mathf.Ceil(height / (float) (dispatchGroupY * 8))
        );
    }
    
    /// <summary>
    /// 计算渲染块的位移
    /// </summary>
    private void UpdateDispatchCount()
    {
        dispatchCount.x += 1f;
        if (dispatchCount.x > dispatchCount.z)
        {
            dispatchCount.x = 0f;
            dispatchCount.y += 1f;
            if (dispatchCount.y > dispatchCount.w)
            {
                dispatchCount.x = 0f;
                dispatchCount.y = 0f;
                _currentSample++;
                shouldRefreshSample = true;
                if (currentDownSampler > 0)
                {
                    currentDownSampler--;
                    _currentSample = 0;
                    RefreshTargetTexture();
                }
            }
        }
    }

    /// <summary>
    /// 获得当前偏移量
    /// </summary>
    /// <returns></returns>
    private Vector2 GetPixelOffset()
    {
        Vector2 offset = new Vector2(Random.value, Random.value);
        offset.x += dispatchCount.x * dispatchGroupX * 8;
        offset.y += dispatchCount.y * dispatchGroupY * 8;
        return offset;
    }
    
    
    //判断null并传递给GPU
    private void SetComputeBuffer(string name, ComputeBuffer buffer)
    {
        if (buffer != null)
        {
            RayTracingShader.SetBuffer(0, name, buffer);
        }
    }

    private void SetTextureArray(string name, Texture2DArray buffer)
    {
        if (buffer != null)
        {
            RayTracingShader.SetTexture(0,name,buffer);
        }
    }

    private Vector4 GetAddMask()
    {
        Vector2 offset = new Vector2(
            dispatchCount.x * dispatchGroupX * 8,
            dispatchCount.y * dispatchGroupY * 8);
        return new Vector4(offset.x / _target.width, offset.y / _target.height,
            dispatchGroupX * 8 / (float)_target.width, dispatchGroupY * 8 / (float)_target.height);
    }
    
    //将RenderTexture保存成一张png图片  
    public bool SaveRenderTextureToPNG(RenderTexture rt, string contents, string pngName)
    {
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D png = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, false);
        png.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        byte[] bytes = png.EncodeToPNG();
        if (!Directory.Exists(contents))
            Directory.CreateDirectory(contents);
        FileStream file = File.Open(contents + "/" + pngName + ".png", FileMode.Create);
        BinaryWriter writer = new BinaryWriter(file);
        writer.Write(bytes);
        file.Close();
        Texture2D.DestroyImmediate(png);
        png = null;
        RenderTexture.active = prev;
        return true;
    }  
}
