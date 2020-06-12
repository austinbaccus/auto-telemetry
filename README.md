# AutoTelemetry
AutoTelemetry is a project with the goal of transmitting real-time telemetry from my 2012 Audi A6 3.0 to the cloud, and from the cloud to client devices via a web app. This will allow anyone with access to the web app to see real-time telemetry (RPM, gear, speed, etc.) for my car. 
## Hardware
To get data from the car I use a DB9-RS32 cable. This cable is connected to the car's OBDII port on one end and an Arduino CAN-BUS shield on the other end. This shield is conencted to an Arduino Uno. This Arduino ultimately reads CAN messages from the OBDII port, and sends the data to a Raspberry Pi 3B+ via serial cable. The Pi then translates the CAN message into readable data for later use. The Pi connects to the internet using a mobile hotspot, which allows it to send the car's telemtry within the CAN messages to an IoT Hub hosted in Azure.
## Cloud
As the Azure IoT Hub recieves telemetry from the car, it broadcasts that data to any client services that are listening. 
## Web App
The AutoTelemetry web app is one of the clients that listens for new data coming in from the IoT Hub. From here, the car's telemetry is visualized with fancy graphs and charts.
## The Data
The car's telemetry is displayed and saved in multiple places along the data pipeline. After the Pi translates the CAN message the data is saved data locally. This allows me to review my car's latest telemetry from a desktop in case I can't connect the Pi to the Cloud.
