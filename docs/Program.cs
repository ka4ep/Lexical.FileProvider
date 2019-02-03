using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace docs
{
    class Program
    {
        static void Main(string[] args)
        {
            SharpZipLibExamples.Run(args);
            SharpCompressExamples.Run(args);
            ZipExamples.Run(args);
            PackageAbstractionsExamples.Run(args);
            Console.ReadKey();
        }
    }
}
