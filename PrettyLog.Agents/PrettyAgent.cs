using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using PrettyLog.Core.DataAccess;

namespace PrettyLog.Agents
{
    public class PrettyAgent
    {
        private readonly IDataContextFactory _contextFactory;

        public PrettyAgent(IDataContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public void Start()
        {
            var context = _contextFactory.Create();
            new Task(() =>
            {
                while (true)
                {
                    var t = 0;
                    for (var i = 0; i < 10000; i++)
                    {
                        t *= i;
                    }
                }
            }).Start();
            while (true)
            {
                var logItem = new LogItem()
                {
                    TimeStamp = DateTime.UtcNow,
                    Message = "machine status",
                    Type = "pretty.agent",
                    Object = new
                    {
                        CpuUsage = GetCpuUsage(),
                        AvaliableMemory = GetMemory(),
                        NetworkUsage = GetNetwork()
                    }
                };
                Console.WriteLine(logItem.ToJson());
                new LogInserter(context).Insert("logs", logItem);
                Thread.Sleep(5000);
            }
        }

        object GetNetwork()
        {
            var cards = printNetworkCards();
            float total = 0;
            foreach (var c in cards)
            {
                total = (float)getNetworkUtilization(c);
            }

            return total;
        }

        public IEnumerable<string> printNetworkCards()
        {
            PerformanceCounterCategory category = new PerformanceCounterCategory("Network Interface");
            return category.GetInstanceNames();
        }

        public double getNetworkUtilization(string networkCard)
        {
            const int numberOfIterations = 10;

            PerformanceCounter bandwidthCounter = new PerformanceCounter("Network Interface", "Current Bandwidth", networkCard);
            float bandwidth = bandwidthCounter.NextValue();//valor fixo 10Mb/100Mn/

            PerformanceCounter dataSentCounter = new PerformanceCounter("Network Interface", "Bytes Sent/sec", networkCard);

            PerformanceCounter dataReceivedCounter = new PerformanceCounter("Network Interface", "Bytes Received/sec", networkCard);

            float sendSum = 0;
            float receiveSum = 0;
            Console.WriteLine(bandwidth);
            for (int index = 0; index < numberOfIterations; index++)
            {
                sendSum += dataSentCounter.NextValue();
                receiveSum += dataReceivedCounter.NextValue();
            }
            float dataSent = sendSum;
            float dataReceived = receiveSum;

            if (bandwidth == 0) return 0;
            if (numberOfIterations == 0) return 0;
            double utilization = (8 * (dataSent + dataReceived)) / (bandwidth * numberOfIterations) * 100;
            return utilization;
        }

        float GetMemory()
        {
            return new PerformanceCounter("Memory", "Available MBytes").NextValue();
        }

        float GetCpuUsage()
        {
            var c =
             new PerformanceCounter()
            {
                CategoryName = "Processor",
                CounterName = "% Processor Time",
                InstanceName = "_Total"
            };
            c.NextValue();
            Thread.Sleep(500);
            return c.NextValue();

        }
    }
}
