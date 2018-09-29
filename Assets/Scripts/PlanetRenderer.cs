using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public static class PlanetRenderer
{
    public static Mesh createMesh(PlanetData planet)
    {
        int size = planet.triangles.Length;
        int[] triangles = new int[size * 3];
        Color32[] colors = new Color32[size * 3];
        Vector3[] points = new Vector3[size * 3];

        for(int i = 0; i < size; i++)
        {
            points[3 * i] = planet.points[planet.triangles[i].v1].point * (planet.points[planet.triangles[i].v1].height + 1) * planet.scale;
            points[3 * i + 1] = planet.points[planet.triangles[i].v2].point * (planet.points[planet.triangles[i].v2].height + 1) * planet.scale;
            points[3 * i + 2] = planet.points[planet.triangles[i].v3].point * (planet.points[planet.triangles[i].v3].height + 1) * planet.scale;

            var biomeID = planet.points[planet.triangles[i].v1].biomeID;
            Color biomeColor;
            if (biomeID < 0)
                biomeColor = new Color(255, 0, 255);
            else biomeColor = planet.biomes[biomeID].color;
            colors[3 * i] = biomeColor;
            colors[3 * i + 1] = biomeColor;
            colors[3 * i + 2] = biomeColor;
        }

        for (int i = 0; i < size * 3; i++)
            triangles[i] = i;

        Mesh m = new Mesh();
        m.vertices = points;
        m.colors32 = colors;
        m.triangles = triangles;
        m.RecalculateNormals();
        m.RecalculateTangents();
        m.RecalculateBounds();
        return m;
    }
}
