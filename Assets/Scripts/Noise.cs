using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise {
   
   public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, float noiseScale, int octaves, float persistance, float lacunarity, int seed, Vector2 offset) {
       float[,] noiseMap = new float[mapWidth, mapHeight];

       System.Random prng = new System.Random(seed);
       Vector2[] octaveOffsets = new Vector2[octaves];
       for(int i = 0; i < octaves; i++) {
            float offsetX = prng.Next(-100000,100000) + offset.x; //Pseudo Random Numbers between -100000 and 100000
            float offsetY = prng.Next(-100000,100000) + offset.y;        
            octaveOffsets[i] = new Vector2(offsetX,offsetY);
        }

       if(noiseScale <= 0) {
           noiseScale = 0.0001f;

       }

        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        float halfWidth = mapWidth / 2f;
        float halfHeight = mapHeight / 2f;

       for(int y = 0; y < mapHeight; y++) {
           for(int x = 0; x < mapWidth; x++) {


               float amplitude = 1;
               float frequency = 1;
               float noiseHeight = 0;

               for(int i = 0; i < octaves; i++) {
                    float sampleX = (x - halfWidth) / noiseScale * frequency + octaveOffsets[i].x; //Zooming to center
                    float sampleY = (y - halfHeight) / noiseScale * frequency + octaveOffsets[i].y;

                    float perlinValue= Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1; //Makes it possible for values to be between -1 and 1
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                    
                }
                if(noiseHeight > maxNoiseHeight) {
                    maxNoiseHeight = noiseHeight;
                } else if(noiseHeight < minNoiseHeight) {
                    minNoiseHeight = noiseHeight;
                }
                noiseMap[x,y] = noiseHeight;
            }
        }
        for(int y = 0; y < mapHeight; y++) {
           for(int x = 0; x < mapWidth; x++) {
               noiseMap[x,y] = Mathf.InverseLerp(minNoiseHeight,maxNoiseHeight,noiseMap[x,y]); //Normalize Values to be between 0-1
           }
        }
        return noiseMap;
    }
}
