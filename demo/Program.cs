using dbcnet;
using System;

namespace demo
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            var dbc = new DbcParse(@"E:\WorkSpace\VS\dbcnet\docs\DBC_template.dbc");
            dbc.Parse();
        }
    }
}
