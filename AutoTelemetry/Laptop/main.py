import serial
import time
import csv

ser = serial.Serial(port = "COM7", baudrate=115200, bytesize=8, timeout=2, stopbits=serial.STOPBITS_ONE)
ser.flushInput()

while True:
    try:
        ser_bytes = ser.readline()

        rpm = ser_bytes.decode("utf-8")
        print(rpm)
    except:
        print("Keyboard Interrupt")
        break

with open("rpm_data.csv","a",newline='') as f:
    writer = csv.writer(f,delimiter=",")
    
    while True:
        try:
            ser_bytes = ser.readline()

            rpm = ser_bytes.decode("utf-8")
            print(rpm)

            localtime = time.asctime(time.localtime(time.time())).split()
            writer.writerow([localtime[0],localtime[1],localtime[2],localtime[3],localtime[4],rpm])
            time.sleep(0.1)
        except:
            print("Keyboard Interrupt")
            break