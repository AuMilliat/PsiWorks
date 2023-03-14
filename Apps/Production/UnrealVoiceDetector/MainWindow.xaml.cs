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
            InitializeComponent();
            // Enabling diagnotstics !!!
            pipeline = Pipeline.Create("WpfPipeline", enableDiagnostics: true);
            unrealConnector = new UnrealRemoteConnector(pipeline);
        }
        private void PipelineSetup()
        {
            // Create the biopac component
            Biopac.Biopac biopac = new Biopac.Biopac(this.pipeline);

            // Create the audio capture component
            var audio = new AudioCapture(this.pipeline, WaveFormat.Create16kHz1Channel16BitPcm());

            // Create an voice extractor component and pipe the audio to it
            SystemVoiceActivityDetectorConfiguration systemVoiceActivityDetectorConfiguration = new SystemVoiceActivityDetectorConfiguration();
            systemVoiceActivityDetectorConfiguration.Language = "fr-FR";
            SystemVoiceActivityDetector systemVoiceActivityDetector = new SystemVoiceActivityDetector(pipeline, systemVoiceActivityDetectorConfiguration);
            audio.Out.PipeTo(systemVoiceActivityDetector);

            // Create a timer component that produces a message every second
            var timer = Timers.Timer(pipeline, TimeSpan.FromSeconds(1));

            // Update every second the state of the speaking detection 
            timer.Out.Join(systemVoiceActivityDetector.Out).Do(
                value =>
                {
                    log = "LogEnergy :" + value;
                    // Send to unreal only if a speech is detected.
                    if (value.Item2) 
                    {
                        UnrealActionRequest req = new UnrealActionRequest("BP_Vivian_2", "/Game/Levels/UEDPIE_0_MainLevel.MainLevel:PersistentLevel.", "Start Welcome");
                        unrealConnector.Send(req);
                    }
                },
                DeliveryPolicy.LatestMessage);

            if ((Audio.IsChecked | VoiceDetection.IsChecked | UnrealRequest.IsChecked | Biopac.IsChecked) == true)
            {
                // Create a new Dataset
                var dataset = new Dataset("InterviewRoom");
                var session = dataset.CreateSession(Session);
                if (Audio.IsChecked == true)
                {
                    var micro = PsiStore.Create(pipeline, "Microphone", "D:\\Stores\\InterviewRoom");
                    var partition = session.AddPsiStorePartition("Microphone", "D:\\Stores\\InterviewRoom", "Audio");
                    micro.Write(audio.Out, "Microphone");
                }
                if (VoiceDetection.IsChecked == true)
                {
                    var voice = PsiStore.Create(pipeline, "VoiceDetecion", "D:\\Stores\\InterviewRoom");
                    var partition = session.AddPsiStorePartition("Microphone", "D:\\Stores\\InterviewRoom", "Audio");
                    voice.Write(systemVoiceActivityDetector.Out, "VoiceDetecion");
                }
                if (UnrealRequest.IsChecked == true)
                {
                    var unreal = PsiStore.Create(pipeline, "UnrealRequest", "D:\\Stores\\InterviewRoom");
                    var partition = session.AddPsiStorePartition("UnrealRequest", "D:\\Stores\\InterviewRoom", "Audio");
                    unreal.Write(unrealConnector.OutActionRequest, "UnrealRequest");
                }
                if (Biopac.IsChecked == true)
                {
                    var storeBiopac = PsiStore.Create(pipeline, "Biopac", "D:\\Stores");
                    var partition = session.AddPsiStorePartition("HR", "D:\\Stores\\InterviewRoom", "Biopac");
                    storeBiopac.Write(biopac.Out, "HR");
                }
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
