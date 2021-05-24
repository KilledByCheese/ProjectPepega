using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour {

	

	private const float viewerMoveThresholdForChunkUpdate = 25f;
	private const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;
	private const float colliderGenerationDistanceThreshold = 5;

	public int colliderLODIndex;
	public LODInfo[] detailLevels;
	public static float maxViewDistance;

	public Transform viewer;
    public Material mapMaterial;

	public static Vector2 viewerPosition;
	private Vector2 viewerPositionOld;

    static MapGenerator mapGenerator;
	private float meshWorldSize;
	private int chunksVisibleInViewDst;

	private Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
	private static List<TerrainChunk> visibleTerrainChunks = new List<TerrainChunk>();

	void Start() {
        mapGenerator = FindObjectOfType<MapGenerator>();

		maxViewDistance = detailLevels[detailLevels.Length-1].visibleDistanceThreshold;
		meshWorldSize = mapGenerator.meshSettings.meshWorldSize;
		chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDistance / meshWorldSize);

		UpdateVisibleChunks();
	}

	void Update() { //runs every frame
		viewerPosition = new Vector2 (viewer.position.x, viewer.position.z);

		if(viewerPosition != viewerPositionOld) {
			foreach(TerrainChunk chunk in visibleTerrainChunks) {
				chunk.UpdateCollisionMesh();
			}
		}

		if((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate) {
			viewerPositionOld = viewerPosition;
			UpdateVisibleChunks();
		}

	}
		
	void UpdateVisibleChunks() {
		HashSet<Vector2> alreadyUpdatedChunkCoords = new HashSet<Vector2>();
		for (int i = visibleTerrainChunks.Count-1; i >= 0; i--) {
			alreadyUpdatedChunkCoords.Add(visibleTerrainChunks[i].coord);
			visibleTerrainChunks [i].UpdateTerrainChunk();
		}
		
			
		int currentChunkCoordX = Mathf.RoundToInt (viewerPosition.x / meshWorldSize);
		int currentChunkCoordY = Mathf.RoundToInt (viewerPosition.y / meshWorldSize);

		for (int yOffset = -chunksVisibleInViewDst; yOffset <= chunksVisibleInViewDst; yOffset++) {
			for (int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++) {
				Vector2 viewedChunkCoord = new Vector2 (currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

				if(!alreadyUpdatedChunkCoords.Contains(viewedChunkCoord)) {
					if (terrainChunkDictionary.ContainsKey (viewedChunkCoord)) {
					terrainChunkDictionary [viewedChunkCoord].UpdateTerrainChunk ();
					
					} else {
						terrainChunkDictionary.Add (viewedChunkCoord, new TerrainChunk (viewedChunkCoord, meshWorldSize, detailLevels, colliderLODIndex, transform, mapMaterial));
					}
				}			
			}
		}
	}

	public class TerrainChunk {

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
		

		private HeightMap mapData;
		private bool mapDataReceived;
		private int previousLODIndex = -1;
		private bool hasSetCollider;

		public TerrainChunk(Vector2 coord, float meshWorldSize, LODInfo[] detailLevels, int colliderLODIndex, Transform parent, Material material) {
			this.coord = coord;
			this.detailLevels = detailLevels;
			this.colliderLODIndex = colliderLODIndex;

			sampleCenter = coord * meshWorldSize / mapGenerator.meshSettings.meshScale;
			Vector2 position = coord * meshWorldSize;
			bounds = new Bounds(position,Vector2.one * meshWorldSize);

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

            mapGenerator.RequestMapData(sampleCenter, OnMapDataReceived);
		}

        private void OnMapDataReceived(HeightMap mapData) {
			this.mapData = mapData;
			mapDataReceived = true;

			UpdateTerrainChunk();
        }

       // private void OnMeshDataReceived(MeshData meshData) {
       //     meshFilter.mesh = meshData.CreateMesh();
       // }

		public void UpdateTerrainChunk() {
			if(mapDataReceived) {
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
							lodMesh.RequestMesh(mapData);
						}
					}			
				}

				if(wasVisible != visible) {
					if(visible) {
						visibleTerrainChunks.Add(this);
					} else {
						visibleTerrainChunks.Remove(this);
					}
					SetVisible (visible);
				}
			}
		}

		public void UpdateCollisionMesh() {
			if(!hasSetCollider) {
				float sqrDistanceFromViewerToEdge = bounds.SqrDistance(viewerPosition);

				if(sqrDistanceFromViewerToEdge < detailLevels[colliderLODIndex].sqrVisibleDistanceThreshold) {
					if(!lodMeshes[colliderLODIndex].hasRequestedMesh) {
						lodMeshes[colliderLODIndex].RequestMesh(mapData);
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

		private void OnMeshDataReceived(MeshData meshData) {
			mesh = meshData.CreateMesh();
			hasMesh = true;

			updateCallback();
		}

		public void RequestMesh(HeightMap mapData) {
			hasRequestedMesh = true;
			mapGenerator.RequestMeshData(mapData, lod, OnMeshDataReceived);
		}

	}

	[System.Serializable]
	public struct LODInfo {
		[Range(0,MeshSettings.numSupportedLODs-1)]
		public int lod;
		public float visibleDistanceThreshold; //Outside of this Threshold switching to next lod
		

		public float sqrVisibleDistanceThreshold {
			get  {
				return visibleDistanceThreshold * visibleDistanceThreshold;
			}
		}
	}
}