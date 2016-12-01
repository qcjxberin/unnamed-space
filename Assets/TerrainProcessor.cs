using UnityEngine;
//using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class TerrainProcessor {
    int maxHeight;
    float worldUnitsPerTile;
    //Mesh thisMesh;

	// Use this for initialization
	public TerrainProcessor(float perTile, int h) {
        maxHeight = h;
        worldUnitsPerTile = perTile;
    }

    public float GetUnitsPerTile() {
        return worldUnitsPerTile;
    }


    //private void OnDrawGizmos() {
        
    //    Gizmos.color = Color.black;
    //    for(int i = 0; i < thisMesh.vertices.Length; i++) {
    //        Gizmos.DrawSphere(thisMesh.vertices[i], 0.1f);
    //    }
    //}

    //dim = # verts on one side
    Mesh CreateMesh(int dim) {
        Mesh m = new Mesh();
        
        Vector3[] verts = new Vector3[dim * dim];
        Vector2[] uvs = new Vector2[verts.Length];
        int[] tris = new int[(dim-1)*(dim-1)*6];
        float increment = worldUnitsPerTile / (dim-1);
        //Debug.Log("increment = " + increment);
        int vertCounter = 0;
        for(int y = 0; y < dim; y++) {
            for(int x = 0; x < dim; x++) {
                verts[vertCounter].x = x * increment;
                verts[vertCounter].z = y * increment;
                verts[vertCounter].y = 0;
                uvs[vertCounter] = new Vector2((float)x / (dim - 1), (float)y / (dim - 1));
                vertCounter++;
            }
        }

        
        for (int ti = 0, vi = 0, y = 0; y < dim-1; y++, vi++) {
            for (int x = 0; x < dim-1; x++, ti += 6, vi++) {
                tris[ti] = vi;
                tris[ti + 3] = tris[ti + 2] = vi + 1;
                tris[ti + 4] = tris[ti + 1] = vi + (dim-1) + 1;
                tris[ti + 5] = vi + (dim-1) + 2;
            }
        }

        m.vertices = verts;
        m.triangles = tris;
        m.uv = uvs;
        return m;
    }

    public Mesh SplitVerts(Mesh input) {
        int[] tris = input.triangles;
        List<int> newTris = new List<int>(0);
        Vector3[] verts = input.vertices;
        List<Vector3> newVerts = new List<Vector3>(0);
        Vector2[] uvs = input.uv;
        List<Vector2> newUvs = new List<Vector2>(0);
        Vector3[] normals = input.normals;
        

        //Debug.Log(newVerts.Count);
        //Debug.Log(verts.Length);

        
        int newVertCounter = 0;
        for(int i = 0; i < tris.Length; i += 3) {
            for(int j = 0; j < 3; j++) {
                newVerts.Add(verts[tris[i+j]]);
                newUvs.Add(uvs[tris[i+j]]);
                
                newTris.Add(newVertCounter);
                newVertCounter++;
            }
            
        }

        List<Vector3> newNormals = new List<Vector3>(newVerts.Count);
        //Debug.Log("Length of newTris = " + newTris.Count);
        //Debug.Log("Length of newVerts = " + newVerts.Count);
        
        for (int i = 0; i < newTris.Count; i += 3) {
            Vector3 vector1 = newVerts[newTris[i + 1]] - newVerts[newTris[i]];
            Vector3 vector2 = newVerts[newTris[i + 2]] - newVerts[newTris[i]];

            Vector3 normal = Vector3.Cross(vector1, vector2).normalized;
            //Debug.Log("i = " + i);
            //Debug.Log("newTris[i] = " + newTris[i]);
            newNormals.Add(normal);
            newNormals.Add(normal);
            newNormals.Add(normal);

            //Vector3 n1 = newNormals[tris[i]];
            //Vector3 n2 = newNormals[tris[i+1]];
            //Vector3 n3 = newNormals[tris[i+2]];

            //Vector3 average = new Vector3((n1.x + n2.x + n3.x) / 3, (n1.y + n2.y + n3.y) / 3, (n1.z + n2.z + n3.z) / 3);
            //newNormals[tris[i]] = average;
            //newNormals[tris[i+1]] = average;
            //newNormals[tris[i+2]] = average;

        }


        //Debug.Log(newVerts.Count);

        Mesh outputMesh = new Mesh();

        outputMesh.vertices = newVerts.ToArray();
        outputMesh.triangles = newTris.ToArray();
        outputMesh.normals = newNormals.ToArray();
        outputMesh.uv = newUvs.ToArray();

        //outputMesh.RecalculateNormals();
        outputMesh.RecalculateBounds();
        return outputMesh;

    }

    public Mesh Process(float[,] data) {
        //Debug.Log("Process(): Length of recieved data is " + data.GetLength(0));
        int width = 0;
        float[,] alphaArray = data;
        

        width = alphaArray.GetLength(0);
        //Debug.Log("Process() sees width = " + width);
        Mesh output = CreateMesh(width);

        Vector3[] tempArray = output.vertices;
        int counter = 0;
        for (int y = 0; y < Mathf.Sqrt(output.vertexCount); y++) {
            for (int x = 0; x < Mathf.Sqrt(output.vertexCount); x++) {
                tempArray[x + (y*width)].y += alphaArray[x, y] * maxHeight;
                

            }
        }
        output.vertices = tempArray;
        output = SplitVerts(output);
        //Debug.Log("End of Process(): Vert count is" + output.vertices.Length);
        return output;
    }

    




}
