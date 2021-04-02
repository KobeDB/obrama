using System;

namespace Assembler
{
    class Program
    {
        static void Main(string[] args)
        {
            MachineCodeGenerator generator = new(@"C:\Dev\drama\factorial.dra", @"C:\Users\Kobe De Broeck\source\repos\obrama\Assembler\dictionary.txt");
        }
    }
}
