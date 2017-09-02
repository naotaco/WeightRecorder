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
using System.IO.Ports;

namespace WeightRecorder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Connect();
        }

        SerialPort serial = null;
        WeightHistory history = null;

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            Connect();
        }

        private void Connect()
        {
            if (serial != null && serial.IsOpen)
            {
                Console.WriteLine("It's already opened. Close first.");
                return;
            }
            serial = new SerialPort(ComPort.Text, 9600, Parity.None, 8, StopBits.One);
            serial.NewLine = Environment.NewLine;
            serial.DataReceived += Serial_DataReceived;
            serial.ErrorReceived += Serial_ErrorReceived;
            serial.DtrEnable = true;
            try
            {
                serial.Open();
            }
            catch (System.IO.IOException ex)
            {
                Console.WriteLine("Failed to connect: " + ex.Message);
                return;
            }

            Console.WriteLine("Connected");
        }

        private void Serial_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            Console.WriteLine("error received.");
        }

        private void Serial_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (history == null)
            {
                history = new WeightHistory();
            }

            var raw = serial?.ReadExisting();
            var lines = raw.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var data = line.Split(new char[] { ' ' });
                if (data.Length == 5)
                {
                    history.Add(
                        int.Parse(data[0], System.Globalization.NumberStyles.HexNumber),
                        int.Parse(data[2]));
                }
                else
                {
                    Console.WriteLine("Invalid length of data. " + data);
                }

            }
        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Disconnect.");
            serial?.Close();
            serial?.Dispose();
            serial = null;
        }

        private void DumpButton_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Dump");
            history?.Dump();
        }
    }
}
