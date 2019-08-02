# Messages Buffer Processor
Helps to controll large number of messages received in a short amount of time.
All Messages are collected and stored in buffer, then processed in configurable batches and timespans. 
Messages are separated by subjects.

Example - Windows service can process max 4 messages at time in paralel and processing time for each message is more than 5 sec, service received 100 messges in 2 seconds and than no more for then next hour. Message processor will store all messgaes in a buffer and every 6 seconds will take 4 and process them. 
