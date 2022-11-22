using Microsoft.Psi;
using SIPSorcery.Net;
using System.Net;
using Microsoft.Psi.Components;

namespace WebRTC
{
    public class WebRTConnectorConfiguration
    {
        public uint WebsocketPort { get; set; } = 80;
        public IPAddress WebsocketAddress { get; set; } = IPAddress.Any;
    }

    public class WebRTConnector : ISourceComponent
    {
        protected RTCPeerConnection? PeerConnection = null;
        protected WebRTCWebSocketClient WebRTClient; 
        protected CancellationToken CToken;

        public string Name { get; set; }

        /// <summary>
        /// Gets the emitter of groups detected.
        /// </summary>
        public Emitter<string> OutLog { get; private set; }

        public WebRTConnector(Pipeline parent, WebRTConnectorConfiguration configuration, string name = nameof(WebRTConnector), DeliveryPolicy? defaultDeliveryPolicy = null)
        {
            Name = name;
            OutLog = parent.CreateEmitter<string>(this, nameof(OutLog));
            CToken = new CancellationToken();
            WebRTClient = new WebRTCWebSocketClient("ws://" + configuration.WebsocketAddress.ToString() + ':' + configuration.WebsocketPort.ToString(), this.CreatePeerConnection);
        }

        public void Start(Action<DateTime> notifyCompletionTime)
        {
            WebRTClient.Start(CToken);
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
            try
            {
                OutLog.Post(message, DateTime.Now);
            }
            catch(Exception ex) 
            {
                //do nothing
            }
        }

        private Task<RTCPeerConnection> CreatePeerConnection()
        {
            PeerConnection = new RTCPeerConnection(null);

            PrepareActions(); 
            PeerConnection.onconnectionstatechange += (state) =>
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
