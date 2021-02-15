using System;
using System.Collections.Generic;
using GameSystem.Utility;
using Items.Weapons;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Jobs;

namespace GameSystem
{
    public enum BulletType : uint
    {
        Dummy,
        //Lead,
        //Laser,
        //Acid,
        //Electric,
        //Rocket,
        NumberOfBulletTypes
    }
    
    [Serializable]
    public struct BulletPrefab
    {
        public GameObject prefab;
        public float baseDamage;
        public float baseSpeed;
        
        public uint initialPoolSize;
#if UNITY_EDITOR
        [InspectorReadOnly]
        public uint currentPoolSize;
        [InspectorReadOnly]
        public uint activeInstances;  
        #endif
    }

    public struct BulletData
    {
        public readonly BulletType Type;
        public readonly float StartTime;
        public Vector3 Position;
        public Vector3 Velocity;
        public Vector3 PositionOld;

        public bool ToBeKilled;

        public BulletData(BulletType type, Vector3 position, Vector3 velocity, float startTime)
        {
            Type = type;
            Position = position;
            PositionOld = position;
            Velocity = velocity;
            StartTime = startTime;
            ToBeKilled = false;
        }
    }
    
    public class BulletManager : MonoBehaviour
    {
        [EnumNamedArray(typeof(BulletType))]
        public BulletPrefab[] bulletPrefabs = new BulletPrefab[(int)BulletType.NumberOfBulletTypes];

        public int initialArrayAllocation = 100;
        public LayerMask bulletCollisionLayer;
        
        [Header("Info")]
        [InspectorReadOnly]
        public uint numberOfActiveBullets;
        
        
        
        private NativeList<BulletData> _bulletDataArray;

        private List<Transform>[] _objectPool;
        private TransformAccessArray _transformAccessArray;
        
        private NativeList<RaycastCommand> _raycastCommandArray;
        private NativeList<RaycastHit> _raycastHitArray;
        
        //Initialization
        private void Start()
        {
            _objectPool = new List<Transform>[(uint)BulletType.NumberOfBulletTypes];
            
            _transformAccessArray = new TransformAccessArray(initialArrayAllocation);
            

            for (int i = 0; i < (uint)BulletType.NumberOfBulletTypes; ++i)
            {
                _objectPool[i] = new List<Transform>(initialArrayAllocation);

                for (int j = 0; j < bulletPrefabs[i].initialPoolSize; ++j)
                {
                    CreateBulletObject((BulletType)i);
                }
            }

            _bulletDataArray = new NativeList<BulletData>(initialArrayAllocation, Allocator.Persistent);
            
            _raycastCommandArray = new NativeList<RaycastCommand>(initialArrayAllocation, Allocator.Persistent);
            _raycastHitArray = new NativeList<RaycastHit>(initialArrayAllocation, Allocator.Persistent);
        }
        
        //Method to be used by the actors
        public void FireBullet(BulletType type, Vector3 pos, Vector3 direction, float damageMultiplier = 1, float speedMultiplier = 1)
        {
            Vector3 velocity = direction * bulletPrefabs[(uint)type].baseSpeed;
            
            _raycastCommandArray.Add(new RaycastCommand(pos, velocity, 0, bulletCollisionLayer));
            _raycastHitArray.Add(new RaycastHit());

            for (int i = 0; i < _objectPool[(uint) type].Count; ++i)
            {
                if (!_objectPool[(uint)type][i].gameObject.activeSelf)
                {
                    _bulletDataArray.Add(new BulletData(type, pos, velocity, Time.time));
                    _objectPool[(uint)type][i].gameObject.SetActive(true);
                    _objectPool[(uint) type][i].position = pos;
                    _transformAccessArray.Add(_objectPool[(uint)type][i]);
                    numberOfActiveBullets++;
                    return;
                }
            }

            var obj = CreateBulletObject(type, true);
            obj.position = pos;
            _bulletDataArray.Add(new BulletData(type, pos, velocity, Time.time));
            _transformAccessArray.Add(obj);
            
            numberOfActiveBullets++;
        }

        //Method to add create new object and add it to the pool
        private Transform CreateBulletObject(BulletType type, bool isActive = false)
        {
            var obj = Instantiate(bulletPrefabs[(uint)type].prefab, transform).transform;
            _objectPool[(uint)type].Add(obj.transform);
            obj.gameObject.SetActive(isActive);

            #if UNITY_EDITOR
            bulletPrefabs[(uint) type].activeInstances++;
            #endif

            return obj;
        }

        private void BulletCollision(int index)
        {
            if (TryGetComponent<Surface>(out var surface))
            {
                _transformAccessArray[index].gameObject.GetComponent<BulletController>().OnContact(surface.type);
            }
            else
            {
                _transformAccessArray[index].gameObject.GetComponent<BulletController>().OnContact();
            }

            _raycastHitArray.RemoveAtSwapBack(index);
            _raycastCommandArray.RemoveAtSwapBack(index);
            _transformAccessArray.RemoveAtSwapBack(index);
            _bulletDataArray.RemoveAtSwapBack(index);
            
            numberOfActiveBullets--;
        }



        private void MainThreadProcess()
        {
            for (int i = 0; i < _bulletDataArray.Length; ++i)
            {
                //Check timers
                if (_bulletDataArray[i].ToBeKilled)
                {
                    BulletCollision(i);
                    continue;
                }
                
                //Read raycast hits
                if (_raycastHitArray[i].collider)
                {
                    _transformAccessArray[i].position = _raycastHitArray[i].point;
                    BulletCollision(i);
                }
            }
        }

        private void Update()
        {
            var jobMove = new BulletMove { BulletData = _bulletDataArray.AsDeferredJobArray(), RaycastCommands = _raycastCommandArray.AsDeferredJobArray(), LayerMask = bulletCollisionLayer, DeltaTime = Time.deltaTime, Time = Time.time};

            var handlerMove = jobMove.Schedule(_transformAccessArray);
            handlerMove.Complete();

            var handlerRaycast = RaycastCommand.ScheduleBatch(_raycastCommandArray, _raycastHitArray, 16);
            handlerRaycast.Complete();
            
            MainThreadProcess();
        }

        private void OnDestroy()
        {
            _raycastCommandArray.Dispose();
            _raycastHitArray.Dispose();
            
            _transformAccessArray.Dispose();
            _bulletDataArray.Dispose();
        }

        [BurstCompile]
        struct BulletMove : IJobParallelForTransform {
            public NativeArray<BulletData> BulletData;
            public NativeArray<RaycastCommand> RaycastCommands;
            public int LayerMask;
            public float DeltaTime;
            public float Time;

            void IJobParallelForTransform.Execute(int i, TransformAccess t) {
                BulletData data = BulletData[i];
                data.PositionOld = data.Position;
                Vector3 delta = Vector3.zero;
                switch (BulletData[i].Type)
                {
                    case BulletType.Dummy:
                        if (Time - data.StartTime > 2.0f)
                        {
                            data.ToBeKilled = true;
                            break;
                        }
                        data.Velocity += new Vector3(0, 0, 0) * DeltaTime;
                        delta = data.Velocity * DeltaTime;
                        data.Position += delta;
                        
                        break;
                }

                Vector3 direction = data.Velocity.normalized;
                
                t.position = data.Position;
                t.rotation = Quaternion.LookRotation(direction);
                
                BulletData[i] = data;
                
                
                RaycastCommands[i] = new RaycastCommand(data.PositionOld, direction, delta.magnitude, LayerMask);
            }
        }
    }
}