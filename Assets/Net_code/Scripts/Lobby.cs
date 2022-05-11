using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

namespace Miner.NetCode
{
	public enum RoomState
	{
		Created,
		Running,
		Finished,
		Undefined
	}

	public enum LobbyState
    {
		Idle,
		Matchmaking
    }

	public class Lobby : MonoBehaviourPunCallbacks
    {
		public GameObject playButton;
		public GameObject loadingLabel;

		[SerializeField] private byte maxPlayersPerRoom = 2;
		private string gameVersion = "1";

		private bool isConnecting = false;
		private bool playImmidiately = false;
		private bool matchmakingInProgress = false;

		private List<RoomInfo> roomList = new List<RoomInfo>();

		public static readonly string ROOM_STATE_KEY = "RoomState";

		private void Awake()
        {
            // this makes sure we can use PhotonNetwork.LoadLevel() on the master client and all clients in the same room sync their level automatically
            PhotonNetwork.AutomaticallySyncScene = true;
        }

        private void Start()
        {
			SetState(LobbyState.Idle);

			Connect();
        }


		public void ConnectAndPlay()
		{
			if (PhotonNetwork.IsConnectedAndReady)
			{
				Play();
			}
			else
			{
				playImmidiately = true;
				Connect();
			}
		}

		/// <summary>
		/// Start the connection process. 
		/// - If already connected, we attempt joining a random room
		/// - if not yet connected, Connect this application instance to Photon Cloud Network
		/// </summary>
		public void Connect()
		{
			// keep track of the will to join a room, because when we come back from the game we will get a callback that we are connected, so we need to know what to do then
			isConnecting = true;

			Debug.Log("Connecting...");

			// #Critical, we must first and foremost connect to Photon Online Server.
			PhotonNetwork.ConnectUsingSettings();
			PhotonNetwork.GameVersion = this.gameVersion;

		}

		public void Play()
		{
			if (!PhotonNetwork.IsConnectedAndReady)
			{
				playImmidiately = true;
				Connect();
				return;
			}

			Debug.Log("Joining Room...");
			RunMatchmaking();
		}

		private void RunMatchmaking()
		{
			SetState(LobbyState.Matchmaking);

			playImmidiately = false;
			matchmakingInProgress = true;
			Debug.Log("RunMatchmaking. InLobby = " + PhotonNetwork.InLobby);
			if (PhotonNetwork.InLobby)
			{
				//Now we are ready to create or join the room

				if (roomList.Count == 0)
				{
					//No rooms created. Create the new one
					CreateRoom();
				}
				else
				{
					matchmakingInProgress = false;

					List<RoomInfo> accaptableRooms = new List<RoomInfo>();
					foreach (RoomInfo room in roomList)
					{
						if (room.IsOpen)
						{
							accaptableRooms.Add(room);
						}
						Debug.Log($"Room {room.Name} isOpen = {room.IsOpen}; PlayerCount = {room.PlayerCount}");
					}

					if (accaptableRooms.Count == 0)
					{
						//No accaptable rooms found. Create the new one
						CreateRoom();
					}
					else
					{
						//Join random room from the list
						JoinRoom(accaptableRooms[Random.Range(0, accaptableRooms.Count)]);
					}
				}
			}
			else
			{
				//Join lobby first
				PhotonNetwork.JoinLobby();
			}

		}

		private void CreateRoom()
		{
			PhotonNetwork.CreateRoom(System.Guid.NewGuid().ToString(), new RoomOptions { MaxPlayers = maxPlayersPerRoom });
		}

		private void JoinRoom(RoomInfo room)
		{
			PhotonNetwork.JoinRoom(room.Name);
		}

		private void SetState(LobbyState state)
        {
			playButton.SetActive(state == LobbyState.Idle);
			loadingLabel.SetActive(state == LobbyState.Matchmaking);
        }

		#region MonoBehaviourPunCallbacks CallBacks

		/// <summary>
		/// Called after the connection to the master is established and authenticated
		/// </summary>
		public override void OnConnectedToMaster()
		{
			// we don't want to do anything if we are not attempting to join a room. 
			// this case where isConnecting is false is typically when you lost or quit the game, when this level is loaded, OnConnectedToMaster will be called, in that case
			// we don't want to do anything.
			if (isConnecting)
			{
				Debug.Log("OnConnectedToMaster");
			}

			if (playImmidiately)
			{
				// #Critical: The first we try to do is to join a potential existing room. If there is, good, else, we'll be called back with OnJoinRandomFailed()
				RunMatchmaking();
			}
		}

		public override void OnRoomListUpdate(List<RoomInfo> roomList)
		{
			if (!matchmakingInProgress)
			{
				return;
			}
			this.roomList = roomList;
			RunMatchmaking();
		}


		public override void OnCreatedRoom()
		{
			PhotonNetwork.CurrentRoom.IsOpen = true;
			PhotonNetwork.CurrentRoom.SetCustomProperties(GetRoomStateHashtable(RoomState.Created));
			Debug.Log("Room created, state changed");
		}

		/// <summary>
		/// Called after disconnecting from the Photon server.
		/// </summary>
		public override void OnDisconnected(DisconnectCause cause)
		{
			Debug.Log("<Color=Red>OnDisconnected</Color> " + cause);

			SetState(LobbyState.Idle);
		}

		/// <summary>
		/// Called when entering a room (by creating or joining it). Called on all clients (including the Master Client).
		/// </summary>
		/// <remarks>
		/// This method is commonly used to instantiate player characters.
		/// If a match has to be started "actively", you can call an [PunRPC](@ref PhotonView.RPC) triggered by a user's button-press or a timer.
		///
		/// When this is called, you can usually already access the existing players in the room via PhotonNetwork.PlayerList.
		/// Also, all custom properties should be already available as Room.customProperties. Check Room..PlayerCount to find out if
		/// enough players are in the room to start playing.
		/// </remarks>
		public override void OnJoinedRoom()
		{
			Debug.Log("<Color=Green>OnJoinedRoom</Color> with " + PhotonNetwork.CurrentRoom.PlayerCount + " Player(s)");

			//Second player joined. We can start
			if (PhotonNetwork.CurrentRoom.PlayerCount == maxPlayersPerRoom)
			{
				StartMultiplayer();
			}

		}

		public override void OnPlayerEnteredRoom(Player newPlayer)
		{
			base.OnPlayerEnteredRoom(newPlayer);

			//Player joined existing room. It means that 2 players are in the room already. 
			StartMultiplayer();
		}

		#endregion

		private void StartMultiplayer()
		{
			Debug.Log("StartMultiplayer");
			playImmidiately = false;

			PhotonNetwork.CurrentRoom.IsOpen = false;
			PhotonNetwork.CurrentRoom.SetCustomProperties(GetRoomStateHashtable(RoomState.Running));

			// #Critical
			// Load the Game Level. 
			PhotonNetwork.LoadLevel(1);
		}


		public static ExitGames.Client.Photon.Hashtable GetRoomStateHashtable(RoomState roomState)
		{
			ExitGames.Client.Photon.Hashtable hash = new ExitGames.Client.Photon.Hashtable();
			hash.Add(Lobby.ROOM_STATE_KEY, roomState);
			return hash;
		}

	}
}