using Microsoft.Psi;
using SIPSorcery.Media;
using SIPSorcery.Net;
using SIPSorceryMedia.Encoders;
using Microsoft.Psi.Imaging;
using Microsoft.Psi.Audio;
using WebSocketSharp.Server;
using System.Net;
using SIPSorceryMedia.Abstractions;
using DirectShowLib;
using Microsoft.Psi.Components;
using System.Xml.Linq;

namespace WebRTC
{
    public class WebRTConnectorConfiguration
    {
        public uint WebsocketPort { get; set; } = 80;
        public IPAddress WebsocketAddress { get; set; } = IPAddress.Any;
        public List<RTCIceServer> IceServers = new List<RTCIceServer> { new RTCIceServer { urls = "" } };
    }

    public class WebRTConnector : ISourceComponent
    {
        protected RTCPeerConnection PeerConnection;
        protected WebRTConnectorConfiguration Configuration;

        public string Name { get; set; }

        /// <summary>
        /// Gets the emitter of groups detected.
        /// </summary>
        public Emitter<string> OutLog { get; private set; }

        public WebRTConnector(Pipeline parent, WebRTConnectorConfiguration configuration, string name = nameof(WebRTConnector), DeliveryPolicy? defaultDeliveryPolicy = null)
        {
            Name = name;
            OutLog = parent.CreateEmitter<string>(this, nameof(OutLog));
            Configuration  = configuration;
        }

        public void Start(Action<DateTime> notifyCompletionTime)
        {
            StartWebsockectHandleShake();
            notifyCompletionTime(DateTime.MaxValue);
        }

        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            if (PeerConnection != null)
            {
                PeerConnection.Close("Stoping PSI");
                PeerConnection.Dispose();
            }
            notifyCompleted();
        }

        private void log(string message)
        {
            Console.WriteLine(message);
            OutLog.Post(message, DateTime.Now);
        }

        private void StartWebsockectHandleShake()
        {
            log("Starting web socket server...");
            var webSocketServer = new WebSocketServer(Configuration.WebsocketAddress, (int)Configuration.WebsocketPort);
            webSocketServer.AddWebSocketService<WebRTCWebSocketPeer>("/", (peer) => peer.CreatePeerConnection = CreatePeerConnection);
            webSocketServer.Start();
            log($"Waiting for web socket connections on {webSocketServer.Address}:{webSocketServer.Port}...");
        }

        private Task<RTCPeerConnection> CreatePeerConnection()
        {
            RTCConfiguration config = new RTCConfiguration
            {
                iceServers = Configuration.IceServers,
                X_UseRtpFeedbackProfile = true
            };
            PeerConnection = new RTCPeerConnection(config);


            PrepareActions(); 
            PeerConnection.onconnectionstatechange += async (state) =>
            {
                log($"Peer connection state change to {state}.");
                if (state == RTCPeerConnectionState.connected)
                {
                }
                else if (state == RTCPeerConnectionState.failed)
                {
                    PeerConnection.Close("ice disconnection");
                }
            };

            // Diagnostics.
            PeerConnection.OnReceiveReport += PeerConnection_OnReceiveReport;
            PeerConnection.OnSendReport += PeerConnection_OnSendReport;
            PeerConnection.GetRtpChannel().OnStunMessageReceived += WebRTConnector_OnStunMessageReceived;
            PeerConnection.oniceconnectionstatechange += PeerConnection_oniceconnectionstatechange;

            return Task.FromResult(PeerConnection);
        }

        private void PeerConnection_OnReceiveReport(IPEndPoint re, SDPMediaTypesEnum media, RTCPCompoundPacket rr)
        {
            log($"RTCP Receive for {media} from {re}\n{rr.GetDebugSummary()}");
        }

        private void PeerConnection_OnSendReport(SDPMediaTypesEnum media, RTCPCompoundPacket sr)
        {
            log($"RTCP Send for {media}\n{sr.GetDebugSummary()}");
        }

        private void WebRTConnector_OnStunMessageReceived(STUNMessage msg, IPEndPoint ep, bool isRelay)
        {
            log($"STUN {msg.Header.MessageType} received from {ep}.");
        }
        
        private void PeerConnection_oniceconnectionstatechange(RTCIceConnectionState state)
        {
           log($"ICE connection state change to {state}.");
        }

        protected virtual void PrepareActions()
        {}

    }
}
