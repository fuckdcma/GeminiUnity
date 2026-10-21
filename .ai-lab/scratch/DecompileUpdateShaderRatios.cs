using System;
using System.Reflection;

class Program
{
    static void Main()
    {
        var asm = Assembly.LoadFrom(@"C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp\PlateUp_Data\Managed\Unity.TextMeshPro.dll");
        var t = asm.GetType("TMPro.ShaderUtilities");
        var m = t.GetMethod("UpdateShaderRatios", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        var mb = m.GetMethodBody();
        var bytes = mb.GetILAsByteArray();
        Console.WriteLine("UpdateShaderRatios IL bytes: " + BitConverter.ToString(bytes));
    }
}
