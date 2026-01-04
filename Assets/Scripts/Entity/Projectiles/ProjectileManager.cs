using System;
using System.Collections.Generic;
using FishNet;
using FishNet.Object;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Entity.Projectiles
{
    public class ProjectileManager : NetworkBehaviour
    {
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private float projectileForce;
        [SerializeField] private Transform projectileOrigin;
        [SerializeField] private float projectileLifetime;
        [Header("FireRate (RPM)")]
        [SerializeField] private int fireRate;

        [Header("Bullet Spread")]
        [SerializeField] private float bulletSpread = 1f;

        private float lastFireTime;
        private List<Projectile> _projectiles = new();

        private void Start()
        {
            lastFireTime = 60f / fireRate;
        }

        private void Update()
        {
            for (int i = 0; i < _projectiles.Count; i++)
            {
                _projectiles[i].ProjectileLifetime -= Time.deltaTime;
                if (_projectiles[i].ProjectileLifetime <= 0)
                {
                    _projectiles.Remove(_projectiles[i]);
                    InstanceFinder.ServerManager.Despawn(_projectiles[i].ReferencedGameObject);
                }
            }

            if (!IsOwner) return;
            
            lastFireTime += Time.deltaTime;
            if (Input.GetKey(KeyCode.Mouse0) && lastFireTime >= 60f / fireRate )
                FireProjectile();
        }

        private void FireProjectile()
        {
            lastFireTime = 0;
            Vector3 spawnPosition = projectileOrigin.position;
            Vector3 direction = projectileOrigin.forward;

            SpawnProjectileLocal(spawnPosition, direction);
            // SpawnProjectile(spawnPosition, direction, TimeManager.Tick);
        }

        private void SpawnProjectileLocal(Vector3 spawnPosition, Vector3 direction)
        {
            GameObject projectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
            Vector3 spreadOffset = Random.insideUnitSphere * bulletSpread;
            _projectiles.Add(new Projectile()
            {
                ProjectileLifetime = projectileLifetime, ReferencedGameObject = projectile
            });
            InstanceFinder.ServerManager.Spawn(projectile);
            projectile.GetComponent<Rigidbody>().AddForce((direction + spreadOffset).normalized * projectileForce, ForceMode.Impulse);
        }

        // [ServerRpc]
        // private void SpawnProjectile(Vector3 startPosition, Vector3 direction, uint startTick)
        // {
        //     SpawnProjectileObserver(startPosition, direction, startTick);
        // }
        //
        // [ObserversRpc(ExcludeOwner = true)]
        // private void SpawnProjectileObserver(Vector3 startPosition, Vector3 direction, uint startTick)
        // {
        //     float timeDiff = (float)(TimeManager.Tick - startTick) / TimeManager.TickRate;
        //     Vector3 compensatedPosition = startPosition + direction * (timeDiff * projectileSpeed);
        //     SpawnProjectileLocal(compensatedPosition, direction);
        // }
    }
}