#include <Canbus.h>  // don't forget to include these
#include <defaults.h>
#include <global.h>
#include <mcp2515.h>
#include <mcp2515_defs.h>

void setup()
{
  Serial.begin(9600);
  
  //Initialise MCP2515 CAN controller at the specified speed
  bool inited = false;
  while(!inited)
  {
    if(Canbus.init(CANSPEED_500))
    {
      Serial.println("CAN Init ok - 500");
      inited = true;
    }
    else if(Canbus.init(CANSPEED_250))
    {
      Serial.println("CAN Init ok - 250");
      inited = true;
    }
    else if(Canbus.init(CANSPEED_125))
    {
      Serial.println("CAN Init ok - 125");
      inited = true;
    }
    else
    {
      Serial.println("Can't Init CAN");
    }
    delay(1000);
  }
  

  delay(1000);
}

void loop()
{ 
  tCAN message;
  
  if (mcp2515_check_message()) 
  {
    if (mcp2515_get_message(&message)) 
    {
      Serial.print("ID: ");
      Serial.print(message.id,HEX);
      Serial.print(", ");
      Serial.print("Data: ");
      
      for(int i=0;i<message.header.length;i++)
      {
        Serial.print(message.data[i],HEX);
        Serial.print(" ");
      }
      
      Serial.println("");
    }
  }
}
