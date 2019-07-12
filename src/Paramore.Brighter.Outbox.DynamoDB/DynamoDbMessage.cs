using System;
using System.Collections.Generic;
using Amazon.DynamoDBv2.DataModel;
using Newtonsoft.Json;

namespace Paramore.Brighter.Outbox.DynamoDB
{
    public class DynamoDbMessage
    {
       /// <summary>
        /// The message body
        /// </summary>
        [DynamoDBProperty]
        public string Body { get; set; }
        
        /// <summary>
        /// The time at which the message was created, formatted as a string yyyy-MM-dd
        /// </summary>
        [DynamoDBProperty]
        public string CreatedAt { get; set; }

        /// <summary>
        /// The time at which the message was created, in ticks
        /// </summary>
        [DynamoDBRangeKey]
        public string CreatedTime { get; set; }
        
        /// <summary>
        /// The time at which the message was delivered, formatted as a string yyyy-MM-dd
        /// </summary>
        [DynamoDBProperty]
        public string DeliveredAt { get; set; }
        /// <summary>
        /// The time that the message was delivered to the broker, in ticks
        /// </summary>
        [DynamoDBGlobalSecondaryIndexRangeKey(indexName:"Delivered")]
        public string DeliveryTime { get; set; }
             
        /// <summary>
        /// A JSON object representing a dictionary of additional properites set on the message
        /// </summary>
        [DynamoDBProperty]
        public string HeaderBag { get; set; }
        
        /// <summary>
        /// The Id of the Message. Used as a Global Secondary Index
        /// </summary>
        [DynamoDBGlobalSecondaryIndexHashKey("MessageId")]
        public string MessageId { get; set; }
       
        /// <summary>
        /// The type of message i.e. MT_COMMAND, MT_EVENT etc. An enumeration rendered as a string
        /// </summary>
        [DynamoDBProperty]
        public string MessageType { get; set; }

       /// <summary>
        /// The Topic the message was published to
        /// </summary>
        [DynamoDBProperty]
        public string Topic { get; set; }

        /// <summary>
        /// A Composite Key consisting of the Topic the message has been sent to and the Date it was sent on
        /// </summary>
        [DynamoDBHashKey("Topic+Date")]
        public string TopicDate { get; set; }

         public DynamoDbMessage()
        {
        }

        public DynamoDbMessage(Message message)
        {
            var date = message.Header.TimeStamp == DateTime.MinValue ? DateTime.UtcNow : message.Header.TimeStamp;

            TopicDate = $"{message.Header.Topic}+{date:yyyy-MM-dd}";
            CreatedTime = $"{date.Ticks}";
            MessageId = message.Id.ToString();
            Topic = message.Header.Topic;
            MessageType = message.Header.MessageType.ToString();
            CreatedAt = $"{date}";
            HeaderBag = JsonConvert.SerializeObject(message.Header.Bag);
            Body = message.Body.Value;
        }

        public Message ConvertToMessage()
        {
            var messageId = Guid.Parse(MessageId);
            var messageType = (MessageType)Enum.Parse(typeof(MessageType), MessageType);
            var timestamp = DateTime.Parse(CreatedAt);
            var bag = JsonConvert.DeserializeObject<Dictionary<string, string>>(HeaderBag);

            var header = new MessageHeader(messageId, Topic, messageType, timestamp);

            foreach (var key in bag.Keys)
            {
                header.Bag.Add(key, bag[key]);
            }

            var body = new MessageBody(Body);

            return new Message(header, body);
        }

        public void MarkMessageDelivered(DateTime deliveredAt)
        {
            DeliveryTime = $"{deliveredAt.Ticks}";
            DeliveredAt = $"{deliveredAt:yyyy-MM-dd}";
        }
    }
}