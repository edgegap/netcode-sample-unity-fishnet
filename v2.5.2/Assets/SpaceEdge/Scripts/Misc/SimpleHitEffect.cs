using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceEdge
{
    public class SimpleHitEffect : MonoBehaviour
    {
        [SerializeField] private AudioSource sfx;
        [SerializeField] private ParticleSystem vfx;

        private void Awake()
        {
            float destroyDelay = sfx.clip.length > vfx.main.duration ? sfx.clip.length : vfx.main.duration;
            Destroy(gameObject,destroyDelay);
        }
    }

}
