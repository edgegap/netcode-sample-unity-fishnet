using System;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceEdge
{
    public class PlayerShipHealth : NetworkBehaviour
    {
        
        public Action<int> OnZeroHealth;
       
        [SerializeField] private float maxHealth;

        private Slider _healthBar;
        private float _health;
        
        public override void OnStartServer()
        {
            base.OnStartServer();
            _health = maxHealth;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (IsOwner)
                _healthBar = GameObject.FindWithTag("HealthBar").GetComponent<Slider>();
        }

        [Server]
        public void Damage(float amount, int ownerId)
        {
            _health -= amount;
            HealthChangeTargetRpc(Owner, _health / maxHealth);
            if (_health <= 0f)
                OnZeroHealth?.Invoke(OwnerId);
        }

        [Server]
        public void MaxHealth()
        {
            _health = maxHealth;
            HealthChangeTargetRpc(Owner, _health / maxHealth);
        }

        [TargetRpc(RunLocally = false, ValidateTarget = true)]
        private void HealthChangeTargetRpc(NetworkConnection connection, float healthPercentage)
        {
            _healthBar.value = healthPercentage;
        }
    }
}