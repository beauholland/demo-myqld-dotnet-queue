
Update program.cs with:
- accessKeyId = 
- secretAccessKey = 
- newRequestQueueName = 
- newRequestQueueUrl =



Considerations:

New Request Queue

--- What happens is everything goes wrong ---
- Messages will be deleted after x days
- How can we keep a copy of all messages, just in case?
- If error on client correlate messageId to link info about what went wrong with message in dead letter 




--- Visibility Timeout ---
Q: How long should it take to process a message and download attachments?
NOTE: messages need to be processed and deleted within this timeframe - if not the "Receive Count" will increase and the message will not be able to be deleted.
NOTE: This has considerations for the dead letter queue configuration: "maximum receives"

More info:
Visibility timeout sets the length of time that a message received from a queue (by one consumer) will not be visible to the other message consumers.

The visibility timeout begins when Amazon SQS returns a message. If the consumer fails to process and delete the message before the visibility timeout expires, 
the message becomes visible to other consumers. If a message must be received only once, your consumer must delete it within the duration of the visibility timeout.

The default visibility timeout setting is 30 seconds. This setting applies to all messages in the queue. Typically, you should set the visibility timeout to the 
maximum time that it takes your application to process and delete a message from the queue.

For optimal performance, set the visibility timeout to be larger than the AWS SDK read timeout. 
This applies to using the ReceiveMessage API action with either short polling or long polling.




--- MaxNumberOfMessages ---
Q: How many messages do you want to processes at a time?
Q: How frequently will we be polling for new messages?

More Info:
MaxNumberOfMessages is a setting when polling to find new messages




--- Message retention period ---
Q: How long should we keep messages in the "New Request" queue?
Q: How long should we keep messages in the "Dead Letter New Request" queue?

More info:
The message retention period is the amount of time that Amazon SQS retains a message that does not get deleted. Amazon SQS automatically deletes messages that have 
been in a queue for more than the maximum message retention period. The default retention period is 4 days. The retention period has a range of 60 seconds to 1,209,600 seconds (14 days).

The expiration of a message is always based on its original enqueue timestamp. When a message is moved to a dead-letter queue, the enqueue timestamp remains unchanged. 
For example, if a message spends 1 day in the original queue before being moved to a dead-letter queue, and the retention period of the dead-letter queue is set to 4 days, 
the message is deleted from the dead-letter queue after 3 days. For this reason, we recommend that you always set the retention period of a dead-letter queue to be longer 
than the retention period of the original queue.


--- Dead Letter Queue: Maximum receives ---
Q: What is the maximum number of times a message can be recieved before going into the dead letter queue?

More info:
Maximum receives:The maximum number of times that a message can be received by consumers. When this value is exceeded for a message the message will be automatically 
sent to the Dead Letter Queue.