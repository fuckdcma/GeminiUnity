import clr_test = False
# Let's inspect using ICSharpCode.Decompiler if available, or .NET Reflection via dotnet run
cs_code = """
using System;
using System.Reflection;
using System.IO;

class Program
{
    static void Main()
    {
        var asm = Assembly.LoadFrom(@"C:\\Program Files (x86)\\Steam\\steamapps\\common\\PlateUp\\PlateUp\\PlateUp_Data\\Managed\\Unity.TextMeshPro.dll");
        var t = asm.GetType("TMPro.ShaderUtilities");
        var m = t.GetMethod("UpdateShaderRatios", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Console.WriteLine("Method: " + m);
        var mb = m.GetMethodBody();
        Console.WriteLine("IL length: " + mb.GetILAsByteArray().Length);
    }
}
"""
print("OK")
