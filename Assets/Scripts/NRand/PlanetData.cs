using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

[Serializable]
public class Biome
{
    [Serializable]
    public class StructureData
    {
        public GameObject prefab;
        public float weight;
    }

    public string name;
    [HorizontalGroup, LabelWidth(100), TableColumnWidth(30)]
    public bool isOceanBiome = false;
    [HorizontalGroup, LabelWidth(100)]
    public bool isLakeBiome = false;
    [HorizontalGroup, LabelWidth(100)]
    public bool isRiverBiome = false;
    public Color color;
    public float moisture;
    public float height;

    public float structureDensity;
    public List<StructureData> structures;
}

public struct Triangle
{
    public int v1;
    public int v2;
    public int v3;

    public Triangle(int _1, int _2, int _3)
    {
        v1 = _1; v2 = _2; v3 = _3;
    }
}

public struct RiverPoint
{
    public float width;
    public int nextIndex;
    public List<int> previousIndexs;

    public bool isRiver()
    {
        return width > 0;
    }
}

public struct PlanetPoint
{
    public Vector3 point;
    public int biomeID;
    public float height;
    public float moisture;
    public List<int> connectedPoints;
    public RiverPoint riverInfo;
}

public struct StructureInfo
{
    public StructureInfo(int _structureIndex, int _triangleIndex, Vector3 _position, float _rotation)
    {
        structureIndex = _structureIndex;
        triangleIndex = _triangleIndex;
        position = _position;
        rotation = _rotation;
    }

    public int structureIndex;
    public int triangleIndex;
    public Vector3 position;
    public float rotation;
}

public class PlanetData
{
    public float scale;
    public float riverWidth;
    public float riverMoreHeight;
    public PlanetPoint[] points;
    public Triangle[] triangles;
    public Biome[] biomes;

    public List<GameObject> structuresPrefabs;
    public List<StructureInfo> structures;
}

public static class PlanetEx
{
    public static int biomeIndexOfTriangle(PlanetData planet, int triangleIndex)
    {
        var t = planet.triangles[triangleIndex];

        int b1 = planet.points[t.v1].biomeID;
        int b2 = planet.points[t.v2].biomeID;
        int b3 = planet.points[t.v3].biomeID;

        if (b1 < 0 || b2 < 0 || b3 < 0)
            return -1;

        if (planet.biomes[b1].isOceanBiome || planet.biomes[b2].isOceanBiome || planet.biomes[b3].isOceanBiome)
        {
            return planet.biomes[b1].isOceanBiome ? b1 : planet.biomes[b2].isOceanBiome ? b2 : b3;
        }
        if (planet.biomes[b1].isLakeBiome && planet.biomes[b2].isLakeBiome && planet.biomes[b3].isLakeBiome)
            return b1;
        else if (planet.biomes[b1].isLakeBiome || planet.biomes[b2].isLakeBiome || planet.biomes[b3].isLakeBiome)
        {
            return !planet.biomes[b1].isLakeBiome ? b1 : !planet.biomes[b2].isLakeBiome ? b2 : b3;
        }

        if (b1 == b2)
            return b1;
        if (b2 == b3)
            return b2;
        if (b3 == b1)
            return b3;

        int t3 = triangleIndex % 3;
        if (t3 == 0)
            return b1;
        if (t3 == 1)
            return b2;
        return b3;
    }
}