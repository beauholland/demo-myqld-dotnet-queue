using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace dotnet_client_consumer
{
    class Program
    {
        // IAM access keys and secrets
        private static string accessKeyId = "XXXX";
        private static string secretAccessKey = "XXXX";

        // QUEUE SETTINGS
        // New Request Queue Name
        private static string newRequestQueueName = "myqld_blarga_newservicerequest_test.fifo";
        // Get the Queue Url for Queue Name
        private static string newRequestQueueUrl = "https://sqs.ap-southeast-2.amazonaws.com/230234428082/myqld_blarga_newservicerequest_test.fifo";
        // New Request Queue Name
        private static string newRequestNumber = "BLW2000000";
        private static string newRequestMessageGroup = "NewRequests";

        // S3 SETTINGS
        private static string bucketName = "myqldservicerequestattachments";


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
            Console.WriteLine("1) SQS - Create Message");
            Console.WriteLine("2) SQS - List Messages");
            Console.WriteLine("3) SQS - Delete Message");
            Console.WriteLine("4) S3  - Upload File");
            Console.WriteLine("5) S3  - List Files");
            Console.WriteLine("6) S3  - Download File");
            Console.WriteLine("7) --Exit--");
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
                    UploadFile();
                    return true;
                case "5":
                    ListFiles();
                    return true;
                case "6":
                    DownloadFile();
                    return true;
                case "7":
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

        public static void UploadFile()
        {
            Console.WriteLine("\n--- UPLOAD FILE");
            Console.Write("Enter Filename:");
            var newS3fileName = Console.ReadLine();
            
            using (IAmazonS3 client = new AmazonS3Client(accessKeyId, secretAccessKey, RegionEndpoint.APSoutheast2))
            {
                WritingAnObjectAsync(client, newS3fileName).Wait();
            }
            
            Console.WriteLine($"\n--- FILE UPLOADED");

            Console.WriteLine("\n\n--- PRESS <ENTER>");
            Console.ReadLine();
        }

        static async Task WritingAnObjectAsync(IAmazonS3 client, string newS3fileName)
        {
            try
            {
                // Put the object-set ContentType and add metadata if required
                var putRequest = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = newS3fileName,
                    FilePath = "example.pdf",
                    ContentType = "application/json"
                };

                putRequest.Metadata.Add("x-amz-meta-title", "BLW2222222");
                PutObjectResponse response = await client.PutObjectAsync(putRequest);
            }
            catch (AmazonS3Exception e)
            {
                Console.WriteLine("Error encountered ***. Message:'{0}' when writing an object", e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unknown encountered on server. Message:'{0}' when writing an object", e.Message);
            }
        }

        public static void ListFiles() 
        {
            Console.WriteLine("\n--- LIST FILES");
            using (IAmazonS3 client = new AmazonS3Client(accessKeyId, secretAccessKey, RegionEndpoint.APSoutheast2))
            {
                ListingObjectsAsync(client).Wait();
            }

            Console.WriteLine("\n\n--- PRESS <ENTER>");
            Console.ReadLine();
        }
        static async Task ListingObjectsAsync(IAmazonS3 client)
        {
            try
            {
                ListObjectsV2Request request = new ListObjectsV2Request
                {
                    BucketName = bucketName,
                    MaxKeys = 10
                };
                ListObjectsV2Response response;
                do
                {
                    response = await client.ListObjectsV2Async(request);

                    // Process the response.
                    foreach (S3Object entry in response.S3Objects)
                    {
                        Console.WriteLine("key = {0} size = {1}",
                            entry.Key, entry.Size);
                    }
                    Console.WriteLine("Next Continuation Token: {0}", response.NextContinuationToken);
                    request.ContinuationToken = response.NextContinuationToken;
                } while (response.IsTruncated);
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
                Console.WriteLine("S3 error occurred. Exception: " + amazonS3Exception.ToString());
                Console.ReadKey();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.ToString());
                Console.ReadKey();
            }
        }

        public static void DownloadFile()
        {
            Console.WriteLine("\n--- DOWNLOAD FILE");
            Console.Write("Enter Filename:");
            var downloadFileName = Console.ReadLine();
            using (IAmazonS3 client = new AmazonS3Client(accessKeyId, secretAccessKey, RegionEndpoint.APSoutheast2))
            {
                ReadObjectDataAsync(client, downloadFileName).Wait();
            }

            Console.WriteLine("\n\n--- PRESS <ENTER>");
            Console.ReadLine();
        }

        static async Task ReadObjectDataAsync(IAmazonS3 client, string downloadFileName)
        {
            // string responseBody = "";
            try
            {
                GetObjectRequest request = new GetObjectRequest
                {
                    BucketName = bucketName,
                    Key = downloadFileName
                };
                using (GetObjectResponse response = await client.GetObjectAsync(request))
                using (Stream responseStream = response.ResponseStream)
                using (Stream s = File.Create(downloadFileName))
                {
                    responseStream.CopyTo(s);
                }
                //using (StreamReader reader = new StreamReader(responseStream))
                //{
                //    //string title = response.Metadata["x-amz-meta-title"]; // Assume you have "title" as medata added to the object.
                //    //string contentType = response.Headers["Content-Type"];
                //    //Console.WriteLine("Object metadata, Title: {0}", title);
                //    //Console.WriteLine("Content type: {0}", contentType);

                //    responseBody = reader.ReadToEnd(); // Now you process the response body.

                //}
                
            }
            catch (AmazonS3Exception e)
            {
                // If bucket or object does not exist
                Console.WriteLine("Error encountered ***. Message:'{0}' when reading object", e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unknown encountered on server. Message:'{0}' when reading object", e.Message);
            }
        }

    }
}
