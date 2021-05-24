using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HeightMapGenerator {

   public static HeightMap GenerateHeightMap(int width, int height, HeightMapSettings settings,MeshSettings meshSettings, Vector2 sampleCenter) {
       float[,] values = Noise.GenerateNoiseMap(width,height,settings.noiseSettings,sampleCenter);
       float[,] falloffMap = FalloffGenerator.GenerateFalloffMap(width);

        AnimationCurve heightCurve_threadsafe = new AnimationCurve(settings.heightCurve.keys);

        float minValue = float.MaxValue;
        float maxValue = float.MinValue;
        if(settings.useFalloff) {
            
           // Debug.Log("Applying Falloff for verticesPerLine:  " + meshSettings.numVerticesPerLine);
        }
       for(int i = 0; i < width; i++) {
           for(int j = 0; j < height; j++) {
               if(settings.useFalloff) {
                   values[i,j] = Mathf.Clamp(values[i,j] - falloffMap[i,j],0,2);
                   if( (0 + Mathf.Abs(sampleCenter.x)) > (meshSettings.numVerticesPerLine/2)  ) { //Every chunk that is not the center chunk when using a falloff map is flat
                       values[i,j] = 0;
                   } else if((0 + Mathf.Abs(sampleCenter.y)) > (meshSettings.numVerticesPerLine/2)) {
                       values[i,j] = 0;
                   }
               }

               values[i,j] *= heightCurve_threadsafe.Evaluate(values[i,j]) * settings.heightMultiplier; 
               if(values[i,j]>maxValue) {
                   maxValue = values[i,j];
               }
               if(values[i,j]<minValue) {
                   minValue = values[i,j];
               }
           }
       }
        
       return new HeightMap(values, minValue, maxValue);
   }

}

public struct HeightMap {
    public readonly float[,] values;
    public readonly float minValue;
    public readonly float maxValue;
  
    public HeightMap(float[,] values, float minValue, float maxValue) {
        this.values = values;
        this.minValue = minValue;
        this.maxValue = maxValue;
    }
}
