using System;
using System.IO;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Web;
using System.Xml;
using ProtoBuf;

namespace _01.ReplaceSerializer
{
    internal class SayHelloServiceHost
    {
        private static readonly Uri BaseAddress = new Uri("http://localhost");

        public static void Main()
        {
            var service = new SayHelloService();
            var host = new WebServiceHost(service, BaseAddress);

            // This will not work for WebHttpBehavior!
            //foreach (var endpoint in host.Description.Endpoints)
            //{
            //    endpoint.Behaviors.Add(new ProtoBuf.ServiceModel.ProtoEndpointBehavior());
            //}

            try
            {
                host.Open();
                Console.WriteLine("Listening...");
                Console.ReadKey(true);
            }
            finally
            {
                host.Close();
            }
        }
    }

    [ServiceContract]
    public interface ISayHello
    {
        [OperationContract, WebGet(UriTemplate = "hello/{name}")]
        [Proto]
        Hello SayHello(string name);
    }

    [ProtoContract]
    public class Hello
    {
        [ProtoMember(1)]
        public DateTime DateTime { get; set; }

        [ProtoMember(2)]
        public string Message { get; set; }
    }

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    internal class SayHelloService : ISayHello
    {
        public Hello SayHello(string name)
        {
            return new Hello
            {
                DateTime = DateTime.Now,
                Message = String.Format("Hello {0}", name)
            };
        }
    }

    public class Proto : Attribute, IDispatchMessageFormatter, IOperationBehavior
    {
        private const string StartElementName = "Binary";
        public static readonly MessageVersion MessageVersion = MessageVersion.None;
        public static readonly string Action = String.Empty;

        public void DeserializeRequest(Message message, object[] parameters)
        {

            if (parameters != null && parameters.Length > 0)
            {
                if (!message.IsFault)
                {
                    try
                    {
                        parameters[0] = Deserialize(message);
                    }
                    catch (Exception)
                    {
                        parameters[0] = null;
                    }
                }
            }
        }

        public Message SerializeReply(MessageVersion messageVersion, object[] parameters, object result)
        {
            if (result != null)
            {
                return Serialize(result);
            }

            return EmptyMessage();
        }

        private class StreamBodyWriter : BodyWriter
        {
            private readonly byte[] _data;

            public StreamBodyWriter(byte[] data)
                : base(true)
            {
                _data = data;
            }

            protected override void OnWriteBodyContents(XmlDictionaryWriter writer)
            {
                writer.WriteStartElement(StartElementName);
                writer.WriteBase64(_data, 0, _data.Length);
                writer.WriteEndElement();
            }
        }

        private static Message EmptyMessage()
        {
            return Message.CreateMessage(MessageVersion, Action);
        }

        private static Message Serialize(object obj)
        {
            Message message;

            using (var stream = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(stream, obj);
                message = Message.CreateMessage(MessageVersion, Action, new StreamBodyWriter(stream.ToArray()));
            }

            var contentType = "application/octet-stream";
            var httpResponseMessageProperty = new HttpResponseMessageProperty();
            httpResponseMessageProperty.Headers.Add(HttpResponseHeader.ContentType, contentType);
            var webBodyFormatMessageProperty = new WebBodyFormatMessageProperty(WebContentFormat.Raw);

            message.Properties[HttpResponseMessageProperty.Name] = httpResponseMessageProperty;
            message.Properties[WebBodyFormatMessageProperty.Name] = webBodyFormatMessageProperty;

            return message;
        }

        private object Deserialize(Message message)
        {
            var reader = message.GetReaderAtBodyContents();
            reader.ReadStartElement(StartElementName);
            var content = reader.ReadContentAsBase64();
            var obj = ProtoBuf.Serializer.Deserialize<object>(new MemoryStream(content));
            return obj;
        }

        public void AddBindingParameters(OperationDescription operationDescription, BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyClientBehavior(OperationDescription operationDescription, ClientOperation clientOperation)
        {
        }

        public void ApplyDispatchBehavior(OperationDescription operationDescription, DispatchOperation dispatchOperation)
        {
            dispatchOperation.Formatter = this;
        }

        public void Validate(OperationDescription operationDescription)
        {
        }
    }
}
