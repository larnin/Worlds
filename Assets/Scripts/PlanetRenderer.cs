using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;

public static class PlanetRenderer
{
    public static Mesh createSurfaceMesh(PlanetData planet)
    {
        Stopwatch sw = new Stopwatch();
        sw.Start();

        int size = planet.triangles.Length;
        int[] triangles = new int[size * 3];
        Color32[] colors = new Color32[size * 3];
        Vector3[] points = new Vector3[size * 3];

        for(int i = 0; i < size; i++)
        {
            points[3 * i] = planet.points[planet.triangles[i].v1].point * (planet.points[planet.triangles[i].v1].height + 1) * planet.scale;
            points[3 * i + 1] = planet.points[planet.triangles[i].v2].point * (planet.points[planet.triangles[i].v2].height + 1) * planet.scale;
            points[3 * i + 2] = planet.points[planet.triangles[i].v3].point * (planet.points[planet.triangles[i].v3].height + 1) * planet.scale;

            var biomeID = biomeIndexOfTriangle(planet, i);
            Color biomeColor;
            if (biomeID < 0)
                biomeColor = new Color(255, 255, 255);
            else biomeColor = planet.biomes[biomeID].color;
            colors[3 * i] = biomeColor;
            colors[3 * i + 1] = biomeColor;
            colors[3 * i + 2] = biomeColor;
        }

        for (int i = 0; i < size * 3; i++)
            triangles[i] = i;

        Mesh m = new Mesh();
        m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        m.vertices = points;
        m.colors32 = colors;
        m.triangles = triangles;
        m.RecalculateNormals();
        m.RecalculateTangents();
        m.RecalculateBounds();

        sw.Stop();
        UnityEngine.Debug.Log("Elapsed renderer " + sw.Elapsed);

        return m;
    }

    public static Mesh CreateWaterMesh(PlanetData planet)
    {
        Stopwatch sw = new Stopwatch();
        sw.Start();

        int oceanIndex = -1;
        for(int i = 0; i < planet.biomes.Length; i++)
            if(planet.biomes[i].isOceanBiome)
            {
                oceanIndex = i;
                break;
            }
        if (oceanIndex < 0)
            return null;

        int size = 0;
        for(int i = 0; i < planet.triangles.Length; i++)
        {
            int b1 = planet.points[planet.triangles[i].v1].biomeID;
            int b2 = planet.points[planet.triangles[i].v2].biomeID;
            int b3 = planet.points[planet.triangles[i].v3].biomeID;
            if (b1 == oceanIndex || b2 == oceanIndex || b3 == oceanIndex)
                size++;
        }

        int[] triangles = new int[size * 3];
        Color32[] colors = new Color32[size * 3];
        Vector3[] points = new Vector3[size * 3];

        int index = 0;
        for (int i = 0; i < planet.triangles.Length; i++)
        {
            int b1 = planet.points[planet.triangles[i].v1].biomeID;
            int b2 = planet.points[planet.triangles[i].v2].biomeID;
            int b3 = planet.points[planet.triangles[i].v3].biomeID;
            if (b1 != oceanIndex && b2 != oceanIndex && b3 != oceanIndex)
                continue;

            points[3 * index] = planet.points[planet.triangles[i].v1].point * planet.scale;
            points[3 * index + 1] = planet.points[planet.triangles[i].v2].point * planet.scale;
            points[3 * index + 2] = planet.points[planet.triangles[i].v3].point * planet.scale;

            var biomeID = biomeIndexOfTriangle(planet, i);
            Color biomeColor = planet.biomes[oceanIndex].color;
            colors[3 * index] = biomeColor;
            colors[3 * index + 1] = biomeColor;
            colors[3 * index + 2] = biomeColor;

            index++;
        }

        for (int i = 0; i < size * 3; i++)
            triangles[i] = i;

        Mesh m = new Mesh();
        m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        m.vertices = points;
        m.colors32 = colors;
        m.triangles = triangles;
        m.RecalculateNormals();
        m.RecalculateTangents();
        m.RecalculateBounds();

        sw.Stop();
        UnityEngine.Debug.Log("Elapsed water renderer " + sw.Elapsed);

        return m;
    }

    static int biomeIndexOfTriangle(PlanetData planet, int triangleIndex)
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
        else if(planet.biomes[b1].isLakeBiome || planet.biomes[b2].isLakeBiome || planet.biomes[b3].isLakeBiome)
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
