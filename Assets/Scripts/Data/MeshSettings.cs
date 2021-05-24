using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class MeshSettings : UpdateableData {

    public const int numSupportedLODs = 5;
    public const int numSupportedChunkSizes = 10;
    public const int numSupportedFlatShadedChunkSizes = 4;
    public static readonly int[] supportedChunkSizes = {24,48,72,96,120,144,168,192,216,240};
    //public static readonly int[] supportedFlatShadedChunkSizes = {24,48,72,96};

    [Range(0,numSupportedChunkSizes-1)]
    public int chunkSizeIndex;
    [Range(0,numSupportedFlatShadedChunkSizes-1)]
    public int flatShadedChunkSizeIndex;

    public float meshScale = 2.5f;

    public bool useFlatShading;    
    
    //num of vertices per line of mesh rendered at LOD = 0. Includes the 2 extra verts that are excluded from final mesh, but used for calculating normals
    public int numVerticesPerLine { //mapChunkSize+1 must be divideable by 1,2,4,6,8 
        get{            
            return supportedChunkSizes[(useFlatShading) ? flatShadedChunkSizeIndex : chunkSizeIndex]  + 1;
        }
    }

    public float meshWorldSize {
        get {
            return (numVerticesPerLine - 3) * meshScale;
        }
    }
    
}
