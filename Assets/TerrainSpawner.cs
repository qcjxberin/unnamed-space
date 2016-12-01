using UnityEngine;
using System.Collections;

public class TerrainSpawner : MonoBehaviour {
    public Material terrainMaterial;
    public GameObject water;
    Perlin perl;
    Perlin secondaryPerl;
    TerrainProcessor tp;
    float[,] majorData;

    //Texture2D testReference = new Texture2D(2048, 2048);
	// Use this for initialization
	void MakeChunk (int vertices, Vector3 position, int x, int y, float[,] major) {
        Debug.Log("MakeChunk");
        
        float[,] perlinData = new float[vertices, vertices];
        for(int i = 0; i < vertices; i++) {
            for(int j = 0; j < vertices; j++) {
                float mjd = requestMajorFeature(vertices, i, j, x, y);
                perlinData[i, j] = (float)perl.OctavePerlin(((i + ((vertices-1) * x)) * 0.05d), (j + ((vertices-1) * y)) * 0.05d, 0.1d, 5, 0.4d * mjd) + (5 * mjd);
            }
        }   
        Mesh m = tp.Process(perlinData);
        GameObject newTile = new GameObject();
        newTile.transform.position = position;
        MeshFilter mf = newTile.AddComponent<MeshFilter>();
        mf.mesh = m;
        MeshRenderer mr = newTile.AddComponent<MeshRenderer>();
        mr.material = terrainMaterial;
        Debug.Log("making water");
        Instantiate(water, new Vector3(position.x + 1000, 400, position.z + 1000), Quaternion.identity);
	}
	
	// Update is called once per frame
	void Start () {
        perl = new Perlin();
        secondaryPerl = new Perlin(5);
        tp = new TerrainProcessor(2000, 1000);

        majorData = new float[2048, 2048];
        for (int i = 0; i < 2048; i++) {
            for (int j = 0; j < 2048; j++) {
                //majorData[i, j] = (float)perl.OctavePerlin(i * 0.01d, j * 0.01d, 0.4d, 2, 0.3d);
                //majorData[i, j] += (float)perl.OctavePerlin(i * 0.002d, j * 0.002d, 0.4d, 1, 0.3d);
            }
        }
        //for (int i = 0; i < 8; i++) {
        //    for (int j = 0; j < 8; j++) {
        //        MakeChunk(100, new Vector3(i*10, 0, j*10), i, j, majorData);
        //    }
        //}

        for (int i = 0; i < 8; i++) {
            for (int j = 0; j < 8; j++) {
                MakeChunkNew(100, new Vector3(i * 2000, 0, j * 2000), i, j);
            }
        }
    }

    float requestMajorFeature(int vertices, int vertexX, int vertexY, int x, int y) {
        float majorDataPoint = majorData[(vertexX + ((vertices - 1) * x))%2048, (vertexY + ((vertices - 1) * y))%2048];
        //return 0;
        return majorDataPoint;
    }

    void MakeChunkNew(int vertices, Vector3 position, int x, int y) {
        double min, max;
        min = float.MaxValue;
        max = float.MinValue;

        float[,] perlinData = new float[vertices, vertices];
        for (int i = 0; i < vertices; i++) {
            for (int j = 0; j < vertices; j++) {
                //float mjd = requestMajorFeature(vertices, i, j, x, y);
                //perlinData[i, j] = (float)perl.OctavePerlin(((i + ((vertices - 1) * x)) * 0.05d), (j + ((vertices - 1) * y)) * 0.05d, 0.1d, 5, 0.4d * mjd) + (5 * mjd);
                float mountainData = (float)mountains(i + ((vertices - 1) * x), j + ((vertices - 1) * y), 0.1);
                float desertData = (float)desert(i + ((vertices - 1) * x), j + ((vertices - 1) * y), 0.1);
                float lerpData = Mathf.Clamp01((8 * (float)perl.perlin((i + ((vertices - 1) * x)) * 0.002, (j + ((vertices - 1) * y)) * 0.002, 0.1)) - 4f);
                perlinData[i, j] = Mathf.Lerp(mountainData, desertData, lerpData);

                //perlinData[i, j] = Mathf.Clamp01((10 * (float)perl.perlin((i + ((vertices - 1) * x)) * 0.005, (j + ((vertices - 1) * y)) * 0.005, 0.1)) - 5f);
                //perlinData[i, j] = (float)mountains(i + ((vertices - 1) * x), j + ((vertices - 1) * y), 0.1);
                if (perlinData[i, j] < min) {
                    min = perlinData[i, j];
                }
                if(perlinData[i, j] > max) {
                    max = perlinData[i, j];
                }
            }
        }
        Debug.Log("Min " + min + " Max " + max);
        Mesh m = tp.Process(perlinData);
        GameObject newTile = new GameObject();
        newTile.transform.position = position;
        MeshFilter mf = newTile.AddComponent<MeshFilter>();
        mf.mesh = m;
        MeshRenderer mr = newTile.AddComponent<MeshRenderer>();
        mr.material = terrainMaterial;
        Instantiate(water, new Vector3(position.x + 1000, 1000*1f, position.z + 1000), Quaternion.identity);
    }

    double desert(double x, double y, double z) {
        double dunes = perl.OctavePerlin(x * 0.01, y * 0.01, z, 1, 0.5) * 0.3;
        double detail = perl.OctavePerlin(x * 0.1, y * 0.1, z, 3, 0.4) * 0.005;
        return dunes + detail + 1;
    }

    double mountains(double x, double y, double z) {

        double peaks = perl.OctavePerlin(x * 0.01, y * 0.01, z, 3, 0.6);
        double detail = perl.OctavePerlin(x * 0.2, y * 0.2, z, 3, 0.5) * 0.1 * ((peaks - 0.45)/0.1);
        //return peaks * 2 + detail;
        //return peaks * 2;
        return peaks*2 + detail;
        //return peaks + detail;
        //return 1;
    }
}
