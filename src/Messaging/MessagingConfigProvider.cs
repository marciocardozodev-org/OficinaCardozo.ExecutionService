using System;
using Microsoft.Extensions.Configuration;

namespace OficinaCardozo.ExecutionService.Messaging
{
    public class MessagingConfigProvider
    {
        public static MessagingConfig GetConfig(IConfiguration configuration)
        {
            return new MessagingConfig
            {
                InputQueue = Environment.GetEnvironmentVariable("INPUT_QUEUE") ?? configuration["Messaging:InputQueue"],
                OutputTopic = Environment.GetEnvironmentVariable("OUTPUT_TOPIC") ?? configuration["Messaging:OutputTopic"]
            };
        }
    }
}
