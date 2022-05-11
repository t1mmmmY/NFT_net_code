using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

namespace Miner.NetCode
{
    public class GameLogic : MonoBehaviour
    {
        [SerializeField] Transform characterStartPosition;
        [SerializeField] GameObject endGamePanel;
        [SerializeField] Text endGameLabel;

        private MultiplayerDigger localPlayer;

        public static int finishLine = 10;

        private void Start()
        {
            //Instantiate local character
    		LoadCharacter();

            //Generate all formulas and share it with another player
            if (PhotonNetwork.IsMasterClient)
            {
                UpdateFormulas();
            }
        }

        public void Answer(bool isRightAnswer)
        {
            if (localPlayer != null)
            {
                localPlayer.Answer(isRightAnswer);
            }
        }

        public void GameOver(bool isWinner)
        {
            endGamePanel.SetActive(true);
            endGameLabel.text = isWinner ? "Winner" : "Looser";
        }

        public void GoHome()
        {
            LeaveMultiplayer();
            LevelLoader.Instance.LoadLevel(0);
        }

        public void Restart()
        {
            LeaveMultiplayer();
            LevelLoader.Instance.LoadLevel(0, () =>
            {
                Lobby lobby = GameObject.FindObjectOfType<Lobby>();
                if (lobby != null)
                {
                    lobby.ConnectAndPlay();
                }
            });
        }


        private void LoadCharacter()
        {
            Debug.Log("LocalPlayer " + PhotonNetwork.LocalPlayer.ActorNumber);
            MultiplayerDigger playerController = PhotonNetwork.Instantiate("MultiplayerDigger", Vector3.zero, Quaternion.identity).GetComponent<MultiplayerDigger>();

            playerController.transform.parent = characterStartPosition;
            playerController.transform.localPosition = PhotonNetwork.LocalPlayer.ActorNumber == 1 ? Vector3.zero : Vector3.zero + Vector3.right * 2;
            localPlayer = playerController;
            playerController.Init(this);
        }

        private void UpdateFormulas()
        {
            //All formulas is just a placeholder
            string allFormulas = GenerateFormulas();

            ExitGames.Client.Photon.Hashtable hash = new ExitGames.Client.Photon.Hashtable();
            hash.Add("AllFormulas", allFormulas);
            PhotonNetwork.CurrentRoom.SetCustomProperties(hash);
        }

        private string GenerateFormulas()
        {
            return "1 + 1 = ?";
        }

        public void LeaveMultiplayer()
        {
            PhotonNetwork.Disconnect();
        }

    }
}