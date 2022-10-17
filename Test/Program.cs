using System;
using ExtraList.TimeList;

namespace Test
{
    internal class Program
    {
        static void Main(string[] args)
        {
            TimeList<int> ints = new TimeList<int>(6);



            ints.Add(0);
            ints.Add(1);
            ints.Add(2);
            ints.Add(3);
            ints.Add(123, DateTime.Now.AddSeconds(-15));
            ints.Add(4);
            ints.Add(5);
            ints.Add(6);

            ITime time = ints;


        }
    }
}
