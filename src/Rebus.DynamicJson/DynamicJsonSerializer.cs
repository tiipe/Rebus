using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rebus.Messages;
using Rebus.Serialization.Json;
using Rebus.Shared;

namespace Rebus.DynamicJson
{
    /// <summary>
    /// Implementation of <see cref="ISerializeMessages"/> that will deserialize incoming JSON messages into a <see cref="JObject"/>.
    /// This means that a message handler must implement <see cref="IHandleMessages{JObject}"/> in order to be able to handle a message.
    /// Outgoing messages will be serialized by using Rebus' ordinary <see cref="JsonMessageSerializer"/>, allowing for sending and
    /// publishing strongly typed messages.
    /// </summary>
    public class DynamicJsonSerializer : ISerializeMessages
    {
        static readonly JsonMessageSerializer InnerJsonSerializer = new JsonMessageSerializer();

        /// <summary>
        /// Serializes the transport message <see cref="Message"/> using JSON.NET and wraps it in a <see cref="TransportMessageToSend"/>.
        /// Delegates the call directly to <see cref="JsonMessageSerializer"/> to perform the actual serialization.
        /// </summary>
        public TransportMessageToSend Serialize(Message message)
        {
            return InnerJsonSerializer.Serialize(message);
        }

        /// <summary>
        /// Deserializes the transport message using JSON.NET from a <see cref="ReceivedTransportMessage"/> and wraps it in a <see cref="Message"/>.
        /// Will always deserialize the incoming message to a <see cref="JObject"/>, allowing an endpoint to receive arbitrary JSON
        /// </summary>
        public Message Deserialize(ReceivedTransportMessage transportMessage)
        {
            var headers = transportMessage.Headers;

            if (!headers.ContainsKey(Headers.ContentType))
            {
                throw Error("Incoming message does not have a {0} header!", Headers.ContentType);
            }

            var contentType = headers[Headers.ContentType] as string;
            if (contentType == null)
            {
                throw Error("Incoming {0} has the value null!", Headers.ContentType);
            }

            if (!contentType.Equals("text/json", StringComparison.InvariantCultureIgnoreCase))
            {
                throw Error("Unknown content type: {0} - must be text/json", contentType);
            }

            if (!headers.ContainsKey(Headers.Encoding))
            {
                throw Error("Incoming message does not have a {0} header!", Headers.Encoding);
            }

            var encoding = headers[Headers.Encoding] as string;
            if (encoding == null)
            {
                throw Error("Incoming {0} has the value null!", Headers.Encoding);
            }

            var decoder = Encoding.GetEncoding(encoding);
            var bodyText = decoder.GetString(transportMessage.Body);

            var jsonObject = JsonConvert.DeserializeObject<JObject>(bodyText);

            if (jsonObject["$type"].ToString()
                .Replace(" ", "")
                .Contains("System.Object[],mscorlib"))
            {
                var arrayOfObjects = jsonObject["$values"].ToArray()
                    .Cast<object>()
                    .ToArray();

                return new Message
                       {
                           Headers = headers.ToDictionary(h => h.Key, h => h.Value),
                           Messages = arrayOfObjects
                       };
            }

            return new Message
            {
                Headers = headers.ToDictionary(h => h.Key, h => h.Value),
                Messages = new object[] { jsonObject }
            };
        }

        Exception Error(string message, params object[] objs)
        {
            return new ApplicationException(string.Format(message, objs));
        }
    }
}
