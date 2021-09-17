using Padoru.Networking;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Padoru.Networking
{
	public class NetworkManager : MonoBehaviour
	{
		private static NetworkManager instance;

		[SerializeField] private GameObject playerPrefab;

		private Peer localClient;
		private List<int> clientIds;
		private Dictionary<int, NetworkIdentity> networkObjects;

		private bool isInitialized;

		public static NetworkManager Instance
		{
			get
			{
				if (instance == null)
				{
					instance = FindObjectOfType<NetworkManager>();
				}

				return instance;
			}
		}

		private void Update()
		{
			if (!isInitialized) return;

			ReceiveMessages();
		}

		#region Public Interface
		public void Init()
		{
			instance = this;

			clientIds = new List<int>();
			networkObjects = new Dictionary<int, NetworkIdentity>();

			localClient = new Peer();

			var sceneIdentities = FindObjectsOfType<NetworkIdentity>();
			for (int i = 0; i < sceneIdentities.Length; i++)
			{
				var id = 1000 + i;
				var identity = sceneIdentities[i];

				AddNetworkObject(identity, id);
			}

			localClient.onConnection += OnClientConnected;
			localClient.onDisconnection += OnClientDisconnected;

			isInitialized = true;
		}

		public void Shutdown()
		{
			instance = null;

			localClient.onConnection -= OnClientConnected;
			localClient.onDisconnection -= OnClientDisconnected;

			isInitialized = false;
		}

		public void StartClient(int port)
		{
			localClient.Start(port);
		}

		public void Connect(string address, int port)
		{
			localClient.Connect(address, port);
		}

		public void SendMessageToAll(NetworkWriter networkWriter)
		{
			foreach (var clientId in clientIds)
			{
				var payload = networkWriter.ToArray();

				localClient.Send(clientId, payload);
			}
		}

		public void RegisterObject(NetworkIdentity identity)
		{
			if (networkObjects.ContainsKey(identity.Id))
			{
				Debug.LogError($"Trying to register the same object twice: {identity}");
				return;
			}

			networkObjects.Add(identity.Id, identity);
		}
		#endregion Public Interface

		#region Private Methods
		private void ReceiveMessages()
		{
			Message message;
			while (localClient.GetNextMessage(out message))
			{
				if (message.data == null) continue;

				var networkReader = new NetworkReader(message.data);

				var identityID = networkReader.ReadInt32();

				networkObjects[identityID].HandleMessage(networkReader);
			}
		}

		private void OnClientConnected(int connectionId)
		{
			Debug.Log($"Client connected: {connectionId}");

			clientIds.Add(connectionId);
		}

		private void OnClientDisconnected(int connectionId)
		{
			Debug.Log($"Client disconnected: {connectionId}");

			clientIds.Remove(connectionId);
		}

		private void CreatePlayer(int connectionId)
		{
			var obj = Instantiate(playerPrefab);

			var playerIdentity = obj.GetComponent<NetworkIdentity>();

			if (playerIdentity == null)
			{
				Debug.LogError($"Player prefab must have a NetworkIdentity component");
				return;
			}

			AddNetworkObject(playerIdentity, connectionId);
		}

		private void AddNetworkObject(NetworkIdentity identity, int id)
		{
			identity.Init(id);
			networkObjects.Add(id, identity);
		}
		#endregion Private Methods
	}
}
