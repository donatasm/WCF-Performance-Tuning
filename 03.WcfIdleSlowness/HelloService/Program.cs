namespace WCF.Performance.Samples
{
    using System;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.Threading;

    enum TransportType
    {
        Tcp,
        Pipe,
        Http
    }

    class Program
    {
        const string ServiceBaseAddressSuffix = "/HelloApp/HelloService.svc";
        const TransportType transportType = TransportType.Tcp;

        static void Main(string[] args)
        {
          // Workaround the CLR Threadpool issue
          ThreadPoolTimeoutWorkaround.DoWorkaround();

            var host = new ServiceHost(typeof(HelloService), new Uri(GetServiceBaseAddress()));
            host.AddServiceEndpoint(typeof(IHelloService), GetBinding(), "");
            host.Open();
            Console.WriteLine("Service started. Press any key to exit ...");
            Console.ReadLine();
        }

        static string GetServiceBaseAddress()
        {
            switch (transportType)
            {
                case TransportType.Tcp:
                    return "net.tcp://localhost" + ServiceBaseAddressSuffix;
                case TransportType.Http:
                    return "http://localhost" + ServiceBaseAddressSuffix;
                case TransportType.Pipe:
                    return "net.pipe://localhost" + ServiceBaseAddressSuffix;
            }

            return null;
        }

        static Binding GetBinding()
        {
            switch (transportType)
            {
                case TransportType.Tcp:
                    return new NetTcpBinding(SecurityMode.None);
                case TransportType.Http:
                    {
                        var binding = new BasicHttpBinding();
                        binding.UseDefaultWebProxy = false;
                        return binding;
                    }
                case TransportType.Pipe:
                    return new NetNamedPipeBinding(NetNamedPipeSecurityMode.None);
            }

            return null;
        }
    }

    static class ThreadPoolTimeoutWorkaround
    {
      static ManualResetEvent s_dummyEvent;
      static RegisteredWaitHandle s_registeredWait;

      public static void DoWorkaround()
      {
        // Create an event that is never set
        s_dummyEvent = new ManualResetEvent(false);

        // Register a wait for the event, with a periodic timeout. This causes callbacks
        // to be queued to an IOCP thread, keeping it alive
        s_registeredWait = ThreadPool.RegisterWaitForSingleObject(
            s_dummyEvent,
            (a, b) =>
            {
              // Do nothing
            },
            null,
            1000,
            false);
      }
    }

    [ServiceContract]
    interface IHelloService
    {
        [OperationContract]
        string Greet(string message);
    }

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,
        ConcurrencyMode = ConcurrencyMode.Multiple)]
    class HelloService : IHelloService
    {
        public string Greet(string message)
        {
            Console.WriteLine("Greet service: " + message);
            // Thread.Sleep(1000);
            return message.ToUpper() + ":" + DateTime.Now;
        }
    }
}
