using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using TMPro;
using UnityEngine;

namespace SpaceEdge
{
    public class PlayerShip : NetworkBehaviour
    {
        [SyncVar] public string PlayerName;

        [SerializeField] private GameObject interpolatedObjects;
        [SerializeField] private Camera shipCamera;
        [SerializeField] private AudioListener shipAudioListener;
        [SerializeField] private TextMeshProUGUI displayName;
        [SerializeField] private AudioSource spawnSfx;
        [SerializeField] private AudioSource destructionSfx;
        [SerializeField] private ParticleSystem spawnVfx;
        [SerializeField] private ParticleSystem destructionVfx;

        private PlayerShipMovement _playerShipMovement;
        private PlayerShipHealth _playerShipHealth;
        private PlayerShipWeapon _playerShipWeapon;
        private PlayerShipScore _playerShipScore;
        private ScoreBoard _scoreBoard;
        private CharacterController _characterController;

        public override void OnStartNetwork()
        {

            base.OnStartNetwork();
            _playerShipWeapon = GetComponentInChildren<PlayerShipWeapon>();
            _playerShipHealth = GetComponent<PlayerShipHealth>();
            _playerShipMovement = GetComponent<PlayerShipMovement>();
            _playerShipScore = GetComponent<PlayerShipScore>();
            _characterController = GetComponent<CharacterController>();

            _playerShipWeapon.SetCamera(shipCamera);
            _playerShipWeapon.OnScore += _playerShipScore.AddScore;
            _playerShipHealth.OnZeroHealth += ShipDestroyedObserversRpc;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
                InitializeClient();
        }

        [Client]
        private void InitializeClient()
        {
            displayName.text = IsOwner?"":PlayerName;
            _playerShipScore.SetName(PlayerName);

            string temp = IsOwner ? "You" : PlayerName;
            NotificationSystem.Instance.ShowNotification($"{temp} Joined The Battle!!!");

            if (IsOwner)
            {
                shipCamera.enabled = true;
                shipAudioListener.enabled = true;

                _scoreBoard = GameObject.FindWithTag("ScoreBoard").GetComponent<ScoreBoard>();
                _scoreBoard.OnSpawnRequested += ShipSpawnServerRpc;
            }
        }


        [ObserversRpc(RunLocally = true, ExcludeOwner = false, BufferLast = true)]
        private void ShipDestroyedObserversRpc(int ownerId)
        {
            if (IsOwner || IsServer)
            {
                _playerShipMovement.CanMove = false;
                _playerShipWeapon.CanFire = false;
                _characterController.enabled = false;

                if (IsOwner)
                    _scoreBoard.SetActive(true);
            }

            destructionSfx.PlayOneShot(destructionSfx.clip);
            destructionVfx.Play(withChildren: true);
            interpolatedObjects.SetActive(false);
        }


        [ServerRpc]
        private void ShipSpawnServerRpc()
        {


            ShipSpawnObserversRpc();
        }

        [ObserversRpc(RunLocally = true, ExcludeOwner = false, BufferLast = true)]
        private void ShipSpawnObserversRpc()
        {
            
            if (IsOwner || IsServer)
            {
                _playerShipMovement.CanMove = true;
                _playerShipWeapon.CanFire = true;
                _characterController.enabled = true;

                if (IsServer)
                {
                    _playerShipHealth.MaxHealth();
                    transform.position = SpawnPointSystem.Points.GetRandom().position;
                }
            }


            spawnSfx.PlayOneShot(spawnSfx.clip);
            spawnVfx.Play(withChildren: true);
            interpolatedObjects.SetActive(true);
        }
    }
}