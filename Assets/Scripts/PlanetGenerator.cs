using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using NRand;
using System.Diagnostics;
using Sirenix.OdinInspector;

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

    public int riverNb = 5;
    public float riverMoistureMin = 0.5f;
    public float moisturePropagationResistance = 1.0f;
}

[Serializable]
public class Biome
{
    public string name;
    [HorizontalGroup, LabelWidth(100),TableColumnWidth(30)]
    public bool isOceanBiome = false;
    [HorizontalGroup, LabelWidth(100)]
    public bool isLakeBiome = false;
    [HorizontalGroup, LabelWidth(100)]
    public bool isRiverBiome = false;
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

public struct RiverPoint
{
    public RiverPoint(float _width, int _index)
    {
        width = _width;
        index = _index;
    }

    public float width;
    public int index;
}

public class PlanetData
{
    public float scale;
    public PlanetPoint[] points;
    public Triangle[] triangles;
    public List<RiverPoint[]> rivers = new List<RiverPoint[]>();
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
        makeRivers(planet, gen, data.riverNb);
        makeMoisture(planet, data.riverMoistureMin, data.moisturePropagationResistance);
        makeBiomes(planet, data.elevationData.maxHeight);


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
            planet.points[i].connectedPoints = new List<int>();
        }
        foreach(var t in planet.triangles)
        {
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
                    if (planet.points[p].biomeID == oceanBiomeIndex)
                    {
                        toAdd = true;
                        break;
                    }
                }
                if (toAdd)
                    nearPoints.Add(i);
            }
        }
        if (nearPoints.Count == 0)
            nearPoints.Add(0);
        UnityEngine.Debug.Log(nearPoints.Count);
        
        List<int> nextPoints = new List<int>();

        float[] newHeight = new float[planet.points.Length];
        bool[] setPoints = new bool[planet.points.Length];

        foreach(var p in nearPoints)
        {
            newHeight[p] = 0; 
            setPoints[p] = true;
            foreach (var nextP in planet.points[p].connectedPoints)
                if (!nearPoints.Contains(nextP) && !nextPoints.Contains(nextP))
                    nextPoints.Add(nextP);
        }

        sw.Stop(); UnityEngine.Debug.Log("Elapsed near points " + sw.Elapsed); sw.Start();

        while(nextPoints.Count > 0)
        {
            var current = nextPoints[0];
            nextPoints.RemoveAt(0);
            setPoints[current] = true;
            
            int lowestIndex = -1;
            foreach(var p in planet.points[current].connectedPoints)
            {
                if(setPoints[p])
                {
                    if (lowestIndex < 0 || Mathf.Abs(newHeight[p]) < Mathf.Abs(newHeight[lowestIndex]))
                        lowestIndex = p;
                }
                else if(!nextPoints.Contains(p))
                {
                    nextPoints.Add(p);
                }
            }

            float d = (planet.points[lowestIndex].point - planet.points[current].point).magnitude;
            float dHeight = Mathf.Abs(planet.points[lowestIndex].height - planet.points[current].height);
            float sign = planet.points[current].biomeID == oceanBiomeIndex ? -1 : 1;
            newHeight[current] = newHeight[lowestIndex] + (d * forcedOffsetElevation + dHeight) * sign;
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

    static void makeRivers(PlanetData planet, IRandomGenerator gen, int riverNb)
    {
        bool[] possiblePositions = new bool[planet.points.Length];
        for (int i = 0; i < planet.points.Length; i++)
        {
            if (planet.points[i].biomeID >= 0)
            {
                possiblePositions[i] = false;
                continue;
            }
            bool connectedNotOk = false;
            foreach (var p in planet.points[i].connectedPoints)
            {
                if (planet.points[p].biomeID >= 0)
                {
                    connectedNotOk = true;
                    possiblePositions[i] = false;
                    break;
                }
            }
            if (connectedNotOk)
                continue;
            possiblePositions[i] = true;
        }

        const int testCount = 20;
        var dStart = new UniformIntDistribution(0, planet.points.Length);

        for (int i = 0; i < riverNb; i++)
        {
            int startIndex = -1;
            for (int j = 0; j < testCount; j++)
            {
                int index = dStart.Next(gen);
                if (possiblePositions[j])
                {
                    startIndex = index;
                    break;
                }
            }
            if (startIndex < 0)
                continue;

            List<RiverPoint> points = new List<RiverPoint>();
            points.Add(new RiverPoint(1, startIndex));

            while (true)
            {
                int bestIndex = -1;
                int current = points[points.Count - 1].index;
                foreach (var p in planet.points[current].connectedPoints)
                {
                    if (planet.points[current].height > planet.points[p].height && (bestIndex == -1 || planet.points[p].height < planet.points[bestIndex].height))
                        bestIndex = p;
                }
                if (bestIndex < 0)
                    break;
                points.Add(new RiverPoint(points[points.Count - 1].width + 1, bestIndex));
                if (!possiblePositions[bestIndex])
                    break;
            }
            var river = new RiverPoint[points.Count];
            for (int j = 0; j < points.Count; j++)
            {
                river[j] = points[j];
                possiblePositions[points[j].index] = false;
            }
            var lastPoint = river[river.Length - 1];
            int currentRiverIndex = -1;
            bool found = false;
            for (int j = 0; j < planet.rivers.Count; j++)
            {
                if (j == currentRiverIndex)
                    continue;
                for (int k = 0; k < planet.rivers[j].Length; k++)
                {
                    if (found)
                        planet.rivers[j][k].width += lastPoint.width;
                    else
                    {
                        if (planet.rivers[j][k].index == lastPoint.index)
                            found = true;
                        planet.rivers[j][k].width += lastPoint.width;
                    }
                }
                if (found)
                {
                    found = false;
                    currentRiverIndex = j;
                    j = -1;
                }
            }
            planet.rivers.Add(river);
        }

        float maxRiverSize = 0;
        for (int j = 0; j < planet.rivers.Count; j++)
            for (int k = 0; k < planet.rivers[j].Length; k++)
                maxRiverSize = Mathf.Max(maxRiverSize, planet.rivers[j][k].width);
        for (int j = 0; j < planet.rivers.Count; j++)
            for (int k = 0; k < planet.rivers[j].Length; k++)
                planet.rivers[j][k].width /= maxRiverSize;
    }

    static void makeMoisture(PlanetData planet, float riverMoistureMin, float moisturePropagationResistance)
    {
        List<int> nextPoints = new List<int>();
        bool[] setPoints = new bool[planet.points.Length];

        foreach(var river in planet.rivers)
            for(int i = 0; i < river.Length; i++)
            {
                int index = river[i].index;
                float m = i / river.Length * (1 - riverMoistureMin) + riverMoistureMin;
                planet.points[index].moisture = Mathf.Max(planet.points[index].moisture, m);
                setPoints[index] = true;
                if (!nextPoints.Contains(index))
                    nextPoints.Add(index);
            }

        while(nextPoints.Count > 0)
        {
            int current = nextPoints[0];
            nextPoints.RemoveAt(0);
            setPoints[current] = true;

            int bestMoistureIndex = -1;
            foreach (var p in planet.points[current].connectedPoints)
            {
                if (setPoints[p])
                {
                    if (bestMoistureIndex < 0 || Mathf.Abs(planet.points[bestMoistureIndex].moisture) > Mathf.Abs(planet.points[p].moisture))
                        bestMoistureIndex = p;
                }
                else if (!nextPoints.Contains(p))
                {
                    nextPoints.Add(p);
                }
            }

            float d = (planet.points[bestMoistureIndex].point - planet.points[current].point).magnitude;
            planet.points[current].moisture = Mathf.Max(0, planet.points[bestMoistureIndex].moisture - d * moisturePropagationResistance);
        }
    }

    static void makeBiomes(PlanetData planet, float maxHeight)
    {
        for(int i = 0; i < planet.points.Length; i++)
        {
            if (planet.points[i].biomeID > 0)
                continue;

            int bestBiomeIndex = -1;
            var pos = new Vector2(planet.points[i].height / maxHeight, planet.points[i].moisture);
            for(int j = 0; j < planet.biomes.Length; j++)
            {
                if (planet.biomes[j].isLakeBiome || planet.biomes[j].isOceanBiome || planet.biomes[j].isRiverBiome)
                    continue;
                if (bestBiomeIndex < 0 || (new Vector2(planet.biomes[j].height, planet.biomes[j].moisture) - pos).sqrMagnitude < (new Vector2(planet.biomes[bestBiomeIndex].height, planet.biomes[bestBiomeIndex].moisture) - pos).sqrMagnitude)
                    bestBiomeIndex = j;
            }
            if (bestBiomeIndex < 0)
                continue;
            planet.points[i].biomeID = bestBiomeIndex;
        }
    }
}