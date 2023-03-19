using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;


struct Vertex
{
    public Vector3 position;
    public Vector3 normal;
    public Vector2 uv;
    
    public static int TypeSize = sizeof(int) * 8;

}
public class ObjectTracingManager : MonoBehaviour
{
    private static bool _meshObjectsNeedRebuilding = false;
    private static List<RayTracingObject> _rayTracingObjects = new List<RayTracingObject>();
    
    //Mesh相关参数
    private static List<MeshObject> _meshObjects = new List<MeshObject>();
    private static List<Vertex> _vertices = new List<Vertex>();
    private static List<int> _indices = new List<int>();
    private static List<MaterialData> _materials = new List<MaterialData>();


    public static ComputeBuffer _meshObjectBuffer;
    public static ComputeBuffer _vertexBuffer;
    public static ComputeBuffer _indexBuffer;
    public static ComputeBuffer _materialBuffer;
    public static Texture2DArray AlbedoTextures = null;
    public static Texture2DArray EmissionTextures = null;
    public static Texture2DArray MetallicTextures = null;
    public static Texture2DArray NormalTextures = null;
    public static Texture2DArray RoughnessTextures = null;

    public static void RegisterObject(RayTracingObject obj)
    {
        _rayTracingObjects.Add(obj);
        _meshObjectsNeedRebuilding = true;
    }

    public static void UnregisterObject(RayTracingObject obj)
    {
        _rayTracingObjects.Remove(obj);
        _meshObjectsNeedRebuilding = true;
    }
    
    public static void RefreshObjects()
    {
        _meshObjectsNeedRebuilding = true;
    }

    public static void RebuildMeshObjectBuffer()
    {
        if (!_meshObjectsNeedRebuilding)
        {
            return;
        }

        _meshObjectsNeedRebuilding = false;

        RayTracingMaster._currentSample = 0;
        //清空List
        _meshObjects.Clear();
        _vertices.Clear();
        _indices.Clear();
        _materials.Clear();
        
        //贴图列表
        List<Texture2D> albedoTexs = new List<Texture2D>();
        List<Texture2D> emitTexs = new List<Texture2D>();
        List<Texture2D> metalTexs = new List<Texture2D>();
        List<Texture2D> normTexs = new List<Texture2D>();
        List<Texture2D> roughTexs = new List<Texture2D>();
        
        //创建默认材质为0号
        _materials.Add(new MaterialData()
        {
            Color = new Vector4(1.0f, 1.0f, 1.0f,1.0f),
            Emission = Vector3.zero,
            Metallic = 0.04f,
            Smoothness = 0.5f,
            IOR = 1.0f,
            RenderMode = 0,
            AlbedoIdx = -1,
            EmisIdx = -1,
            MetallicIdx = -1,
            NormalIdx = -1,
            RoughIdx = -1
        });

        foreach (RayTracingObject obj in _rayTracingObjects)
        {
            Mesh mesh = obj.GetComponent<MeshFilter>().sharedMesh;
            Material mat = obj.GetComponent<Renderer>().sharedMaterial;
            bool isTracingMat = false;
            if (mat.shader == Shader.Find("RayTracingShader"))
            {
                isTracingMat = true;
                //获取贴图Index
                int albedoTexIdx = GetTexID(albedoTexs, "_MainTex", mat);
                int normTexIdx = GetTexID(normTexs, "_BumpMap", mat);
                int metalTexIdx = GetTexID(metalTexs, "_MetallicGlossMap", mat);
                int roughTexIdx = GetTexID(roughTexs, "_SpecGlossMap", mat);
                int emiTexIdx = GetTexID(emitTexs, "_EmissionMap", mat);
                _materials.Add(new MaterialData()
                {
                    Color = PathTracingFunction.ColorToVector4(mat.color),
                    Emission = mat.IsKeywordEnabled("_EMISSION") ? PathTracingFunction.ColorToVector3(mat.GetColor("_EmissionColor")) : Vector3.zero,
                    Metallic = mat.GetFloat("_Metallic"),
                    Smoothness = mat.GetFloat("_Glossiness"), // 透明度
                    IOR = mat.HasProperty("_IOR") ? mat.GetFloat("_IOR") : 1.0f,
                    RenderMode = mat.GetFloat("_Mode"), // 如果>0则为透明
                    AlbedoIdx = albedoTexIdx,
                    NormalIdx = normTexIdx,
                    MetallicIdx = metalTexIdx,
                    RoughIdx = roughTexIdx,
                    EmisIdx = emiTexIdx
                });
            }
            
            //添加顶点数据
            int firstVertex = _vertices.Count;
            for (int i = 0; i < mesh.vertexCount; i++)
            {
                Vertex _vertex = new Vertex(){position = mesh.vertices[i],normal = mesh.normals[i],uv = mesh.uv[i]}; 
                _vertices.Add(_vertex);
            }

            //生成AABB包围盒
            Vector3 AABBmax = mesh.vertices[0];
            Vector3 AABBmin = mesh.vertices[0];
            for (int i = 0; i < mesh.vertices.Length; i++)
            {
                UpdateAABB(ref AABBmax,ref AABBmin,mesh.vertices[i]);
            }
            
            //添加索引数据,如果不是第一个网格,需要进行偏移
            int firstIndex = _indices.Count;
            var indices = mesh.GetIndices(0);
            //根据当前mesh在顶点buffer的位置进行偏移
            _indices.AddRange(indices.Select(index => index + firstVertex));
            
            //添加网格数据
            _meshObjects.Add(new MeshObject()
                {
                    localToWorldMatrix = obj.transform.localToWorldMatrix,
                    worldToLocalMatrix = obj.transform.worldToLocalMatrix,
                    indices_count = indices.Length,
                    indices_offset = firstIndex,
                    AABBmax = AABBmax,
                    AABBmin = AABBmin,
                    MaterialID = isTracingMat ? _materials.Count-1 : 0
                });
        }
        CreateComputeBuffer(ref _meshObjectBuffer, _meshObjects, MeshObject.TypeSize);
        CreateComputeBuffer(ref _materialBuffer,_materials,MaterialData.TypeSize);
        CreateComputeBuffer(ref _vertexBuffer, _vertices, Vertex.TypeSize);
        CreateComputeBuffer(ref _indexBuffer, _indices, 4);
        
        // 生成贴图数据
        if (AlbedoTextures != null) UnityEngine.Object.Destroy(AlbedoTextures);
        if (EmissionTextures != null) UnityEngine.Object.Destroy(EmissionTextures);
        if (MetallicTextures != null) UnityEngine.Object.Destroy(MetallicTextures);
        if (NormalTextures != null) UnityEngine.Object.Destroy(NormalTextures);
        if (RoughnessTextures != null) UnityEngine.Object.Destroy(RoughnessTextures);
        AlbedoTextures = CreateTextureArray(ref albedoTexs);
        EmissionTextures = CreateTextureArray(ref emitTexs);
        MetallicTextures = CreateTextureArray(ref metalTexs);
        NormalTextures = CreateTextureArray(ref normTexs);
        RoughnessTextures = CreateTextureArray(ref roughTexs);
        
    }

    private static void UpdateAABB(ref Vector3 AABBmax, ref Vector3 AABBmin, Vector3 vertex)
    {
        AABBmax.x = AABBmax.x > vertex.x ? AABBmax.x : vertex.x;
        AABBmax.y = AABBmax.y > vertex.y ? AABBmax.y : vertex.y;
        AABBmax.z = AABBmax.z > vertex.z ? AABBmax.z : vertex.z;
        AABBmin.x = AABBmin.x < vertex.x ? AABBmin.x : vertex.x;
        AABBmin.y = AABBmin.y < vertex.y ? AABBmin.y : vertex.y;
        AABBmin.z = AABBmin.z < vertex.z ? AABBmin.z : vertex.z;
    }


    /// <summary>
    /// 更新材质ID
    /// </summary>
    /// <param name="Texlist"></param>
    /// <param name="property"></param>
    /// <param name="mat"></param>
    /// <returns></returns>
    private static int GetTexID(List<Texture2D> Texlist,string property,Material mat)
    {
        int index = -1;
        if (mat.HasProperty(property))
        {
            Texture Map = mat.GetTexture(property);
            index = Texlist.IndexOf(Map as Texture2D);
            if (index < 0 && Map != null)
            {
                index = Texlist.Count;
                Texlist.Add(Map as Texture2D);
            }
        }
        return index;
    }

    private static void CreateComputeBuffer<T>(ref ComputeBuffer buffer, List<T> data, int stride)
    where T : struct
    {
        //如果已有buffer
        if (buffer != null)
        {
            //如果buffer和data不一致,则释放data
            if (data.Count == 0 || buffer.count != data.Count || buffer.stride != stride)
            {
                buffer.Release();
                buffer = null;
            }
        }

        if (data.Count != 0)
        {
            //如果buffer被释放了就初始化
            if (buffer == null)
            {
                buffer = new ComputeBuffer(data.Count, stride);
            }
            
            //设置数据
            buffer.SetData(data);
        }
    }

    /// <summary>
    /// 创建TextureArray
    /// </summary>
    /// <param name="textures"></param>
    /// <returns></returns>
    private static Texture2DArray CreateTextureArray(ref List<Texture2D> textures)
    {
        int texWidth = 1, texHeight = 1;
        //求出最大材质大小
        for (int i = 0; i < textures.Count; i++)
        {
            texWidth = Mathf.Max(texWidth, textures[i].width);
            texHeight = Mathf.Max(texHeight, textures[i].height);
        }

        //根据贴图数量限制贴图大小
        int texlegth = Mathf.Max(texWidth, texHeight);
        if (texlegth >= 2048)
        {
            if (textures.Count <= 16) texlegth = 2048;
            else  texlegth = 1024;
        }
        else if(texlegth >= 1024)
        {
            if (texlegth <= 48) texlegth = 1024;
            else texlegth = 1024;
        }
        texWidth = Mathf.Min(texWidth, texlegth);
        texHeight = Mathf.Min(texHeight, texlegth);

        //创建空白图集
        Texture2DArray newTextures = new Texture2DArray(
            texWidth, texHeight, Mathf.Max(1, textures.Count),
            TextureFormat.ARGB32, true, true);
        newTextures.SetPixels(Enumerable.Repeat(Color.white,texWidth * texHeight).ToArray(),0,0);
        //利用RenderTexture将原贴图适配到新贴图上
        RenderTexture rt = new RenderTexture(texWidth, texHeight, 1, RenderTextureFormat.ARGB32);
        Texture2D tmp = new Texture2D(texWidth, texHeight, TextureFormat.ARGB32, false);
        for (int i = 0; i < textures.Count; i++)
        {
            RenderTexture.active = rt;
            Graphics.Blit(textures[i], rt);
            tmp.ReadPixels(new Rect(0, 0, texWidth, texHeight), 0, 0);
            tmp.Apply();
            newTextures.SetPixels(tmp.GetPixels(0), i, 0);
        }
        //释放资源
        newTextures.Apply();
        RenderTexture.active = null;
        UnityEngine.Object.Destroy(rt);
        UnityEngine.Object.Destroy(tmp);
        return newTextures;
    }
    
}
