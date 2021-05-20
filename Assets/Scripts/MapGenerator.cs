using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

[System.Serializable]
public struct TerrainType {
    public string name;
    public float height;
    public Color color;
}

public struct MapData {
    public readonly float[,] heightMap;
    public readonly Color[] colorMap;

    public MapData(float[,] heightMap, Color[] colorMap) {
        this.heightMap = heightMap;
        this.colorMap = colorMap;
    }
}

public class MapGenerator : MonoBehaviour {

    static MapGenerator instance;

    public enum DrawMode {
        NoiseMode,ColorMode,MeshMode,FallOffMode
    }
    public DrawMode drawMode;

    public Noise.NormalizeMode normalizeMode;

    public static int mapChunkSize {
        get{
            if(instance == null) {
                instance = FindObjectOfType<MapGenerator>();
            }
            if(instance.useFlatShading) {
                return 95;
            } else {
                return 239;
            }
        }
    }

    public bool useFlatShading;

    [Range(0,6)]
    public int editorLODpreview;
    public float noiseScale;

    public int octaves;
    [Range(0,1)]
    public float persistance;
    public float lacunarity;

    public int seed;
    public Vector2 offset;

    public bool useFalloff;
    private float[,] falloffMap;

    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;

    public bool autoUpdate;

    public TerrainType[] regions;

    private Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    private Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    void Awake() { //called by unity
        falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize);
    }

    public void DrawMapInEditor() {
        MapData mapData = GenerateMapData(Vector2.zero);

        MapDisplay display = FindObjectOfType<MapDisplay>();
        if(drawMode == DrawMode.NoiseMode) {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
        } else if(drawMode == DrawMode.ColorMode) {
            display.DrawTexture(TextureGenerator.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
        } else if(drawMode == DrawMode.MeshMode) {
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, editorLODpreview, useFlatShading), TextureGenerator.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
        } else if(drawMode == DrawMode.FallOffMode) {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(mapChunkSize)));
        }
    }

    public void RequestMapData(Vector2 center, Action<MapData> callback) {
        ThreadStart threadStart = delegate {
            MapDataThread(center, callback);
        };
        new Thread(threadStart).Start();
    }

    private void MapDataThread(Vector2 center, Action<MapData> callback) { //Thread start relegate
        MapData mapData = GenerateMapData(center);
        lock(mapDataThreadInfoQueue) {
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }

    public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback) {
        ThreadStart threadStart = delegate {
			MeshDataThread (mapData, lod, callback);
		};

		new Thread (threadStart).Start ();
    }

    private void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback) {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, lod, useFlatShading);
        lock (meshDataThreadInfoQueue) {
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback,meshData));
        }
    }

    void Update() {
        if(mapDataThreadInfoQueue.Count > 0) {
            for(int i = 0; i < mapDataThreadInfoQueue.Count; i++) {
                MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
        if(meshDataThreadInfoQueue.Count > 0) {
            for(int i = 0; i < meshDataThreadInfoQueue.Count; i++) {
                MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }

    private MapData GenerateMapData(Vector2 center) {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize + 2, mapChunkSize + 2, noiseScale, octaves, persistance, lacunarity, seed, center + offset, normalizeMode);

        Color[] colorMap = new Color[mapChunkSize * mapChunkSize];
         for(int y = 0; y < mapChunkSize; y++) {
           for(int x = 0; x < mapChunkSize; x++) {

               if(useFalloff) { //using Falloff
                    noiseMap[x,y] = Mathf.Clamp01(noiseMap[x,y] - falloffMap[x,y]);
               }

               float currentHeight = noiseMap[x,y];
               for(int r = 0; r < regions.Length; r++) {
                   if(currentHeight >= regions[r].height) {
                       colorMap[y * mapChunkSize + x] = regions[r].color;                       
                   } else {
                       break;
                   }
               }
           }
        }
        return new MapData(noiseMap, colorMap);        
    }


    void OnValidate() {
      
        if(lacunarity < 1) {
            lacunarity = 1;
        }
        if(octaves < 0) {
            octaves = 0;
        }

        falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize); // Generate FalloffMap when game is not started
    }

    struct MapThreadInfo<T> {
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback, T parameter) {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}
