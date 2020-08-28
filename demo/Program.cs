using dbcnet;
using System;

namespace demo
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            DbcParse.Parse(@"E:\WorkSpace\VS\dbcnet\docs\nidbc.dbc");
            //var dbc = new DbcParse(@"C:\Users\julles\Desktop\test\CharCon_B1_OBC_DCDC_Hybrid_DBC_V1.1.dbc");
        }
    }
}
