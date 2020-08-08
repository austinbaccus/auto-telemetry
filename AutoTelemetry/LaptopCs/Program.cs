using System;
using System.IO.Ports;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;

namespace LaptopCs
{
    class Program
    {
        static SerialPort _serialPort;
        static StringBuilder csv;
        static StringBuilder msg;
        static Stopwatch sw;
        static TimeSpan ts;

        static void Main(string[] args)
        {
            // for whatever reason the console prints out black text on a black background by default, so we have to specify the colors
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;
            
            // flag for whether data has been saved to a file or not
            bool isDataSaved = false;

            // open serial connection
            _serialPort = new SerialPort("COM7", 115200);
            _serialPort.Open();

            // initialize the new StringBuilder objects for better string concat performance
            csv = new StringBuilder();
            msg = new StringBuilder();

            // initialize the stopwatches
            sw = new Stopwatch();

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

            // start the logging loop
            Loop();

            // save the data before exiting the program
            if (isDataSaved == false) 
                SaveTelemetryToFile();
        }
        static void Loop()
        {
            // start the stopwatch
            sw.Start();

            string canMessage;

            // enter the main loop
            while (true)
            {
                // get the elapsed time
                ts = sw.Elapsed;
                string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);

                // read in the data from the CAN
                var task = Task.Run(() => GetCANMessage());

                if (task.Wait(TimeSpan.FromSeconds(10)))
                {
                    canMessage = task.Result;

                    // join the timestamp and the CAN message together
                    msg.AppendJoin(',', new string[] { elapsedTime, canMessage });

                    // append CAN message to the csv file buffer text
                    csv.Append(msg.ToString());

                    // print CAN message to the console
                    Console.WriteLine(msg.ToString());
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
        }
        static string GetCANMessage()
        {
            // read in the latest serial input from the micro-controller
            return _serialPort.ReadLine();
        }
        static void SaveTelemetryToFile()
        {
            string datetime = DateTime.Today.ToString("s");
            string path = @"C:\Users\gunra\source\repos\AutoTelemetry\AutoTelemetry\Laptop\" + datetime + ".csv";
            File.AppendAllText(path, csv.ToString());
        }
    }
}