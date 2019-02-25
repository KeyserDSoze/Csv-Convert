using System;
using System.Collections.Generic;
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
                F = 4,
                Dict = new Dictionary<string, E>
                {
                    { "key1", new B() { O = "r", C = new C() { U = "uu" } } },
                    { "key2", new B() { O = "t", C = new C() { U = "yy" } } }
                },
                //Lister = new List<E> { new B() { O = "r", C = new C() { U = "uu" } }, new B() { O = "t", C = new C() { U = "yy" } } },
                Lister = new List<E>(),
                I = L.A,
                Value = null,
                G = new G() { J = 3, K = 5 }
            };
            string csv = Csv.CsvConvert.Serialize(a);
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(a);
            a = Csv.CsvConvert.Deserialize<A>(csv);
            Console.WriteLine($"end {csv.Length} vs {json.Length}");
            Console.ReadLine();
            Console.ReadLine();
        }
        public class A
        {
            public int F { get; set; }
            public E B { get; set; }
            public Dictionary<string, E> Dict { get; set; }
            public List<E> Lister { get; set; }
            public L I { get; set; }
            public decimal? Value { get; set; }
            public G G { get; set; }
        }
        public enum L
        {
            A,
            B,
            C
        }
        public class B : E
        {
            public string O { get; set; }
            public D C { get; set; }
        }
        public struct G
        {
            public double J { get; set; }
            public double K { get; set; }
        }

        public class C : D
        {

        }

    }
}
