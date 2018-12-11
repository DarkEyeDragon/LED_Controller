

#### Protocol specification

LED_Controller protocol is used for serial communication between microcontrollers/computers.


This protocol is designed for 9600 baud
##### RFC (request for connection)
2 requests with 0x11 should be sent.
Then the receiving device replies with 0x1 or 0x0

##### When replied with 0x0
Your request has been refused. 

##### When replied with 0x1
Your connection has been accepted. And the receiving device will now accept your data.

##### Closing connection
The connection should be closed before disconnecting the device.
This should be done with the following sequence:

0xAD followed with 0xAC