using dbcnet;
using System;

namespace demo
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            var a = DbcParse.Parse(@"C:\WorkSpace\VS\dbcnet\docs\candb.dbc");
        }
    }
}
