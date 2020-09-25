using dbcnet;
using System;

namespace demo
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            DbcParse.Parse(@"E:\WorkSpace\VS\dbcnet\docs\DBC_template.dbc");
            //DbcParse.Parse(@"C:\Users\Julles\Desktop\\xxx.dbc");
        }
    }
}
