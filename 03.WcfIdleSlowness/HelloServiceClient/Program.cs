namespace WCF.Performance.Samples
{
    using System;
    using System.Diagnostics;
    using System.Net;
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
        const int ThreadCount = 1;
        const int SleepTime = 15000;
        static ChannelFactory<IHelloService> channelFactory;
        const string ServiceBaseAddressSuffix = "/HelloApp/HelloService.svc";
        const TransportType transportType = TransportType.Tcp;

        static void Main()
        {
            // Allow max parallel HTTP requests
            ServicePointManager.DefaultConnectionLimit = ThreadCount;
            
            Console.WriteLine("Press any key ...");
            Console.ReadLine();

            GreetOnce();
            Console.WriteLine("Sleep for {0} ms", SleepTime);
            Thread.Sleep(SleepTime);
            GreetTogether();
            
            Console.WriteLine("Press any key to exit");
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

        private static void GreetTogether()
        {
            var threads = new Thread[ThreadCount];
            var clients = new IHelloService[ThreadCount];
            for (int i = 0; i < ThreadCount; i++)
            {
                clients[i] = CreateClient();
                var state = new HelloServiceInvoker(i, clients[i]);
                threads[i] = new Thread(new ThreadStart(state.InvokeService));
            }

            foreach (Thread t in threads)
            {
                t.Start();
            }

            foreach (Thread t in threads)
            {
                t.Join();
            }

            foreach (IHelloService t in clients)
            {
                ((IChannel)t).Close();
            }
        }

        private static void GreetOnce()
        {
            Console.WriteLine("First message invocation: ");
            IHelloService client = CreateClient();
            ((IChannel)client).Open();
            var invoker = new HelloServiceInvoker(0, client);
            invoker.InvokeService();
        }

        static void CloseChannelFactory()
        {
            if (channelFactory != null)
            {
                channelFactory.Close();
                channelFactory = null;
            }
        }

        static IHelloService CreateClient()
        {
            if (channelFactory == null)
            {
                var binding = new BasicHttpBinding();
                binding.UseDefaultWebProxy = false;
                channelFactory = new ChannelFactory<IHelloService>(GetBinding(), new EndpointAddress(GetServiceBaseAddress()));
            }

            return channelFactory.CreateChannel();
        }
    }

    [ServiceContract]
    interface IHelloService
    {
        [OperationContract]
        string Greet(string message);
    }

    class HelloServiceInvoker
    {
        public int Index;
        public IHelloService Client;
        public TimeSpan Time;
        readonly Stopwatch watch;

        public HelloServiceInvoker(int index, IHelloService client)
        {
            this.Index = index;
            this.Client = client;
            this.watch = new Stopwatch();
        }

        public void InvokeService()
        {
            watch.Start();
            this.Client.Greet("Greet from '" + this.Index + "'");
            this.Time = watch.Elapsed;
            Console.WriteLine("Latency for thread {0}: {1} seconds", this.Index, this.Time.TotalSeconds);
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
}
