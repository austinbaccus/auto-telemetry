import serial
import time
import csv

ser = serial.Serial('COM5')
ser.flushInput()

with open("rpm_data.csv","a",newline='') as f:
    writer = csv.writer(f,delimiter=",")
    while True:
        try:
            ser_bytes = ser.readline()

            rpm = int(ser_bytes.decode("utf-8"))
            print(rpm)

            localtime = time.asctime(time.localtime(time.time())).split()
            writer.writerow([localtime[0],localtime[1],localtime[2],localtime[3],localtime[4],rpm])
        except:
            print("Keyboard Interrupt")
            break