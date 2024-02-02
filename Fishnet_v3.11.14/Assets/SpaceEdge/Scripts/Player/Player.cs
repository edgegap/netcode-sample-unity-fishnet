using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceEdge
{
   
    public class Player : NetworkBehaviour
    {
        
        [SerializeField] private GameObject shipPrefab;

        private GameObject _shipObject;

        public override void OnOwnershipClient(NetworkConnection prevOwner)
        {
            base.OnOwnershipClient(prevOwner);

            NewPlayerConnectedServerRpc(PlayerPrefs.GetString("PlayerName"));
            GameObject.FindWithTag("QuitButton").GetComponent<Button>().onClick.AddListener(() => ClientManager.StopConnection());
        }


        [ServerRpc]
        public void NewPlayerConnectedServerRpc(string playerNameString)
        {

            if (playerNameString.Length > 15 || playerNameString == string.Empty)
                playerNameString = "Player " + Random.Range(1, 100);
          
            SpawnShip(playerNameString);
        }


        [Server]
        private void SpawnShip(string playerNameString)
        {
           
            var point = SpawnPointSystem.Points.GetRandom();

            _shipObject = Instantiate(shipPrefab, point.position, point.rotation);

            _shipObject.GetComponent<PlayerShip>().PlayerName = playerNameString;

            ServerManager.Spawn(_shipObject, Owner);
           
        }
        
    }
}