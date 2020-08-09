#include <SPI.h>         // SPI is used to talk to the CAN Controller
#include <mcp_can.h>

#define CAN_ID_PID 0x7DF

MCP_CAN CAN(9);          // set SPI Chip Select to pin 9

unsigned char len = 0;
unsigned char buf[8];
unsigned int canID;
uint8_t sent_speed_rq = 0; // sent speed request flag

void setup()
{
    Serial.begin(115200); // this is for communicating with the serial monitor

    //tries to initialize, if failed --> it will loop here forever
    START_INIT:

    if (CAN_OK == CAN.begin(CAN_500KBPS)) // setting CAN baud rate to 500Kbps
    {
        Serial.println("Init success!");
    }
    else
    {
        Serial.println("Init failed");
        delay(1000);
        goto START_INIT;
    }
}

void loop()
{
    // if we just received a message from the CAN and haven't sent another request yet...
    if (!sent_speed_rq)
    {
        requestSpeed();
    }
    readIncomingCANData();
}

void readIncomingCANData()
{
    if (CAN_MSGAVAIL == CAN.checkReceive()) // check if data coming
    {
        CAN.readMsgBuf(&len, buf);    // read data,  len: data length, buf: data buffer
        canID = CAN.getCanId();       // getting the ID of the incoming message
        
        if (canID == 0x7E8 && buf[2] == 0x0D) // reading only our 0x7E8 message
        {
            Serial.println(buf[3]);
            sent_speed_rq = 0;
        }
    }
}

void requestSpeed()
{
    unsigned char tmp[8] = {0x02, 0x01, 0x0D, 0, 0, 0, 0, 0};
    CAN.sendMsgBuf(0x7DF, 0, 8, tmp);
    sent_speed_rq = 1; // set request flag
}
