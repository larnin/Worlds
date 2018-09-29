using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using NRand;
using System.Diagnostics;

public struct Pair<T, U>
{
    public Pair(T _first, U _second)
    {
        first = _first;
        second = _second;
    }

    public T first;
    public U second;

    public override bool Equals(object obj)
    {
        if ((obj == null) || !this.GetType().Equals(obj.GetType()))
        {
            return false;
        }
        else
        {
            Pair<T, U> p = (Pair<T, U>)obj; 
            return EqualityComparer<T>.Default.Equals(first, p.first)&& EqualityComparer<U>.Default.Equals(second, p.second);
        }
    }

    public override int GetHashCode()
    {
        int hash = 17;
        hash = hash * 23 + first.GetHashCode();
        hash = hash * 23 + second.GetHashCode();
        return hash;
    }
}

[Serializable]
public class PlanetGeneratorData
{
    [Serializable]
    public class ElevationData
    {
        [Serializable]
        public class PerlinFactors
        {
            public float amplitude;
            public float frequency;
        }

        public AnimationCurve elevationCurve;
        public PerlinFactors[] perlinFactors;
        public float maxHeight;
        public float minHeight;
        public float oceanLevel; // [-1;1] value the proportion of the perlin elevation
        public float lakeSize = 0.1f;
        public float forcedOffsetElevation = 0.1f;
    }

    public int seed;
    public int sphereDivisionLevel;
    public float planetScale;

    public ElevationData elevationData = new ElevationData();
    public Biome[] biomes;
}

[Serializable]
public class Biome
{
    public bool isOceanBiome = false;
    public bool isLakeBiome = false;
    public Color color;
    public float moisture;
    public float height;
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

public struct PlanetPoint
{
    public Vector3 point;
    public int biomeID;
    public float height;
    public float moisture;
    public List<int> connectedPoints;
}

public class PlanetData
{
    public float scale;
    public PlanetPoint[] points;
    public Triangle[] triangles;
    public List<int[]> rivers = new List<int[]>();
    public Biome[] biomes;
}

public static class PlanetGenerator
{
    public static PlanetData generate(PlanetGeneratorData data)
    {
        var planet = createSpherePlanet(data.sphereDivisionLevel);

        planet.scale = data.planetScale;
        planet.biomes = data.biomes;

        var gen = new DefaultRandomGenerator(data.seed);
        makePerlinElevation(planet, gen, data.elevationData.perlinFactors);
        makeFinalElevation(planet, data.elevationData.minHeight, data.elevationData.maxHeight, data.elevationData.forcedOffsetElevation
                         , data.elevationData.oceanLevel, data.elevationData.lakeSize, data.elevationData.elevationCurve);
        
        return planet;
    }

    public static PlanetData createSpherePlanet(int divisonLevel)
    {
        const float X = .525731112119133606f;
        const float Z = .850650808352039932f;

        Vector3[] points = new Vector3[]{ new Vector3(-X,0,Z), new Vector3(X,0,Z) , new Vector3(-X,0,-Z), new Vector3( X,0,-Z),
                                          new Vector3(0,Z,X) , new Vector3(0,Z,-X), new Vector3(0,-Z,X) , new Vector3(0,-Z,-X),
                                          new Vector3(Z,X,0) , new Vector3(-Z,X,0), new Vector3(Z,-X,0) , new Vector3(-Z,-X,0) };

        Triangle[] triangles = new Triangle[]{ new Triangle(0,1,4) , new Triangle(0,4,9) , new Triangle(9,4,5) , new Triangle(4,8,5) , new Triangle(4,1,8),
                                               new Triangle(8,1,10), new Triangle(8,10,3), new Triangle(5,8,3) , new Triangle(5,3,2) , new Triangle(2,3,7),
                                               new Triangle(7,3,10), new Triangle(7,10,6), new Triangle(7,6,11), new Triangle(11,6,0), new Triangle(0,6,1),
                                               new Triangle(6,10,1), new Triangle(9,11,0), new Triangle(9,2,11), new Triangle(9,5,2) , new Triangle(7,11,2) };

        for (int i = 0; i < divisonLevel; i++)
            subdivise(ref points, ref triangles);

        PlanetData planet = new PlanetData();
        planet.points = new PlanetPoint[points.Length];
        planet.triangles = triangles;
        for (int i = 0; i < points.Length; i++)
        {
            planet.points[i].point = points[i];
            planet.points[i].biomeID = -1;
        }
        foreach(var t in planet.triangles)
        {
            if (planet.points[t.v1].connectedPoints == null)
                planet.points[t.v1].connectedPoints = new List<int>();
            if (planet.points[t.v2].connectedPoints == null)
                planet.points[t.v2].connectedPoints = new List<int>();
            if (planet.points[t.v3].connectedPoints == null)
                planet.points[t.v3].connectedPoints = new List<int>();

            if (!planet.points[t.v1].connectedPoints.Contains(t.v2))
                planet.points[t.v1].connectedPoints.Add(t.v2);
            if (!planet.points[t.v1].connectedPoints.Contains(t.v3))
                planet.points[t.v1].connectedPoints.Add(t.v3);
            if (!planet.points[t.v2].connectedPoints.Contains(t.v1))
                planet.points[t.v2].connectedPoints.Add(t.v1);
            if (!planet.points[t.v2].connectedPoints.Contains(t.v3))
                planet.points[t.v2].connectedPoints.Add(t.v3);
            if (!planet.points[t.v3].connectedPoints.Contains(t.v1))
                planet.points[t.v3].connectedPoints.Add(t.v1);
            if (!planet.points[t.v3].connectedPoints.Contains(t.v2))
                planet.points[t.v3].connectedPoints.Add(t.v2);
        }

        return planet;
    }

    static void subdivise(ref Vector3[] points, ref Triangle[] triangles)
    {
        int pointsSize = points.Length;
        int trianglesSize = triangles.Length;

        Vector3[] newPoints = new Vector3[pointsSize * 4];
        Array.Copy(points, newPoints, pointsSize);
        Triangle[] newTriangles = new Triangle[trianglesSize * 4];

        int newPointIndex = pointsSize;
        int newTriangleIndex = 0;

        Dictionary<Pair<int, int>, int> indexLookup = new Dictionary<Pair<int, int>, int>();

        for(int i = 0; i < trianglesSize; i++)
        {
            Triangle t = triangles[i];

            Func<int, int, int> lookup = (int index1, int index2) =>
            {
                int min = Mathf.Min(index1, index2);
                int max = Mathf.Max(index1, index2);

                int v;
                if (!indexLookup.ContainsKey(new Pair<int, int>(min, max)))
                {
                    v = newPointIndex++;
                    newPoints[v] = ((newPoints[min] + newPoints[max]) / 2).normalized;
                    indexLookup.Add(new Pair<int, int>(min, max), v);
                }
                else
                    v = indexLookup[new Pair<int, int>(min, max)];
                return v;
            };
            int v1 = lookup(t.v1, t.v2);
            int v2 = lookup(t.v2, t.v3);
            int v3 = lookup(t.v3, t.v1);

            newTriangles[newTriangleIndex++] = new Triangle(t.v1, v1, v3);
            newTriangles[newTriangleIndex++] = new Triangle(t.v2, v2, v1);
            newTriangles[newTriangleIndex++] = new Triangle(t.v3, v3, v2);
            newTriangles[newTriangleIndex++] = new Triangle(v1, v2, v3);
        }
        triangles = newTriangles;

        points = new Vector3[newPointIndex];
        Array.Copy(newPoints, points, newPointIndex);
    }

    static void makePerlinElevation(PlanetData planet, IRandomGenerator gen, PlanetGeneratorData.ElevationData.PerlinFactors[] perlinFactors)
    {
        const float maxPerlinOffset = 10000;

        float minValue = float.MaxValue;
        float maxValue = float.MinValue;

        Vector3[] perlinOffset = new Vector3[perlinFactors.Length];
        for (int i = 0; i < perlinOffset.Length; i++)
            perlinOffset[i] = new UniformVector3BoxDistribution(-maxPerlinOffset, maxPerlinOffset, -maxPerlinOffset, maxPerlinOffset, -maxPerlinOffset, maxPerlinOffset).Next(gen);

        for(int i = 0; i < planet.points.Length; i++)
        {
            float scale = 0;
            for (int j = 0; j < perlinFactors.Length; j++)
                scale += Perlin.Noise(perlinOffset[j] + planet.points[i].point * perlinFactors[j].frequency) * perlinFactors[j].amplitude;

            planet.points[i].height = scale;
            minValue = Mathf.Min(minValue, scale);
            maxValue = Mathf.Max(maxValue, scale);
        }
        for (int i = 0; i < planet.points.Length; i++)
            planet.points[i].height = (planet.points[i].height - minValue) / (maxValue - minValue) * 2 - 1; // range [-1,1]
    }

    static void makeFinalElevation(PlanetData planet, float minHeight, float maxHeight, float forcedOffsetElevation, float oceanLevel, float lakeSize, AnimationCurve elevationCurve)
    {
        int oceanBiomeIndex = -1;
        int lakeBiomeIndex = -1;

        Stopwatch sw = new Stopwatch();
        sw.Start();
        
        for (int i = 0; i < planet.biomes.Length; i++)
        {
            if (planet.biomes[i].isLakeBiome)
                lakeBiomeIndex = i;
            if (planet.biomes[i].isOceanBiome)
                oceanBiomeIndex = i;
        }

        setWaterBiomes(planet, oceanLevel, oceanBiomeIndex, lakeBiomeIndex, lakeSize);

        sw.Stop(); UnityEngine.Debug.Log("Elapsed water biomes " + sw.Elapsed); sw.Start();

        List<int> nearPoints = new List<int>();
        if (oceanBiomeIndex >= 0)
        {
            for (int i = 0; i < planet.points.Length; i++)
            {
                if (planet.points[i].biomeID >= 0)
                    continue;
                bool toAdd = false;
                foreach (var p in planet.points[i].connectedPoints)
                {
                    if (planet.points[i].biomeID == oceanBiomeIndex)
                        toAdd = true;
                }
                if (toAdd)
                    nearPoints.Add(i);
            }
        }
        if (nearPoints.Count == 0)
            nearPoints.Add(0);

        List<int> setPoints = new List<int>();
        List<int> nextPoints = new List<int>();

        float[] newHeight = new float[planet.points.Length];

        foreach(var p in nearPoints)
        {
            newHeight[p] = 0;
            setPoints.Add(p);
            foreach (var nextP in planet.points[p].connectedPoints)
                if (!nearPoints.Contains(nextP) && !nextPoints.Contains(nextP))
                    nextPoints.Add(nextP);
        }

        sw.Stop(); UnityEngine.Debug.Log("Elapsed near points " + sw.Elapsed); sw.Start();
        
        while(nextPoints.Count > 0)
        {
            var current = nextPoints[0];
            nextPoints.RemoveAt(0);
            if(!setPoints.Contains(current))
                setPoints.Add(current);
            int lowestIndex = 0;
            foreach(var p in planet.points[current].connectedPoints)
            {
                if (setPoints.Contains(p))
                {
                    if (Mathf.Abs(newHeight[p]) < Mathf.Abs(newHeight[lowestIndex]))
                        lowestIndex = p;
                }
                else if (!nextPoints.Contains(p))
                    nextPoints.Add(p);
            }

            float delta = Mathf.Abs(planet.points[lowestIndex].height - planet.points[current].height) + forcedOffsetElevation;
            newHeight[current] = newHeight[lowestIndex] + delta * Mathf.Sign(newHeight[lowestIndex]);
        }

        float min = Mathf.Min(newHeight);
        float max = Mathf.Max(newHeight);

        for(int i = 0; i < newHeight.Length; i++)
        {
            float h = newHeight[i];
            if(h < 0)
            {
                h /= -min;
                h = elevationCurve.Evaluate(h);
                h *= -minHeight;
            }
            else
            {
                h /= max;
                h = elevationCurve.Evaluate(h);
                h *= maxHeight;
            }
            planet.points[i].height = h;
        }

        sw.Stop(); UnityEngine.Debug.Log("Elapsed heights " + sw.Elapsed); sw.Start();
    }

    static void setWaterBiomes(PlanetData planet, float oceanLevel, int oceanBiomeIndex, int lakeBiomeIndex, float lakeSize)
    {
        if (oceanBiomeIndex < 0)
            return;

        List<int> visitedPoints = new List<int>();

        for(int i = 0; i < planet.points.Length; i++)
        {
            if(planet.points[i].height >= oceanLevel)
            {
                visitedPoints.Add(i);
                continue;
            }

            if (visitedPoints.Contains(i))
                continue;

            List<int> fill = new List<int>();
            List<int> next = new List<int>();
            next.Add(i);
            while(next.Count > 0)
            {
                int current = next[0];
                next.RemoveAt(0);
                fill.Add(current);
                
                foreach (var p in planet.points[current].connectedPoints)
                    if (planet.points[p].height <= oceanLevel && !next.Contains(p) && !fill.Contains(p))
                        next.Add(p);
            }
            int biomeIndex = oceanBiomeIndex;
            if (lakeBiomeIndex >= 0 && fill.Count <= lakeSize * planet.points.Length)
                biomeIndex = lakeBiomeIndex;
            foreach (var p in fill)
            {
                visitedPoints.Add(p);
                planet.points[p].biomeID = biomeIndex;
            }
        }
    }
}