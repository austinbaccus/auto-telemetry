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

            // ask the user whether they'd like to run the telelemtry logger or the 0-60 stopwatch
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
        StringBuilder csv; // used to contain the text that we'll eventually write to the file
        Stopwatch sw = new Stopwatch();
        TimeSpan ts;
        SerialPort _serialPort;

        private string msg;
        private string elapsedTime;
        private bool isDataSaved;
        private string serialPort = "COM7"; // replace this with whatever serial port you're using to connect with the Arduino

        public TelemetryLogger()
        {
            this.csv = new StringBuilder();

            // flag for whether data has been saved to a file or not
            isDataSaved = false;

            // open serial connection
            _serialPort = new SerialPort(serialPort, 115200);
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

            // execute the 'LogCANMessage()' function every 0.1 seconds
            Timer timer = new Timer(100); // 100 milliseconds = 0.1 seconds
            timer.AutoReset = true; 
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

                // read in the data from the CAN/Arduino
                var task = Task.Run(() => GetCANMessage());

                if (task.Wait(TimeSpan.FromSeconds(10)))
                {
                    // this is the message we recieved from the CAN/Arduino. Right now, this is set to be your vehicle's speed
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

        /// <summary>
        /// Gets the latest CAN data from the Arduino.
        /// </summary>
        private string GetCANMessage()
        {
            // read in the latest serial input from the micro-controller
            return _serialPort.ReadLine();
        }

        /// <summary>
        /// Records the latest CAN message to the StringBuilder object we use to store telemetry.
        /// </summary>
        private void LogCANMessage(object sender, EventArgs e)
        {
            ts = sw.Elapsed;
            elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);

            // append CAN message to the csv file buffer text
            csv.Append(elapsedTime + "," + msg);
        }

        /// <summary>
        /// Saves the telemetry data to a unique file in this directory.
        /// </summary>
        private void SaveTelemetryToFile()
        {
            string datetime = DateTime.Today.ToString("s");
            datetime = datetime.Replace('.', '_').Replace(':', '-');
            int runNumber = 0;
            string path = @".\" + datetime + "_" + runNumber + ".csv";

            // if this current filename is already taken, increment the file number and try again until we have a unique filename
            while (File.Exists(path))
            {
                runNumber++;
                path = @".\" + datetime + "_" + runNumber + ".csv";
            }

            File.AppendAllText(path, this.csv.ToString());
        }
    }

    public class AccelerationStopwatch
    {
        SerialPort _serialPort;

        private string msg;
        private string elapsedTime;
        private string serialPort = "COM7"; // chamge this to be whatever serial port is connected to the Arduino

        public AccelerationStopwatch()
        {
            // open serial connection
            _serialPort = new SerialPort(serialPort, 115200);
            _serialPort.Open();
        }

        public void Loop()
        {
            while (true)
            {
                BeginRun();
                Console.WriteLine("Press 'enter' to start a new run");
                Console.ReadLine();
            }
        }

        /// <summary>
        /// Begins a new 0-60 MPH run. This function will record how long it takes the car to hit the target speed.
        /// </summary>
        private void BeginRun()
        {
            Stopwatch sw = new Stopwatch();
            bool runInProgress = false;

            while (true)
            {
                int targetSpeed = 96; // this is the speed you're trying to reach. in a 0-60 MPH test, that speed is 60 MPH. 96 KM/H = 60 MPH.
                int startingSpeed = 5; // this is the speed that you start recording at. traditionally, 0-60 tests actually start with a 1-foot rollout. according to Car & Driver, this is when you hit approximately 3 MPH (or 5 KMH).
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
                else if (!runInProgress && speed > 0 && speed < targetSpeed)
                {
                    // the run has started
                    runInProgress = true;

                    // start the stopwatch
                    sw.Start();
                }
                // 60 mph
                else if (!runInProgress && speed >= targetSpeed)
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

        /// <summary>
        /// Gets the ccar's current speed in kilometers per hour.
        /// </summary>
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

        /// <summary>
        /// Gets the latest CAN data from the Arduino.
        /// </summary>
        private string GetCANMessage()
        {
            // read in the latest serial input from the micro-controller
            return _serialPort.ReadLine();
        }
    }
}