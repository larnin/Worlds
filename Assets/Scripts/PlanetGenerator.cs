using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using NRand;
using System.Diagnostics;
using Sirenix.OdinInspector;
using Priority_Queue;

public class Pair<T, U>
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
        int hash = 5;
        hash = hash * 11 + first.GetHashCode();
        hash = hash * 7 + second.GetHashCode();
        return hash;
    }
}

class NextQueueNode : FastPriorityQueueNode
{
    public NextQueueNode(int _index)
    {
        index = _index;
    }
    public int index;
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
        public float randomizeElevation = 0.1f;
    }

    public int seed;
    public int sphereDivisionLevel;
    public float planetScale;

    public ElevationData elevationData = new ElevationData();
    public Biome[] biomes;

    public int riverNb = 5;
    public int riverTestCount = 20;
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
        Stopwatch sw = new Stopwatch();
        sw.Start();
        var planet = createSpherePlanet(data.sphereDivisionLevel);
        UnityEngine.Debug.Log("Elapsed sphere " + sw.Elapsed); sw.Reset(); sw.Start();

        planet.scale = data.planetScale;
        planet.biomes = data.biomes;

        var gen = new DefaultRandomGenerator(data.seed);
        makePerlinElevation(planet, gen, data.elevationData.perlinFactors);
        UnityEngine.Debug.Log("Elapsed perlin " + sw.Elapsed); sw.Reset(); sw.Start();
        makeFinalElevation(planet, data.elevationData.minHeight, data.elevationData.maxHeight, data.elevationData.forcedOffsetElevation
                         , data.elevationData.oceanLevel, data.elevationData.lakeSize, data.elevationData.elevationCurve, gen, data.elevationData.randomizeElevation);
        UnityEngine.Debug.Log("Elapsed elevation " + sw.Elapsed); sw.Reset(); sw.Start();
        makeRivers(planet, gen, data.riverNb, data.riverTestCount);
        UnityEngine.Debug.Log("Elapsed rivers " + sw.Elapsed); sw.Reset(); sw.Start();
        makeMoisture(planet, data.riverMoistureMin, data.moisturePropagationResistance);
        UnityEngine.Debug.Log("Elapsed moisture " + sw.Elapsed); sw.Reset(); sw.Start();
        makeBiomes(planet, data.elevationData.maxHeight);
        UnityEngine.Debug.Log("Elapsed biomes " + sw.Elapsed); sw.Reset(); sw.Start();

        return planet;
    }

    public static PlanetData createSpherePlanet(int divisonLevel)
    {
        const float X = .525731112119133606f;
        const float Z = .850650808352039932f;

        Vector3[] points = new Vector3[]{ new Vector3(-X,0,Z), new Vector3(X,0,Z) , new Vector3(-X,0,-Z), new Vector3( X,0,-Z),
                                          new Vector3(0,Z,X) , new Vector3(0,Z,-X), new Vector3(0,-Z,X) , new Vector3(0,-Z,-X),
                                          new Vector3(Z,X,0) , new Vector3(-Z,X,0), new Vector3(Z,-X,0) , new Vector3(-Z,-X,0) };

        Triangle[] triangles = new Triangle[]{ new Triangle(0,1,4) , new Triangle(0,4,9), new Triangle(9,4,5) , new Triangle(4,8,5) , new Triangle(4,1,8),
                                               new Triangle(8,1,10), new Triangle(8,10,3), new Triangle(5,8,3) , new Triangle(5,3,2) , new Triangle(2,3,7),
                                               new Triangle(7,3,10), new Triangle(7,10,6), new Triangle(7,6,11), new Triangle(11,6,0), new Triangle(0,6,1),
                                               new Triangle(6,10,1), new Triangle(9,11,0), new Triangle(9,2,11), new Triangle(9,5,2) , new Triangle(7,11,2) };

        var sub = subdivise(points, triangles, divisonLevel);

        PlanetData planet = new PlanetData();
        planet.points = new PlanetPoint[sub.pointsCount];
        planet.triangles = new Triangle[sub.trianglesCount];
        for(int i = 0; i < planet.points.Length; i++)
        {
            planet.points[i].point = sub.points[i];
            planet.points[i].biomeID = -1;
            planet.points[i].connectedPoints = new List<int>();
        }
        for (int i = 0; i < planet.triangles.Length; i++)
            planet.triangles[i] = sub.triangles[i];

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

        UnityEngine.Debug.Log("Points count : " + planet.points.Length);
        UnityEngine.Debug.Log("Triangle count : " + planet.triangles.Length);

        return planet;
    }

    class SubdiviseReturn
    {
        public Vector3[] points;
        public Triangle[] triangles;
        public int pointsCount;
        public int trianglesCount;
    }

    static SubdiviseReturn subdivise(Vector3[] points, Triangle[] triangles, int level)
    {
        SubdiviseReturn sub = new SubdiviseReturn();
        sub.triangles = new Triangle[level * level * triangles.Length];
        sub.points = new Vector3[sub.triangles.Length /2 + 2 ];

        for (int i = 0; i < points.Length; i++)
            sub.points[i] = points[i];

        int triangleIndex = 0;
        int pointIndex = points.Length;

        Dictionary<Pair<int, int>, int[]> lookMap = new Dictionary<Pair<int, int>, int[]>();

        for(int i = 0; i < triangles.Length; i++)
        {
            int[,] p = new int[level + 1,level + 1];

            for (int x = 0; x <= level; x++)
            {
                for (int y = 0; y <= level - x; y++)
                {
                    if(x == 0 && y == 0)
                    {
                        p[x, y] = triangles[i].v2;
                        continue;
                    }
                    if(x == 0 && y == level)
                    {
                        p[x, y] = triangles[i].v3;
                        continue;
                    }
                    if(x == level && y == 0)
                    {
                        p[x, y] = triangles[i].v1;
                        continue;
                    }

                    bool isOn = false;

                    if (x == 0 || y == 0 || x + y == level)
                    {
                        int p1 = 0, p2 = 0;
                        if (x == 0)
                        {
                            p1 = Mathf.Min(triangles[i].v2, triangles[i].v3);
                            p2 = Mathf.Max(triangles[i].v2, triangles[i].v3);
                        }
                        else if (y == 0)
                        {
                            p1 = Mathf.Min(triangles[i].v1, triangles[i].v2);
                            p2 = Mathf.Max(triangles[i].v1, triangles[i].v2);
                        }
                        else
                        {
                            p1 = Mathf.Min(triangles[i].v1, triangles[i].v3);
                            p2 = Mathf.Max(triangles[i].v1, triangles[i].v3);
                        }

                        int lookIndex = 0;
                        if (y == 0)
                            lookIndex = level - x;
                        else if (x == 0)
                            lookIndex = y;
                        else lookIndex = x;

                        var pos = new Pair<int, int>(p1, p2);
                        bool contains = lookMap.ContainsKey(pos);
                        if (!contains)
                        {
                            int[] pp = new int[level + 1];
                            for (int a = 0; a <= level; a++)
                                pp[a] = -1;
                            lookMap.Add(pos, pp);
                        }

                        int id = lookMap[pos][lookIndex];
                        int id2 = lookMap[pos][level - lookIndex];
                        if (id >= 0 && id2 > 0)
                        {
                            isOn = true;
                            p[x, y] = id2;
                        }
                        else lookMap[pos][lookIndex] = pointIndex;
                    }

                    if (!isOn)
                    {
                        var p1 = points[triangles[i].v1];
                        var p2 = points[triangles[i].v2];
                        var p3 = points[triangles[i].v3];
                        p[x, y] = pointIndex;
                        int xMax = Mathf.Max(level - y, 1);
                        sub.points[pointIndex] = ((p1 * x + p2 * (xMax - x)) / xMax * (level - y) + p3 * y).normalized;
                        pointIndex++;
                    }
                }
            }

            for (int x = 0; x < level; x++)
            {
                for (int y = 0; y < level - x; y++)
                {
                    sub.triangles[triangleIndex].v1 = p[x, y];
                    sub.triangles[triangleIndex].v2 = p[x, y + 1];
                    sub.triangles[triangleIndex].v3 = p[x + 1, y];
                    triangleIndex++;

                    if (y != 0)
                    {
                        sub.triangles[triangleIndex].v1 = p[x, y];
                        sub.triangles[triangleIndex].v2 = p[x + 1, y];
                        sub.triangles[triangleIndex].v3 = p[x + 1, y - 1];
                        triangleIndex++;
                    }
                }
            }
        }

        sub.pointsCount = pointIndex;
        sub.trianglesCount = triangleIndex;

        return sub;
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

    struct NextValue
    {
        public NextValue(int _index, float _height)
        {
            index = _index;
            height = _height;
        }
        public int index;
        public float height;
    }

    static void makeFinalElevation(PlanetData planet, float minHeight, float maxHeight, float forcedOffsetElevation, float oceanLevel, float lakeSize, AnimationCurve elevationCurve, IRandomGenerator gen, float randomizeElevation)
    {
        var distrib = new UniformFloatDistribution(0, randomizeElevation);

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

        UnityEngine.Debug.Log("\tElapsed water biomes " + sw.Elapsed); sw.Reset(); sw.Start();

        bool[] setPoints = new bool[planet.points.Length];
        bool[] nextPoints = new bool[planet.points.Length];
        FastPriorityQueue<NextQueueNode> next = new FastPriorityQueue<NextQueueNode>(planet.points.Length);
        float[] newHeight = new float[planet.points.Length];
        
        for (int i = 0; i < planet.points.Length; i++)
        {
            if (planet.points[i].biomeID >= 0 || planet.points[i].height < oceanLevel)
                continue;

            bool isBorder = false;
            foreach(var p in planet.points[i].connectedPoints)
            {
                if (planet.points[p].height >= oceanLevel || (planet.points[p].biomeID == lakeBiomeIndex && lakeBiomeIndex >= 0))
                    continue;
                isBorder = true;
                break;
            }
            if (!isBorder)
                continue;

            setPoints[i] = true;
        }

        if (next.Count == 0)
            setPoints[0] = true;

        for(int i  = 0; i < planet.points.Length; i++)
        {
            if(setPoints[i])
            {
                foreach(var p in planet.points[i].connectedPoints)
                    if(!setPoints[p] && !nextPoints[p])
                    {
                        next.Enqueue(new NextQueueNode(p), 0);
                        nextPoints[p] = true;
                    }
            }
        }
        
        while(next.Count > 0)
        {
            var current = next.Dequeue().index;
            nextPoints[current] = false;
            setPoints[current] = true;

            int bestIndex = -1;
            bool haveCheckedNotsetPoint = false;
            foreach(var p in planet.points[current].connectedPoints)
            {
                if (setPoints[p])
                {
                    if (bestIndex < 0 || newHeight[p] < newHeight[bestIndex])
                        bestIndex = p;
                }
                else if(!nextPoints[p]) haveCheckedNotsetPoint = true;
            }

            float d = (planet.points[bestIndex].point - planet.points[current].point).magnitude;
            float dHeight = Mathf.Abs(planet.points[bestIndex].height - planet.points[current].height);
            float sign = planet.points[current].biomeID == oceanBiomeIndex ? -1 : 1;
            if (planet.points[current].biomeID == lakeBiomeIndex && lakeBiomeIndex >= 0)
                newHeight[current] = newHeight[bestIndex];
            else newHeight[current] = newHeight[bestIndex] + (d * (forcedOffsetElevation + distrib.Next(gen)) + dHeight) * sign;

            if (!haveCheckedNotsetPoint)
                continue;

            foreach (var p in planet.points[current].connectedPoints)
            {
                if (!setPoints[p] && !nextPoints[p])
                {
                    next.Enqueue(new NextQueueNode(p), newHeight[current]);
                    nextPoints[p] = true;
                }
            }

            //if (next.First == null || newHeight[current] <= next.First.Value.height)
            //{
            //    foreach (var p in planet.points[current].connectedPoints)
            //    {
            //        if (!setPoints[p] && !nextPoints[p])
            //        {
            //            next.Enqueue(new NextQueueNode(p), newHeight[current]);
            //            next.AddFirst(new NextValue(p, newHeight[current]));
            //            nextPoints[p] = true;
            //        }
            //    }
            //}
            //else
            //{
            //    var item = next.Last;
            //    while (item.Previous != null)
            //    {
            //        if (item.Value.height <= newHeight[current])
            //            break;
            //        item = item.Previous;
            //    }

            //    foreach (var p in planet.points[current].connectedPoints)
            //    {
            //        if (!setPoints[p] && !nextPoints[p])
            //        {
            //            next.AddAfter(item, new NextValue(p, newHeight[current]));
            //            nextPoints[p] = true;
            //        }
            //    }
            //}
        }

        UnityEngine.Debug.Log("\tElapsed heights " + sw.Elapsed); sw.Reset(); sw.Start();

        for (int i = 0; i < planet.points.Length; i++)
        {
            if (newHeight[i] == 0)
                continue;

            float sum = 0;

            foreach (var p in planet.points[i].connectedPoints)
                sum += newHeight[p];
            planet.points[i].height = sum / planet.points[i].connectedPoints.Count;
        }

        UnityEngine.Debug.Log("\tElapsed smooth " + sw.Elapsed); sw.Reset(); sw.Start();

        float min = Mathf.Min(newHeight);
        float max = Mathf.Max(newHeight);

        for (int i = 0; i < newHeight.Length; i++)
        {
            float h = planet.points[i].height;
            if (h < 0)
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

        UnityEngine.Debug.Log("\tElapsed height curve " + sw.Elapsed); sw.Reset(); sw.Start();
    }

    static void setWaterBiomes(PlanetData planet, float oceanLevel, int oceanBiomeIndex, int lakeBiomeIndex, float lakeSize)
    {
        if (oceanBiomeIndex < 0)
            return;

        bool[] visitedPoints = new bool[planet.points.Length];
        bool[] nextPoints = new bool[planet.points.Length];
        bool[] fillPoints = new bool[planet.points.Length];
        List<int> fill = new List<int>();
        List<int> next = new List<int>();

        for (int i = 0; i < planet.points.Length; i++)
        {
            if (visitedPoints[i])
                continue;

            if (planet.points[i].height >= oceanLevel)
            {
                visitedPoints[i] = true;
                continue;
            }

            next.Add(i);
            nextPoints[i] = true;
            while(next.Count > 0)
            {
                int current = next[0];
                next.RemoveAt(0);
                nextPoints[current] = false;
                fill.Add(current);
                fillPoints[current] = true;

                foreach(var p in planet.points[current].connectedPoints)
                {
                    if (planet.points[p].height >= oceanLevel || fillPoints[p] || nextPoints[p])
                        continue;
                    nextPoints[p] = true;
                    next.Add(p);
                }
            }

            int biomeIndex = oceanBiomeIndex;
            if (lakeBiomeIndex >= 0 && fill.Count <= lakeSize * planet.points.Length)
                biomeIndex = lakeBiomeIndex;
            foreach (var p in fill)
            {
                visitedPoints[p] = true;
                fillPoints[p] = false;
                planet.points[p].biomeID = biomeIndex;
            }
            fill.Clear();
        }
    }

    static void makeRivers(PlanetData planet, IRandomGenerator gen, int riverNb, int riverTestCount)
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
        
        var dStart = new UniformIntDistribution(0, planet.points.Length);

        for (int i = 0; i < riverNb; i++)
        {
            int startIndex = -1;
            for (int j = 0; j < riverTestCount; j++)
            {
                int index = dStart.Next(gen);
                if (possiblePositions[j] && (startIndex < 0 || planet.points[startIndex].height < planet.points[index].height))
                    startIndex = index;
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
                    if (planet.points[p].height < planet.points[current].height && (bestIndex == -1 || planet.points[p].height < planet.points[bestIndex].height))
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
        List<int> next = new List<int>();
        bool[] setPoints = new bool[planet.points.Length];
        bool[] nextPoints = new bool[planet.points.Length];

        foreach(var river in planet.rivers)
            for(int i = 0; i < river.Length; i++)
            {
                int index = river[i].index;
                float m = i / river.Length * (1 - riverMoistureMin) + riverMoistureMin;
                planet.points[index].moisture = Mathf.Max(planet.points[index].moisture, m);
                setPoints[index] = true;
            }
        for (int i = 0; i < setPoints.Length; i++)
            if (setPoints[i])
                foreach (var p in planet.points[i].connectedPoints)
                    if (!setPoints[p] && !nextPoints[p])
                    {
                        nextPoints[p] = true;
                        next.Add(p);
                    }
        
        while(next.Count > 0)
        {
            int current = next[0];
            next.RemoveAt(0);
            nextPoints[current] = false;
            setPoints[current] = true;

            int bestMoistureIndex = -1;
            foreach (var p in planet.points[current].connectedPoints)
            {
                if (setPoints[p])
                {
                    if (bestMoistureIndex < 0 || planet.points[bestMoistureIndex].moisture < planet.points[p].moisture)
                        bestMoistureIndex = p;
                }
            }

            float d = (planet.points[bestMoistureIndex].point - planet.points[current].point).magnitude;
            planet.points[current].moisture = Mathf.Max(0, planet.points[bestMoistureIndex].moisture - d * moisturePropagationResistance);

            if (planet.points[current].moisture > 0)
            {
                foreach (var p in planet.points[current].connectedPoints)
                {
                    if (!setPoints[p] && !nextPoints[p])
                    {
                        nextPoints[p] = true;
                        next.Add(p);
                    }
                }
            }
        }
    }

    static void makeBiomes(PlanetData planet, float maxHeight)
    {
        for(int i = 0; i < planet.points.Length; i++)
        {
            if (planet.points[i].biomeID >= 0)
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