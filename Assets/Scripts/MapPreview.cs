using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapPreview : MonoBehaviour {

    public Renderer textureRenderer;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

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
    public bool autoUpdate; 

    public void DrawTexture(Texture2D texture) {
        textureRenderer.sharedMaterial.mainTexture = texture;
        textureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height) / 10f;

        textureRenderer.gameObject.SetActive(true);
        meshFilter.gameObject.SetActive(false);
    }

    public void DrawMesh(MeshData meshData) {
        meshFilter.sharedMesh = meshData.CreateMesh();   
        textureRenderer.gameObject.SetActive(false);
        meshFilter.gameObject.SetActive(true);  
    }

    public void DrawMapInEditor() {
        textureData.ApplyToMaterial(terrainMaterial);

        textureData.UpdateMeshHeights(terrainMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);
        HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(meshSettings.numVerticesPerLine,meshSettings.numVerticesPerLine,heightMapSettings,Vector2.zero);
       
        if(drawMode == DrawMode.NoiseMode) {
            DrawTexture(TextureGenerator.TextureFromHeightMap(heightMap));       
        } else if(drawMode == DrawMode.MeshMode) {
            DrawMesh(MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, editorLODpreview));
        } else if(drawMode == DrawMode.FallOffMode) {
            DrawTexture(TextureGenerator.TextureFromHeightMap(new HeightMap(FalloffGenerator.GenerateFalloffMap(meshSettings.numVerticesPerLine),0,1)));
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
    
    void OnValuesUpdated() {
        if(!Application.isPlaying) {
            DrawMapInEditor();
        }
    }

    void OnTextureValuesUpdated() {
        textureData.ApplyToMaterial(terrainMaterial);
    }
}
