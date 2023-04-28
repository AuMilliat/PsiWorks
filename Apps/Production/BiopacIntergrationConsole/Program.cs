using BiopacDataIntegration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiopacIntergrationConsole
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("BiopacIntergrationConsole [PsiDataset] [BiopacData]");
                return;
            }
            GTLoader gTLoader = new GTLoader(args[0]);
            DateTime dateTimeReference = DateTime.UtcNow;
            if (!gTLoader.LoadReferenceTime(out dateTimeReference))
            {
                Console.WriteLine("Failed to load reference time!");
                return;
            }
            Console.WriteLine("Precessing...");
            gTLoader.Parse(args[1], dateTimeReference);
            Console.WriteLine("Done!");
        }
    }
}
