using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct TerrainType {
    public string name;
    public float height;
    public Color color;
}


public class MapGenerator : MonoBehaviour {

    public enum DrawMode {
        NoiseMode,ColorMode,MeshMode
    }
    public DrawMode drawMode;

    const int mapChunkSize = 241;
    [Range(0,6)]
    public int levelOfDetail;
    public float noiseScale;

    public int octaves;
    [Range(0,1)]
    public float persistance;
    public float lacunarity;

    public int seed;
    public Vector2 offset;

    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;

    public bool autoUpdate;

    public TerrainType[] regions;

    public void GenerateMap() {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, noiseScale, octaves, persistance, lacunarity, seed, offset);

        Color[] colorMap = new Color[mapChunkSize * mapChunkSize];
         for(int y = 0; y < mapChunkSize; y++) {
           for(int x = 0; x < mapChunkSize; x++) {
               float currentHeight = noiseMap[x,y];
               for(int r = 0; r < regions.Length; r++) {
                   if(currentHeight <= regions[r].height) {
                       colorMap[y * mapChunkSize + x] = regions[r].color;
                       break;
                   }
               }
           }
        }

        MapDisplay display = FindObjectOfType<MapDisplay>();
        if(drawMode == DrawMode.NoiseMode) {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(noiseMap));
        } else if(drawMode == DrawMode.ColorMode) {
            display.DrawTexture(TextureGenerator.TextureFromColorMap(colorMap, mapChunkSize, mapChunkSize));
        } else if(drawMode == DrawMode.MeshMode) {
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(noiseMap, meshHeightMultiplier, meshHeightCurve, levelOfDetail), TextureGenerator.TextureFromColorMap(colorMap, mapChunkSize, mapChunkSize));
        }
    }


    void OnValidate() {
      
        if(lacunarity < 1) {
            lacunarity = 1;
        }
        if(octaves < 0) {
            octaves = 0;
        }
    }
}
