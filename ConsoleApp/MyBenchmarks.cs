using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BloomFilter;

namespace ConsoleApp
{
    [MemoryDiagnoser]
    public class MyBenchmarks
    {

        public static Process process = Process.GetCurrentProcess();
        public static IBloomFilter bloomFilter;
        public static Dictionary<string, bool> dictFilter;

        private IList<string> dictData;
        public int DataSize = 3_000_000;
        public int dataLen = 1;

        [GlobalCleanup]
        public void Cleanup()
        {
            dictData.Clear();
        }

        [GlobalSetup]
        public void Setup()
        {

            //Console.WriteLine($"A {process.WorkingSet64}");
            dictData = new List<string>();
            var s = "";
            for (var x = 0; x < dataLen; x++)
            {
                s += $"property_{x}{Guid.NewGuid().ToString()}_name";
            }

            for (var i = 0; i < DataSize; i++)
            {
                dictData.Add($"{i}-{s}");
            }

            //Console.WriteLine($"B {process.WorkingSet64}");
        }

        [Benchmark]
        public void BloomFilter()
        {
            //Console.WriteLine("BloomFilter");
            bloomFilter = FilterBuilder.Build(DataSize, 0.01);

            for (var i = 0; i < DataSize; i++)
            {
                //filter.Add(filterData[i]);
                bloomFilter.Add(dictData[i]);
            }

            for (var i = 0; i < DataSize; i++)
            {
                if (!bloomFilter.Contains(dictData[i]))
                {
                }
            }
        }

        [Benchmark]
        public void Dictionary()
        {
            //Console.WriteLine("Dictionary");
            dictFilter = new Dictionary<string, bool>();

            for (var i = 0; i < DataSize; i++)
            {
                dictFilter.Add(dictData[i], true);
            }

            for (var i = 0; i < DataSize; i++)
            {
                if (!dictFilter.ContainsKey(dictData[i]))
                {
                }
            }
        }
    }
}

