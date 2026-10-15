using System;
using FishNet.Object;
using UnityEngine;

namespace SpaceEdge
{
    public class PlayerShipWeapon : NetworkBehaviour
{
    public bool CanFire { set; private get; } = true;

    public Action OnScore;
    
    [SerializeField] private float cooldown;
    [SerializeField] private float damage;
    [SerializeField] private GameObject hitEffect;
    [SerializeField] private AudioSource muzzleSfx;
    [SerializeField] private ParticleSystem muzzleVfx;

    private Camera _camera;
    private float _cooldown;

    
    public void SetCamera(Camera cam)
    {
        _camera = cam;
    }

    
    private void Awake()
    {
        _cooldown = cooldown;
    }


    private void Update()
    {
        if(!CanFire) return;
        if (IsOwner && _cooldown >= cooldown && InputPollingSystem.FireInput)
            Fire();

        if (IsServer || IsOwner)
            if (_cooldown < cooldown)
                _cooldown += Time.deltaTime;
      
    }

   
    private void Fire()
    {
        var ray = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out var hit, Mathf.Infinity))
        {
            _cooldown = 0f;
            FireServerRpc();
            muzzleSfx.PlayOneShot(muzzleSfx.clip);
            muzzleVfx.Play(true);
            Instantiate(hitEffect, hit.point, Quaternion.Euler(hit.normal));
        }
    }

    [ServerRpc]
    private void FireServerRpc()
    {
        if (_cooldown < cooldown) return;
        var ray = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out var hit, Mathf.Infinity))
        {
            _cooldown = 0f;
            Instantiate(hitEffect, hit.point, Quaternion.Euler(hit.normal));
            if (hit.transform.TryGetComponent(out PlayerShipHealth sh))
            {
                sh.Damage(damage, OwnerId);
                OnScore?.Invoke();
            }
            FireObserversRpc(hit.point,hit.normal,OwnerId);
        }
    }


    [ObserversRpc(BufferLast = false, ExcludeOwner = true, RunLocally = false)]
    private void FireObserversRpc(Vector3 point, Vector3 normal, int ownerId)
    {
        if (OwnerId == ownerId)
        {
            muzzleSfx.PlayOneShot(muzzleSfx.clip);
            muzzleVfx.Play(true);
        }
        Instantiate(hitEffect, point, Quaternion.Euler(normal));
        
    }
}
}
