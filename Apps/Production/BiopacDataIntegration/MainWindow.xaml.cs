using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Collections.Specialized.BitVector32;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace BiopacDataIntegration
{
    
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window, INotifyPropertyChanged
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

        private string acqFile = "D:\\Missive\\data\\ID0B2.txt";
        public string AcqFile
        {
            get => acqFile;
            set => SetProperty(ref acqFile, value);
        }
        public void DelegateMethodAcqFile(string acqFile)
        {
            AcqFile = acqFile;
        }

        private string dataset = "D:\\Stores\\InterviewRoom_456456456\\InterviewRoom_456456456.pds";
        public string Dataset
        {
            get => dataset;
            set => SetProperty(ref dataset, value);
        }
        public void DelegateMethodSession(string dataset)
        {
            Dataset = dataset;
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

        public MainWindow()
        {
            DataContext = this;
            InitializeComponent();
        }

        private void Browse_AcqFile(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDlg = new OpenFileDialog();
            fileDlg.Filter = "data|*.txt";
            fileDlg.Multiselect = false;
            DialogResult result = fileDlg.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
                acqFile = fileDlg.FileName;
        }

        private void Browse_Dataset(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDlg = new OpenFileDialog();
            fileDlg.Filter = "dataset|*.pds";
            fileDlg.Multiselect = false;
            DialogResult result = fileDlg.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
                dataset = fileDlg.FileName;
        }

        private void BtnStartClick(object sender, RoutedEventArgs e)
        {
            if ((acqFile.Length > 0 & dataset.Length > 0) == false)
            {
                Log = "Check input!";
                return;
            }
            GTLoader gTLoader = new GTLoader(Dataset);
            DateTime dateTimeReference = DateTime.UtcNow;
            if (!gTLoader.LoadReferenceTime(out dateTimeReference))
            {
                Log = "Failed to load reference time!";
                return;
            }
            gTLoader.Parse(AcqFile, dateTimeReference);
            Log = "Done!";
        }

        private void BtnQuitClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
