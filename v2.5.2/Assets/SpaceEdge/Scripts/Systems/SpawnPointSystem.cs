using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SpaceEdge
{
    public class SpawnPointSystem : MonoBehaviour
    {
        public static SpawnPointSystem Points;
    
        [SerializeField] private List<Transform> spawnPoints;


        private void Awake()
        {
            if(Points!=null)
                Destroy(Points);

            Points = this;
        }

        public Transform GetRandom()
        {
            return spawnPoints[Random.Range(0, spawnPoints.Count)];
        }
    }
}
