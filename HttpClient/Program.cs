using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace HttpClient
{
    internal class Program
    {
        private const int DefaultRunCount = 1;
        private const int DefaultParallelCount = 1;

        public static void Main(string[] args)
        {
            Uri uriAddress;
            if (args.Length < 1 || !Uri.TryCreate(args[0], UriKind.Absolute, out uriAddress))
            {
                Console.WriteLine("Unspecified or invalid URI address");
                return;
            }

            int runCount;
            if (args.Length < 2 || !Int32.TryParse(args[1], out runCount))
            {
                runCount = DefaultRunCount;
            }

            int parallelCount;
            if (args.Length < 3 || !Int32.TryParse(args[2], out parallelCount))
            {
                parallelCount = DefaultParallelCount;
            }

            Run(uriAddress, runCount, parallelCount);
        }

        private static void Run(Uri uriAddress, int runCount, int parallelCount)
        {
            var tasks = new Task<IEnumerable<HttpResult>>[parallelCount];

            var stopwatch = Stopwatch.StartNew();
            for (var i = 0; i < parallelCount; i++)
            {
                tasks[i] = Task.Factory.StartNew(() => HttpGet(uriAddress, runCount));
            }
            Task.WaitAll(tasks);

            var totalTime = stopwatch.Elapsed;

            PrintResults(totalTime, parallelCount, tasks.SelectMany(t => t.Result).ToArray());
        }

        private static IEnumerable<HttpResult> HttpGet(Uri address, int count)
        {
            var stopwatch = new Stopwatch();
            var results = new HttpResult[count];

            for (var i = 0; i < count; i++)
            {
                var client = new HttpClient();
                var isError = false;
                var elapsed = 0L;
                stopwatch.Reset();

                try
                {
                    stopwatch.Start();
                    client.DownloadString(address);
                    elapsed = stopwatch.ElapsedMilliseconds;
                }
                catch (Exception)
                {
                    elapsed = stopwatch.ElapsedMilliseconds;
                    isError = true;
                }
                finally
                {
                    results[i] = new HttpResult
                    {
                        TimeTakenMs = (int)elapsed,
                        IsError = isError
                    };
                }
            }

            return results;
        }

        private static void PrintResults(TimeSpan totalTime, int parllelCount, ICollection<HttpResult> results)
        {
            var count = results.Count;
            var orderedResults = results.OrderBy(r => r.TimeTakenMs).ToList();

            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine("Total time taken:    {0}", totalTime);
            Console.WriteLine("Total requests:      {0}", count);
            Console.WriteLine("Total errors:        {0}", results.Count(r => r.IsError));
            Console.WriteLine("Throughput QPS:      {0}", count/totalTime.TotalSeconds);
            Console.WriteLine("Parallel count:      {0}", parllelCount);
            Console.WriteLine("50th percentile:     {0}", orderedResults[(count * 10) / 20].TimeTakenMs);
            Console.WriteLine("85th percentile:     {0}", orderedResults[(count * 17) / 20].TimeTakenMs);
            Console.WriteLine("95th percentile:     {0}", orderedResults[(count * 19) / 20].TimeTakenMs);
            Console.WriteLine("--------------------------------------------------");
        }

        private struct HttpResult
        {
            public int TimeTakenMs { get; set; }
            public bool IsError { get; set; }
        }
    }
}
