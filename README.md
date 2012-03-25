# HttpClient
HttpClient used for the demos is available here: https://github.com/donatasm/WCF-Performance-Tuning/tree/master/HttpClient


# Demo 1. Replace serializer
Demonstrates how DataContractSerializer can be replaced with custom serializer (e. g. protobuf-net) by implementing custom message formatter.


# Demo 2. Service throttling
Demonstrates how increase MaxConcurrentCalls value. After starting the service, execute

HttpClient http://localhost/hello/dotnetgroup-lt 50 64

and check ServiceModelService 4.0.0.0/Percent Of Max Concurrent Calls counter.


# Demo 3. WCF idle slowness
Demonstrates WCF thread pool issue and a workaround.


# Demo 4. Asynchronous WCF service, APM pattern
Demonstrates how AsyncPattern = true works. After starting the service, execute

HttpClient.exe http://localhost/data 1 5

Note, that code provided when working with Task library, does not cover all the cases for handling exceptions.