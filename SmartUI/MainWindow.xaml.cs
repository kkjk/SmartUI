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
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using SensorMsg;
using Google.Protobuf;

namespace SmartUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
       
        private void MqttStart(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("MqttStarted");
            status.Text = "Connected";
            Sensor.Text = "False";
            MqttClient myClient = new MqttClient("192.168.0.103");

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
