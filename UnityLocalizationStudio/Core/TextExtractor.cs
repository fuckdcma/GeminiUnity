using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnityLocalizationStudio.Models;

namespace UnityLocalizationStudio.Core
{
    public class TextExtractor
    {
        private static readonly Regex GuidRegex = new Regex(@"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$", RegexOptions.Compiled);
        private static readonly Regex FileExtensionRegex = new Regex(@"\.(png|jpg|jpeg|wav|mp3|ogg|asset|prefab|mat|shader|unity|dll|json|txt|xml|bytes)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex ShaderOrInternalPropRegex = new Regex(@"^(_[A-Za-z0-9]+|m_[A-Za-z0-9]+|k__[A-Za-z0-9]+)$", RegexOptions.Compiled);
        private static readonly Regex HexColorRegex = new Regex(@"^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$", RegexOptions.Compiled);

        /// <summary>
        /// Trích xuất toàn bộ chuỗi văn bản tiềm năng từ các Managed Assembly của game
        /// </summary>
        public static List<TranslationEntry> ExtractFromManagedAssemblies(GameInfo gameInfo, Action<string>? logCallback = null)
        {
            var results = new List<TranslationEntry>();
            var seenExactKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string managedDir = Path.Combine(gameInfo.DataDirectory, "Managed");
            if (!Directory.Exists(managedDir))
            {
                logCallback?.Invoke("Không tìm thấy thư mục Managed để quét chuỗi tĩnh.");
                return results;
            }

            // Chọn các Assembly có liên quan trực tiếp đến nội dung game, loại trừ .NET Core, Unity Engine chuẩn
            var targetDlls = Directory.GetFiles(managedDir, "*.dll", SearchOption.TopDirectoryOnly)
                .Where(dll =>
                {
                    string name = Path.GetFileName(dll);
                    // Bỏ qua các DLL của hệ điều hành, runtime Mono, engine core và thư viện hạ tầng bên thứ ba
                    if (name.StartsWith("System.", StringComparison.OrdinalIgnoreCase) ||
                        name.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase) ||
                        name.StartsWith("mscorlib", StringComparison.OrdinalIgnoreCase) ||
                        name.StartsWith("Mono.", StringComparison.OrdinalIgnoreCase) ||
                        name.StartsWith("I18N", StringComparison.OrdinalIgnoreCase) ||
                        name.StartsWith("UnityEngine", StringComparison.OrdinalIgnoreCase) ||
                        (name.StartsWith("Unity.", StringComparison.OrdinalIgnoreCase) && !name.Contains("Kitchen")) ||
                        name.StartsWith("Photon", StringComparison.OrdinalIgnoreCase) ||
                        name.StartsWith("Sentry", StringComparison.OrdinalIgnoreCase) ||
                        name.StartsWith("Newtonsoft", StringComparison.OrdinalIgnoreCase) ||
                        name.StartsWith("websocket", StringComparison.OrdinalIgnoreCase) ||
                        name.StartsWith("Novell", StringComparison.OrdinalIgnoreCase) ||
                        name.StartsWith("TwitchLib", StringComparison.OrdinalIgnoreCase) ||
                        name.StartsWith("Epic", StringComparison.OrdinalIgnoreCase) ||
                        name.StartsWith("PlayEveryWare", StringComparison.OrdinalIgnoreCase) ||
                        name.StartsWith("Steamworks", StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                    return true;
                }).ToList();

            logCallback?.Invoke($"Phát hiện {targetDlls.Count} assemblies nghiệp vụ của game cần phân tích...");

            var readerParams = new ReaderParameters
            {
                AssemblyResolver = new DefaultAssemblyResolver()
            };
            ((DefaultAssemblyResolver)readerParams.AssemblyResolver).AddSearchDirectory(managedDir);

            foreach (var dllPath in targetDlls)
            {
                string dllName = Path.GetFileNameWithoutExtension(dllPath);
                try
                {
                    using var assembly = AssemblyDefinition.ReadAssembly(dllPath, readerParams);
                    foreach (var module in assembly.Modules)
                    {
                        foreach (var type in module.Types)
                        {
                            ExtractFromType(type, dllName, results, seenExactKeys);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logCallback?.Invoke($"[Cảnh báo] Không thể đọc DLL {dllName}: {ex.Message}");
                }
            }

            logCallback?.Invoke($"Trích xuất hoàn tất: {results.Count} chuỗi văn bản hợp lệ.");
            return results;
        }

        private static void ExtractFromType(TypeDefinition type, string dllName, List<TranslationEntry> results, HashSet<string> seenExactKeys)
        {
            // Bỏ qua các kiểu sinh tự động của compiler nếu không chứa text
            if (type.Name.StartsWith("<PrivateImplementationDetails>", StringComparison.OrdinalIgnoreCase))
                return;

            // Xử lý các nested types
            foreach (var nestedType in type.NestedTypes)
            {
                ExtractFromType(nestedType, dllName, results, seenExactKeys);
            }

            foreach (var method in type.Methods)
            {
                if (method.Body == null || method.Body.Instructions == null)
                    continue;

                foreach (var instr in method.Body.Instructions)
                {
                    if (instr.OpCode == OpCodes.Ldstr && instr.Operand is string rawText)
                    {
                        if (IsValidUiString(rawText))
                        {
                            string contextPath = $"{dllName}/{type.Name}/{method.Name}";
                            string keyId = ContextManager.ComputeKeyId(contextPath, rawText);

                            if (seenExactKeys.Add(keyId))
                            {
                                results.Add(new TranslationEntry
                                {
                                    KeyId = keyId,
                                    Language = "EN",
                                    ContextPath = contextPath,
                                    OriginalText = rawText,
                                    VietnameseTranslation = string.Empty, // Để trống cho người dịch
                                    Status = "Pending",
                                    Notes = $"Trích xuất từ {dllName}.{type.Name}.{method.Name}"
                                });
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Bộ lọc thông minh phân biệt chuỗi hiển thị giao diện với các hằng số kỹ thuật/hệ thống
        /// </summary>
        public static bool IsValidUiString(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            text = text.Trim();

            // Độ dài tối thiểu 2 ký tự
            if (text.Length < 2)
                return false;

            // Bỏ qua GUID
            if (GuidRegex.IsMatch(text))
                return false;

            // Bỏ qua tên tệp tin
            if (FileExtensionRegex.IsMatch(text))
                return false;

            // Bỏ qua shader property hoặc internal variables (_MainTex, m_Value)
            if (ShaderOrInternalPropRegex.IsMatch(text))
                return false;

            // Bỏ qua mã màu hex (#FFFFFF, #FFF)
            if (HexColorRegex.IsMatch(text))
                return false;

            // Bỏ qua nếu chỉ toàn chữ số và dấu chấm, phẩy, gạch nối
            if (text.All(c => char.IsDigit(c) || char.IsPunctuation(c) || char.IsWhiteSpace(c)))
                return false;

            // Bỏ qua format JSON, XML hoặc tag lập trình thuần túy
            if ((text.StartsWith("{") && text.EndsWith("}")) || (text.StartsWith("<") && text.EndsWith(">") && !text.Contains(" ")))
                return false;

            // Bỏ qua đường dẫn URL
            if (text.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                text.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                text.StartsWith("steam://", StringComparison.OrdinalIgnoreCase))
                return false;

            // Phải chứa ít nhất một ký tự chữ cái (Letter)
            if (!text.Any(char.IsLetter))
                return false;

            return true;
        }
    }
}
