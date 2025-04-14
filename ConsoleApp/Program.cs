using BloomFilter;

namespace ConsoleApp
{
    internal class Program
    {
        static void Main(string[] args)
        {

            IBloomFilter bf = FilterBuilder.Build(10000000, 0.01);


            bf.Add("Value");
            
            Console.WriteLine(bf.Contains("Value"));



            Console.WriteLine("Hello, World!");
        }
    }
}
