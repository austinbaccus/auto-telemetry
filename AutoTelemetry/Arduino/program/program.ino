#include <SPI.h>
#include <mcp_can.h>
#include "mcp_can.h"
#include <mcp_can_dfs.h>


const int SPI_CS_PIN = 9;

MCP_CAN CAN(SPI_CS_PIN); // Set CS pin

#define PID_ENGIN_PRM       0x0C
#define PID_VEHICLE_SPEED   0x0D
#define PID_COOLANT_TEMP    0x05

#define CAN_ID_PID          0x7DF

unsigned char PID_INPUT;
unsigned char getPid = 0;

void set_mask_filt() 
{
    /*
        set mask, set both the mask to 0x3ff
    */
    CAN.init_Mask(0, 0, 0x7FC);
    CAN.init_Mask(1, 0, 0x7FC);

    /*
        set filter, we can receive id from 0x04 ~ 0x09
    */
    CAN.init_Filt(0, 0, 0x7E8);
    CAN.init_Filt(1, 0, 0x7E8);

    CAN.init_Filt(2, 0, 0x7E8);
    CAN.init_Filt(3, 0, 0x7E8);
    CAN.init_Filt(4, 0, 0x7E8);
    CAN.init_Filt(5, 0, 0x7E8);
}

void sendPid(unsigned char __pid) 
{
    unsigned char tmp[8] = {0x02, 0x01, __pid, 0, 0, 0, 0, 0};
    Serial.print("SEND PID: 0x");
    Serial.println(__pid, HEX);
    CAN.sendMsgBuf(CAN_ID_PID, 0, 8, tmp);
}

void setup() 
{
    Serial.begin(115200);
    
    while (CAN_OK != CAN.begin(CAN_500KBPS)) 
    {  
        // init can bus : baudrate = 500k
        Serial.println("CAN BUS Shield init fail");
        Serial.println(" Init CAN BUS Shield again");
        delay(100);
    }
    
    Serial.println("CAN BUS Shield init ok!");
    set_mask_filt();
}


void loop() 
{
    taskCanRecv();
    taskDbg();

    if (getPid) 
    {       
        // GET A PID
        getPid = 0;
        sendPid(PID_INPUT);
        PID_INPUT = 0;
    }
}

void taskCanRecv() 
{
    unsigned char len = 0;
    unsigned char buf[8];

    if (CAN_MSGAVAIL == CAN.checkReceive()) 
    {                
        // check if get data
        CAN.readMsgBuf(&len, buf);    // read data,  len: data length, buf: data buf

        Serial.println("\r\n------------------------------------------------------------------");
        Serial.print("Get Data From id: 0x");
        Serial.println(CAN.getCanId(), HEX);
        
        for (int i = 0; i < len; i++) 
        { 
            // print the data
            Serial.print("0x");
            Serial.print(buf[i], HEX);
            Serial.print("\t");
        }
        
        Serial.println();
    }
}

void taskDbg() 
{
    while (Serial.available()) 
    {
        char c = Serial.read();

        if (c >= '0' && c <= '9') 
        {
            PID_INPUT *= 0x10;
            PID_INPUT += c - '0';

        } 
        else if (c >= 'A' && c <= 'F') 
        {
            PID_INPUT *= 0x10;
            PID_INPUT += 10 + c - 'A';
        } 
        else if (c >= 'a' && c <= 'f') 
        {
            PID_INPUT *= 0x10;
            PID_INPUT += 10 + c - 'a';
        } 
        else if (c == '\n') 
        { // END
            getPid = 1;
        }
    }
}
