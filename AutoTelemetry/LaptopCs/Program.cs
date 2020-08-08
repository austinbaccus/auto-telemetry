using System;
using System.IO.Ports;
using System.Text;
using System.IO;

namespace LaptopCs
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;

            SerialPort _serialPort = new SerialPort("COM7", 115200);
            _serialPort.Open();

            var csv = new StringBuilder();

            Console.CancelKeyPress += delegate
            {
                File.AppendAllText(@"C:\Users\gunra\source\repos\AutoTelemetry\AutoTelemetry\Laptop\stephen_car_data_2.csv", csv.ToString());
            };

            while (true)
            {
                string s = _serialPort.ReadLine();
                csv.Append(s);
                Console.WriteLine(s);
            }
        }
    }
}