using BloomFilter;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using System.Data;
using BloomFilter.Configurations;

namespace ConsoleApp
{
    internal class Program
    {
        static void Main(string[] args)
        {

            // create a new async task and start it here
            Task.Run(async () =>
            {
                {

                    //if (true)
                    //{
                    //    var bloomFilter2 = FilterMemory.Deserialize(File.ReadAllBytes(@"c:\temp\bloomTest.bin"));

                    //    var x2 = bloomFilter2.Contains($"HelloWorld");
                    //    var y2 = bloomFilter2.Contains($"HelloWorldx");
                    //    if (x2 != true || y2 != false)
                    //    {
                    //        Console.WriteLine("!");
                    //    }
                    //}

                    var DataSize = 1_000_000;

                    var dictData = new List<string>();
                    var s = $"property_{Guid.NewGuid().ToString()}_name";

                    for (var x = 0; x < 5; x++)
                    {
                        s += $"property_{Guid.NewGuid().ToString()}_name";
                    }

                    dictData.Add($"HelloWorld");


                    for (var i = 0; i < DataSize; i++)
                    {
                        dictData.Add($"{i}-{s}{Guid.NewGuid().ToString()}");
                    }


                    //FilterMemoryOptions options = new FilterMemoryOptions();
                    //options.ExpectedElements = DataSize;
                    //options.ErrorRate = 0.01;
                    //options.Name = "TestBloomFilter!";

                    //var bloomFilter = FilterBuilder.Build(options);
                    var bloomFilter = FilterBuilder.Build(DataSize, 0.01, "X!!!!filterX");
                    var dictFilter = new Dictionary<string, bool>();

                    for (var i = 0; i < DataSize; i++)
                    {
                        dictFilter.Add(dictData[i], true);
                        bloomFilter.Add(dictData[i]);
                    }

                    //for (var i = 0; i < DataSize; i++)
                    //{
                    //    if (!bloomFilter.Contains(dictData[i]))
                    //    {
                    //        throw new Exception("!");
                    //    }
                    //}



                    while (int.Parse("0") == 0)
                    {
                        GC.Collect();
                        dictData.Clear();
                        Thread.Sleep(1000);
                        var x = bloomFilter.Contains($"HelloWorld");
                        var y = bloomFilter.Contains($"HelloWorldx");
                        if (x != true || y != false)
                        {
                            Console.WriteLine("!");
                        }


                        if (bloomFilter is FilterMemory)
                        {


                            // serialize the filter to a file
                            using (var fileStream = new FileStream(@"c:\temp\bloomTest.bin", FileMode.Create, FileAccess.Write))
                            {
                                await ((FilterMemory)bloomFilter).SerializeAsync(fileStream);                                
                            }

                            // create a IBloomFilter with an arbitrary expectedElements since it won't matter
                            // this is needed since DeserializeAsync is not a static and so we need and instance 
                            var bloomFilter2 = FilterBuilder.Build(1);

                            // deserialize the filter from the file
                            await ((FilterMemory)bloomFilter2).DeserializeAsync(File.OpenRead(@"c:\temp\bloomTest.bin"));


                            //var bytes = bloomFilter.Serialize();
                            //File.WriteAllBytes(@"c:\temp\bloomTest.bin", bytes);

                            //var bloomFilter2 = FilterBuilder.Build(FilterMemoryOptions.Deserialize(bytes));
                            //var bloomFilter2 = FilterMemory.Deserialize(File.ReadAllBytes(@"c:\temp\bloomTest.bin"));

                            var x2 = bloomFilter2.Contains($"HelloWorld");
                            var y2 = bloomFilter2.Contains($"HelloWorldx");
                            if (x2 != true || y2 != false)
                            {
                                Console.WriteLine("!");
                            }
                        }


                    }

                    while (true)
                    {
                        GC.Collect();
                        dictFilter.Clear();
                        Thread.Sleep(1000);
                    }

                }


                //var config = DefaultConfig.Instance.WithOptions(ConfigOptions.DisableOptimizationsValidator);
                BenchmarkRunner.Run<MyBenchmarks>();

                //var config = DefaultConfig.Instance;
                //BenchmarkRunner.Run<MyBenchmarks>(config);

                Console.WriteLine($"X1 {MyBenchmarks.process.WorkingSet64}");
                GC.Collect();
                Console.WriteLine($"X2 {MyBenchmarks.process.WorkingSet64}");
                while (true)
                {
                    Thread.Sleep(1000);
                    Console.Write(".");
                }

                Environment.Exit(0);

                IBloomFilter bf = FilterBuilder.Build(10000000, 0.01);


                bf.Add("Value");

                Console.WriteLine(bf.Contains("Value"));



                Console.WriteLine("Hello, World!");
            }).Wait();


        }
    }
}
