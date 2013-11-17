using System;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;

namespace PrettyLog.Tests
{
    [TestFixture]
    public class PerformanceFixture
    {
         [Test]
         public void foo()
         {
             var c = new PerformanceCounter("Processor", "% Processor Time", "_Total");
             Enumerable.Range(1,10).ToList().ForEach(x =>
             {
                 Console.WriteLine(c.NextValue());
             });
             

         }

    }
}