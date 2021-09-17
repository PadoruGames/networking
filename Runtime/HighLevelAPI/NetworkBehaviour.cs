using Padoru.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Padoru.Networking
{

	[RequireComponent(typeof(NetworkIdentity))]
	public abstract class NetworkBehaviour : MonoBehaviour
	{
		private NetworkIdentity identity;
		private int observerId;

		public void SetupBehaviour(NetworkIdentity identity, int observerId)
		{
			this.identity = identity;
			this.observerId = observerId;
		}

		public abstract void HandleMessage(NetworkReader networkReader);

		protected void SendMessageToAll(NetworkWriter pNetworkWriter)
		{
			var networkWriter = new NetworkWriter();

			networkWriter.Write(observerId);
			networkWriter.Write(pNetworkWriter.ToArray());

			identity.SendMessageToAll(networkWriter);
		}
	}
}