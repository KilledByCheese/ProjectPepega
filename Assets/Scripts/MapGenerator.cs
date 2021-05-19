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

    public int mapWidth;
    public int mapHeight;
    public float noiseScale;

    public int octaves;
    [Range(0,1)]
    public float persistance;
    public float lacunarity;

    public int seed;
    public Vector2 offset;

    public bool autoUpdate;

    public TerrainType[] regions;

    public void GenerateMap() {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapWidth, mapHeight, noiseScale, octaves, persistance, lacunarity, seed, offset);

        Color[] colorMap = new Color[mapWidth * mapHeight];
         for(int y = 0; y < mapHeight; y++) {
           for(int x = 0; x < mapWidth; x++) {
               float currentHeight = noiseMap[x,y];
               for(int r = 0; r < regions.Length; r++) {
                   if(currentHeight <= regions[r].height) {
                       colorMap[y * mapWidth + x] = regions[r].color;
                       break;
                   }
               }
           }
        }

        MapDisplay display = FindObjectOfType<MapDisplay>();
        if(drawMode == DrawMode.NoiseMode) {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(noiseMap));
        } else if(drawMode == DrawMode.ColorMode) {
            display.DrawTexture(TextureGenerator.TextureFromColorMap(colorMap, mapWidth, mapHeight));
        } else if(drawMode == DrawMode.MeshMode) {
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(noiseMap), TextureGenerator.TextureFromColorMap(colorMap, mapWidth, mapHeight));
        }
    }


    void OnValidate() {
        if(mapWidth < 1) {
            mapWidth = 1;
        }
        if(mapHeight < 1) {
            mapHeight = 1;
        }
        if(lacunarity < 1) {
            lacunarity = 1;
        }
        if(octaves < 0) {
            octaves = 0;
        }
    }
}
