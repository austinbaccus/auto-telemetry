using System;
using System.IO.Ports;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Timers;

namespace LaptopCs
{
    public class Program
    {
        static void Main(string[] args)
        {
            // initialize stuff here
            Init();

            Console.WriteLine("  [1] Speed logger\n  [2] 0-60 Stopwatch\n");
            int selection = 1;

            int.TryParse(Console.ReadLine(), out selection);

            switch (selection)
            {
                case (1):
                    RunTelemetryLogger();
                    break;
                case (2):
                    RunAccelerationStopwatch();
                    break;
                default:
                    break;
            }
        }

        static void RunTelemetryLogger()
        {
            TelemetryLogger tl = new TelemetryLogger();
            tl.Loop();
        }

        static void RunAccelerationStopwatch()
        {
            AccelerationStopwatch accs = new AccelerationStopwatch();
            accs.Loop();
        }

        static void Init()
        {
            // for whatever reason the console prints out black text on a black background by default, so we have to specify the colors
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
    
    public class TelemetryLogger
    {
        StringBuilder csv;
        Timer timer1;
        Stopwatch sw = new Stopwatch();
        TimeSpan ts;
        SerialPort _serialPort;

        private string msg;
        private string elapsedTime;
        private bool isDataSaved;

        public TelemetryLogger()
        {
            this.csv = new StringBuilder();

            // flag for whether data has been saved to a file or not
            isDataSaved = false;

            // open serial connection
            _serialPort = new SerialPort("COM7", 115200);
            _serialPort.Open();

            // stop logging delegate
            Console.CancelKeyPress += delegate
            {
                // the user wants to exit the program. this saves the data before exiting
                SaveTelemetryToFile();
                isDataSaved = true;
            };

            // wait for user input to start recording data
            Console.Write("Press 'enter' to start logging data. Press Ctrl-C to stop logging data. ");
            Console.ReadLine();

            Timer timer = new Timer(100);
            timer.AutoReset = true; // the key is here so it repeats
            timer.Elapsed += LogCANMessage;
            timer.Start();
        }

        public void Loop()
        {
            // start the stopwatch
            sw.Start();

            // enter the main loop
            while (true)
            {
                // get the elapsed time
                ts = sw.Elapsed;
                elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);

                // read in the data from the CAN
                var task = Task.Run(() => GetCANMessage());

                if (task.Wait(TimeSpan.FromSeconds(10)))
                {
                    msg = task.Result;

                    // print CAN message to the console
                    Console.WriteLine(msg);
                }
                else
                {
                    // it's been a while since we got the last message, the car is probably turned off at this point
                    // in this case, we can exit the loop and save the data to the file
                    break;
                }
            }

            // stop the stopwatch
            sw.Stop();

            if (isDataSaved == false)
                SaveTelemetryToFile();
        }

        private string GetCANMessage()
        {
            // read in the latest serial input from the micro-controller
            return _serialPort.ReadLine();
        }

        private void LogCANMessage(object sender, EventArgs e)
        {
            ts = sw.Elapsed;
            elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);

            // append CAN message to the csv file buffer text
            csv.Append(elapsedTime + "," + msg);
        }

        private void SaveTelemetryToFile()
        {
            string datetime = DateTime.Today.ToString("s");
            datetime = datetime.Replace('.', '_').Replace(':', '-');
            string path = @"C:\Users\gunra\source\repos\AutoTelemetry\AutoTelemetry\Laptop\" + datetime + ".csv";
            File.AppendAllText(path, this.csv.ToString());
        }
    }

    public class AccelerationStopwatch
    {
        SerialPort _serialPort;

        private string msg;
        private string elapsedTime;

        public AccelerationStopwatch()
        {
            // open serial connection
            _serialPort = new SerialPort("COM7", 115200);
            _serialPort.Open();
        }

        public void Loop()
        {
            Console.WriteLine("Press 'q' + 'enter' to quit, press any other key + 'enter' to start the 0-60 run");
            while (true)
            {
                string input = Console.ReadLine();

                if (input == "q")
                {
                    break;
                }
                else
                {
                    BeginRun();
                }
            }
        }

        private void BeginRun()
        {

            Stopwatch sw = new Stopwatch();
            bool runStarted = false;

            // enter the main loop
            while (true)
            {
                // read in the data from the CAN
                var task = Task.Run(() => GetCANMessage());

                if (task.Wait(TimeSpan.FromSeconds(10)))
                {
                    msg = task.Result;
                }
                else
                {
                    // it's been a while since we got the last message, the car is probably turned off at this point
                    // in this case, we can exit the loop and save the data to the file
                    break;
                }

                int speed = 0;
                int.TryParse(msg, out speed);

                if (!runStarted)
                {
                    if (speed > 0)
                    {
                        // run has begun, car is now moving
                        runStarted = true;

                        // start the stopwatch
                        sw.Start();
                    }
                }
                else
                {
                    if (speed > 96) // 96 km/h = 60 mph
                    {
                        // run finished, car has hit 60 mph

                        TimeSpan ts = sw.Elapsed;

                        // get the elapsed time
                        elapsedTime = String.Format("{0:00}.{1:00}", ts.Seconds, ts.Milliseconds / 10);

                        // print out the time
                        Console.WriteLine("time: {0}", elapsedTime);

                        break;
                    }
                }
                
            }

            // stop the stopwatch
            sw.Stop();
        }

        private string GetCANMessage()
        {
            // read in the latest serial input from the micro-controller
            return _serialPort.ReadLine();
        }
    }
}