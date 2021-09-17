using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace Padoru.Networking
{
    public class ClientToken
    {
        public int connectionId;

        public TcpClient client;

        public Thread sendThread;

        public Thread receiveThread;

        // send queue
        // SafeQueue is twice as fast as ConcurrentQueue, see SafeQueue.cs!
        public SafeQueue<byte[]> sendQueue = new SafeQueue<byte[]>();

        // ManualResetEvent to wake up the send thread. better than Thread.Sleep
        // -> call Set() if everything was sent
        // -> call Reset() if there is something to send again
        // -> call WaitOne() to block until Reset was called
        public ManualResetEvent sendPending = new ManualResetEvent(false);

        public ClientToken(TcpClient client, int connectionId)
        {
            this.client = client;
            this.connectionId = connectionId;
        }

        public override string ToString()
        {
            return $"Client {connectionId}";
        }
    }
}
