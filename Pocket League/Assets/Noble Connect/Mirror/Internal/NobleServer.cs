using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NobleConnect.Ice;
using NobleConnect.Stun;
using NobleConnect.Turn;
using UnityEngine;
using Mirror;
using System.Collections.Concurrent;
#if LITENETLIB4MIRROR
using Mirror.LiteNetLib4Mirror;
#endif
#if IGNORANCE
#if !IGNORANCE_1_1 && !IGNORANCE_1_2
using IgnoranceTransport = Mirror.IgnoranceClassic;
#endif
#endif
using System.Reflection;

namespace NobleConnect.Mirror
{
    /// <summary>Adds relay, punchthrough, and port-forwarding support to the Mirror NetworkServer</summary>
    /// <remarks>
    /// Use the Listen method to start listening for incoming connections.
    /// </remarks>
    public class NobleServer
    {

        #region Public Properties

        /// <summary>You can use this to force the relays to be used for testing purposes.</summary>
        public static bool ForceRelayOnly {
            get {
                if (iceController == null) return _forceRelayOnly;
                return iceController.forceRelayOnly;
            }
            set {
                _forceRelayOnly = value;
                if (iceController == null)
                    throw new Exception("Can't set ForceRelay until the client has been initialized");
                iceController.forceRelayOnly = value;
            }
        }

        static IPEndPoint LocalHostEndPoint = null;
        /// <summary>This is the address that clients should connect to. It is assigned by the relay server.</summary>
        /// <remarks>
        /// Note that this is not the host's actual IP address, but one assigned to the host by the relay server.
        /// When clients connect to this address, Noble Connect will find the best possible connection and use it.
        /// This means that the client may actually end up connecting to an address on the local network, or an address
        /// on the router, or an address on the relay. But you don't need to worry about any of that, it is all
        /// handled for you internally.
        /// </remarks>
        public static IPEndPoint HostEndPoint {
            get {
                if (iceController != null)
                {
                    return iceController.sipRelayEndPoint;
                }
                else
                {
                    return LocalHostEndPoint;
                }
            }
            set {
                LocalHostEndPoint = value;
            }
        }

        /// <summary>Initial timeout before resending refresh messages. This is doubled for each failed resend.</summary>
        public static float allocationResendTimeout = .1f;

        /// <summary>Max number of times to try and resend refresh messages before giving up and shutting down the relay connection.</summary>
        /// <remarks>
        /// If refresh messages fail for 30 seconds the relay connection will be closed remotely regardless of these settings.
        /// </remarks>
        public static int maxAllocationResends = 8;

        #endregion Public Properties

        #region Internal Properties

        const string TRANSPORT_WARNING_MESSAGE = "You must download and install a UDP transport in order to use Mirror with NobleConnect.\n" +
                                               "I recommend LiteNetLib4Mirror: https://github.com/MichalPetryka/LiteNetLib4Mirror/releases";

        static Server baseServer;

        /// <summary>The Ice controller that determines the best connection method and sets up relay and punchthrough connections</summary>
        static Ice.Controller iceController;
        /// <summary>Store force relay so that we can pass it on to the iceController</summary>
        static private bool _forceRelayOnly;
        /// <summary>A method to call if something goes wrong like reaching ccu or bandwidth limit</summary>
        static Action onFatalError = null;
        /// <summary>Keeps track of which end point each NetworkConnection belongs to so that when they disconnect we know which Bridge to destroy</summary>
        static Dictionary<NetworkConnection, IPEndPoint> endPointByConnection = new Dictionary<NetworkConnection, IPEndPoint>();

        /// <summary>The ip address or domain of the ice server</summary>
        static string relayServerAddress;
        /// <summary>The port to use when connecting to the ice server</summary>
        static ushort relayServerPort;
        /// <summary>The username to use when connecting to the ice server</summary>
        static string relayUsername;
        /// <summary>The password to use when connecting to the ice server</summary>
        static string relayPassword;
        /// <summary>The origin to use when connecting to the ice server</summary>
        static string relayOrigin;

        #endregion Internal Properties

        #region Public Interface

        /// <summary>Initialize the server using NobleConnectSettings. The region used is determined by the Relay Server Address in the NobleConnectSettings.</summary>
        /// <remarks>\copydetails NobleClient::NobleClient(HostTopology,Action)</remarks>
        /// <param name="topo">The HostTopology to use for the NetworkClient. Must be the same on host and client.</param>
        /// <param name="onFatalError">A method to call if something goes horribly wrong.</param>
        public static void Init(GeographicRegion region = GeographicRegion.AUTO, Action onFatalError = null)
        {
            var settings = (NobleConnectSettings)Resources.Load("NobleConnectSettings", typeof(NobleConnectSettings));

            relayServerAddress = RegionURL.FromRegion(region);
            relayServerPort = settings.relayServerPort;

            if (!string.IsNullOrEmpty(settings.gameID))
            {
                if (settings.gameID.Length % 4 != 0) throw new System.ArgumentException("Game ID is wrong. Re-copy it from the Dashboard on the website.");
                string decodedGameID = Encoding.UTF8.GetString(Convert.FromBase64String(settings.gameID));
                string[] parts = decodedGameID.Split('\n');

                if (parts.Length == 3)
                {
                    relayOrigin = parts[0];
                    relayUsername = parts[1];
                    relayPassword = parts[2];
                }
            }

            Init(relayServerAddress, relayServerPort, relayUsername, relayPassword, relayOrigin, onFatalError);
        }

        /// <summary>
        /// Initialize the client using NobleConnectSettings but connect to specific relay server address.
        /// This method is useful for selecting the region to connect to at run time when starting the client.
        /// </summary>
        /// <remarks>\copydetails NobleClient::NobleClient(HostTopology,Action)</remarks>
        /// <param name="relayServerAddress">The url or ip of the relay server to connect to</param>
        /// <param name="topo">The HostTopology to use for the NetworkClient. Must be the same on host and client.</param>
        /// <param name="onPrepared">A method to call when the host has received their HostEndPoint from the relay server.</param>
        /// <param name="onFatalError">A method to call if something goes horribly wrong.</param>
        public static void Init(string relayServerAddress, Action onFatalError = null)
        {
            var settings = (NobleConnectSettings)Resources.Load("NobleConnectSettings", typeof(NobleConnectSettings));

            ushort relayServerPort = settings.relayServerPort;

            string relayUsername = null;
            string relayPassword = null;
            string relayOrigin = null;
            if (!string.IsNullOrEmpty(settings.gameID))
            {
                if (settings.gameID.Length % 4 != 0) throw new System.ArgumentException("Game ID is wrong. Re-copy it from the Dashboard on the website.");
                string decodedGameID = Encoding.UTF8.GetString(Convert.FromBase64String(settings.gameID));
                string[] parts = decodedGameID.Split('\n');

                if (parts.Length == 3)
                {
                    relayOrigin = parts[0];
                    relayUsername = parts[1];
                    relayPassword = parts[2];
                }
            }

            Init(relayServerAddress, relayServerPort, relayUsername, relayPassword, relayOrigin, onFatalError);
        }


        /// <summary>Initialize the NetworkServer and Ice.Controller</summary>
        /// <param name="relayServerAddress">The ip address or domain of the ice server</param>
        /// <param name="relayServerPort">The port to use when connecting to the ice server</param>
        /// <param name="relayUsername">The username to use when connecting to the ice server</param>
        /// <param name="relayPassword">The password to use when connecting to the ice server</param>
        /// <param name="relayOrigin">The origin to use when connecting to the ice server</param>
        /// <param name="topo">The HostTopology to use for the NetworkClient. Must be the same as on the host.</param>
        /// <param name="onFatalError">A method to call if something goes wrong like reaching ccu or bandwidth limit</param>
        public static void Init(string relayServerAddress, ushort relayServerPort,
                        string relayUsername, string relayPassword, string relayOrigin, 
                        Action onFatalError = null)
        {
            baseServer = new Server();
            NobleServer.onFatalError = onFatalError;
            iceController = new Ice.Controller(
                relayServerAddress, relayServerPort,
                relayUsername, relayPassword, relayOrigin,
                null, OnFatalError
            );
            iceController.forceRelayOnly = _forceRelayOnly;
            iceController.allocationRetransmissionTimeout = (int)(1000 * allocationResendTimeout);
            iceController.maxAllocationRetransmissionCount = maxAllocationResends;
            NetworkServer.RegisterHandler<ConnectMessage>(OnServerConnect);
            NetworkServer.RegisterHandler<DisconnectMessage>(OnServerDisconnect);
        }

        /// <summary>If you are using the NetworkServer directly you must call this method every frame.</summary>
        /// <remarks>
        /// The NobleNetworkManager and NobleNetworkLobbyManager handle this for you but you if you are
        /// using the NobleServer directly you must make sure to call this method every frame.
        /// </remarks>
        static public void Update()
        {
            if (iceController != null) iceController.Update();
        }

        /// <summary>Start listening for incoming connections</summary>
        /// <param name="maxPlayers">The maximum number of players</param>
        /// <param name="port">The port to listen on. Defaults to 0 which will use a random port</param>
        /// <param name="onPrepared">A method to call when the host has received their HostEndPoint from the relay server.</param>
        static public void Listen(int maxPlayers, int port = 0, Action<string, ushort> onPrepared = null)
        {
            // Store or generate the server port
            if (port == 0)
            {
                // Use a randomly generated endpoint
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                {
                    socket.Bind(new IPEndPoint(IPAddress.Any, 0));
                    port = (ushort)((IPEndPoint)socket.LocalEndPoint).Port;
                }
            }

            InitializeNobleServices(port, onPrepared);
            baseServer.SetLocalEndPoint((ushort)port);

            bool hasUDP = false;
            var transportType = Transport.activeTransport.GetType();
#if LITENETLIB4MIRROR
            if (transportType == typeof(LiteNetLib4MirrorTransport))
            {
                hasUDP = true;
                var liteNet = (LiteNetLib4MirrorTransport)Transport.activeTransport;
                liteNet.port = (ushort)port;
            }
#endif 
#if IGNORANCE
            if (transportType.IsSubclassOf(typeof(IgnoranceTransport)) ||
                transportType == typeof(IgnoranceTransport))
            {
                hasUDP = true;
                var ignorance = (IgnoranceTransport)Transport.activeTransport;
#if IGNORANCE_1_1 || IGNORANCE_1_2
                ignorance.port = (ushort)port;
#else
                ignorance.CommunicationPort = (ushort)port;
#endif
            }
            else if (transportType == typeof(IgnoranceThreaded))
            {
                hasUDP = true;
                var ignorance = (IgnoranceThreaded)Transport.activeTransport;
                ignorance.CommunicationPort = (ushort)port;
            }
#endif
            if (!hasUDP)
            {
                throw new Exception(TRANSPORT_WARNING_MESSAGE);
            }
            // Server Go!
            NetworkServer.Listen(10);
        }

        public static ushort GetTransportPort()
        {
            bool hasUDP = false;
            var transportType = Transport.activeTransport.GetType();
#if LITENETLIB4MIRROR
            if (transportType == typeof(LiteNetLib4MirrorTransport))
            {
                hasUDP = true;
                return (ushort)((LiteNetLib4MirrorTransport)Transport.activeTransport).port;
            }
#endif
#if IGNORANCE
            if (transportType.IsSubclassOf(typeof(IgnoranceTransport)) || 
                transportType == typeof(IgnoranceTransport))
            {
                hasUDP = true;
#if IGNORANCE_1_1 || IGNORANCE_1_2
                return (ushort)((IgnoranceTransport)Transport.activeTransport).port;
#else
                return (ushort)((IgnoranceTransport)Transport.activeTransport).CommunicationPort;
#endif
            }
            else if (transportType == typeof(IgnoranceThreaded))
            {
                hasUDP = true;
                return (ushort)((IgnoranceThreaded)Transport.activeTransport).CommunicationPort;
            }
#endif
            if (!hasUDP)
            {
                throw new Exception(TRANSPORT_WARNING_MESSAGE);
            }

            return 0;
        }

        public static void SetTransportPort(ushort port)
        {
            bool hasUDP = false;
            var transportType = Transport.activeTransport.GetType();
#if LITENETLIB4MIRROR
            if (transportType == typeof(LiteNetLib4MirrorTransport))
            {
                hasUDP = true;
                var liteNet = (LiteNetLib4MirrorTransport)Transport.activeTransport;
                liteNet.port = (ushort)port;
            }
#endif
#if IGNORANCE
            if (transportType.IsSubclassOf(typeof(IgnoranceTransport)) ||
                transportType == typeof(IgnoranceClassic))
            {
                hasUDP = true;
                var ignorance = (IgnoranceTransport)Transport.activeTransport;
#if IGNORANCE_1_1 || IGNORANCE_1_2
                ignorance.port = port;
#else
                ignorance.CommunicationPort = port;
#endif
            }
            else if (transportType == typeof(IgnoranceThreaded))
            {
                hasUDP = true;
                var ignorance = (IgnoranceThreaded)Transport.activeTransport;
                ignorance.CommunicationPort = port;
            }
#endif
            if (!hasUDP)
            {
                throw new Exception(TRANSPORT_WARNING_MESSAGE);
            }
        }

        static public void InitializeNobleServices(int hostPort, Action<string, ushort> onPrepared = null)
        {
            if (iceController == null)
            {
                Init(relayServerAddress, relayServerPort,
                    relayUsername, relayPassword, relayOrigin, onFatalError);
            }

            iceController.useSimpleAddressGathering = (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.Android) && !Application.isEditor;
            iceController.StartAsHost(onPrepared, OnCandidatePairSelected);
            // Set the local endpoint so our bridges will know where to send to / receive from
            baseServer.SetLocalEndPoint((ushort)hostPort);
        }

        /// <summary>
        /// Register a handler for a particular message type.
        /// <para>There are several system message types which you can add handlers for. You can also add your own message types.</para>
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <param name="handler">Function handler which will be invoked for when this message type is received.</param>
        /// <param name="requireAuthentication">True if the message requires an authenticated connection</param>
        public static void RegisterHandler(Action<NetworkConnection, ConnectMessage> handler, bool requireAuthentication = true)
        {
            NetworkServer.RegisterHandler<ConnectMessage>((conn, msg) => {
                OnServerConnect(conn, msg);
                handler(conn, msg);
            }, requireAuthentication);
        }

        /// <summary>
        /// Register a handler for a particular message type.
        /// <para>There are several system message types which you can add handlers for. You can also add your own message types.</para>
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <param name="handler">Function handler which will be invoked for when this message type is received.</param>
        /// <param name="requireAuthentication">True if the message requires an authenticated connection</param>
        public static void RegisterHandler(Action<NetworkConnection, DisconnectMessage> handler, bool requireAuthentication = true)
        {
            NetworkServer.RegisterHandler<DisconnectMessage>((conn, msg) => {
                handler(conn, msg);
                OnServerDisconnect(conn, msg);
            }, requireAuthentication);
        }

        static public void UnregisterHandler<T>() where T : MessageBase, new()
        {
            NetworkServer.UnregisterHandler<T>();
        }

        static public void ClearHandlers()
        {
            NetworkServer.ClearHandlers();
        }

        /// <summary>Clean up and free resources. Called automatically when garbage collected.</summary>
        /// <remarks>
        /// You shouldn't need to call this directly. It will be called automatically when an unused
        /// NobleServer is garbage collected or when shutting down the application.
        /// </remarks>
        /// <param name="disposing"></param>
        public static void Dispose()
        {
            if (baseServer != null)
            {
                baseServer.Dispose();
                baseServer = null;
            }
            if (iceController != null)
            {
                iceController.Dispose();
                iceController = null;
            }
        }

#endregion Public Interface

#region Handlers

        /// <summary>Called on the server when a client connects</summary>
        /// <remarks>
        /// We store some stuff here so that things can be cleaned up when the client disconnects.
        /// </remarks>
        /// <param name="message"></param>
        static private void OnServerConnect(NetworkConnection conn, ConnectMessage message)
        {
            OnServerConnect(conn);
        }

        static public void OnServerConnect(NetworkConnection conn)
        {
            if (conn.GetType() == typeof(NetworkConnection))
            {
                var transportType = Transport.activeTransport.GetType();
#if LITENETLIB4MIRROR
                if (transportType == typeof(LiteNetLib4MirrorTransport))
                {
                    var liteNet = (LiteNetLib4MirrorTransport)Transport.activeTransport;
                    endPointByConnection[conn] = LiteNetLib4MirrorServer.Peers[conn.connectionId].EndPoint;
                }
#endif
#if IGNORANCE
                if (transportType.IsSubclassOf(typeof(IgnoranceTransport)) ||
                    transportType == typeof(IgnoranceTransport))
                {
                    var ignorance = ((IgnoranceTransport)Transport.activeTransport);
#if IGNORANCE_1_1 || IGNORANCE_1_2
                    var knownConnIDToPeersField = typeof(IgnoranceTransport).GetField("knownConnIDToPeers", BindingFlags.NonPublic | BindingFlags.Instance);
#else
                    var knownConnIDToPeersField = typeof(IgnoranceTransport).GetField("ConnectionIDToPeers", BindingFlags.NonPublic | BindingFlags.Instance);
#endif
                    var knownConnIDToPeers = (Dictionary<int, ENet.Peer>)knownConnIDToPeersField.GetValue(ignorance);
                    ENet.Peer result;
                    if (knownConnIDToPeers.TryGetValue(conn.connectionId, out result))
                    {
                        endPointByConnection[conn] = new IPEndPoint(IPAddress.Parse(result.IP), result.Port);
                    }
                }
                else if (transportType == typeof(IgnoranceThreaded))
                {
                    var ignorance = ((IgnoranceThreaded)Transport.activeTransport);
                    var knownConnIDToPeersField = typeof(IgnoranceThreaded).GetField("ConnectionIDToPeers", BindingFlags.NonPublic | BindingFlags.Static);
                    var knownConnIDToPeers = (ConcurrentDictionary<int, ENet.Peer>)knownConnIDToPeersField.GetValue(ignorance);
                    ENet.Peer result;
                    if (knownConnIDToPeers.TryGetValue(conn.connectionId, out result))
                    {
                        endPointByConnection[conn] = new IPEndPoint(IPAddress.Parse(result.IP), result.Port);
                    }
                }
#endif
            }
        }

        /// <summary>Called on the server when a client disconnects</summary>
        /// <remarks>
        /// Some memory and ports are freed here.
        /// </remarks>
        /// <param name="message"></param>
        static private void OnServerDisconnect(NetworkConnection conn, DisconnectMessage message)
        {
            OnServerDisconnect(conn);
        }

        static public void OnServerDisconnect(NetworkConnection conn)
        {
            if (endPointByConnection.ContainsKey(conn))
            {
                IPEndPoint endPoint = endPointByConnection[conn];
                if (baseServer != null)
                {
                    baseServer.DestroyBridgeByClient(endPoint);
                }
                endPointByConnection.Remove(conn);
            }
            //conn.Dispose();
        }

        /// <summary>Called when Ice has selected a candidate pair to use for an incoming client connection.</summary>
        /// <remarks>
        /// Handles creating the Bridge between Ice and Mirror
        /// </remarks>
        /// <param name="selectedPair">The CandidatePair selected by Ice</param>
        /// <param name="allCandidatePairs">The full list of all potential CandidatePairs</param>
        static private void OnCandidatePairSelected(CandidatePair selectedPair, List<CandidatePair> allCandidatePairs)
        {
            var stunController = selectedPair.localCandidate.stunController;
            if (!baseServer.stunControllers.Contains(stunController))
            {
                baseServer.AddStunController(stunController);
            }

            bool isRelay = selectedPair.localCandidate.candidateType == CandidateType.RELAYED;
            var peerAddress = selectedPair.remoteCandidate.transportEndPoint.Address.ToString();
            var peerPort = (ushort)selectedPair.remoteCandidate.transportEndPoint.Port;
            baseServer.AddPeer(peerAddress, peerPort, isRelay, stunController);

            // Stop refreshing channels, and permissions for non-selected candidates
            var localCandidates = allCandidatePairs.ConvertAll(cp => cp.localCandidate).Distinct();
            foreach (var pair in allCandidatePairs)
            {
                if (pair == selectedPair) continue;
                if (pair.localCandidate.candidateType == CandidateType.RELAYED)
                {
                    var turn = pair.localCandidate.stunController.GetExtension<TurnExtension>();
                    turn.RevokePermission(pair.remoteCandidate.TransportAddress.ToString());
                    turn.RevokeChannel(pair.remoteCandidate.transportEndPoint);
                }
            }
        }

        /// <summary>Called when a fatal error occurs.</summary>
        /// <remarks>
        /// This usually means that the ccu or bandwidth limit has been exceeded. It will also
        /// happen if connection is lost to the relay server for some reason.
        /// </remarks>
        /// <param name="errorString">A string with more info about the error</param>
        static private void OnFatalError(string errorString)
        {
            Logger.Log("Shutting down because of fatal error: " + errorString, Logger.Level.Fatal);
            if (iceController != null)
            {
                iceController.Dispose();
            }
            iceController = null;
            NetworkServer.Shutdown();
            if (onFatalError != null) onFatalError();
        }

#endregion Handlers

#region Static NetworkServer Wrapper
        static public NetworkConnection localConnection { get { return NetworkServer.localConnection; } }
        static public Dictionary<int, NetworkConnectionToClient> connections { get { return NetworkServer.connections; } }
        public static bool dontListen { get { return NetworkServer.dontListen; } set { NetworkServer.dontListen = value; } }
        public static bool active { get { return NetworkServer.active; } }
        public static bool localClientActive { get { return NetworkServer.localClientActive; } }
        public static void Reset() { NetworkServer.Reset(); }
        public static void Shutdown() { NetworkServer.Shutdown(); }
        static public bool SendToAll<T>(T msg) where T : MessageBase, new()
        {
            return NetworkServer.SendToAll<T>(msg);
        }
        public static bool SendToReady<T>(NetworkIdentity identity, T msg, bool includeSelf = true, int channelId = Channels.DefaultReliable) where T : IMessageBase
        {
            return NetworkServer.SendToReady<T>(identity, msg, includeSelf, channelId);
        }
        public static bool SendToReady<T>(NetworkIdentity identity, T msg, int channelId = Channels.DefaultReliable) where T : IMessageBase
        {
            return SendToReady(identity, msg, true, channelId);
        }
        static public void DisconnectAll() { NetworkServer.DisconnectAll(); }
        static public void SendToClientOfPlayer<T>(NetworkIdentity player, T msg) where T : MessageBase, new()
        { 
            NetworkServer.SendToClientOfPlayer<T>(player, msg);
        }
        static public void SendToClient<T>(NetworkIdentity networkIdentity, T msg) where T : MessageBase, new()
        { 
            NetworkServer.SendToClientOfPlayer<T>(networkIdentity, msg);
        }
        static public bool ReplacePlayerForConnection(NetworkConnection conn, GameObject player, Guid assetId)
        {
            return NetworkServer.ReplacePlayerForConnection(conn, player, assetId);
        }
        static public bool ReplacePlayerForConnection(NetworkConnection conn, GameObject player)
        {
            return NetworkServer.ReplacePlayerForConnection(conn, player);
        }
        static public bool AddPlayerForConnection(NetworkConnection conn, GameObject player, Guid assetId)
        {
            return NetworkServer.AddPlayerForConnection(conn, player, assetId);
        }
        static public bool AddPlayerForConnection(NetworkConnection conn, GameObject player)
        {
            return NetworkServer.AddPlayerForConnection(conn, player);
        }
        static public void SetClientReady(NetworkConnection conn) { NetworkServer.SetClientReady(conn); }
        static public void SetAllClientsNotReady() { NetworkServer.SetAllClientsNotReady(); }
        static public void SetClientNotReady(NetworkConnection conn) { NetworkServer.SetClientNotReady(conn); }
        static public void DestroyPlayerForConnection(NetworkConnection conn) { NetworkServer.DestroyPlayerForConnection(conn); }
        static public void Spawn(GameObject obj) { NetworkServer.Spawn(obj); }
        static public void SpawnWithClientAuthority(GameObject obj, GameObject player) {  NetworkServer.Spawn(obj, player); }
        static public void SpawnWithClientAuthority(GameObject obj, NetworkConnection conn) {  NetworkServer.Spawn(obj, conn); }
        static public void SpawnWithClientAuthority(GameObject obj, Guid assetId, NetworkConnection conn) {  NetworkServer.Spawn(obj, assetId, conn); }
        static public void Spawn(GameObject obj, Guid assetId) { NetworkServer.Spawn(obj, assetId); }
        static public void Destroy(GameObject obj) { NetworkServer.Destroy(obj); }
        static public void UnSpawn(GameObject obj) { NetworkServer.UnSpawn(obj); }
        static public NetworkIdentity FindLocalObject(uint netId) { return NetworkIdentity.spawned[netId]; }
        static public bool SpawnObjects() { return NetworkServer.SpawnObjects(); }

#endregion
    }
}
