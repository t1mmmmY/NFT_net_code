using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;
using Photon.Pun;
using UnityEngine.UI;

namespace Miner.NetCode
{
	public class MultiplayerDigger : MonoBehaviourPunCallbacks, IPunObservable
	{
		public Text levelLabel;

		private float _progress = 0;
		public float progress
		{
			get
			{
				return _progress;
			}
			set
			{
				_progress = value;
				levelLabel.text = value.ToString();
			}
		}

		private GameLogic gameLogic;

		void Start()
		{
		}

		public void Init(GameLogic gameLogic)
        {
			this.gameLogic = gameLogic;
        }

		public void Answer(bool isCoorect)
		{
			DigRPC(isCoorect);
		}

		public void DigRPC(bool isCoorect)
		{
			if (isCoorect)
			{
				progress++;
			}

			//Local player wins!
			if (progress >= GameLogic.finishLine)
			{
				if (gameLogic != null)
				{
					gameLogic.GameOver(true);
				}
				Debug.Log("<Color=Green>WIN</Color>");
			}
			photonView.RPC("OnDig_RPC", RpcTarget.Others, isCoorect);
		}

		[PunRPC]
		public void OnDig_RPC(bool isCoorect)
		{
			//Other character dig
			if (isCoorect)
			{
				progress++;
			}

			//Remote player wins
			if (progress >= GameLogic.finishLine)
			{
				if (gameLogic != null)
                {
					gameLogic.GameOver(false);
                }
				Debug.Log("<Color=Yellow>LOOSE</Color>");
			}
		}

		public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
		{
		}

	}
}