using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Mirror;

#if LITENETLIB4MIRROR
using Mirror.LiteNetLib4Mirror;
#endif

#if IGNORANCE
#if !IGNORANCE_1_1 && !IGNORANCE_1_2
using IgnoranceTransport = Mirror.IgnoranceClassic;
#endif
#endif

using NobleConnect;
using NobleConnect.Mirror;
using UnityEngine;

namespace NobleConnect.Mirror
{
    public class NobleNetworkManager : NetworkManager
    {
        #region Public Properties

        /// <summary>The NobleClient that will be used to connect to the host.</summary>
        public NobleClient client;

        public int networkPort;

        /// <summary>This is the address that clients should connect to. It is assigned by the relay server.</summary>
        /// <remarks>
        /// Note that this is not the host's actual IP address, but one assigned to the host by the relay server.
        /// When clients connect to this address, Noble Connect will find the best possible connection and use it.
        /// This means that the client may actually end up connecting to an address on the local network, or an address
        /// on the router, or an address on the relay. But you don't need to worry about any of that, it is all
        /// handled for you internally.
        /// </remarks>
        public IPEndPoint HostEndPoint {
            get {
                return NobleServer.HostEndPoint;
            }
        }

        /// <summary>The geographic region to use when selecting a relay server.</summary>
        /// <remarks>
        /// Defaults to AUTO which will automatically select the closest region.
        /// This is useful if you would like your players to be able to choose
        /// their region at run time.
        /// Note that players are not prevented from connecting across regions.
        /// That would need to be implementing separately via matchmaking for
        /// example, by filtering out matches from undesired regions.
        /// </remarks>
        [Tooltip("The geographic region to use when selecting a relay server")]
        public GeographicRegion region = GeographicRegion.AUTO;

        /// <summary>You can enable this to force relay connections to be used for testing purposes.</summary>
        /// <remarks>
        /// Disables punchthrough and direct connections. Forces connections to use the relays.
        /// This is useful if you want to test your game with the unavoidable latency that is 
        /// introduced when the relay servers are used.
        /// Note that you will tend to use more bandwidth on the relay servers while this is 
        /// enabled than you typically would. 
        /// </remarks>
        [Tooltip("You can enable this to force relay connections to be used for testing purposes")]
        public bool forceRelayConnection;

        /// <summary>How much output do you want to see?</summary>
        public NobleConnect.Logger.Level logLevel = NobleConnect.Logger.Level.Info;

        /// <summary>Initial timeout before resending refresh messages. This is doubled for each failed resend.</summary>
        public float relayRefreshTimeout = .1f;

        /// <summary>Max number of times to try and resend refresh messages before giving up and shutting down the relay connection.</summary>
        /// <remarks>
        /// If refresh messages fail for 30 seconds the relay connection will be closed remotely regardless of these settings.
        /// </remarks>
        public int maxRelayRefreshAttempts = 8;

        #endregion Public Properties

        #region Internal Properties

        /// <summary>We need to call the internal method that Mirror does not expose, so we get it via reflection</summary>
        MethodInfo registerClientMessagesMethod;

        /// <summary>We need a reference to Mirror's internal connect method but it is not public so we get it via reflection.</summary>
        /// <remarks>
        /// This is used so that we can replace Mirror's Connect message handler with our own in NetworkClient, 
        /// but still call Mirror's internal connect method.
        /// </remarks>
        public Action<NetworkConnection, ConnectMessage> onClientConnectInternal;

        /// <summary>We need a reference to Mirror's internal disconnect method but it is not public so we get it via reflection.</summary>
        /// <remarks>
        /// This is used so that we can replace Mirror's Disconnect message handler with our own in NetworkClient, 
        /// but still call Mirror's internal disconnect method.
        /// </remarks>
        public Action<NetworkConnection, DisconnectMessage> onClientDisconnectInternal;

		/// <summary>True when in the middle of disconnecting</summary>
		/// <remarks>
		/// Disconnecting is asynchronous so that the disconnect message gets a chance to get sent out
		/// before the network connection is disposed. This will be set to true when the disconnect message is sent out
		/// and will remain true until the network connection is disposed.
		/// </remarks>
		public bool isDisconnecting;

		const string TRANSPORT_WARNING_MESSAGE = "You must download and install a UDP transport in order to use Mirror with NobleConnect.\n" +
                                    "I recommend LiteNetLib4Mirror: https://github.com/MichalPetryka/LiteNetLib4Mirror/releases";
        
        /// <summary>If the current connection is LAN only</summary>
        bool isLANOnly;

        #endregion Internal Properties

        #region Unity Stuff

        /// <summary>Initialize connectionConfig and HostTopology plus some other important setup.</summary>
        /// <remarks>
        /// If you override this method you must call the base method or everything will explode.
        /// </remarks>
        override public void Awake()
        {
            Application.runInBackground = runInBackground;
            NobleConnect.Logger.logLevel = logLevel;
            NobleConnect.Logger.logger = Debug.Log;
            NobleConnect.Logger.warnLogger = Debug.LogWarning;
            NobleConnect.Logger.errorLogger = Debug.LogError;

            var args = System.Environment.GetCommandLineArgs();
            if (Array.Exists(args, x => x == "force_relay"))
            {
                forceRelayConnection = true;
            }
            
            registerClientMessagesMethod = typeof(NetworkManager).GetMethod("RegisterClientMessages", BindingFlags.Instance | BindingFlags.NonPublic);
            var onClientConnectInternalMethod = typeof(NetworkManager).GetMethod("OnClientConnectInternal", BindingFlags.Instance | BindingFlags.NonPublic);
            var onClientDisconnectInternalMethod = typeof(NetworkManager).GetMethod("OnClientDisconnectInternal", BindingFlags.Instance | BindingFlags.NonPublic);

            onClientConnectInternal = (Action<NetworkConnection, ConnectMessage>)Delegate.CreateDelegate(typeof(Action<NetworkConnection, ConnectMessage>), this, onClientConnectInternalMethod);
            onClientDisconnectInternal = (Action<NetworkConnection, DisconnectMessage>)Delegate.CreateDelegate(typeof(Action<NetworkConnection, DisconnectMessage>), this, onClientDisconnectInternalMethod);

            base.Awake();

            bool hasUDPTransport = false;
            var transportType = Transport.activeTransport.GetType();
#if LITENETLIB4MIRROR
            if (transportType == typeof(LiteNetLib4MirrorTransport))
            {
                hasUDPTransport = true;
            }
#endif
#if IGNORANCE
            if (transportType.IsSubclassOf(typeof(IgnoranceTransport)) ||
                transportType == typeof(IgnoranceTransport) || 
                transportType == typeof(IgnoranceThreaded))
            {
                hasUDPTransport = true;
            }
#endif
            if (!hasUDPTransport)
            {
                Debug.LogError(transportType.ToString());
                Debug.LogError(TRANSPORT_WARNING_MESSAGE);
                // Unset the transport otherwise it will appear to work even though it's not connecting using the relays/punchthrough
                Transport.activeTransport = null;
            }
        }

        override public void Start()
        {
            base.Start();
            var args = System.Environment.GetCommandLineArgs();
            if (Array.Exists(args, x => x == "host"))
            {
                StartHost();
            }
        }

        /// <summary>Updates the NobleClient and NobleServer.</summary>
        /// <remarks>
        /// If you override this method you must call the base method or everything will explode.
        /// </remarks>
        virtual public void Update()
        {
            NobleServer.Update();
            if (client != null) client.Update();
			if (client != null)
			{
				if (isNetworkActive && !client.isConnecting && !client.isConnected && !isDisconnecting) StopClient();
			}
		}

        #endregion Unity Stuff

        #region Public Interface

        /// <summary>
        /// Initialize the NobleClient and allocate a relay.
        /// </summary>
        /// <remarks>
        /// You can call as soon as you know that the player intends to connect to host or even as soon as your game is launched.
        /// Initializing takes a few seconds, so doing it early can make connections seem quicker.
        /// If you do not call this method it will be called for you when you try and connect.
        /// </remarks>
        public void InitClient()
        {
            client = new NobleClient(region, OnFatalError, relayRefreshTimeout, maxRelayRefreshAttempts);
            client.ForceRelayOnly = forceRelayConnection;
            //base.client = client.unetClient;
            client.Init();
        }

        /// <summary>Like NetworkManager.StartClient() but utilizes the Noble Connect relay and punchthrough services.</summary>
        /// <remarks>
        /// Just like Mirror's StartClient(). This method uses the NetworkManager's networkAddress and networkPort, so make sure to 
        /// set those to the host's HostEndPoint before calling this method.
        /// </remarks>
        new public void StartClient()
        {
            isLANOnly = false;
            var relayEndPoint = new IPEndPoint(IPAddress.Parse(networkAddress), networkPort);
            StartClient(relayEndPoint);
        }

        /// <summary>Connect to a HostEndPoint, utilizing the Noble Connect relay and punchthrough services.</summary>
        public void StartClient(IPEndPoint hostEndPoint)
        {
            isLANOnly = false;
            isNetworkActive = true;

            if (client == null)
            {
                try
                {
                    InitClient();
                }
                catch (SocketException)
                {
                    Logger.Log("Failed to resolve relay server address. Starting in LAN only mode", Logger.Level.Warn);
                    isLANOnly = true;
                    client = new NobleClient();
                }
            }

            RegisterClientHandlers();
            client.Connect(hostEndPoint, isLANOnly);
            OnStartClient();
        }

        /// <summary>
        /// Start a client in LAN only mode. No relays or punchthrough will be used.
        /// </summary>
        /// <remarks>
        /// You will need the host's LAN ip to connect using this method.
        /// Just like Mirror's StartClient(), this method uses the NetworkManager's networkAddress and networkPort, so make sure to 
        /// set those to the host's LAN ip before calling this method.
        /// </remarks>
        public void StartClientLANOnly()
        {
            var relayEndPoint = new IPEndPoint(IPAddress.Parse(networkAddress), networkPort);
            StartClientLANOnly(relayEndPoint);
        }

        /// <summary>
        /// Start a client in LAN only mode. No relays or punchthrough will be used.
        /// </summary>
        /// <remarks>
        /// You will need the host's LAN ip to connect using this method.
        /// </remarks>
        /// <param name="hostEndPoint"></param>
        public void StartClientLANOnly(IPEndPoint hostEndPoint)
        {
            isLANOnly = true;
            isNetworkActive = true;

            if (client != null) client.Dispose();

            client = new NobleClient();

            RegisterClientHandlers();
            client.Connect(hostEndPoint, isLANOnly);
            OnStartClient();
        }

        /// <summary>Start hosting, connect a local client, and request a HostEndPoint from the NobleConnectServices</summary>
        /// <remarks>
        /// OnServerPrepared will be called when the HostEndPoint has been retrieved and the host
        /// is ready to receive relay / punchthrough connections.
        /// </remarks>
        new virtual public void StartHost()
        {
            isLANOnly = false;

            if (networkPort == 0) networkPort = UnityEngine.Random.Range(49152, 65536);
            NobleServer.SetTransportPort((ushort)networkPort);

            base.StartHost();
        }

        /// <summary>Start hosting, connect a local client, but do no create a relay or enable punchthrough</summary>
        /// <returns>
        /// OnServerPrepared will be called immediately and passed the local LAN address that clients can use to connect.
        /// </returns>
        public void StartHostLANOnly()
        {
            isLANOnly = true;
            base.StartHost();
        }

        /// <summary>Start a server and request a HostEndPoint from the NobleConnectServices</summary>
        /// <remarks>
        /// OnServerPrepared will be called when the HostEndPoint has been retrieved and the host
        /// is ready to receive relay / punchthrough connections.
        /// </remarks>
        new public void StartServer()
        {
            isLANOnly = false;

            if (networkPort == 0) networkPort = UnityEngine.Random.Range(49152, 65536);
            NobleServer.SetTransportPort((ushort)networkPort);

            base.StartServer();
        }

        /// <summary>Start a server, but do no create a relay or enable punchthrough</summary>
        /// <remarks>
        /// OnServerPrepared will be called immediately and passed the local LAN address that clients can use to connect.
        /// </remarks>
        public void StartServerLANonly()
        {
            isLANOnly = true;
            base.StartServer();
        }

        /// <summary>Stop the client.</summary>
        /// <remarks>
        /// In most cases it is not recommended to call this method to disconnect the client as it will
        /// cause a timeout on the host. You should instead call client.connection.Disconenct()
        /// to send a disconnect message to the host and disconnect cleanly.
        /// </remarks>
        new public void StopClient()
        {
            base.StopClient();
            client.connection.InvokeHandler(new DisconnectMessage(), -1);
            if (client != null)
            {
                client.Dispose();
            }
            client = null;
        }

        /// <summary>Register default client message handlers</summary>
        public void RegisterClientHandlers()
        {
            registerClientMessagesMethod.Invoke(this, new object[] { });
            client.RegisterHandler(onClientConnectInternal, false);
            client.RegisterHandler(onClientDisconnectInternal, false);
        }

        #endregion Public Interface

        #region Handlers

        /// <summary>Called when hosting starts.</summary>
        /// <remarks>
        /// If you override this method you must call the base method or everything will explode.
        /// </remarks>
        override public void OnStartServer()
        {
            base.OnStartServer();

            if (!isLANOnly)
            {
                try
                {
                    NobleServer.maxAllocationResends = maxRelayRefreshAttempts;
                    NobleServer.allocationResendTimeout = relayRefreshTimeout;
                    NobleServer.Init(region, OnFatalError);
                    NobleServer.ForceRelayOnly = forceRelayConnection;
                    ushort port = NobleServer.GetTransportPort();
                    NobleServer.InitializeNobleServices(port, OnServerPrepared);

                    return;
                }
                catch (SocketException)
                {
                    isLANOnly = true;
                    Logger.Log("Failed to resolve relay server address. Starting in LAN only mode", Logger.Level.Warn);
                }
            }

            // If we made it to here then either this is a LAN only host or Noble Connect failed. 
            // Start in LAN only mode.
            OnStartServerLANOnly();
        }

        public void OnStartServerLANOnly()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress localIP = IPAddress.Loopback;
            foreach (IPAddress address in host.AddressList)
            {
                if (!address.Equals(IPAddress.Loopback) && address.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = address;
                    break;
                }
            }
            networkPort = NobleServer.GetTransportPort();
            networkAddress = localIP.ToString();
            NobleServer.HostEndPoint = new IPEndPoint(localIP, networkPort);
            OnServerPrepared(localIP.ToString(), (ushort)networkPort);
        }

        /// <summary>Override this method to be informed when something goes horribly wrong.</summary>
        /// <remarks>
        /// You should see an error in your console with more info any time this is called. Generally
        /// it will either mean you've completely lost connection to the relay server or you
        /// have exceeded your CCU or bandwidth limit.
        /// </remarks>
        virtual public void OnFatalError()
        {
            StopHost();
        }

        /// <summary>Cleans up the server</summary>
        /// <remarks>
        /// If you override this method you must call the base method or resources will not be properly cleaned up.
        /// </remarks>
        override public void OnStopServer()
        {
            base.OnStopServer();
            StartCoroutine(CleanUpStoppedServer());
        }

        IEnumerator CleanUpStoppedServer()
        {
            yield return 0;
            NobleServer.Dispose();
        }
        
        public override void OnStopClient()
        {
            base.OnStopClient();
            StartCoroutine(CleanUpStoppedClient());
        }

        IEnumerator CleanUpStoppedClient()
        {
            yield return 0;
            if (client != null)
            {
                client.Shutdown();
            }
        }

        /// <summary>Called when the server receives a client connection.</summary>
        /// <remarks>
        /// If you override this method you must call the base method or everything will explode.
        /// </remarks>
        /// <param name="conn">The NetworkConnection of the connecting client</param> 
        override public void OnServerConnect(NetworkConnection conn)
        {
            NobleServer.OnServerConnect(conn);
            base.OnServerConnect(conn);
        }

        public override void OnClientDisconnect(NetworkConnection conn)
        {
            // Don't clean up immediately or the disconnect message doesn't get a chance to go out
            StartCoroutine(StopClientAfterDelay(conn));
        }

        IEnumerator StopClientAfterDelay(NetworkConnection conn)
        {
			isDisconnecting = true;
			yield return new WaitForSeconds(40 / 1000.0f);
            base.OnClientDisconnect(conn);
            client = null;
            isDisconnecting = false;
		}

        /// <summary>Called on the server when a client disconnects</summary>
        /// <remarks>
        /// If you override this method you must call the base method or resources will not be properly cleaned up.
        /// </remarks>
        /// <param name="conn">The NetworkConnection of the disconnecting client</param>
        override public void OnServerDisconnect(NetworkConnection conn)
        {
            NobleServer.OnServerDisconnect(conn);
            base.OnServerDisconnect(conn);
        }

        /// <summary>Override this method to know when a Server has received their HostEndPoint</summary>
        /// <remarks>
        /// If you are using some sort matchmaking this is a good time to create a match now that you have 
        /// the HostEndPoint that clients will need to connect to.
        /// </remarks>
        /// <param name="hostAddress">The address of the HostEndPoint the clients should use when connecting to the host.</param>
        /// <param name="hostPort">The port of the HostEndPoint that clients should use when connecting to the host</param>
        virtual public void OnServerPrepared(string hostAddress, ushort hostPort) { }

        /// <summary>Clean up the client and server.</summary>
        /// <remarks>
        /// If you override this method you must call the base method or resources will not be properly cleaned up.
        /// </remarks>
        override public void OnDestroy()
        {
            base.OnDestroy();
            NobleServer.Dispose();
            if (client != null) client.Dispose();
        }

        #endregion Handlers
    }
}