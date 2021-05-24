using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise {

    public enum NormalizeMode{
        Local, Global
    };
   
   public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, NoiseSettings settings, Vector2 sampleCenter) {
       float[,] noiseMap = new float[mapWidth, mapHeight];

       System.Random prng = new System.Random(settings.seed);
       Vector2[] octaveOffsets = new Vector2[settings.octaves];

        float maxPossibleHeight = 0;
        float amplitude = 1;
        float frequency = 1;

       for(int i = 0; i < settings.octaves; i++) {
            float offsetX = prng.Next(-100000,100000) + settings.offset.x + sampleCenter.x; //Pseudo Random Numbers between -100000 and 100000
            float offsetY = prng.Next(-100000,100000) - settings.offset.y - sampleCenter.y;  //Subtracting to get right map movement when changin the y offset      
            octaveOffsets[i] = new Vector2(offsetX,offsetY);

            maxPossibleHeight += amplitude;
            amplitude *= settings.persistance;
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

               for(int i = 0; i < settings.octaves; i++) {
                    float sampleX = (x - halfWidth + octaveOffsets[i].x) / settings.scale * frequency ; //Zooming to center
                    float sampleY = (y - halfHeight + octaveOffsets[i].y) / settings.scale * frequency ;

                    float perlinValue= Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1; //Makes it possible for values to be between -1 and 1
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= settings.persistance;
                    frequency *= settings.lacunarity;
                    
                }
                if(noiseHeight > maxLocalNoiseHeight) {
                    maxLocalNoiseHeight = noiseHeight;
                } 
                if(noiseHeight < minLocalNoiseHeight) {
                    minLocalNoiseHeight = noiseHeight;
                }
                noiseMap[x,y] = noiseHeight;

                if(settings.normalizeMode == NormalizeMode.Global) {
                    float normalizedHeight = (noiseMap[x,y] + 1) / (2f * maxPossibleHeight / 2f);      
                    noiseMap[x,y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);         
                }
            }
        }
        if(settings.normalizeMode == NormalizeMode.Local) {
            for(int y = 0; y < mapHeight; y++) {
                for(int x = 0; x < mapWidth; x++) {                
                    noiseMap[x,y] = Mathf.InverseLerp(minLocalNoiseHeight,maxLocalNoiseHeight,noiseMap[x,y]); //Normalize Values to be between 0-1
                } 

            }
        }   
        return noiseMap;
    }
}

[System.Serializable]
public class NoiseSettings {
    public Noise.NormalizeMode normalizeMode;

    public float scale = 50;

    public int octaves = 6;
    [Range(0,1)]
    public float persistance = 0.6f;
    public float lacunarity = 2;

    public int seed;
    public Vector2 offset;

    public void ValidateValues() {
        scale = Mathf.Max(scale, 0.01f);
        octaves = Mathf.Max(octaves,1);
        lacunarity = Mathf.Max(lacunarity,1);
        persistance = Mathf.Clamp01(persistance);
    }
}
