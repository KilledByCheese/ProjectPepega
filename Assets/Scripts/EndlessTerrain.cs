﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour {

	const float scale = 5f;

	private const float viewerMoveThresholdForChunkUpdate = 25f;
	private const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;

	public LODInfo[] detailLevels;
	public static float maxViewDistance;

	public Transform viewer;
    public Material mapMaterial;

	public static Vector2 viewerPosition;
	private Vector2 viewerPositionOld;

    static MapGenerator mapGenerator;
	private int chunkSize;
	private int chunksVisibleInViewDst;

	private Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
	private static List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

	void Start() {
        mapGenerator = FindObjectOfType<MapGenerator>();

		maxViewDistance = detailLevels[detailLevels.Length-1].visibleDistanceThreshold;
		chunkSize = MapGenerator.mapChunkSize - 1;
		chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDistance / chunkSize);

		UpdateVisibleChunks();
	}

	void Update() { //runs every frame
		viewerPosition = new Vector2 (viewer.position.x, viewer.position.z) / scale;

		if((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate) {
			viewerPositionOld = viewerPosition;
			UpdateVisibleChunks();
		}

	}
		
	void UpdateVisibleChunks() {

		for (int i = 0; i < terrainChunksVisibleLastUpdate.Count; i++) {
			terrainChunksVisibleLastUpdate [i].SetVisible (false);
		}
		terrainChunksVisibleLastUpdate.Clear ();
			
		int currentChunkCoordX = Mathf.RoundToInt (viewerPosition.x / chunkSize);
		int currentChunkCoordY = Mathf.RoundToInt (viewerPosition.y / chunkSize);

		for (int yOffset = -chunksVisibleInViewDst; yOffset <= chunksVisibleInViewDst; yOffset++) {
			for (int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++) {
				Vector2 viewedChunkCoord = new Vector2 (currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

				if (terrainChunkDictionary.ContainsKey (viewedChunkCoord)) {
					terrainChunkDictionary [viewedChunkCoord].UpdateTerrainChunk ();
					
				} else {
					terrainChunkDictionary.Add (viewedChunkCoord, new TerrainChunk (viewedChunkCoord, chunkSize, detailLevels, transform, mapMaterial));
				}

			}
		}
	}

	public class TerrainChunk {

		private GameObject meshObject;
		private Vector2 position;
		private Bounds bounds;
        
        private MeshRenderer meshRenderer;
        private MeshFilter meshFilter;
		private MeshCollider meshCollider;

		private LODInfo[] detailLevels;
		private LODMesh[] lodMeshes;
		private LODMesh collisionLODMesh;

		private MapData mapData;
		private bool mapDataReceived;
		private int previousLODIndex = -1;

		public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels, Transform parent, Material material) {
			this.detailLevels = detailLevels;

			position = coord * size;
			bounds = new Bounds(position,Vector2.one * size);
			Vector3 positionV3 = new Vector3(position.x,0,position.y);

			meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();            
            meshFilter = meshObject.AddComponent<MeshFilter>();
			meshCollider = meshObject.AddComponent<MeshCollider>();
            meshRenderer.material = material;

			meshObject.transform.position = positionV3 * scale;			
			meshObject.transform.parent = parent;
			meshObject.transform.localScale = Vector3.one * scale;
			SetVisible(false);

			lodMeshes = new LODMesh[detailLevels.Length];
			for(int i = 0; i < detailLevels.Length; i++) {
				lodMeshes[i] = new LODMesh(detailLevels[i].lod, UpdateTerrainChunk);
				if(detailLevels[i].useForCollider) {
					collisionLODMesh = lodMeshes[i];
				}
			}

            mapGenerator.RequestMapData(position, OnMapDataReceived);
		}

        private void OnMapDataReceived(MapData mapData) {
			this.mapData = mapData;
			mapDataReceived = true;

			Texture2D texture = TextureGenerator.TextureFromColorMap(mapData.colorMap, MapGenerator.mapChunkSize, MapGenerator.mapChunkSize);
			meshRenderer.material.mainTexture = texture;

			UpdateTerrainChunk();
        }

       // private void OnMeshDataReceived(MeshData meshData) {
       //     meshFilter.mesh = meshData.CreateMesh();
       // }

		public void UpdateTerrainChunk() {
			if(mapDataReceived) {
				float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance (viewerPosition));
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
							lodMesh.RequestMesh(mapData);
						}
					}

					if(lodIndex == 0) {
						if(collisionLODMesh.hasMesh) {
							meshCollider.sharedMesh = collisionLODMesh.mesh;
						} else if(!collisionLODMesh.hasRequestedMesh) {
							collisionLODMesh.RequestMesh(mapData);
						}
					}

					terrainChunksVisibleLastUpdate.Add(this);
				}

				SetVisible (visible);
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
		System.Action updateCallback; //to "manually" call update when meshes are received

		public LODMesh(int lod, System.Action updateCallback) {
			this.lod = lod;
			this.updateCallback = updateCallback;
		}

		private void OnMeshDataReceived(MeshData meshData) {
			mesh = meshData.CreateMesh();
			hasMesh = true;

			updateCallback();
		}

		public void RequestMesh(MapData mapData) {
			hasRequestedMesh = true;
			mapGenerator.RequestMeshData(mapData, lod, OnMeshDataReceived);
		}

	}

	[System.Serializable]
	public struct LODInfo {
		public int lod;
		public float visibleDistanceThreshold; //Outside of this Threshold switching to next lod
		public bool useForCollider;
	}
}