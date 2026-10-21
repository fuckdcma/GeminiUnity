using System;
using System.Collections.Generic;

namespace UnityLocalizationStudio.Models
{
    public enum StepStatus
    {
        NotStarted,
        InProgress,
        Passed,
        Failed,
        Skipped
    }

    public class ProjectStepInfo
    {
        public int StepNumber { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public StepStatus Status { get; set; } = StepStatus.NotStarted;
        public string Summary { get; set; } = "";
        public DateTime? CompletedAt { get; set; }
    }

    public class LocalizationProject
    {
        public string ProjectId { get; set; } = Guid.NewGuid().ToString("N");
        public string ProjectName { get; set; } = "Dự Án Việt Hóa";
        public string GameDirectory { get; set; } = "";
        public string GameExeName { get; set; } = "";
        public string UnityVersion { get; set; } = "";
        public string Architecture { get; set; } = "Mono x64"; // Mono x64, IL2CPP x64, Mono x86
        public string ModProfile { get; set; } = "BepInEx 5 (Mono)"; // BepInEx 5 (Mono), BepInEx 6 (IL2CPP)
        public string BaseLanguage { get; set; } = "English (en-US)";
        public string TargetLanguage { get; set; } = "Tiếng Việt (vi-VN)";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime LastModified { get; set; } = DateTime.Now;

        public int CurrentStep { get; set; } = 1;
        public List<ProjectStepInfo> Steps { get; set; } = new List<ProjectStepInfo>();

        // File Paths
        public string RawTextCsvPath { get; set; } = "";
        public string RawFontsJsonPath { get; set; } = "";
        public string LiveTextMappingCsvPath { get; set; } = "";
        public string FontMappingConfigPath { get; set; } = "";
        public string TranslatedCsvPath { get; set; } = "";
        public string CustomFontBundlePath { get; set; } = "";
        public string EncryptedDatPath { get; set; } = "";
        public string PrereleaseZipPath { get; set; } = "";
        public string PrereleaseExePath { get; set; } = "";
        public string LastPrereleaseZipPath { get; set; } = "";
        public string LastPrereleaseExePath { get; set; } = "";
        public string ReleaseZipPath { get; set; } = "";
        public string ReleaseExePath { get; set; } = "";

        public LocalizationProject()
        {
            InitializeDefaultSteps();
        }

        public void InitializeDefaultSteps()
        {
            if (Steps.Count > 0) return;

            Steps = new List<ProjectStepInfo>
            {
                new ProjectStepInfo { StepNumber = 1, Title = "Tạo, quản lý Project", Description = "Tạo, lưu trữ và thiết lập cấu hình thông số dự án Việt hóa." },
                new ProjectStepInfo { StepNumber = 2, Title = "Quét cấu trúc game được chọn để chọn chế độ MOD phù hợp", Description = "Quét tệp thực thi, nhận diện Engine Unity Mono/IL2CPP và tự động chọn chế độ MOD phù hợp." },
                new ProjectStepInfo { StepNumber = 3, Title = "Quét Font/Text của game có thể xuất hiện trong Asset, DLL, Bundle,... (Quét Thô)", Description = "Quét và trích xuất chuỗi text thô và font tiềm năng từ Assets nhị phân, Managed DLLs và Bundles." },
                new ProjectStepInfo { StepNumber = 4, Title = "Cài thử Prototype (Mod Plugins Việt Hoá, Text mẫu, Font mẫu) vào Folder game", Description = "Cài đặt bản thử nghiệm gồm Mod Plugin, Text mẫu (LIVETEST_1..N) và Font mẫu (ARIAL SDF) vào thư mục game." },
                new ProjectStepInfo { StepNumber = 5, Title = "Tự bật game, tạo livetest thực tế, thông qua hệ thống hook, ram, màn hình,... để xác định chính xác các thông tin cần thiết như phương pháp hook,mock,... để ra được Text, Font", Description = "Tự bật game, tạo Live Test thực tế qua hook, RAM, telemetry màn hình để xác định phương pháp hook, mock Text và Font." },
                new ProjectStepInfo { StepNumber = 6, Title = "Qua Livetest tự bật, Quét text thực trong game (Xác định ngôn ngữ chính (Sau này sẽ MOD) và tất cả ngôn ngữ để đối chiếu) - Mapping text, Thay thử text mẫu vào để làm đánh giá kiểm tra hook (Ưu tiên tạo Log để phân tích lỗi nếu phát sinh)", Description = "Quét text thực tế trên UI, xác định ngôn ngữ chính và tất cả ngôn ngữ đối chiếu, thay thử text mẫu (LIVETEST_X) để đánh giá kiểm tra hook và tạo log chẩn đoán lỗi." },
                new ProjectStepInfo { StepNumber = 7, Title = "Qua Livetest tự bật, quét Font thực trong game (Quét và đánh dấu Font của ngôn ngữ được mod và các ngôn ngữ còn lại) - Mapping Font, Thay thử Font mẫu vào làm đánh giá kiểm tra hook (Ưu tiên tạo Log để phân tích lỗi nếu phát sinh)", Description = "Quét và đánh dấu font ngôn ngữ mod và các ngôn ngữ còn lại, mapping font, thay thử Font mẫu (ARIAL SDF) để đánh giá kiểm tra hook và tạo log chẩn đoán lỗi." },
                new ProjectStepInfo { StepNumber = 8, Title = "Cho phép Xuất/ Nhập file File text/font/Font Mapping để người dùng nhập file đã được việt hoá. mapping chuẩn font cần điều chỉnh", Description = "Xuất/Nhập file text, font và font mapping để người dùng nạp bản dịch tiếng Việt và cấu hình chuẩn font cần điều chỉnh." },
                new ProjectStepInfo { StepNumber = 9, Title = "Livetest thực tế sau Việt hoá, kiểm tra hook,ram,màn hình,... về ngôn ngữ, Font chữ, Hiệu ứng Font chữ (Ưu tiên tạo Log để phân tích lỗi nếu phát sinh)", Description = "Live Test thực tế sau Việt hóa, kiểm tra hook, RAM, màn hình về ngôn ngữ, font chữ, hiệu ứng font chữ và tạo log phân tích lỗi." },
                new ProjectStepInfo { StepNumber = 10, Title = "Xoá sạch Prototype nếu được đánh dấu là đạt test (để nhà phát hành việt hoá Test trực tiếp ZIP/EXE release)", Description = "Xóa sạch 100% tệp prototype sau khi đạt test để nhà phát hành kiểm thử trực tiếp gói cài đặt ZIP/EXE release." },
                new ProjectStepInfo { StepNumber = 11, Title = "Đóng gói tạm (Prerelease) Client Zip, Exe file chưa mã hoá để test lỗi Install, Unpack,...", Description = "Đóng gói bản Prerelease Client Zip và Exe chưa mã hóa để kiểm thử lỗi cài đặt, giải nén và vận hành." },
                new ProjectStepInfo { StepNumber = 12, Title = "Nếu sản được đánh đạt trong bước Prerelease, Đóng gói (LastPrerelease) Client Zip, Exe file mã hoá, Test lỗi Install, Unpack,... sau khi mã hoá", Description = "Đóng gói LastPrerelease mã hóa AES-256 bảo vệ bản quyền khi Prerelease đạt test, kiểm thử lỗi cài đặt và giải nén sau mã hóa." },
                new ProjectStepInfo { StepNumber = 13, Title = "Nếu LastPrerelease -> Đóng gói Release, Phát hành bản việt hoá", Description = "Xuất gói cài đặt Release chính thức và hoàn tất phát hành bản Việt hóa tới cộng đồng." }
            };
        }

        public void SetStepStatus(int stepNumber, StepStatus status, string summary = "")
        {
            var step = Steps.Find(s => s.StepNumber == stepNumber);
            if (step != null)
            {
                step.Status = status;
                if (!string.IsNullOrEmpty(summary)) step.Summary = summary;
                if (status == StepStatus.Passed) step.CompletedAt = DateTime.Now;
            }
            LastModified = DateTime.Now;
        }

        public StepStatus GetStepStatus(int stepNumber)
        {
            var step = Steps.Find(s => s.StepNumber == stepNumber);
            return step?.Status ?? StepStatus.NotStarted;
        }
    }
}
