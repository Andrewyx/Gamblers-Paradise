using UnityEngine;

namespace Entity.Projectiles
{
    public class Projectile
    {
        // Vector3 
        public Transform projectileTransform;
        public Vector3 direction;
        // public float maxRangeMeters;
        public float rangeMeters;
        public GameObject reference;
    }
}