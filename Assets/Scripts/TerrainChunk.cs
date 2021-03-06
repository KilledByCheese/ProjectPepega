using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainChunk {

    public event System.Action<TerrainChunk, bool> onVisibilityChanged;

    private const float colliderGenerationDistanceThreshold = 5;

    public Vector2 coord;

    private GameObject meshObject;
    private Vector2 sampleCenter;
    private Bounds bounds;
    
    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;

    private LODInfo[] detailLevels;
    private LODMesh[] lodMeshes;
    private int colliderLODIndex;
    

    private HeightMap heightMap;
    private bool heightMapReceived;
    private int previousLODIndex = -1;
    private bool hasSetCollider;
    private float maxViewDistance;

    private HeightMapSettings heightMapSettings;
    private MeshSettings meshSettings;

    private Transform viewer;

    public TerrainChunk(Vector2 coord, HeightMapSettings heightMapSettings, MeshSettings meshSettings, LODInfo[] detailLevels, int colliderLODIndex, Transform parent, Transform viewer, Material material) {
        this.coord = coord;
        this.detailLevels = detailLevels;
        this.colliderLODIndex = colliderLODIndex;
        this.heightMapSettings = heightMapSettings;
        this.meshSettings = meshSettings;
        this.viewer = viewer;

        sampleCenter = coord * meshSettings.meshWorldSize / meshSettings.meshScale;
        Vector2 position = coord * meshSettings.meshWorldSize;
        bounds = new Bounds(position,Vector2.one * meshSettings.meshWorldSize);

        meshObject = new GameObject("Terrain Chunk");
        meshRenderer = meshObject.AddComponent<MeshRenderer>();            
        meshFilter = meshObject.AddComponent<MeshFilter>();
        meshCollider = meshObject.AddComponent<MeshCollider>();
        meshRenderer.material = material;

        meshObject.transform.position = new Vector3(position.x,0,position.y);			
        meshObject.transform.parent = parent;
        
        SetVisible(false);

        lodMeshes = new LODMesh[detailLevels.Length];
        for(int i = 0; i < detailLevels.Length; i++) {
            lodMeshes[i] = new LODMesh(detailLevels[i].lod);
            lodMeshes[i].updateCallback += UpdateTerrainChunk;
            if(i == colliderLODIndex) {
                lodMeshes[i].updateCallback += UpdateCollisionMesh;
            }
        }

        maxViewDistance = detailLevels[detailLevels.Length-1].visibleDistanceThreshold;

    }

    Vector2 viewerPosition {
        get {
            return new Vector2(viewer.position.x, viewer.position.z);
        }
    }

    public void Load() {
         ThreadedDataRequester.RequestData(() => HeightMapGenerator.GenerateHeightMap(meshSettings.numVerticesPerLine, meshSettings.numVerticesPerLine, heightMapSettings,meshSettings, sampleCenter), OnHeightMapReceived);
    }

    private void OnHeightMapReceived(object heightMapObject) {

        this.heightMap = (HeightMap)heightMapObject;
        heightMapReceived = true;

        UpdateTerrainChunk();
    }

    // private void OnMeshDataReceived(MeshData meshData) {
    //     meshFilter.mesh = meshData.CreateMesh();
    // }

    public void UpdateTerrainChunk() {
        if(heightMapReceived) {
            float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance (viewerPosition));

            bool wasVisible = IsVisible();
            bool visible = viewerDstFromNearestEdge <= maxViewDistance;

            if(visible) {
                int lodIndex = 0;
                for(int i = 0; i < detailLevels.Length-1; i++) {
                    if(viewerDstFromNearestEdge > detailLevels[i].visibleDistanceThreshold) {
                        lodIndex = i + 1;
                    } else {
                        break;
                    }
                }

                if(lodIndex != previousLODIndex) {
                    LODMesh lodMesh = lodMeshes[lodIndex];
                    if(lodMesh.hasMesh) {
                        previousLODIndex = lodIndex;
                        meshFilter.mesh = lodMesh.mesh;							
                    } else if(!lodMesh.hasRequestedMesh) {
                        lodMesh.RequestMesh(heightMap, meshSettings);
                    }
                }			
            }

            if(wasVisible != visible) {
                SetVisible (visible);
                if(onVisibilityChanged != null) {
                    onVisibilityChanged(this,visible);
                }
            }
        }
    }

    public void UpdateCollisionMesh() {
        if(!hasSetCollider) {
            float sqrDistanceFromViewerToEdge = bounds.SqrDistance(viewerPosition);

            if(sqrDistanceFromViewerToEdge < detailLevels[colliderLODIndex].sqrVisibleDistanceThreshold) {
                if(!lodMeshes[colliderLODIndex].hasRequestedMesh) {
                    lodMeshes[colliderLODIndex].RequestMesh(heightMap, meshSettings);
                }
            }

            if(sqrDistanceFromViewerToEdge < colliderGenerationDistanceThreshold * colliderGenerationDistanceThreshold) {
                if(lodMeshes[colliderLODIndex].hasMesh) {
                    meshCollider.sharedMesh = lodMeshes[colliderLODIndex].mesh;
                    hasSetCollider = true;
                }
            }
        }			
    }

    public void SetVisible(bool visible) {
        meshObject.SetActive (visible);
    }

    public bool IsVisible() {
        return meshObject.activeSelf;
    }

}

class LODMesh { //Level Of Detail - chunks in distance will be rendered in Lower res

    public Mesh mesh;
    public bool hasRequestedMesh;
    public bool hasMesh;
    private int lod;
    public event System.Action updateCallback; //to "manually" call update when meshes are received

    public LODMesh(int lod) {
        this.lod = lod;			
    }

    private void OnMeshDataReceived(object meshDataObject) {
        mesh = ((MeshData)meshDataObject).CreateMesh();
        hasMesh = true;

        updateCallback();
    }

    public void RequestMesh(HeightMap heightMap, MeshSettings meshSettings) {
        hasRequestedMesh = true;
        ThreadedDataRequester.RequestData(() => MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, lod), OnMeshDataReceived);
    }

}
