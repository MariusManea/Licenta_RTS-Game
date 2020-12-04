using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointMeshGenerator : MonoBehaviour
{
    public void Start()
    {
        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = new Material(Shader.Find("PCGShader"));

        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();

        Mesh mesh = new Mesh();

        Vector3[] vertices = new Vector3[1]
        {
            new Vector3(0, 0, 0),
        };
        mesh.vertices = vertices;

        Vector3[] normals = new Vector3[1]
        {
            -Vector3.forward,
        };
        mesh.normals = normals;

        Vector2[] uv = new Vector2[1]
        {
            new Vector2(0, 0),
        };
        mesh.uv = uv;

        meshFilter.mesh = mesh;
    }
}
