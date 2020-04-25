using NobleConnect.Ice;
using NobleConnect.Turn;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using UnityEngine;
using Mirror;

#if LITENETLIB4MIRROR
using Mirror.LiteNetLib4Mirror;
#endif

#if IGNORANCE
#if !IGNORANCE_1_1 && !IGNORANCE_1_2
using IgnoranceTransport = Mirror.IgnoranceClassic;
#endif
#endif

using System.Collections;
using System.Net.Sockets;

namespace NobleConnect.Mirror
{
    /// <summary>Adds relay, punchthrough, and port-forwarding support to the Mirror NetworkClient</summary>
    /// <remarks>
    /// Use the Connect method to connect to a host.
    /// </remarks>
    public class NobleClient : NobleConnect.Client
    {

		#region Public Properties

        /// <summary>You can check this in OnClientConnect(), it will either be Direct, Punchthrough, or Relay.</summary>
        public ConnectionType latestConnectionType = ConnectionType.NONE;
        /// <summary>You can use this to force the relays to be used for testing purposes.</summary>
        public bool ForceRelayOnly {
            get {
                if (iceController == null) return _forceRelayOnly;
                return iceController.forceRelayOnly;
            }
            set {
                _forceRelayOnly = value;
                if (iceController == null)
                    throw new Exception("Can't get ForceRelay until the client has been initialized");
                iceController.forceRelayOnly = value;
            }
        }

        /// <summary>A convenient way to check if a connection is in progress</summary>
        public bool isConnecting = false;

        public static bool active {
            get {
                return NetworkClient.active;
            }
        }

		#endregion

		#region Internal Properties

        const string TRANSPORT_WARNING_MESSAGE = "You must download and install a UDP transport in order to use Mirror with NobleConnect.\n" +
                                                "I recommend LiteNetLib4Mirror: https://github.com/MichalPetryka/LiteNetLib4Mirror/releases";
        /// <summary>Store force relay so that we can pass it on to the iceController</summary>
        private bool _forceRelayOnly;
        /// <summary>The end of the bridge that the Mirror client connects to</summary>
        IPEndPoint bridgeEndPoint;
        /// <summary>The Ice controller that determines the best connection method and sets up relay and punchthrough connections</summary>
        Ice.Controller iceController;
        /// <summary>A method to call if something goes wrong like reaching ccu or bandwidth limit</summary>
        Action onFatalError = null;

        /// <summary>The ip address or domain of the ice server</summary>
        string relayServerAddress;
        /// <summary>The port to use when connecting to the ice server</summary>
        ushort relayServerPort;
        /// <summary>The username to use when connecting to the ice server</summary>
        string relayUsername;
        /// <summary>The password to use when connecting to the ice server</summary>
        string relayPassword;
        /// <summary>The origin to use when connecting to the ice server</summary>
        string relayOrigin;

        /// <summary>We store the HostEndPoint so that we know which session to clean up after the client connects.</summary>
        IPEndPoint hostNobleConnectAddress;
        
        uint originalTimeout, originalMultiplier;
        bool originalIsCustomTimeout;

        #endregion Internal Properties

        #region Public Interface

        /// <summary>Initialize the client using NobleConnectSettings. The region used is determined by the Relay Server Address in the NobleConnectSettings.</summary>
        /// <remarks>
        /// The default address is connect.noblewhale.com, which will automatically select the closest 
        /// server based on geographic region.
        /// 
        /// If you would like to connect to a specific region you can use one of the following urls:
        /// <pre>
        ///     us-east.connect.noblewhale.com - Eastern United States
        ///     us-west.connect.noblewhale.com - Western United States
        ///     eu.connect.noblewhale.com - Europe
        ///     ap.connect.noblewhale.com - Asia-Pacific
        ///     sa.connect.noblewhale.com - South Africa
        ///     hk.connect.noblewhale.com - Hong Kong
        /// </pre>
        /// 
        /// Note that region selection will ensure each player connects to the closest relay server, but it does not 
        /// prevent players from connecting across regions. If you want to prevent joining across regions you will 
        /// need to implement that separately (by filtering out unwanted regions during matchmaking for example).
        /// </remarks>
        /// <param name="topo">The HostTopology to use for the NetworkClient. Must be the same on host and client.</param>
        /// <param name="onFatalError">A method to call if something goes horribly wrong.</param>
        /// <param name="allocationResendTimeout">Initial timeout before resending refresh messages. This is doubled for each failed resend.</param>
        /// <param name="maxAllocationResends">Max number of times to try and resend refresh messages before giving up and shutting down the relay connection. If refresh messages fail for 30 seconds the relay connection will be closed remotely regardless of these settings.</param>
        public NobleClient(GeographicRegion region = GeographicRegion.AUTO, Action onFatalError = null, float allocationResendTimeout = .1f, int maxAllocationResends = 8) : base()
        {
            var settings = (NobleConnectSettings)Resources.Load("NobleConnectSettings", typeof(NobleConnectSettings));

            relayServerAddress = RegionURL.FromRegion(region);
            relayServerPort = settings.relayServerPort;

            if (!string.IsNullOrEmpty(settings.gameID))
            {
                string decodedGameID = Encoding.UTF8.GetString(Convert.FromBase64String(settings.gameID));
                string[] parts = decodedGameID.Split('\n');

                if (parts.Length == 3)
                {
                    relayUsername = parts[1];
                    relayPassword = parts[2];
                    relayOrigin = parts[0];
                }
            }
            Init(relayServerAddress, relayServerPort, relayUsername, relayPassword, relayOrigin, onFatalError, allocationResendTimeout, maxAllocationResends);
        }

        public NobleClient() : base()
        {

        }

        /// <summary>
        /// Initialize the client using NobleConnectSettings but connect to specific relay server address.
        /// This method is useful for selecting the region to connect to at run time when starting the client.
        /// </summary>
        /// <remarks>\copydetails NobleClient::NobleClient(HostTopology,Action)</remarks>
        /// <param name="relayServerAddress">The url or ip of the relay server to connect to</param>
        /// <param name="topo">The HostTopology to use for the NetworkClient. Must be the same on host and client.</param>
        /// <param name="onFatalError">A method to call if something goes horribly wrong.</param>
        /// <param name="allocationResendTimeout">Initial timeout before resending refresh messages. This is doubled for each failed resend.</param>
        /// <param name="maxAllocationResends">Max number of times to try and resend refresh messages before giving up and shutting down the relay connection. If refresh messages fail for 30 seconds the relay connection will be closed remotely regardless of these settings.</param>
        public NobleClient(string relayServerAddress, Action onFatalError = null, float allocationResendTimeout = .1f, int maxAllocationResends = 8)
        {
            var settings = (NobleConnectSettings)Resources.Load("NobleConnectSettings", typeof(NobleConnectSettings));

            relayServerPort = settings.relayServerPort;

            if (!string.IsNullOrEmpty(settings.gameID))
            {
                string decodedGameID = Encoding.UTF8.GetString(Convert.FromBase64String(settings.gameID));
                string[] parts = decodedGameID.Split('\n');

                if (parts.Length == 3)
                {
                    relayUsername = parts[1];
                    relayPassword = parts[2];
                    relayOrigin = parts[0];
                }
            }
            Init(relayServerAddress, relayServerPort, relayUsername, relayPassword, relayOrigin, onFatalError, allocationResendTimeout, maxAllocationResends);
        }

        /// <summary>Gather candidates but don't try and connect yet</summary>
        /// <remarks>
        /// This is used when initializing a client early before connecting. Getting this
        /// out of the way earlier can make the actual connection seem quicker.
        /// </remarks>
        public void Init()
        {
            iceController.useSimpleAddressGathering = (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.Android) && !Application.isEditor;
            iceController.GatherLocalCandidates();
        }

        /// <summary>If you are using the NetworkClient directly you must call this method every frame.</summary>
        /// <remarks>
        /// The NobleNetworkManager and NobleNetworkLobbyManager handle this for you but you if you are
        /// using the NobleClient directly you must make sure to call this method every frame.
        /// </remarks>
        override public void Update()
        {
			base.Update();
            if (iceController != null) iceController.Update();
        }

        /// <summary>Connect to the provided host ip and port</summary>
        /// <remarks>
        /// Note that the host address used here should be the one provided to the host by 
        /// the relay server, not the actual ip of the host's computer. You can get this 
        /// address on the host from Server.HostEndPoint.
        /// </remarks>
        /// <param name="hostIP">The IP of the server's HostEndPoint</param>
        /// <param name="hostPort">The port of the server's HostEndPoint</param>
        /// <param name="topo">The HostTopology to use for the NetworkServer.</param>
        public void Connect(string hostIP, ushort hostPort, bool isLANOnly = false)
        {
            Connect(new IPEndPoint(IPAddress.Parse(hostIP), hostPort), isLANOnly);
        }

        /// <summary>Connect to the provided HostEndPoint</summary>
        /// <remarks>
        /// Note that the host address used here should be the one provided to the host by 
        /// the relay server, not the actual ip of the host's computer. You can get this 
        /// address on the host from Server.HostEndPoint.
        /// </remarks>
        /// <param name="hostEndPoint">The HostEndPoint of the server to connect to</param>
        /// <param name="hostPort">The port of the server's HostEndPoint</param>
        /// <param name="topo">The HostTopology to use for the NetworkServer.</param>
        public void Connect(IPEndPoint hostEndPoint, bool isLANOnly = false)
        {
            if (isConnecting || isConnected) return;
            isConnecting = true;
            this.hostNobleConnectAddress = hostEndPoint;

            if (isLANOnly)
            {
                SetConnectPort((ushort)hostEndPoint.Port);
                NetworkClient.Connect(hostEndPoint.Address.ToString());
            }
            else
            {
                if (iceController == null)
                {
                    Init(relayServerAddress, relayServerPort,
                        relayUsername, relayPassword, relayOrigin, onFatalError);
                }
                iceController.StartAsClient(hostEndPoint, (cp, pairs) => OnCandidatePairSelectedUnsafe(cp, pairs));
            }
        }

        public void SetConnectPort(ushort port)
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

        /// <summary>Shut down the client and clean everything up.</summary>
        /// <remarks>
        /// You can call this method if you are totally done with a client and don't plan
        /// on using it to connect again.
        /// </remarks>
        public void Shutdown()
        {
            if (iceController != null)
            {
                lock (iceController.candidateStunControllersLock)
                {
                    foreach (var stunController in iceController.candidateStunControllers)
                    {
                        var turnExtension = stunController.GetExtension<TurnExtension>();
                        turnExtension.RevokeAllPermissions();
                    }
                }

                iceController.Dispose();
                iceController = null;
            }

            DestroyBridges();

            NetworkClient.Shutdown();
        }

        /// <summary>Clean up and free resources. Called automatically when garbage collected.</summary>
        /// <remarks>
        /// You shouldn't need to call this directly. It will be called automatically when an unused
        /// NobleClient is garbage collected or when shutting down the application.
        /// </remarks>
        /// <param name="disposing"></param>
        override protected void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                NetworkClient.Shutdown();
                if (iceController != null) iceController.Dispose();
            }
            isConnecting = false;
        }

        #endregion Public Interface

        #region Internal Methods

        /// <summary>Initialize the NetworkClient and Ice.Controller</summary>
        /// <param name="relayServerAddress">The ip address or domain of the ice server</param>
        /// <param name="relayServerPort">The port to use when connecting to the ice server</param>
        /// <param name="relayUsername">The username to use when connecting to the ice server</param>
        /// <param name="relayPassword">The password to use when connecting to the ice server</param>
        /// <param name="relayOrigin">The origin to use when connecting to the ice server</param>
        /// <param name="topo">The HostTopology to use for the NetworkClient. Must be the same as on the host.</param>
        /// <param name="onFatalError">A method to call if something goes wrong like reaching ccu or bandwidth limit</param>
        /// <param name="allocationResendTimeout">Initial timeout before resending refresh messages. This is doubled for each failed resend.</param>
        /// <param name="maxAllocationResends">Max number of times to try and resend refresh messages before giving up and shutting down the relay connection. If refresh messages fail for 30 seconds the relay connection will be closed remotely regardless of these settings.</param>
        private void Init(string relayServerAddress, ushort relayServerPort,
                        string relayUsername, string relayPassword, string relayOrigin,
                        Action onFatalError = null, float allocationResendTimeout = .1f, int maxAllocationResends = 8)
        {
            NetworkClient.RegisterHandler<ConnectMessage>(OnClientConnect, false);
            NetworkClient.RegisterHandler<DisconnectMessage>(OnClientDisconnect, false);

            this.onFatalError = onFatalError;

            iceController = new Ice.Controller(
                relayServerAddress, relayServerPort,
                relayUsername, relayPassword, relayOrigin,
				FinishDisconnecting, OnFatalError
            );
            iceController.forceRelayOnly = _forceRelayOnly;
            iceController.maxAllocationRetransmissionCount = maxAllocationResends;
            iceController.allocationRetransmissionTimeout = (int)(allocationResendTimeout*1000);
        }

        /// <summary>Finish the disconnect process and clean everything up</summary>
        private void FinishDisconnecting()
        {
            var iceControllerToDispose = iceController;
            iceController = null;

			NetworkClient.Disconnect();

            isConnecting = false;

            if (iceControllerToDispose != null)
            {
                lock (iceControllerToDispose.candidateStunControllersLock)
                {
                    foreach (var stunController in iceControllerToDispose.candidateStunControllers)
                    {
                        var turnExtension = stunController.GetExtension<TurnExtension>();
                        turnExtension.RevokeAllPermissions();
                        turnExtension.RevokeAllocation();

                        stunController.openTransactions.CancelAllTransactions();
                    }
                }
            }

            DestroyBridges();

            Timer timer = null;
			if (iceControllerToDispose != null)
			{
				timer = new Timer(
					(ob) => {
						if (iceControllerToDispose != null)
						{
							iceControllerToDispose.Dispose();
							iceControllerToDispose = null;
						}
					// Disposing of a timer in the timer thread via a hoisted local can't be good right?
					timer.Dispose();
						timer = null;
					},
					null,
					20,
					Timeout.Infinite
				);
			}
        }

		#endregion Internal Methods

		#region Handlers

        /// <summary>Called when a fatal error occurs.</summary>
        /// <remarks>
        /// This usually means that the ccu or bandwidth limit has been exceeded. It will also
        /// happen if connection is lost to the relay server for some reason.
        /// </remarks>
        /// <param name="errorString">A string with more info about the error</param>
        private void OnFatalError(string errorString)
        {
            Logger.Log("Shutting down because of a fatal error: " + errorString, Logger.Level.Fatal);
            FinishDisconnecting();
            if (onFatalError != null) onFatalError();
        }

        /// <summary>Called when Ice has selected a candidate pair to use to connect to the host. This is not called on the main thread.</summary>
        /// <remarks>
        /// Handles cleaning up Ice things
        /// </remarks>
        /// <param name="selectedPair">The CandidatePair selected by Ice</param>
        /// <param name="allCandidatePairs">The full list of all potential CandidatePairs</param>
        private void OnCandidatePairSelectedUnsafe(CandidatePair selectedPair, List<CandidatePair> allCandidatePairs)
        {
            try
            {
                Logger.Log("Connecting via route: " + selectedPair.ToString(), Logger.Level.Debug);

                // Determine the type of connection for the user accessible variable latestConnectionType.
                if (selectedPair.localCandidate.candidateType == CandidateType.RELAYED ||
                    selectedPair.remoteCandidate.candidateType == CandidateType.RELAYED)
                {
                    latestConnectionType = ConnectionType.RELAY;
                }
                else if (selectedPair.localCandidate.candidateType == CandidateType.HOST ||
                    selectedPair.remoteCandidate.candidateType == CandidateType.HOST)
                {
                    latestConnectionType = ConnectionType.DIRECT;
                }
                else
                {
                    latestConnectionType = ConnectionType.PUNCHTHROUGH;
                }

                // Stop refreshing channels, and permissions for non-selected candidates
                foreach (var pair in allCandidatePairs)
                {
                    if (pair == selectedPair) continue;
                    pair.localCandidate.stunController.RevokeBinding(pair.remoteCandidate.transportEndPoint);
                    if (pair.localCandidate.candidateType == CandidateType.RELAYED)
                    {
                        var turn = pair.localCandidate.stunController.GetExtension<TurnExtension>();
                        turn.RevokePermission(pair.remoteCandidate.TransportAddress.ToString());
                        turn.RevokeChannel(pair.remoteCandidate.transportEndPoint);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            // Execute OnCandidatePairSelected on the main thread
            iceController.actionQueue.Enqueue(() => OnCandidatePairSelected(selectedPair));
        }

        /// <summary>Called when Ice has selected a candidate pair to use to connect to the host.</summary>
        /// <remarks>
        /// Handles creating the Bridge between Ice and Mirror as well as initializing the Mirror connection.
        /// </remarks>
        /// <param name="selectedPair">The CandidatePair selected by Ice</param>
        private void OnCandidatePairSelected(CandidatePair selectedPair)
        {
            string hostIP = selectedPair.remoteCandidate.TransportAddress.ToString();
            ushort hostPort = selectedPair.remoteCandidate.TransportPort;
            bool isRelay = selectedPair.localCandidate.candidateType == CandidateType.RELAYED;

            bridgeEndPoint = CreateBridge(selectedPair.localCandidate.stunController, hostIP, hostPort, isRelay);
            AddStunController(selectedPair.localCandidate.stunController);

            SetConnectPort((ushort)bridgeEndPoint.Port);
            NetworkClient.Connect(bridgeEndPoint.Address.ToString());
        }

        /// <summary>Called on the client upon succesfully connecting to a host</summary>
        /// <remarks>
        /// We clean some ice stuff up here.
        /// </remarks>
        /// <param name="message"></param>
        private void OnClientConnect(NetworkConnection conn, ConnectMessage message)
        {
            isConnecting = false;
            if (iceController != null)
            {
                iceController.EndSession(hostNobleConnectAddress);
            }
        }

        /// <summary>Called on the client upon disconnecting from a host</summary>
        /// <remarks>
        /// Some memory and ports are freed here.
        /// </remarks>
        /// <param name="message"></param>
        private void OnClientDisconnect(NetworkConnection conn, DisconnectMessage message)
        {
            FinishDisconnecting();
        }

		#endregion Handlers

		#region Mirror NetworkClient public interface

		#if !DOXYGEN_SHOULD_SKIP_THIS
        /// The rest of this is just a wrapper for Mirror's NetworkClient
        public string serverIp { get { return NetworkClient.serverIp; } }
        public NetworkConnection connection { get { return NetworkClient.connection; } }
        public bool isConnected { get { return NetworkClient.isConnected; } }
        public bool Send<T>(T msg) where T : IMessageBase
        {
            return NetworkClient.Send<T>(msg);
        }
           

        public void RegisterHandler<T>(Action<NetworkConnection, T> handler, bool requireAuthentication = true) where T : IMessageBase, new()
        {
            int msgType = MessagePacker.GetId<T>();
            if (typeof(T) == typeof(ConnectMessage))
            {
                NetworkClient.RegisterHandler<ConnectMessage>((conn, msg) => {
                        OnClientConnect(conn, msg);
                        handler(conn, (T)(msg as IMessageBase));
                    },
                    requireAuthentication
                );
            }
            else if (typeof(T) == typeof(DisconnectMessage))
            {
                NetworkClient.RegisterHandler<DisconnectMessage>((conn, msg) => {
                        OnClientDisconnect(conn, msg);
                        handler(conn, (T)(msg as IMessageBase));
                    },
                    requireAuthentication
                );
            }
            else
            {
                NetworkClient.RegisterHandler<T>(handler, requireAuthentication);
            }
        }

        public void UnregisterHandler<T>() where T : MessageBase, new()
        {
            NetworkClient.UnregisterHandler<T>();
        }
		#endif

		#endregion
    }
}
