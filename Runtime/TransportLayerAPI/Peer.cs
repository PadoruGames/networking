using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace Padoru.Networking
{
    public class Peer
    {
        private bool noDelay = true;
        private int sendTimeout = 1000;
        private int maxConnections = 5;

        private readonly ConcurrentDictionary<int, ClientToken> clients = new ConcurrentDictionary<int, ClientToken>();
        private ConcurrentQueue<Message> receiveQueue = new ConcurrentQueue<Message>();
        
        private int connectionIdCounter;
        private TcpListener listener;
        private Thread listenerThread;
        
        // Static field to store message data without allocation. Different for each thread
        [ThreadStatic] static byte[] payload;
        // Static field to store message size without allocation. Different for each thread
        [ThreadStatic] static byte[] header;

        public event Action<int> onConnection;
        public event Action<int> onDisconnection;

        public static int messageQueueSizeWarning { get; set; } = 100000;
        public int MaxMessageSize { get; set; } = 16 * 1024;
        public int ReceiveQueueCount => receiveQueue.Count;
        public bool Started
        {
            get
            {
                return listenerThread != null && listenerThread.IsAlive;
            }
        }

        #region Public Interface
        public bool Start(int port)
        {
            if (Started) return false;

            listenerThread = new Thread(() => { StartListeningForConnections(port); });
            listenerThread.IsBackground = true;
            listenerThread.Priority = System.Threading.ThreadPriority.BelowNormal;
            listenerThread.Start();

            return true;
        }

        public void Stop()
        {
            // only if started
            if (!Started) return;

            connectionIdCounter = 0;

            listenerThread.Interrupt();
        }

        public void Connect(string address, int port)
        {
            try
            {
                // Creates IPv4 socket
                var client = new TcpClient();

                // Clear internal IPv4 socket until Connect() ------> ????? Gets null reference if uncomented
                //client.Client = null;

                // Start connection in another thread since its blocking
                var connectThread = new Thread(() => { ConnectAndReceiveThreadFunction(client, address, port); });
                connectThread.IsBackground = true;
                connectThread.Start();
            }
            catch (Exception e)
            {
                Debug.LogError($"{e.Message}\n\n{e.StackTrace}");
            }
        }

        public bool Send(int connectionId, byte[] data)
        {
            if (data.Length <= MaxMessageSize)
            {
                // Find the connection
                ClientToken token;
                if (clients.TryGetValue(connectionId, out token))
                {
                    // add to send queue and return immediately.
                    // calling Send here would be blocking (sometimes for long times
                    // if other side lags or wire was disconnected)
                    token.sendQueue.Enqueue(data);
                    // Notify SendThread queue is not empty anymore
                    token.sendPending.Set();
                    return true;
                }

                // It might reach this point when client diconnects. So don't spam the console
                return false;
            }

            Debug.LogError("Client.Send: message too big: " + data.Length + ". Limit: " + MaxMessageSize);
            return false;
        }

        public bool GetNextMessage(out Message message)
        {
            return receiveQueue.TryDequeue(out message);
        }
        #endregion Public Interface

        #region Private Methods
        #region Connection
        private void ConnectAndReceiveThreadFunction(TcpClient client, string address, int port)
        {
            // absolutely must wrap with try/catch, otherwise thread
            // exceptions are silent
            try
            {
                Debug.Log($"Connecting to {address}:{port}");

                // Connect (blocking)
                client.Connect(address, port);

                var token = AddClient(client);

                Debug.Log($"Client connection entablished: {token.connectionId}");
            }
            catch (SocketException exception)
            {
                // this happens if (for example) the ip address is correct
                // but there is no server running on that ip/port
                Debug.Log("Client Recv: failed to connect to ip=" + address + " port=" + port + " reason=" + exception);

                // add 'Disconnected' event to message queue so that the caller
                // knows that the Connect failed. otherwise they will never know
                receiveQueue.Enqueue(new Message(0, EventType.Disconnected, null));
            }
            catch (ThreadInterruptedException)
            {
                // expected if Disconnect() aborts it
            }
            catch (ThreadAbortException)
            {
                // expected if Disconnect() aborts it
            }
            catch (Exception exception)
            {
                // something went wrong. probably important.
                Debug.LogError("Client Recv Exception: " + exception);
            }
        }

        private void StartListeningForConnections(int port)
        {
            try
            {
                listener = TcpListener.Create(port);
                listener.Server.NoDelay = noDelay;
                listener.Server.SendTimeout = sendTimeout;
                listener.Start();

                Debug.Log($"Server listening port {port}");

                Debug.Log("Waiting for clients to connect");

                while (true)
                {
                    TcpClient client = listener.AcceptTcpClient();

                    var token = AddClient(client);
                }
            }
            catch (ThreadAbortException exception)
            {
                // UnityEditor causes AbortException if thread is still
                // running when we press Play again next time. that's okay.
                Debug.Log("Server thread aborted. That's okay. " + exception);
            }
            catch (SocketException exception)
            {
                // Calling StopServer will interrupt this thread with a
                // 'SocketException: interrupted'. that's okay.
                Debug.Log("Server Thread stopped. That's okay. " + exception);
            }
            catch (Exception exception)
            {
                // Something went wrong. probably important.
                Debug.LogError("Server Exception: " + exception);
            }
            finally
            {
                // Stop listening for new clients.
                listener.Stop();
            }
        }

        private ClientToken AddClient(TcpClient client)
        {
            client.NoDelay = noDelay;
            client.SendTimeout = sendTimeout;

            // Generate the next connection id (thread safely)
            int connectionId = NextConnectionId();

            // Create the client token
            ClientToken token = new ClientToken(client, connectionId);
            // Add it to the dictionary
            clients[connectionId] = token;

            // Spawn a send thread
            token.sendThread = new Thread(() => SendLoop(connectionId, client, token.sendQueue, token.sendPending));
            token.sendThread.IsBackground = true;
            token.sendThread.Start();
            // Spawn a receive thread
            token.receiveThread = new Thread(() => ReceiveLoop(connectionId, client, receiveQueue, MaxMessageSize));
            token.receiveThread.IsBackground = true;
            token.receiveThread.Start();

            // Notify connection
            onConnection?.Invoke(token.connectionId);

            return token;
        }

        private void RemoveClient(int connectionId)
        {
            ClientToken token;
            if(clients.TryRemove(connectionId, out token))
            {
                // Stop the threads
                //token.receiveThread.Abort();
                token.sendThread.Abort();

                // Notify disconnection
                onDisconnection?.Invoke(token.connectionId);
            }
            else
            {
                Debug.LogError($"There is no connection for id {connectionId}");
            }
        }

        private int NextConnectionId()
        {
            int id = Interlocked.Increment(ref connectionIdCounter);

            if (id == maxConnections)
            {
                Debug.Log("Max connection count reached: " + id);
            }

            return id;
        }
        #endregion Connection

        #region Receive Message
        private void ReceiveLoop(int connectionId, TcpClient client, ConcurrentQueue<Message> receiveQueue, int MaxMessageSize)
        {
            // Get NetworkStream from client
            NetworkStream stream = client.GetStream();

            // Keep track of last message queue warning
            DateTime messageQueueLastWarning = DateTime.Now;

            // absolutely must wrap with try/catch, otherwise thread exceptions
            // are silent
            try
            {
                // add connected event to queue with ip address as data in case
                // it's needed
                receiveQueue.Enqueue(new Message(connectionId, EventType.Connected, null));

                // let's talk about reading data.
                // -> normally we would read as much as possible and then
                //    extract as many <size,content>,<size,content> messages
                //    as we received this time. this is really complicated
                //    and expensive to do though
                // -> instead we use a trick:
                //      Read(2) -> size
                //        Read(size) -> content 
                //      repeat
                //    Read is blocking, but it doesn't matter since the
                //    best thing to do until the full message arrives,
                //    is to wait.
                // => this is the most elegant AND fast solution.
                //    + no resizing
                //    + no extra allocations, just one for the content
                //    + no crazy extraction logic
                while (true)
                {
                    // Read the next message (blocking) or stop if stream closed
                    byte[] content;
                    if (!ReadMessageBlocking(stream, MaxMessageSize, out content))
                        break; // break instead of return so stream close still happens!

                    // Queue it
                    receiveQueue.Enqueue(new Message(connectionId, EventType.Data, content));

                    // Show a warning if the queue gets too big
                    if (receiveQueue.Count > messageQueueSizeWarning)
                    {
                        TimeSpan elapsed = DateTime.Now - messageQueueLastWarning;
                        if (elapsed.TotalSeconds > 10)
                        {
                            Debug.LogWarning("ReceiveLoop: messageQueue is getting big(" + receiveQueue.Count + "), try calling GetNextMessage more often. You can call it more than once per frame!");
                            messageQueueLastWarning = DateTime.Now;
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                // something went wrong. the thread was interrupted or the
                // connection closed or we closed our own connection or ...
                // -> either way we should stop gracefully
                Debug.Log("ReceiveLoop: finished receive function for connectionId=" + connectionId + " reason: " + exception);
            }
            finally
            {
                // clean up no matter what
                stream.Close();
                client.Close();

                // add 'Disconnected' message after disconnecting properly.
                // -> always AFTER closing the streams to avoid a race condition
                //    where Disconnected -> Reconnect wouldn't work because
                //    Connected is still true for a short moment before the stream
                //    would be closed.
                receiveQueue.Enqueue(new Message(connectionId, EventType.Disconnected, null));

                RemoveClient(connectionId);
            }
        }

        private static bool ReadMessageBlocking(NetworkStream stream, int MaxMessageSize, out byte[] content)
        {
            content = null;

            // Create header buffer if not created yet
            if (header == null)
                header = new byte[4];

            // Calculate the size of the message
            if (!stream.ReadExactly(header, 4))
                return false;

            int size = Utils.BytesToIntBigEndian(header);

            // protect against allocation attacks. an attacker might send
            // multiple fake '2GB header' packets in a row, causing the server
            // to allocate multiple 2GB byte arrays and run out of memory.
            if (size <= MaxMessageSize)
            {
                // Read exactly 'size' bytes for content (blocking)
                content = new byte[size];
                return stream.ReadExactly(content, size);
            }

            Debug.LogWarning("ReadMessageBlocking: possible allocation attack with a header of: " + size + " bytes.");
            return false;
        }
        #endregion Receive Message

        #region Send Message
        private static bool SendMessagesBlocking(NetworkStream stream, byte[][] messages)
        {
            // stream.Write throws exceptions if client sends with high
            // frequency and the server stops
            try
            {
                // Calculate the packet size
                int packetSize = 0;
                for (int i = 0; i < messages.Length; ++i)
                    packetSize += sizeof(int) + messages[i].Length; // header + content

                // Create the payload or resize it if too small
                if (payload == null || payload.Length < packetSize)
                    payload = new byte[packetSize];

                // Ensemble the packet
                int position = 0;
                for (int i = 0; i < messages.Length; ++i)
                {
                    // Create header buffer if not created yet
                    if (header == null)
                        header = new byte[4];

                    // Store the size of the message
                    Utils.IntToBytesBigEndianNonAlloc(messages[i].Length, header);

                    // Copy size + message into buffer
                    Array.Copy(header, 0, payload, position, header.Length);
                    Array.Copy(messages[i], 0, payload, position + header.Length, messages[i].Length);
                    // Stores the position offset for the next message
                    position += header.Length + messages[i].Length;
                }

                // Send the message
                stream.Write(payload, 0, packetSize);

                return true;
            }
            catch (Exception exception)
            {
                // log as regular message because servers do shut down sometimes
                Debug.Log("Send: stream.Write exception: " + exception);
                return false;
            }
        }

        private static void SendLoop(int connectionId, TcpClient client, SafeQueue<byte[]> sendQueue, ManualResetEvent sendPending)
        {
            // get NetworkStream from client
            NetworkStream stream = client.GetStream();

            try
            {
                while (client.Connected)
                {
                    // reset ManualResetEvent before we do anything else. this
                    // way there is no race condition. if Send() is called again
                    // while in here then it will be properly detected next time
                    // -> otherwise Send might be called right after dequeue but
                    //    before .Reset, which would completely ignore it until
                    //    the next Send call.
                    // Reset the ManualResetEvent so it can be blocked by WaitOne() again
                    sendPending.Reset(); 
                    
                    // SafeQueue.TryDequeueAll is twice as fast as ConcurrentQueue, see SafeQueue.cs!
                    byte[][] messages;
                    if (sendQueue.TryDequeueAll(out messages))
                    {
                        // Send message (blocking) or stop if stream is closed
                        if (!SendMessagesBlocking(stream, messages))
                            break; // Break instead of return so stream close still happens!
                    }

                    // Block the thread until queue not empty anymore to improve performance
                    sendPending.WaitOne();
                }
            }
            catch (ThreadAbortException)
            {
                // happens on stop. don't log anything.
            }
            catch (ThreadInterruptedException)
            {
                // happens if receive thread interrupts send thread.
            }
            catch (Exception exception)
            {
                // something went wrong. the thread was interrupted or the
                // connection closed or we closed our own connection or ...
                // -> either way we should stop gracefully
                Debug.Log("SendLoop Exception: connectionId=" + connectionId + " reason: " + exception);
            }
            finally
            {
                // clean up no matter what
                // we might get SocketExceptions when sending if the 'host has
                // failed to respond' - in which case we should close the connection
                // which causes the ReceiveLoop to end and fire the Disconnected
                // message. otherwise the connection would stay alive forever even
                // though we can't send anymore.
                stream.Close();
                client.Close();
            }
        }
        #endregion Send Message
        #endregion Private Methods
    }
}
