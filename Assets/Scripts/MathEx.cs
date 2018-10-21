using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public static class MathEx
{
    public static float triangleArea(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float a = (p2 - p1).magnitude;
        float b = (p3 - p2).magnitude;
        float c = (p1 - p3).magnitude;

        return Mathf.Sqrt((a + b - c) * (a - b + c) * (-a + b + c) * (a + b + c)) / 4; //Heron's formula
    }

    public static Vector3 getLeft(List<Vector3> points, Vector3 center, Vector3 basePoint, Vector3 vertical)
    {
        if (points.Count == 0)
            return basePoint;

        Plane p = new Plane(vertical, center);
        var basePos = p.ClosestPointOnPlane(basePoint);
        basePos = (basePos - center).normalized;

        float bestValue = float.MaxValue;
        int bestIndex = -1;
        for (int i = 0; i < points.Count; i++)
        {
            var pos = p.ClosestPointOnPlane(points[i]);
            pos = (pos - center).normalized;
            float value = Vector3.Dot(pos, basePos);
            if (value < bestValue)
            {
                bestValue = value;
                bestIndex = i;
            }
        }
        if (bestIndex < 0)
            return basePoint;
        return points[bestIndex];
    }

    public static Vector3 getRight(List<Vector3> points, Vector3 center, Vector3 basePoint, Vector3 vertical)
    {
        return getLeft(points, center, basePoint, -vertical);
    }
}
