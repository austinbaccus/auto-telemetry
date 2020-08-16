#include <SPI.h>         // SPI is used to talk to the CAN Controller
#include <mcp_can.h>

#define CAN_ID_PID 0x7DF

MCP_CAN CAN(9);          // set SPI Chip Select to pin 9

unsigned char len = 0;
unsigned char buf[8];
unsigned int canID;

uint8_t sent_rpm_rq = 0; // sent rpm request
uint16_t rpm_byte = 0;
uint16_t rpm_app = 0;

void setup()
{
  Serial.begin(115200); // This is for communicating with the serial monitor

//tries to initialize, if failed --> it will loop here forever
START_INIT:

    if(CAN_OK == CAN.begin(CAN_500KBPS))      //setting CAN baud rate to 500Kbps
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
    if(!sent_rpm_rq)
    {
        unsigned char tmp[8] = {0x02, 0x01, 0x0C, 0, 0, 0, 0, 0};
        CAN.sendMsgBuf(0x7DF, 0, 8, tmp);
        sent_rpm_rq = 1; // don't send another until we've received a response
    }
    
    if(CAN_MSGAVAIL == CAN.checkReceive()) //check if data coming
    {
        CAN.readMsgBuf(&len, buf);    //read data,  len: data length, buf: data buffer
        canID = CAN.getCanId();       //getting the ID of the incoming message
        
        if (canID == 0x7E8)           //reading only our beloved 0xF1 message
        {
            switch(buf[2]) // If you're requesting only RPM...
            {
                case 0x0C: // rpm
                    rpm_byte = (uint16_t)(buf[3] << 8) + (buf[3]);
                    rpm_app = (rpm_byte / 4);
                    Serial.println(rpm_app);
                    sent_rpm_rq = 0;
                    break;
                default:
                    break;
            }
        }
    }

    else 
    {
        // reading a CAN ID that we're not interested in
    }
}
