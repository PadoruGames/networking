using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Padoru.Networking
{
	public class NetworkIdentity : MonoBehaviour
	{
		[SerializeField, HideInInspector] private int id;
		[SerializeField] private NetworkBehaviour[] observedComponents;

		public int Id
		{
			get
			{
				return id;
			}
		}

		public void Init(int id)
		{
			this.id = id;

			observedComponents = GetComponents<NetworkBehaviour>();
			for (int i = 0; i < observedComponents.Length; i++)
			{
				observedComponents[i].SetupBehaviour(this, i);
			}
		}

		public void HandleMessage(NetworkReader networkReader)
		{
			var observerID = networkReader.ReadInt32();

			observedComponents[observerID].HandleMessage(networkReader);
		}

		public void SendMessageToAll(NetworkWriter pNetworkWriter)
		{
			var networkWriter = new NetworkWriter();

			networkWriter.Write(Id);
			networkWriter.Write(pNetworkWriter.ToArray());

			NetworkManager.Instance.SendMessageToAll(networkWriter);
		}
	}
}