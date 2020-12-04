using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SizeSelector : MonoBehaviour
{
    public Texture[] mapTextures;
    public int textureIndex;
    // Start is called before the first frame update
    void Start()
    {
        int size = mapTextures[textureIndex].height;
        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = new Material(Shader.Find("PCGShader"));

        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();

        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uv = new List<Vector2>();
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                vertices.Add(new Vector3(j, i, 0));
                normals.Add(-Vector3.forward);
                uv.Add(new Vector2((float)j / size, (float)i / size));
            }
        }

        mesh.vertices = vertices.ToArray();

        mesh.normals = normals.ToArray();

        mesh.uv = uv.ToArray();

        List<int> tris = new List<int>();
        for (int i = 1; i < size; i++)
        {
            for (int j = 1; j < size; j++)
            {
                tris.Add((i - 1) * size + j - 1);
                tris.Add(i * size + j - 1);
                tris.Add((i - 1) * size + j);

                tris.Add(i * size + j - 1);
                tris.Add(i * size + j);
                tris.Add((i - 1) * size + j);
            }
        }
        mesh.triangles = tris.ToArray();

        meshFilter.mesh = mesh;


        GetComponent<Renderer>().sharedMaterial.SetTexture("_MainTex", mapTextures[textureIndex]);
        GetComponent<Renderer>().sharedMaterial.SetInt("_Size", size);
    }
}
