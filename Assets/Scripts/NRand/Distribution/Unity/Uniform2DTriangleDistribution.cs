using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NRand
{
    public class Uniform2DTriangleDistribution : IRandomDistribution<Vector2>
    {
        UniformFloatDistribution _d = new UniformFloatDistribution(0, 1);
        Vector3 _v1;
        Vector3 _v2;
        Vector3 _v3;

        public Uniform2DTriangleDistribution(Vector2 v1, Vector2 v2, Vector2 v3)
        {
            _v1 = v1;
            _v2 = v2;
            _v3 = v3;
        }

        public Vector2 Max()
        {
            return new Vector2(Mathf.Max(_v1.x, _v2.x, _v3.x), Mathf.Max(_v1.y, _v2.y, _v3.y));
        }

        public Vector2 Min()
        {
            return new Vector2(Mathf.Min(_v1.x, _v2.x, _v3.x), Mathf.Min(_v1.y, _v2.y, _v3.y));
        }

        public Vector2 Next(IRandomGenerator generator)
        {
            float dX = _d.Next(generator);
            float dY = _d.Next(generator);
            if (dX + dY > 0.5f)
            {
                dX = 1 - dX;
                dY = 1 - dY;
            }

            return _v1 * (1 - dX - dY) + _v2 * dX + _v3 * dY;
        }
    }
}
