using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;

namespace dotnet_client_consumer
{
    class Program
    {
        // IAM access keys and secrets
        private static string accessKeyId = "XXX";
        private static string secretAccessKey = "XXX";

        // New Request Queue Name
        private static string newRequestQueueName = "XXXX";
        // Get the Queue Url for Queue Name
        private static string newRequestQueueUrl = "https://sqs.ap-southeast-2.amazonaws.com/XXXX/XXXX.fifo";

        // New Request Queue Name
        private static string newRequestNumber = "BLW2000000";
        private static string newRequestMessageGroup = "NewRequests";

        static void Main(string[] args)
        {
            bool showMenu = true;
            while (showMenu)
            {
                showMenu = MainMenu();
            }
        }

        private static bool MainMenu()
        {
            Console.Clear();
            Console.WriteLine("--------");
            Console.WriteLine("AWS SQS");
            Console.WriteLine("--------");
            Console.WriteLine("Choose an option:");
            Console.WriteLine("1) Create Message");
            Console.WriteLine("2) List Messages");
            Console.WriteLine("3) Delete Message");
            Console.WriteLine("4) Exit");
            Console.Write("\r\nSelect an option: ");

            switch (Console.ReadLine())
            {
                case "1":
                    CreateMessage();
                    return true;
                case "2":
                    ListMessages();
                    return true;
                case "3":
                    DeleteMessage();
                    return true;
                case "4":
                    return false;
                default:
                    return true;
            }
        }



        public static void CreateMessage()
        {
            Console.WriteLine("\n--- CREATE MESSAGE");
            // ask for the request number
            Console.WriteLine($"Example Request Number: {newRequestNumber}"); // System.Windows.Forms.SendKeys.SendWait(newRequestNumber); does not exist in .net core
            Console.Write("Enter Request Number:");
            // store the new request number the user entered
            string requestNumber = Console.ReadLine();

            // load the example message from JSON file
            string exampleMessageString = File.ReadAllText("example-blarga-message.json");
            // parse the JSON string into object we can modify - we want to update the requestNumber >> serviceRequest.requestDetails.requestNumber
            JObject exampleMessageJObj = JObject.Parse(exampleMessageString);
            JObject serviceRequestjObj = (JObject)exampleMessageJObj["serviceRequest"];
            JObject requestDetailsjObj = (JObject)serviceRequestjObj["requestDetails"];
            // update the request number to what the user entered
            requestDetailsjObj.Property("requestNumber").Value = requestNumber;
            // update the example Message String ready for sending the message into the queue
            exampleMessageString = exampleMessageJObj.ToString();

            // Setup SQS connection
            using (IAmazonSQS sqs = new AmazonSQSClient(accessKeyId, secretAccessKey, RegionEndpoint.APSoutheast2))
            {
                var sqsMessageRequest = new SendMessageRequest
                {
                    QueueUrl = newRequestQueueUrl,
                    MessageBody = exampleMessageString,
                    MessageDeduplicationId = requestNumber, // we are using the Request Number entered from the user here
                    MessageGroupId = newRequestMessageGroup // "NewRequests"
                };

                sqs.SendMessageAsync(sqsMessageRequest);
            }

            Console.WriteLine($"--- CREATED MESSAGE > Request Number: {requestNumber}");
            //Console.WriteLine($"Body: {exampleMessageString}");

            Console.WriteLine("\n\n--- PRESS <ENTER>");
            Console.ReadLine();
        }

        public static void ListMessages()
        {
            Console.WriteLine("\n--- LISTING MESSAGES");
            
            // NOTE: "Visibility timeout" when you read a message - these messages will not be available for 30 secs (configurable value on SQS)
            // Visibility timeout sets the length of time that a message received from a queue(by one consumer) will not be visible to the other message consumers.
            // The visibility timeout begins when Amazon SQS returns a message. If the consumer fails to process and delete the message before the visibility timeout expires, 
            // the message becomes visible to other consumers.If a message must be received only once, your consumer must delete it within the duration of the visibility timeout.

            // Setup SQS connection
            using (IAmazonSQS sqs = new AmazonSQSClient(accessKeyId, secretAccessKey, RegionEndpoint.APSoutheast2))
            {
                // Get the Queue Url for Queue Name

                // Setup receive message Request
                var receiveMessageRequest = new ReceiveMessageRequest
                {
                    QueueUrl = newRequestQueueUrl,
                    MaxNumberOfMessages = 10, // how many messages do we want to return for a single poll between 1-10
                    VisibilityTimeout = 20 // overrides the visibility timeout if required
                };

                // Execute receive message request and return receive message response
                var receiveMessageResponse = sqs.ReceiveMessageAsync(receiveMessageRequest).Result;

                // loop over the messages in the response
                var messageCount = 1;
                Console.WriteLine($"\n--- FOUND {receiveMessageResponse.Messages.Count} MESSAGES");
                foreach (var message in receiveMessageResponse.Messages)
                {
                    Console.WriteLine("\n----------------------------------------------");
                    Console.WriteLine($"Message:{messageCount} \nMessageId:{message.MessageId} \nReceiptHandle:{message.ReceiptHandle}");
                    messageCount++;
                }

            }

            Console.WriteLine("\n\n--- PRESS <ENTER>");
            Console.ReadLine();
        }

        public static void DeleteMessage()
        {
            try
            {
                Console.WriteLine("\n--- DELETE MESSAGE");
                Console.Write("Enter Receipt Handle:");
                var receiptHandle = Console.ReadLine();
                // Setup SQS connection
                using (IAmazonSQS sqs = new AmazonSQSClient(accessKeyId, secretAccessKey, RegionEndpoint.APSoutheast2))
                {
                    var deleteRequest = new DeleteMessageRequest
                    {
                        QueueUrl = newRequestQueueUrl,
                        ReceiptHandle = receiptHandle
                    };

                    var deleteMessageResponse = sqs.DeleteMessageAsync(deleteRequest).Result;
                }
                Console.WriteLine($"\n--- DELETED MESSAGE");
            }
            catch (Exception ex) 
            {
                Console.WriteLine($"\n--- DELETE MESSAGE ERROR\n");
                Console.WriteLine(ex.ToString());
            }

            Console.WriteLine("\n\n--- PRESS <ENTER>");
            Console.ReadLine();

        }

    }
}
