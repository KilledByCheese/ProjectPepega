using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;





public class MapGenerator : MonoBehaviour {    

    public enum DrawMode {
        NoiseMode,MeshMode,FallOffMode
    }
    public DrawMode drawMode;     

    public MeshSettings meshSettings;
    public HeightMapSettings heightMapSettings; 
    public TextureData textureData;

    public Material terrainMaterial;

    [Range(0,MeshSettings.numSupportedLODs-1)]
    public int editorLODpreview;     
    private float[,] falloffMap;  
    public bool autoUpdate; 

    private Queue<MapThreadInfo<HeightMap>> heightMapThreadInfoQueue = new Queue<MapThreadInfo<HeightMap>>();
    private Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();   

    void Start() {
        textureData.ApplyToMaterial(terrainMaterial);
        textureData.UpdateMeshHeights(terrainMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);
    }

    void OnValuesUpdated() {
        if(!Application.isPlaying) {
            DrawMapInEditor();
        }
    }

    void OnTextureValuesUpdated() {
        textureData.ApplyToMaterial(terrainMaterial);
    }

    public void DrawMapInEditor() {
        textureData.UpdateMeshHeights(terrainMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);
        HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(meshSettings.numVerticesPerLine,meshSettings.numVerticesPerLine,heightMapSettings,Vector2.zero);

        MapDisplay display = FindObjectOfType<MapDisplay>();
        if(drawMode == DrawMode.NoiseMode) {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(heightMap.values));       
        } else if(drawMode == DrawMode.MeshMode) {
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, editorLODpreview));
        } else if(drawMode == DrawMode.FallOffMode) {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(meshSettings.numVerticesPerLine)));
        }
    }

    public void RequestMapData(Vector2 center, Action<HeightMap> callback) {
        //textureData.UpdateMeshHeights(terrainMaterial, meshSettings.minHeight, meshSettings.maxHeight);
        ThreadStart threadStart = delegate {
            HeightMapThread(center, callback);
        };
        new Thread(threadStart).Start();
    }

    private void HeightMapThread(Vector2 center, Action<HeightMap> callback) { //Thread start relegate
        HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(meshSettings.numVerticesPerLine,meshSettings.numVerticesPerLine,heightMapSettings,center);
        lock(heightMapThreadInfoQueue) {
            heightMapThreadInfoQueue.Enqueue(new MapThreadInfo<HeightMap>(callback, heightMap));
        }
    }

    public void RequestMeshData(HeightMap heightMap, int lod, Action<MeshData> callback) {
        ThreadStart threadStart = delegate {
			MeshDataThread (heightMap, lod, callback);
		};

		new Thread (threadStart).Start ();
    }

    private void MeshDataThread(HeightMap heightMap, int lod, Action<MeshData> callback) {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, lod);
        lock (meshDataThreadInfoQueue) {
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback,meshData));
        }
    }

    void Update() {
        if(heightMapThreadInfoQueue.Count > 0) {
            for(int i = 0; i < heightMapThreadInfoQueue.Count; i++) {
                MapThreadInfo<HeightMap> threadInfo = heightMapThreadInfoQueue.Dequeue();
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

    


    void OnValidate() {   

        if(meshSettings != null) {
            meshSettings.OnValuesUpdated -= OnValuesUpdated; //Unsubscribe - does nothing if not already subscribed -
            meshSettings.OnValuesUpdated += OnValuesUpdated; //Subscribe 
        }
        if(heightMapSettings != null) {
            heightMapSettings.OnValuesUpdated -= OnValuesUpdated;
            heightMapSettings.OnValuesUpdated += OnValuesUpdated;
        }
        if(textureData != null) {
            textureData.OnValuesUpdated -= OnTextureValuesUpdated;
            textureData.OnValuesUpdated += OnTextureValuesUpdated;
        }
        
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


