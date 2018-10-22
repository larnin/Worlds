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

            var biomeID = PlanetEx.biomeIndexOfTriangle(planet, i);
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

            var biomeID = PlanetEx.biomeIndexOfTriangle(planet, i);
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

    public static Mesh CreateRiversMesh(PlanetData planet)
    {
        Stopwatch sw = new Stopwatch();
        sw.Start();

        int biomeIndex = -1;
        for(int i = 0; i < planet.biomes.Length; i++)
            if(planet.biomes[i].isRiverBiome)
            {
                biomeIndex = i;
                break;
            }
        if (biomeIndex < 0)
            return null;

        List<Vector3> points = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Color32> colors = new List<Color32>();

        int index = 0;
        


        for(int i = 0; i < planet.points.Length; i++)
        {
            if (!planet.points[i].riverInfo.isRiver() || planet.points[i].riverInfo.nextIndex < 0)
                continue;

            int nextIndex = planet.points[i].riverInfo.nextIndex;

            Vector3 currentLeft, currentRight, nextLeft, nextRight;

            switch(planet.points[i].riverInfo.previousIndexs.Count)
            {
                case 0:
                    {
                        var h = Vector3.Cross(planet.points[nextIndex].point - planet.points[i].point, planet.points[i].point).normalized;
                        currentLeft = planet.points[i].point + h * planet.points[i].riverInfo.width * planet.riverWidth;
                        currentRight = planet.points[i].point - h * planet.points[i].riverInfo.width * planet.riverWidth;
                    }
                    break;
                case 1:
                    {
                        var h = Vector3.Cross(planet.points[nextIndex].point - planet.points[planet.points[i].riverInfo.previousIndexs[0]].point, planet.points[i].point).normalized;
                        currentLeft = planet.points[i].point + h * planet.points[i].riverInfo.width * planet.riverWidth;
                        currentRight = planet.points[i].point - h * planet.points[i].riverInfo.width * planet.riverWidth;
                    }
                    break;
                default:
                    {
                        List<Vector3> previous = new List<Vector3>();
                        foreach (var pt in planet.points[i].riverInfo.previousIndexs)
                            previous.Add(planet.points[pt].point);
                        var left = MathEx.getLeft(previous, planet.points[i].point, planet.points[nextIndex].point, planet.points[i].point.normalized);
                        var right = MathEx.getRight(previous, planet.points[i].point, planet.points[nextIndex].point, planet.points[i].point.normalized);
                        var hLeft = Vector3.Cross(planet.points[nextIndex].point - left, planet.points[i].point).normalized;
                        var hRight = Vector3.Cross(planet.points[nextIndex].point - right, planet.points[i].point).normalized;

                        currentLeft = planet.points[i].point + hLeft * planet.points[i].riverInfo.width * planet.riverWidth;
                        currentRight = planet.points[i].point - hRight * planet.points[i].riverInfo.width * planet.riverWidth;
                    }
                    break;
            }

            List<Vector3> nextPoints = new List<Vector3>();
            foreach(var pt in planet.points[nextIndex].riverInfo.previousIndexs)
            {
                if (pt == i)
                    continue;
                nextPoints.Add(planet.points[pt].point);
            }
            if (planet.points[nextIndex].riverInfo.nextIndex >= 0)
                nextPoints.Add(planet.points[planet.points[nextIndex].riverInfo.nextIndex].point);

            switch (nextPoints.Count)
            {
                case 0:
                    {
                        var h = Vector3.Cross(planet.points[i].point - planet.points[nextIndex].point, planet.points[nextIndex].point).normalized;
                        nextLeft = planet.points[nextIndex].point - h * planet.points[nextIndex].riverInfo.width * planet.riverWidth;
                        nextRight = planet.points[nextIndex].point + h * planet.points[nextIndex].riverInfo.width * planet.riverWidth;
                    }
                    break;
                case 1:
                    {
                        var h = Vector3.Cross(nextPoints[0] - planet.points[i].point, planet.points[nextIndex].point).normalized;
                        nextLeft = planet.points[nextIndex].point + h * planet.points[nextIndex].riverInfo.width * planet.riverWidth;
                        nextRight = planet.points[nextIndex].point - h * planet.points[nextIndex].riverInfo.width * planet.riverWidth;
                    }
                    break;
                default:
                    {
                        var left = MathEx.getLeft(nextPoints, planet.points[nextIndex].point, planet.points[i].point, planet.points[nextIndex].point.normalized);
                        var right = MathEx.getRight(nextPoints, planet.points[nextIndex].point, planet.points[i].point, planet.points[nextIndex].point.normalized);
                        var hLeft = Vector3.Cross(planet.points[i].point - left, planet.points[nextIndex].point).normalized;
                        var hRight = Vector3.Cross(planet.points[i].point - right, planet.points[nextIndex].point).normalized;

                        nextLeft = planet.points[nextIndex].point - hLeft * planet.points[nextIndex].riverInfo.width * planet.riverWidth;
                        nextRight = planet.points[nextIndex].point + hRight * planet.points[nextIndex].riverInfo.width * planet.riverWidth;
                    }
                    break;
            }

            var current = planet.points[i].point * (planet.points[i].height + 1 + planet.riverMoreHeight) * planet.scale;
            var next = planet.points[nextIndex].point * (planet.points[nextIndex].height + 1 + planet.riverMoreHeight) * planet.scale;

            currentLeft *= (planet.points[i].height + 1 + planet.riverMoreHeight) * planet.scale;
            currentRight *= (planet.points[i].height + 1 + planet.riverMoreHeight) * planet.scale;
            nextLeft *= (planet.points[nextIndex].height + 1 + planet.riverMoreHeight) * planet.scale;
            nextRight *= (planet.points[nextIndex].height + 1 + planet.riverMoreHeight) * planet.scale;

            points.Add(currentLeft);
            points.Add(nextLeft);
            points.Add(next);
            points.Add(current);
            points.Add(currentLeft);
            points.Add(next);
            points.Add(currentRight);
            points.Add(current);
            points.Add(next);
            points.Add(currentRight);
            points.Add(next);
            points.Add(nextRight);

            for(int j = index; j < index + 12; j++)
            {
                triangles.Add(j);
                colors.Add(planet.biomes[biomeIndex].color);
            }
            index += 12;
        }

        Mesh m = new Mesh();
        m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        m.vertices = points.ToArray();
        m.colors32 = colors.ToArray();
        m.triangles = triangles.ToArray();
        m.RecalculateNormals();
        m.RecalculateTangents();
        m.RecalculateBounds();

        sw.Stop();
        UnityEngine.Debug.Log("Elapsed rivers renderer " + sw.Elapsed);

        return m;
    }

    public static void CreateStructures(PlanetData planet, Transform structuresParent)
    {
        Stopwatch sw = new Stopwatch();
        sw.Start();

        for (int i = 0; i < planet.structures.Count; i++)
        {
            var obj = GameObject.Instantiate(planet.structuresPrefabs[planet.structures[i].structureIndex], structuresParent);
            obj.transform.localPosition = planet.structures[i].position;
            obj.transform.up = planet.structures[i].position.normalized;
            obj.transform.rotation = obj.transform.rotation * Quaternion.Euler(0, planet.structures[i].rotation, 0);
        }

        UnityEngine.Debug.Log("Elapsed structures renderer " + sw.Elapsed);
    }
}
