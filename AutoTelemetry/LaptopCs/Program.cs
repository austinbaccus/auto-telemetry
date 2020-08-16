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
            while (true)
            {
                BeginRun();
            }
        }

        private void BeginRun()
        {
            Stopwatch sw = new Stopwatch();
            bool runInProgress = false;

            while (true)
            {
                int speed = GetSpeed();

                // 0 mph
                if (runInProgress && speed <= 0)
                {
                    // the car has come to a stop
                    runInProgress = false;

                    // reset stopwatch back to 0.00s
                    sw.Reset();
                }
                // moving
                else if (!runInProgress && speed > 0 && speed < 96)
                {
                    // the run has started
                    runInProgress = true;

                    // start the stopwatch
                    sw.Start();
                }
                // 60 mph
                else if (speed >= 96)
                {
                    // stop the stopwatch
                    sw.Stop();

                    // calculate the time
                    TimeSpan ts = sw.Elapsed;
                    elapsedTime = String.Format("{0:00}.{1:00}", ts.Seconds, ts.Milliseconds / 10);

                    // print out the time
                    Console.WriteLine("time: {0}", elapsedTime);

                    // exit this run
                    return;
                }
            }
        }

        private int GetSpeed()
        {
            int speed = 0;

            // read in the data from the CAN
            var task = Task.Run(() => GetCANMessage());

            // assign current speed, break if we can't read new data within 10 seconds
            if (task.Wait(TimeSpan.FromSeconds(10))) msg = task.Result;
            else return -1;

            if (!int.TryParse(msg, out speed))
            {
                return -1;
            }

            return speed;
        }

        private string GetCANMessage()
        {
            // read in the latest serial input from the micro-controller
            return _serialPort.ReadLine();
        }
    }
}