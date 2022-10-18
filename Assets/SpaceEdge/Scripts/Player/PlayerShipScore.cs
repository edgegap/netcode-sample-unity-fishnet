using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

namespace SpaceEdge
{
    public class PlayerShipScore : NetworkBehaviour
    {
        [SyncVar] private int _playerScore;
        private string _playerName;
    
        [Server]
        public void AddScore() => _playerScore++;

        [Client]
        public void SetName(string playerName) => _playerName = playerName;
   
        [Client]
        public string GetPlayerScore() => $"Player Name: {_playerName}      PlayerScore: {_playerScore} ";
    
        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            if (IsServer)
                _playerScore = 0;
        }

    
    }

}
