
using System;
class Program
{
    static int Main()
    {
        if (MathSandboxHelper.Add(15, 27) == 42)
        {
            Console.WriteLine("AT38_PASS");
            return 0;
        }
        return 1;
    }
}