using System;
using UnityLocalizationStudio.ResearchModules;

class Program
{
    static int Main()
    {
        Console.WriteLine("[TEST START] Bắt đầu kiểm định module TMPDistanceFieldShaderPreserverCloner...");
        var engine = new TMPDistanceFieldShaderPreserverCloner();

        bool valid = engine.ValidateMaterialPreset("Chewy SDF", 0.28f, 0.45f);
        if (!valid)
        {
            Console.WriteLine("[ASSERT ERROR] ValidateMaterialPreset thất bại với tham số chuẩn.");
            return 1;
        }

        bool invalid = engine.ValidateMaterialPreset("", 1.5f, 0.5f);
        if (invalid)
        {
            Console.WriteLine("[ASSERT ERROR] ValidateMaterialPreset không chặn tham số sai.");
            return 2;
        }

        var cloned = engine.CloneMaterialProperties("Chewy SDF", "ARIAL SDF");
        if (cloned["Status"].ToString() != "CLONED_SUCCESS")
        {
            Console.WriteLine("[ASSERT ERROR] CloneMaterialProperties trả về trạng thái sai.");
            return 3;
        }

        Console.WriteLine("[TEST SUCCESS] 100% Unit Tests & Integration Gates ĐẠT YÊU CẦU.");
        return 0;
    }
}