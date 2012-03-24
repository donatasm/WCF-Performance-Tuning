using System;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Web;
using System.Threading;

namespace _02.ServiceThrottling
{
    internal class SayHelloServiceHost
    {
        private static readonly Uri BaseAddress = new Uri("http://localhost");

        public static void Main()
        {
            var service = new SayHelloService();
            var host = new WebServiceHost(service, BaseAddress);

            host.Description.Behaviors.Add(new ServiceThrottlingBehavior { MaxConcurrentCalls = Int32.MaxValue });

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
        Hello SayHello(string name);
    }

    public class Hello
    {
        public DateTime DateTime { get; set; }
        public string Message { get; set; }
    }

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    internal class SayHelloService : ISayHello
    {
        public Hello SayHello(string name)
        {
            Thread.Sleep(500);

            return new Hello
            {
                DateTime = DateTime.Now,
                Message = String.Format("Hello {0}", name)
            };
        }
    }
}
