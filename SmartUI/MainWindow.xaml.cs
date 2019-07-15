using System;
using System.IO;
using System.Windows;
using System.Reflection;
using System.ComponentModel;
using Vlc.DotNet.Wpf;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using SensorMsg;
using Dragablz;
using Google.Protobuf;
using Vlc.DotNet.Core;
using Vlc.DotNet.Core.Interops;

namespace SmartUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly DirectoryInfo vlcLibDirectory;
        private VlcControl control;

        public MainWindow()
        {
            InitializeComponent();
            var currentAssembly = Assembly.GetEntryAssembly();
            var currentDirectory = new FileInfo(currentAssembly.Location).DirectoryName;
            // Default installation path of VideoLAN.LibVLC.Windows
            vlcLibDirectory = new DirectoryInfo(Path.Combine(currentDirectory, "libvlc", IntPtr.Size == 4 ? "win-x86" : "win-x64"));
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            this.control?.Dispose();
            base.OnClosing(e);
        }

        private void OnPlayButtonClick(object sender, RoutedEventArgs e)
        {
            this.control?.Dispose();
            this.control = new VlcControl();
            this.ControlContainer.Content = this.control;
            this.control.SourceProvider.CreatePlayer(this.vlcLibDirectory);

            // This can also be called before EndInit
            this.control.SourceProvider.MediaPlayer.Log += (_, args) =>
            {
                string message = $"libVlc : {args.Level} {args.Message} @ {args.Module}";
                System.Diagnostics.Debug.WriteLine(message);
            };

            control.SourceProvider.MediaPlayer.Play(new Uri("http://192.168.0.100:8090/?action=stream"));
        }

        private void OnStopButtonClick(object sender, RoutedEventArgs e)
        {
            this.control?.Dispose();
            this.control = null;
        }

        private void MqttStart(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("MqttStarted");
            status.Text = "Connected";
            Sensor.Text = "False";
            MqttClient myClient = new MqttClient("192.168.0.100");

            // Register to message received
            myClient.MqttMsgPublishReceived += client_recievedMessage;

            string clientId = Guid.NewGuid().ToString();
            myClient.Connect(clientId);

            // Subscribe to topic
            myClient.Subscribe(new String[] { "base" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
            System.Console.ReadLine();
        }
         void client_recievedMessage(object sender, MqttMsgPublishEventArgs e)
        {
            // Handle message received
            System.Console.WriteLine("Message received: " + e.Message);
            SensorData NewData;
            // Watch the protbuf magic here
            NewData = SensorData.Parser.ParseFrom(e.Message);
            this.Dispatcher.Invoke(() =>
            {
                UpdateValues(NewData);
                
            });
            
            System.Console.WriteLine("Message received: " + NewData.BatteryLevel);
            System.Console.WriteLine("Message received: " + NewData.Luminance);
            System.Console.WriteLine("Message received: " + NewData.RelativeHumidity);
            System.Console.WriteLine("Message received: " + NewData.Temperature);
            System.Console.WriteLine("Message received: " + NewData.Motion);
            System.Console.WriteLine("Message received: " + NewData.Ultraviolet);

        }
        public void UpdateValues(SensorData NewData)
        {
            Sensor.Text = NewData.Motion.ToString();
            UV.Text = NewData.Ultraviolet.ToString();
            Temperature.Text = NewData.Temperature.ToString();
            Humidity.Text = NewData.RelativeHumidity.ToString();
            Luminance.Text = NewData.Luminance.ToString();
            battery.Text = NewData.BatteryLevel.ToString();
            //Add Time using google protobuf wellknown tyes
        }

       
    }
}
