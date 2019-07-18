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
using WinSCP;
//using System.Windows.Forms.Integration;


namespace SmartUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly DirectoryInfo vlcLibDirectory;
        private VlcControl control;
        public string RaspIp = "192.168.0.102";
        public string PCID = "192.168.0.106";
        int defaultState = 1;
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
            string ipstream = "http://" + RaspIp + ":" + 8081 + "/stream";
            // This can also be called before EndInit
            this.control.SourceProvider.MediaPlayer.Log += (_, args) =>
            {
                string message = $"libVlc : {args.Level} {args.Message} @ {args.Module}";
                System.Diagnostics.Debug.WriteLine(message);
            };

            if (defaultState == 0)
            {
                if (ListBox1.SelectedItem.ToString() == "Live Stream")
                    control.SourceProvider.MediaPlayer.Play(new Uri(ipstream));

                else
                    control.SourceProvider.MediaPlayer.Play(new Uri(ListBox1.SelectedItem.ToString()));
            }
            if (defaultState == 1)
            {
                control.SourceProvider.MediaPlayer.Play(new Uri(ipstream));
                defaultState = 0;
            }

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
            MqttClient myClient = new MqttClient(RaspIp);



            // Register to message received
            myClient.MqttMsgPublishReceived += client_recievedMessage;

            string clientId = Guid.NewGuid().ToString();
            myClient.Connect(clientId);

            // Subscribe to topic
            myClient.Subscribe(new String[] { "base" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
            System.Console.ReadLine();
            myClient.Subscribe(new String[] { "ui" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
            System.Console.ReadLine();
        }

        void client_recievedMessage(object sender, MqttMsgPublishEventArgs e)
        {
            if (e.Topic == "base")
            {
                // Handle message received
                //System.Console.WriteLine("Message received: " + e.Message);
                SensorData NewData;
                // Watch the protbuf magic here
                NewData = SensorData.Parser.ParseFrom(e.Message);

                this.Dispatcher.Invoke(() =>
                {
                    UpdateValues(NewData);

                });

                /* System.Console.WriteLine("Message received: " + NewData.BatteryLevel);
                 System.Console.WriteLine("Message received: " + NewData.Luminance);
                 System.Console.WriteLine("Message received: " + NewData.RelativeHumidity);
                 System.Console.WriteLine("Message received: " + NewData.Temperature);
                 System.Console.WriteLine("Message received: " + NewData.Motion);
                 System.Console.WriteLine("Message received: " + NewData.Ultraviolet);*/
            }
            if (e.Topic == "ui")
            {
                // Handle message received
                System.Console.WriteLine("ALERT: " + e.Message);

                string alert = System.Text.Encoding.UTF8.GetString(e.Message);

                System.Console.WriteLine("Alert Type\t" + alert);
                //printing alert type

                alertlist(alert);

                if (alert == "00")
                {




                }
                else if (alert == "01")
                {
                    string message = "Patient Movement Detected. \r \n Check Video Stream";
                    string title = "Patient data";
                    MessageBox.Show(message, title);
                    
                }
                else if (alert == "10")
                {
                    string message = "HIGH Ultra Violet Radiation Index!! \r\n Engage UV Shield ";
                    string title = "Patient data";
                    MessageBox.Show(message, title);
                    
                }
                else if (alert == "11")
                {
                    string message = "HIGH Ultra Violet Radiation Index!! \r\n Engage UV Shield \r\n Patient Movement Detected. \r \n Check Video Stream";
                    string title = "Patient data";
                    MessageBox.Show(message, title);
                    
                }

            }
        }
        public void UpdateValues(SensorData NewData)
        {
            Sensor.Text = NewData.Motion.ToString();
            UV.Text = NewData.Ultraviolet.ToString();
            Temperature.Text = NewData.Temperature.ToString();
            Humidity.Text = NewData.RelativeHumidity.ToString();
            Luminance.Text = NewData.Luminance.ToString();
            battery.Text = NewData.BatteryLevel.ToString();
            //Add Time using google protobuf wellknown types
        }

        private void Getgraph(object sender, RoutedEventArgs e)
        {
           /* WindowsFormsHost host = new WindowsFormsHost();
            GeckoWebBrowser browser = new GeckoWebBrowser();
            host.Child = browser;
            GridWeb.Children.Add(host);
            browser.Navigate("http://" + PCID + ":8888/");
            */
            //Charts.Navigate(new Uri("http://"+PCID+":8888/"));

            /* try
            {
                Charts.Source = new Uri("http://" + PCID + ":8888/");
            }
            catch(Exception Ex)
            {
                MessageBox.Show(Ex.Message);
            }*/


            System.Diagnostics.Process.Start("http://"+PCID + ":8888/");
        }

        private void sync_Click(object sender, RoutedEventArgs e)
        {
            // Sync.Content = "Syncing...";
            try
            {
                // Setup session options
                SessionOptions sessionOptions = new SessionOptions
                {
                    Protocol = Protocol.Sftp,
                    HostName = RaspIp,
                    UserName = "pi",
                    Password = "raspberry",
                    SshHostKeyFingerprint = "ssh-ed25519 256 33:21:d5:86:67:fd:03:48:d0:7e:53:78:08:cb:0a:13"
                };

                using (Session session = new Session())
                {
                    // Connect
                    session.Open(sessionOptions);

                    // Download files
                    TransferOptions transferOptions = new TransferOptions();
                    transferOptions.TransferMode = TransferMode.Binary;

                    TransferOperationResult transferResult;
                    transferResult =
                        session.GetFiles("/tmp/motion/*", @"C:\Users\keert\Videos\smartiot\", false, transferOptions);

                    // Throw on any error
                    transferResult.Check();

                    // Print results
                    foreach (TransferEventArgs transfer in transferResult.Transfers)
                    {
                        Console.WriteLine("Download of {0} succeeded", transfer.FileName);
                    }
                }

                ;
            }
            catch (Exception eg)
            {
                Console.WriteLine("Error: {0}", eg);

            }
            string[] files = Directory.GetFiles(@"C:\Users\keert\Videos\smartiot", "*.avi");

            ListBox1.Items.Clear();
            ListBox1.Items.Add("Live Stream");
            foreach (string file in files)
            {
                ListBox1.Items.Add(file);
            }
            // Sync.Content = "Sync";
        }

        public void alertlist(string alert)
        {

            this.Dispatcher.Invoke(() =>
                {
                    if (alert == "00")
                        ListBoxAlert.Items.Add("UV Radiation Index: Normal \r\n Patient Movement : Dormant \t\t\t" +  DateTime.Now.ToString());
                    else if (alert == "01")
                        ListBoxAlert.Items.Add("UV Radiation Index: Normal \r\n Patient Movement : ACTIVE ; Check Video Stream \t\t\t" + DateTime.Now.ToString());
                    else if (alert == "10")
                        ListBoxAlert.Items.Add("UV Radiation Index: HIGH!!! \r\n Patient Movement : Dormant \t\t\t" + DateTime.Now.ToString());
                    else if (alert == "11")
                        ListBoxAlert.Items.Add("UV Radiation Index: HIGH \r\n Patient Movement :  ACTIVE ; Check Video Stream \t\t\t" + DateTime.Now.ToString());
                    
                });

        }

        private void TabControl_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {

        }
    }
}


