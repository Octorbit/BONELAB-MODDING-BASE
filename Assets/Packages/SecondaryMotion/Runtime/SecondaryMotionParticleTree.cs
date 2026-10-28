using UnityEngine;
using System.Collections.Generic;
// ReSharper disable UseCollectionExpression

namespace WeatherElectric.SecondaryMotion
{
    public class SecondaryMotionParticleTree
    {
        public Transform Root;
        public float BoneTotalLength;
        public readonly List<SecondaryMotionParticle> Particles = new();
    }
}