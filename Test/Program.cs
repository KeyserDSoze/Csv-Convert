using System;
using TestLibrary;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            A a = new A()
            {
                B = new B() { O = "O", C = new C() { U = "AA" } },
                F = "F"
            };
            string csv = Csv.CsvConvert.Serialize(a);
            a = Csv.CsvConvert.Deserialize<A>(csv);
            Console.WriteLine("end");
            Console.ReadLine();
        }
        public class A
        {
            public string F { get; set; }
            public E B { get; set; }
        }
        public class B : E
        {
            public string O { get; set; }
            public D C { get; set; }
        }
       
        public class C : D
        {
            
        }
        
    }
}
