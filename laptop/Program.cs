using System;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Laptop
{
    public class Program
    {
        private static StringBuilder csv;
        private static string message = "";
        private static SerialPort _serialPort;
        private static Timer timer = new Timer();
        private static Stopwatch sw = new Stopwatch();
        private static readonly HttpClient httpClient = new HttpClient();

        // Settings
        private static string[] ports;
        private static int refreshInterval = 0;
        private static bool enableDataLogging = false;
        private static bool enableDataStreaming = false;

        static void Main(string[] args)
        {
            // Read the config file contents
            ReadConfigFile();

            // Condifure the console output
            ConfigureConsole();

            // Open serial connection
            ConfigureSerial();

            // Configure data logging
            if (enableDataLogging)
            {
                ConfigureDataLogging();
            }

            // Configure data streaming
            if (enableDataStreaming)
            {
                ConfigureDataStreaming();
            }

            // Setup the timer used to execute different features at a specified interval
            ConfigureTimer();

            while (true)
            {
                GetLatestUsbMessage();
            }
        }

        static void ReadConfigFile()
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(PascalCaseNamingConvention.Instance)
                .Build();

            string settingsText = ""; 

            try
            {
                settingsText = File.ReadAllText(@".\config\settings.yaml");
            }
            catch (DirectoryNotFoundException)
            {
                settingsText = File.ReadAllText(@".\..\..\..\config\settings.yaml");
            }

            var settings = deserializer.Deserialize<Settings>(settingsText);

            ports = settings.ports;
            refreshInterval = settings.refresh_interval;
            enableDataLogging = settings.enable_data_logging;
            enableDataStreaming = settings.enable_data_streaming;
        }

        static void ConfigureConsole()
        {
            // For whatever reason the console prints out black text on a black background by default, so we have to specify the colors
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;
        }

        static void ConfigureSerial()
        {
            foreach (string port in ports)
            {
                try
                {
                    _serialPort = new SerialPort(port, 115200);
                    _serialPort.Open();
                    break;
                }
                catch (IOException) { }
            }
        }

        static void ConfigureTimer()
        {
            timer.AutoReset = true;
            timer.Interval = refreshInterval;
            timer.Start();
        }

        static void ConfigureDataLogging()
        {
            // Log data at each new interval
            timer.Elapsed += RecordMessageToFileBuffer;

            // Stop logging delegate
            Console.CancelKeyPress += delegate
            {
                // The user wants to exit the program. this saves the data before exiting
                SaveDataToFile();
            };
        }

        static void ConfigureDataStreaming()
        {
            timer.Elapsed += SendMessageToWebsite;
        }

        static void GetLatestUsbMessage()
        {
            message = _serialPort.ReadLine();
            Console.WriteLine(message);
        }

        static void SendMessageToWebsite(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                // Send the latest message to the website
                var url = "https://autotelemetry.azurewebsites.net/api/values/" + message;
                var response = httpClient.PostAsync(url, null).Result;
                Console.WriteLine($"Response {response.StatusCode}");
            });
        }

        static void RecordMessageToFileBuffer(object sender, EventArgs e)
        {

            TimeSpan ts = sw.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);

            // Append CAN message to the csv file buffer text
            csv.Append(elapsedTime + "," + message);
        }

        static void SaveDataToFile()
        {
            string datetime = DateTime.Today.ToString("s");
            datetime = datetime.Replace('.', '_').Replace(':', '-');
            int runNumber = 0;
            string path = @".\" + datetime + "_" + runNumber + ".csv";

            // if this current filename is already taken, increment the file number and try again until we have a unique filename
            while (File.Exists(path))
            {
                runNumber++;
                path = @".\data\" + datetime + "_" + runNumber + ".csv";
            }

            File.AppendAllText(path, csv.ToString());
        }
    }

    public class Settings
    {
        public string[] ports { get; set; }
        public int refresh_interval { get; set; }
        public bool enable_data_logging { get; set; }
        public bool enable_data_streaming { get; set; }
    }
}
