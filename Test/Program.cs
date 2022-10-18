
using System;

namespace test
{
    internal class Program
    {
        static void Main(string[] args)
        {

            Console.WriteLine(DateTime.Now < DateTime.Now.AddDays(1));

            int i = 10;
            int f = i-- == 9 ? 1 : 0;
            Console.WriteLine(f);
            Console.WriteLine(i);
        }
    }
}
