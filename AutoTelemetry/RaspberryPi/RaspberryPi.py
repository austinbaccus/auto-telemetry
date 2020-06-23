import time
import serial
from azure.iot.device import IoTHubDeviceClient, Message

CONNECTION_STRING = "HostName=AutoTelemetryIoTHub.azure-devices.net;DeviceId=simDevice;SharedAccessKey=piWUk5b+sir8S1/Pw/oL9qaGi6lMQyce/WLM1SQ0+Zg="
ser = serial.Serial('/dev/ttyUSB0', 115200)
ports = ['/dev/ttyUSB0']

def connect_to_arduino():
    connected = False
    port_idx = 0

    while connected == False:
        try:
            # Connect to the serial port
            ser = serial.Serial(ports[port_idx], 115200)
            time.sleep(1)

            # If there are messages waiting to be read from the serial port...
            if (ser.in_waiting > 0):

                # Read the message
                message = ser.readline()

                if (len(message) > 0):
                    # If the message is not empty, consider it a good connection
                    connected = True

                # If that port is not sending any data, try another one. 
                else:
                    # Increment the value of the port_idx
                    port_idx = (port_idx + 1) % len(ports)
        
        except:
            print ("Error trying to connect to Arduino")

def get_rpm():
    rpm = 0
    if (ser.in_waiting > 0):
        # Read the RPM data coming in from the Arduino
        rpm = ser.readline()
    return rpm

def iothub_client_init():
    client = IoTHubDeviceClient.create_from_connection_string(CONNECTION_STRING)
    return client

def iothub_client_telemetry_run():
    try:
        # Try to connect to the Arduino
        print ("Connecting to Arduino...")
        connect_to_arduino()
        print ("Connected to Arduino\n")
        
        # Try to connect to the Azure IoT Hub
        print ("Connecting to IoT Hub...")
        client = iothub_client_init()
        print ("Connected to IoT Hub\n")

        # Keep reading RPM data and sending it to the IoT Hub
        while True:
            rpm = get_rpm()

            # Send the message
            print ("Sending message: {}".format(rpm))
            client.send_message(rpm)
            print ("Message successfully sent")

            time.sleep(1)

    except KeyboardInterrupt:
        print ("IoTHubClient stopped")

if __name__ == '__main__':
    iothub_client_telemetry_run()
            