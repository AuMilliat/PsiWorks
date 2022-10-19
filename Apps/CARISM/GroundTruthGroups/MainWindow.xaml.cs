using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Psi.Remoting;
using Microsoft.Psi.Components;
using Microsoft.Psi;

namespace GroundTruthGroups
{
    //internal sealed class GroundTruthGroups : Microsoft.Psi.Components.ISourceComponent, IProducer<string>
    //{
    //    public Emitter<string> Out { get; private set; }

    //    public GroundTruthGroups(Pipeline pipeline)
    //    {
    //        //this.Out = pipeline.CreateEmitter<string>(this, ServerDataStream);
    //        //PAS BON ->
    //        this.Out = pipeline.CreateEmitter<string>(this, nameof(this.Out));
    //    }

    //    public void Start(Action<DateTime> notifyCompletionTime)
    //    {
    //        // notify that this is an infinite source component
    //        notifyCompletionTime(DateTime.MaxValue);
    //    }

    //    public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
    //    {
    //        notifyCompleted();
    //    }

    //    public void Post(string message)
    //    { 
    //        Out.Post(message, DateTime.Now);
    //    }
    //}
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, ISourceComponent
    {
        public Emitter<uint> Out1 { get; private set; }
        public Emitter<uint> Out2 { get; private set; }
        public Emitter<uint> Out3 { get; private set; }
        public Emitter<uint> Out4 { get; private set; }
        public Emitter<uint> Out5 { get; private set; }

        private Pipeline pipeline; 
        private RemoteExporter exporter;
        //private Microsoft.Psi.Data.PsiExporter store;

        public MainWindow()
        {
            pipeline = Pipeline.Create(enableDiagnostics: true);
            Out1 = pipeline.CreateEmitter<uint>(this, "Group1");
            Out2 = pipeline.CreateEmitter<uint>(this, "Group2");
            Out3 = pipeline.CreateEmitter<uint>(this, "Group3");
            Out4 = pipeline.CreateEmitter<uint>(this, "Group4");
            Out5 = pipeline.CreateEmitter<uint>(this, "Group5");

            exporter = new RemoteExporter(pipeline);
            exporter.Exporter.Write(Out1, "Group1");
            exporter.Exporter.Write(Out2, "Group2");
            exporter.Exporter.Write(Out3, "Group3");
            exporter.Exporter.Write(Out4, "Group4");
            exporter.Exporter.Write(Out5, "Group5");
            //store = PsiStore.Create(pipeline, "GroupsStoring", "F:\\Stores");
            //store.Write(Out1, "Group1");
            //store.Write(Out2, "Group2");
            //store.Write(Out3, "Group3");
            //store.Write(Out4, "Group4");
            //store.Write(Out5, "Group5");
            pipeline.RunAsync();
            InitializeComponent();
        }
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            pipeline.Dispose();
            base.OnClosing(e);
        }
        public void Start(Action<DateTime> notifyCompletionTime)
        {
            // notify that this is an infinite source component
            notifyCompletionTime(DateTime.MaxValue);
        }

        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            notifyCompleted();
        }
        private void RadioButton_Checked_1(object sender, RoutedEventArgs e)
        {
            var button = sender as RadioButton;
            if(button!= null)
            {
                uint number = (uint)Int32.Parse(button.Content.ToString());
                Out1.Post(number, DateTime.UtcNow);
            }
        }
        private void RadioButton_Checked_2(object sender, RoutedEventArgs e)
        {
            var button = sender as RadioButton;
            if (button!= null)
            {
                uint number = (uint)Int32.Parse(button.Content.ToString());
                Out2.Post(number, DateTime.UtcNow);
            }
        }
        private void RadioButton_Checked_3(object sender, RoutedEventArgs e)
        {
            var button = sender as RadioButton;
            if (button!= null)
            {
                uint number = (uint)Int32.Parse(button.Content.ToString());
                Out3.Post(number, DateTime.UtcNow);
            }
        }
        private void RadioButton_Checked_4(object sender, RoutedEventArgs e)
        {
            var button = sender as RadioButton;
            if (button!= null)
            {
                uint number = (uint)Int32.Parse(button.Content.ToString());
                Out4.Post(number, DateTime.UtcNow);
            }
        }
        private void RadioButton_Checked_5(object sender, RoutedEventArgs e)
        {
            var button = sender as RadioButton;
            if (button!= null)
            {
                uint number = (uint)Int32.Parse(button.Content.ToString());
                Out5.Post(number, DateTime.UtcNow);
            }
        }
    }
}
