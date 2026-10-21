using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnityLocalizationStudio.Models;

namespace UnityLocalizationStudio.Core
{
    public class GameDataExtractor
    {
        /// <summary>
        /// Trích xuất chính xác 100% các chuỗi văn bản Gameplay thực tế từ Kitchen DLLs
        /// (Bao gồm Menus, Buttons, Appliances, Dishes, Recipes, Cards, Tutorials, Settings)
        /// </summary>
        public static List<TranslationEntry> ExtractPureGameContent(string managedDir)
        {
            var results = new List<TranslationEntry>();
            var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(managedDir);
            var readerParams = new ReaderParameters { AssemblyResolver = resolver };

            // Danh sách các DLL nghiệp vụ cốt lõi chứa text người chơi thấy
            string[] targetDllNames = new[]
            {
                "Kitchen.Common.dll",
                "Kitchen.GameData.dll",
                "Kitchen.TutorialMode.dll",
                "Kitchen.RestaurantMode.dll",
                "Kitchen.FranchiseMode.dll",
                "Kitchen.ResearchMode.dll",
                "Kitchen.PostgameMode.dll"
            };

            foreach (var dllName in targetDllNames)
            {
                string dllPath = Path.Combine(managedDir, dllName);
                if (!File.Exists(dllPath)) continue;

                using var asm = AssemblyDefinition.ReadAssembly(dllPath, readerParams);
                foreach (var module in asm.Modules)
                {
                    foreach (var type in module.Types)
                    {
                        ExtractFromPureType(type, dllName, results, seenKeys);
                    }
                }
            }

            return results;
        }

        private static void ExtractFromPureType(TypeDefinition type, string dllName, List<TranslationEntry> results, HashSet<string> seenKeys)
        {
            // Xử lý nested types
            foreach (var nested in type.NestedTypes)
            {
                ExtractFromPureType(nested, dllName, results, seenKeys);
            }

            // Chỉ tập trung vào các Type thuộc Menu, UI, GameData, Item, Card, Appliance, Dish, Recipe, Tutorial
            string typeNameLower = type.Name.ToLowerInvariant();
            bool isRelevantType = typeNameLower.Contains("menu") ||
                                  typeNameLower.Contains("view") ||
                                  typeNameLower.Contains("gamedata") ||
                                  typeNameLower.Contains("appliance") ||
                                  typeNameLower.Contains("dish") ||
                                  typeNameLower.Contains("recipe") ||
                                  typeNameLower.Contains("card") ||
                                  typeNameLower.Contains("item") ||
                                  typeNameLower.Contains("tutorial") ||
                                  typeNameLower.Contains("instruction") ||
                                  typeNameLower.Contains("unlock") ||
                                  typeNameLower.Contains("cosmetic") ||
                                  typeNameLower.Contains("setting");

            if (!isRelevantType) return;

            foreach (var method in type.Methods)
            {
                if (method.Body == null || method.Body.Instructions == null)
                    continue;

                foreach (var instr in method.Body.Instructions)
                {
                    if (instr.OpCode == OpCodes.Ldstr && instr.Operand is string text)
                    {
                        if (IsRealGameText(text))
                        {
                            string category = DetermineCategory(typeNameLower, method.Name.ToLowerInvariant());
                            string contextPath = $"GameContent/{type.Name}/{method.Name}";
                            string keyId = ContextManager.ComputeKeyId(contextPath, text);

                            if (seenKeys.Add(keyId))
                            {
                                results.Add(new TranslationEntry
                                {
                                    KeyId = keyId,
                                    ContextPath = contextPath,
                                    OriginalText = text.Trim(),
                                    VietnameseTranslation = string.Empty,
                                    Status = "Pending",
                                    Notes = category
                                });
                            }
                        }
                    }
                }
            }
        }

        private static bool IsRealGameText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            text = text.Trim();

            // Độ dài từ 2 đến 300 ký tự
            if (text.Length < 2 || text.Length > 300) return false;

            // Bỏ qua GUID, Path, URL, JSON, Shader
            if (text.StartsWith("{") || text.StartsWith("<") && text.EndsWith(">") && !text.Contains(" ")) return false;
            if (text.StartsWith("_") || text.StartsWith("http") || text.StartsWith("m_")) return false;
            if (text.EndsWith(".png") || text.EndsWith(".dll") || text.EndsWith(".asset")) return false;

            // Bỏ qua chuỗi kỹ thuật
            if (text.Contains("CPAPI") || text.Contains("Lambda") || text.Contains("OnUpdate") ||
                text.Contains("Exception") || text.Contains("StackTrace") || text.Contains("Photon"))
                return false;

            // Phải chứa ít nhất một chữ cái
            if (!text.Any(char.IsLetter)) return false;

            // Chuỗi phải bắt đầu bằng chữ cái hoặc số, hoặc là câu tiếng Anh chuẩn
            char first = text[0];
            if (!char.IsLetterOrDigit(first) && first != '[' && first != '(' && first != '\"') return false;

            return true;
        }

        private static string DetermineCategory(string typeName, string methodName)
        {
            if (typeName.Contains("menu")) return "[MENU & GIAO DIỆN] Nút bấm, tùy chọn hiển thị";
            if (typeName.Contains("dish") || typeName.Contains("recipe")) return "[MÓN ĂN & CÔNG THỨC] Tên món ăn và cách chế biến";
            if (typeName.Contains("appliance")) return "[THIẾT BỊ NHÀ BẾP] Tên lò nướng, bồn rửa, bàn cắt...";
            if (typeName.Contains("card") || typeName.Contains("unlock")) return "[THẺ BÀI & NÂNG CẤP] Hiệu ứng thẻ bài và buff/nerf";
            if (typeName.Contains("tutorial") || typeName.Contains("instruction")) return "[HƯỚNG DẪN CHƠI] Lời thoại chỉ dẫn người chơi";
            return "[GAMEPLAY] Nội dung trò chơi thực tế";
        }
    }
}
