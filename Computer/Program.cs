using System;

namespace Computer
{
    class Program
    {
        static void Main(string[] args)
        {
            CPU cpu = new CPU();
            cpu.Run(@"C:\Dev\drama\factorial.bin", 0);
        }
    }
}
