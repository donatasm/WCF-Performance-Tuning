using System;
using System.Data.SqlClient;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Threading;
using System.Threading.Tasks;

namespace _04.AsyncService
{
  [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
  internal class DataService : IDataService
  {
    private const string _connectionString = @"data source=localhost\SQLEXPRESS;Integrated Security=SSPI;Asynchronous Processing=true;";
    private const string _command = "WAITFOR DELAY '00:00:10'; SELECT 1";

    public int GetData()
    {
      using (var connection = new SqlConnection(_connectionString))
      {
        connection.Open();

        using (var command = new SqlCommand(_command, connection))
        {
          using (var reader = command.ExecuteReader())
          {
            if (reader.Read())
            {
              return reader.GetInt32(0);
            }
          }
        }
      }

      return 0;
    }

    public IAsyncResult BeginGetData(AsyncCallback callback, object state)
    {
      var connection = new SqlConnection(_connectionString);
      var command = new SqlCommand(_command, connection);

      connection.Open();

      return
        Task.Factory
          .FromAsync(
            (Func<AsyncCallback, object, IAsyncResult>)command.BeginExecuteReader,
            (Func<IAsyncResult, SqlDataReader>)command.EndExecuteReader,
            state)
          .ContinueWith(
            t =>
            {
              var reader = t.Result;

              try
              {
                if (reader.Read())
                {
                  return reader.GetInt32(0);
                }
              }
              finally
              {
                reader.Dispose();
                command.Dispose();
                connection.Dispose();
              }

              return 0;
            })
           .WithAsyncCallback(callback, state);
    }

    public int EndGetData(IAsyncResult result)
    {
      return ((Task<int>)result).Result;
    }
  }

  [ServiceContract]
  public interface IDataService
  {
    //[OperationContract, WebGet(UriTemplate = "data")]
    //int GetData();

    [OperationContract(AsyncPattern = true), WebGet(UriTemplate = "data")]
    IAsyncResult BeginGetData(AsyncCallback callback, object state);
    int EndGetData(IAsyncResult result);
  }

  internal class Host
  {
    private static readonly Uri BaseAddress = new Uri("http://localhost");

    public static void Main()
    {
      var cancellationTokenSource = new CancellationTokenSource();
      ThreadPool.SetMaxThreads(1023, 4);

      var service = new DataService();
      var host = new WebServiceHost(service, BaseAddress);

      try
      {
        host.Open();
        StartPrintThreadPoolInfoWorker(cancellationTokenSource.Token);
        Console.ReadLine();
      }
      finally
      {
        host.Close();
        cancellationTokenSource.Cancel();
      }
    }

    private static void StartPrintThreadPoolInfoWorker(CancellationToken token)
    {
      Task.Factory.StartNew(r =>
      {
        while (!token.IsCancellationRequested)
        {
          int availableWorker, availableCompletionPort;
          int maxWorker, maxCompletionPort;
          ThreadPool.GetMaxThreads(out maxWorker, out maxCompletionPort);
          ThreadPool.GetAvailableThreads(out availableWorker, out availableCompletionPort);
          Console.WriteLine("WorkerThreadPool: {0} Used: {2}, CompletionPortPool: {1} Used: {3}", availableWorker, availableCompletionPort, maxWorker - availableWorker, maxCompletionPort - availableCompletionPort);
          Thread.Sleep(TimeSpan.FromSeconds(1));
        }
      }, token, TaskCreationOptions.LongRunning);
    }
  }

  public static class TaskExtensions
  {
    public static Task<TResult> WithAsyncCallback<TResult>(this Task<TResult> task, AsyncCallback callback, object state)
    {
      var taskCompletionSource = new TaskCompletionSource<TResult>(state);

      task.ContinueWith(
          t =>
          {
            taskCompletionSource.SetResult(task.Result);
            if (callback != null)
            {
              callback(taskCompletionSource.Task);
            }
          });

      return task;
    }
  }
}
