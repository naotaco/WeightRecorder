using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Windows;
using System.Windows.Controls;
using System.Management;
using System.Text.RegularExpressions;

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
            UpdatePortList();
        }

        SerialPort serial = null;
        WeightHistory history = null;

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            Connect();
        }

        void UpdatePortList()
        {
            PortList.Children.Clear();

            try
            {
                ManagementObjectSearcher searcher =
                    new ManagementObjectSearcher("root\\CIMV2",
                    "SELECT * FROM Win32_PnPEntity");

                foreach (ManagementObject queryObj in searcher.Get())
                {
                    if (queryObj["Caption"]?.ToString().Contains("(COM") == true)
                    {
                        var name = queryObj["Caption"] as string;
                        Console.WriteLine("serial port : {0}", name);

                        var button = createButton(name);
                        var portName = "";
                        try
                        {
                            var mc = Regex.Matches(name, "COM[0-9]");
                            foreach (var m in mc)
                            {
                                portName = m.ToString();
                                break;
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("failed: " + e.Message);
                        }

                        button.Click += (obj, args) =>
                        {
                            ComPort.Text = portName;
                        };
                        PortList.Children.Add(button);

                    }
                }
            }
            catch (ManagementException e)
            {
                MessageBox.Show(e.Message);
            }
        }

        Button createButton(string name)
        {
            return new Button()
            {
                Content = name,
                Margin = new Thickness(12, 3, 12, 3),
            };
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

        int ok_count = 0;

        private void Serial_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (history == null)
            {
                history = new WeightHistory();
                history.DumpCompleted += (max) =>
                {
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        MaxWeight.Text = string.Format("Last max: {0}", max);
                    }));
                };
            }

            var lines = new List<string>();
            while (serial.BytesToRead > 0)
            {
                try
                {
                    // ReadExisting may read FROM last newline TO actual last data.
                    // Call ReadLine several times not to read after newline mark.
                    lines.Add(serial.ReadLine());
                }
                catch (TimeoutException) { break; }
            }
            foreach (var line in lines)
            {
                var data = line.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                if (data.Length == 3)
                {
                    try
                    {
                        var weight = (double)(int.Parse(data[1])) / 10000.0;
                        history.Add(int.Parse(data[0]), weight);
                        //Console.WriteLine("OK: " + data);
                        Application.Current.Dispatcher.Invoke(new Action(() =>
                        {
                            CurrentWeight.Text = string.Format("Current: {0:0000.##}", weight);
                        }));
                        ok_count++;
                    }
                    catch (FormatException ex)
                    {
                        Console.WriteLine(ok_count + " records OK.");
                        ok_count = 0;
                        Console.WriteLine("Caught Format Exception: " + ex.Message);
                    }
                }
                else
                {
                    Console.WriteLine(ok_count + " records OK.");
                    ok_count = 0;
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

        private void RefleshButton_Click(object sender, RoutedEventArgs e)
        {
            UpdatePortList();
        }
    }
}
