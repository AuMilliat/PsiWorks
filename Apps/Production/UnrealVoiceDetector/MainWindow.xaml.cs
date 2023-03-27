using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using RemoteConnectors;
using Microsoft.Psi;
using Microsoft.Psi.Audio;
using Microsoft.Psi.Speech;
using Biopac;
using Microsoft.Psi.Data;

namespace UnrealVoiceDetector
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (!System.Collections.Generic.EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        private string state = "Hello";
        public string State
        {
            get => state;
            set => SetProperty(ref state, value);
        }
        public void DelegateMethodStatus(string status)
        {
            State = status;
        }

        private string path = "D:\\Stores\\InterviewRoom_";
        public string Path
        {
            get => path;
            set => SetProperty(ref path, value);
        }
        public void DelegateMethodPath(string path)
        {
            Path = path;
        }

        private string session = "Session";
        public string Session
        {
            get => session;
            set => SetProperty(ref session, value);
        }
        public void DelegateMethodSession(string session)
        {
            Session = session;
        }

        private int actionDelay = 20;
        public int ActionDelay
        {
            get => actionDelay;
            set => SetProperty(ref actionDelay, value);
        }
        public void DelegateMethodActionDelay(int actionDelay)
        {
            ActionDelay = actionDelay;
        }

        private string httpRequestUrl = "http://127.0.0.1:8888/psi/";
        public string HttpRequestUrl
        {
            get => httpRequestUrl;
            set => SetProperty(ref httpRequestUrl, value);
        }
        public void DelegateMethodHttpRequestURL(string httpRequestURL)
        {
            HttpRequestUrl = httpRequestURL;
        }

        private string log = "";
        public string Log
        {
            get => log;
            set => SetProperty(ref log, value);
        }
        public void DelegateMethodLog(string log)
        {
            Log = log;
        }

        private Pipeline pipeline;
        private UnrealRemoteConnector unrealConnector;

        public MainWindow()
        {
            DataContext = this;
            // Enabling diagnotstics !!!
            pipeline = Pipeline.Create("WpfPipeline", enableDiagnostics: true);
            UnrealRemoteConnectorConfiguration config = new UnrealRemoteConnectorConfiguration();
            config.Address = "http://127.0.0.1:30010/remote/object/call";
            unrealConnector = new UnrealRemoteConnector(pipeline, config);
            InitializeComponent();
        }
        private void PipelineSetup()
        {
            // Create the audio capture component
            var audio = new AudioCapture(pipeline, WaveFormat.Create16kHz1Channel16BitPcm());

            // Create an voice extractor component and pipe the audio to it
            SystemVoiceActivityDetectorConfiguration systemVoiceActivityDetectorConfiguration = new SystemVoiceActivityDetectorConfiguration();
            systemVoiceActivityDetectorConfiguration.Language = "fr-fr";
            SystemVoiceActivityDetector systemVoiceActivityDetector = new SystemVoiceActivityDetector(pipeline, systemVoiceActivityDetectorConfiguration);
            audio.Out.PipeTo(systemVoiceActivityDetector);

            // Create a timer component that produces a message every second
            var timer = Timers.Timer(pipeline, TimeSpan.FromSeconds(actionDelay));

            // Update every second the state of the speaking detection 
            timer.Out.Pair(systemVoiceActivityDetector.Out).Do(
                value =>
                {
                    // Send to unreal only if a speech is detected.
                    if (value.Item2) 
                    {
                        UnrealActionRequest req = new UnrealActionRequest("BP_Cooper_C_1", "/Game/Levels/UEDPIE_0_InterviewMap.InterviewMap:PersistentLevel.", "HocherLaTete", "Target:" + ((int)value.Item1.TotalMilliseconds%3).ToString());
                        unrealConnector.Send(req);
                    }
                },
                DeliveryPolicy.LatestMessage);

            if ((Audio.IsChecked | VoiceDetection.IsChecked | UnrealRequest.IsChecked | Biopac.IsChecked) == true)
            {
                // Create a new Dataset
                var dataset = new Dataset("InterviewRoom_" + Session);
                var session = dataset.CreateSession(Session);
                if (Audio.IsChecked == true)
                {
                    var micro = PsiStore.Create(pipeline, "Microphone", Path + Session);
                    var partition = session.AddPsiStorePartition("Microphone", Path + Session, "Microphone");
                    micro.Write(audio.Out, "Microphone");
                }
                if (VoiceDetection.IsChecked == true)
                {
                    var voice = PsiStore.Create(pipeline, "VoiceDetecion", Path + Session);
                    var partition = session.AddPsiStorePartition("VoiceDetecion",Path + Session, "VoiceDetecion");
                    voice.Write(systemVoiceActivityDetector.Out, "VoiceDetecion");
                }
                if (UnrealRequest.IsChecked == true)
                {
                    var unreal = PsiStore.Create(pipeline, "UnrealRequest",Path + Session);
                    var partition = session.AddPsiStorePartition("UnrealRequest",Path + Session, "UnrealRequest");
                    unreal.Write(unrealConnector.OutActionRequest, "UnrealRequest");
                    var animationID = unrealConnector.OutActionRequest.Select(req => req.Parameters.Substring(7));
                    unreal.Write(animationID, "AnimationID");
                }
                if (Biopac.IsChecked == true)
                {
                    // Create the biopac component
                    Biopac.Biopac biopac = new Biopac.Biopac(pipeline);
                    var storeBiopac = PsiStore.Create(pipeline, "Biopac",Path + Session);
                    var partition = session.AddPsiStorePartition("Biopac",Path + Session, "Biopac");
                    storeBiopac.Write(biopac.Out, "Biopac");
                }
                if(HttpRequest.IsChecked == true)
                {
                    HttpListenerConfiguration httpListenerConfiguration = new HttpListenerConfiguration();
                    httpListenerConfiguration.Prefixes.Add(HttpRequestUrl);
                    HttpListener httpListener = new HttpListener(pipeline, httpListenerConfiguration);
                    var http = PsiStore.Create(pipeline, "HttpRequest", Path + Session);
                    var partition = session.AddPsiStorePartition("HttpRequest", Path + Session, "HttpRequest");
                    http.Write(httpListener.Out, "HttpRequest");
                }
                dataset.SaveAs(Path + Session + "\\InterviewRoom_" + Session +".pds");
            }

            //State = "Waiting for synch server";
            pipeline.RunAsync();
            State = "Running";
        }

        private void StopPipeline()
        {
            // Stop correctly the pipeline.
            State = "Stopping";
            pipeline.Dispose();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            StopPipeline();
            base.OnClosing(e);
        }

        private void BtnQuitClick(object sender, RoutedEventArgs e)
        {
            StopPipeline();
            Close();
        }

        private void BtnStartClick(object sender, RoutedEventArgs e)
        {
            State = "Initializing";
            PipelineSetup();
        }
    }
}
