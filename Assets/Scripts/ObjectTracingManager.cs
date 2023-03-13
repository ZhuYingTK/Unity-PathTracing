using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

struct MeshObject
{
    public Matrix4x4 localToWorldMatrix;
    public Matrix4x4 worldToLocalMatrix;
    public int indices_offset;
    public int indices_count;
    public Vector3 AABBmax;
    public Vector3 AABBmin;
    public Vector3 albedo;   
    public Vector3 specular; 
    public float  smoothness;
    public Vector3 emission;
    public float opactiy;
    public float refractivity;
}

struct Vertex
{
    public Vector3 position;
    public Vector3 normal;
}
public class ObjectTracingManager : MonoBehaviour
{
    private static bool _meshObjectsNeedRebuilding = false;
    private static List<RayTracingObject> _rayTracingObjects = new List<RayTracingObject>();
    
    //Mesh相关参数
    private static List<MeshObject> _meshObjects = new List<MeshObject>();
    private static List<Vertex> _vertices = new List<Vertex>();
    private static List<int> _indices = new List<int>();

    public static ComputeBuffer _meshObjectBuffer;
    public static ComputeBuffer _vertexBuffer;
    public static ComputeBuffer _indexBuffer;

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

        foreach (RayTracingObject obj in _rayTracingObjects)
        {
            Mesh mesh = obj.GetComponent<MeshFilter>().sharedMesh;
            
            //添加顶点数据
            int firstVertex = _vertices.Count;
            for (int i = 0; i < mesh.vertexCount; i++)
            {
                Vertex _vertex = new Vertex(){position = mesh.vertices[i],normal = mesh.normals[i]}; 
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
                    albedo = obj.albedo,
                    specular = obj.specular,
                    smoothness = obj.smoothness,
                    emission = obj.emission,
                    opactiy = obj.opacity,
                    refractivity = obj.refractivity
                });
            
            CreateComputeBuffer(ref _meshObjectBuffer, _meshObjects, 208);
            CreateComputeBuffer(ref _vertexBuffer, _vertices, 24);
            CreateComputeBuffer(ref _indexBuffer, _indices, 4);
        }
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
}
