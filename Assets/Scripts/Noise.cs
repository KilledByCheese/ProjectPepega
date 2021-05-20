using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise {

    public enum NormalizeMode{
        Local, Global
    };
   
   public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, float noiseScale, int octaves, float persistance, float lacunarity, int seed, Vector2 offset, NormalizeMode normalizeMode) {
       float[,] noiseMap = new float[mapWidth, mapHeight];

       System.Random prng = new System.Random(seed);
       Vector2[] octaveOffsets = new Vector2[octaves];

        float maxPossibleHeight = 0;
        float amplitude = 1;
        float frequency = 1;

       for(int i = 0; i < octaves; i++) {
            float offsetX = prng.Next(-100000,100000) + offset.x; //Pseudo Random Numbers between -100000 and 100000
            float offsetY = prng.Next(-100000,100000) - offset.y;  //Subtracting to get right map movement when changin the y offset      
            octaveOffsets[i] = new Vector2(offsetX,offsetY);

            maxPossibleHeight += amplitude;
            amplitude *= persistance;
        }

       if(noiseScale <= 0) {
           noiseScale = 0.0001f;

       }

        float maxLocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;

        float halfWidth = mapWidth / 2f;
        float halfHeight = mapHeight / 2f;

       for(int y = 0; y < mapHeight; y++) {
           for(int x = 0; x < mapWidth; x++) {


               amplitude = 1;
               frequency = 1;
               float noiseHeight = 0;

               for(int i = 0; i < octaves; i++) {
                    float sampleX = (x - halfWidth + octaveOffsets[i].x) / noiseScale * frequency ; //Zooming to center
                    float sampleY = (y - halfHeight + octaveOffsets[i].y) / noiseScale * frequency ;

                    float perlinValue= Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1; //Makes it possible for values to be between -1 and 1
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                    
                }
                if(noiseHeight > maxLocalNoiseHeight) {
                    maxLocalNoiseHeight = noiseHeight;
                } else if(noiseHeight < minLocalNoiseHeight) {
                    minLocalNoiseHeight = noiseHeight;
                }
                noiseMap[x,y] = noiseHeight;
            }
        }
        for(int y = 0; y < mapHeight; y++) {
           for(int x = 0; x < mapWidth; x++) {
               if(normalizeMode == NormalizeMode.Local) {
                    noiseMap[x,y] = Mathf.InverseLerp(minLocalNoiseHeight,maxLocalNoiseHeight,noiseMap[x,y]); //Normalize Values to be between 0-1
               } else if(normalizeMode == NormalizeMode.Global) {
                   float normalizedHeight = (noiseMap[x,y] + 1) / (2f * maxPossibleHeight / 2f);      
                   noiseMap[x,y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);         
               }

           }
        }
        return noiseMap;
    }
}
