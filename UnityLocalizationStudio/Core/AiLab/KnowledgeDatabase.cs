using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;

namespace UnityLocalizationStudio.Core.AiLab
{
    // =========================================================================
    // SECTION IV DOMAIN MODELS (KNOWLEDGE BASE THỰC SỰ)
    // =========================================================================

    public class KnowledgeItem
    {
        public long Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Category { get; set; } = "TMP_Font";
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string DetailedContent { get; set; } = string.Empty;
        public string CodeSnippet { get; set; } = string.Empty;
        public string VerifiedStatus { get; set; } = "VERIFIED"; // VERIFIED, HYPOTHESIS, DEPRECATED
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string Tags { get; set; } = string.Empty;
    }

    public class ExperimentRecord
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string ProblemStatement { get; set; } = string.Empty;
        public string Hypothesis { get; set; } = string.Empty;
        public string ProposedModuleCode { get; set; } = string.Empty;
        public string TestCode { get; set; } = string.Empty;
        public string Status { get; set; } = "PENDING"; // PENDING, COMPILING, TESTING, SUCCESS, FAILED
        public string CompileOutput { get; set; } = string.Empty;
        public string TestOutput { get; set; } = string.Empty;
        public string FailureReason { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? CompletedAt { get; set; }
        public long ExecutionDurationMs { get; set; }
        public bool PromotedToLibrary { get; set; }
    }

    public class ModuleLibraryItem
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Capability { get; set; } = string.Empty;
        public string SourceCode { get; set; } = string.Empty;
        public string UnitTestSourceCode { get; set; } = string.Empty;
        public string Version { get; set; } = "1.0.0";
        public string Author { get; set; } = "AI_Ollama_Qwen2.5Coder";
        public string VerificationEvidence { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;
    }

    public class ResearchCase
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string ProblemDescription { get; set; } = string.Empty;
        public string GameTitle { get; set; } = string.Empty;
        public string Status { get; set; } = "OPEN"; // OPEN, RESEARCHING, RESOLVED, FAILED
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? ResolvedAt { get; set; }
    }

    public class GameFingerprint
    {
        public string Id { get; set; } = string.Empty;
        public string GameTitle { get; set; } = string.Empty;
        public string EngineVersion { get; set; } = string.Empty;
        public string Platform { get; set; } = "Windows_x64";
        public string UnityFsVersion { get; set; } = string.Empty;
        public string TextureFormat { get; set; } = string.Empty;
        public string DetectedShaders { get; set; } = string.Empty;
        public DateTime AnalyzedAt { get; set; } = DateTime.Now;
    }

    public class ObservationRecord
    {
        public string Id { get; set; } = string.Empty;
        public string CaseId { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string RawTelemetryJson { get; set; } = string.Empty;
        public DateTime ObservedAt { get; set; } = DateTime.Now;
    }

    public class HypothesisRecord
    {
        public string Id { get; set; } = string.Empty;
        public string CaseId { get; set; } = string.Empty;
        public string Statement { get; set; } = string.Empty;
        public string Rationale { get; set; } = string.Empty;
        public double ConfidenceScore { get; set; } = 0.5;
        public string Status { get; set; } = "PROPOSED"; // PROPOSED, VALIDATED, REFUTED
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class StrategyRecord
    {
        public string Id { get; set; } = string.Empty;
        public string HypothesisId { get; set; } = string.Empty;
        public string PlanStepsJson { get; set; } = string.Empty;
        public string EstimatedComplexity { get; set; } = "MEDIUM";
        public string Status { get; set; } = "PLANNED";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class CapabilityVersionRecord
    {
        public string Id { get; set; } = string.Empty;
        public string CapabilityId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string SemanticVersion { get; set; } = "1.0.0";
        public string InputContract { get; set; } = string.Empty;
        public string OutputContract { get; set; } = string.Empty;
        public string SourceCode { get; set; } = string.Empty;
        public string Status { get; set; } = "STAGE"; // PROPOSED, BUILT, SYNTHETIC_VERIFIED, INTEGRATION_VERIFIED, RELEASE_APPROVED
        public string ContentHash { get; set; } = string.Empty;
        public DateTime PromotedAt { get; set; } = DateTime.Now;
    }

    public class TestRunRecord
    {
        public string Id { get; set; } = string.Empty;
        public string ExperimentId { get; set; } = string.Empty;
        public string CapabilityVersionId { get; set; } = string.Empty;
        public int TotalTests { get; set; }
        public int PassedTests { get; set; }
        public int FailedTests { get; set; }
        public string LogOutput { get; set; } = string.Empty;
        public long DurationMs { get; set; }
        public bool Success { get; set; }
        public DateTime RunAt { get; set; } = DateTime.Now;
    }

    public class EvidenceArtifactRecord
    {
        public string Id { get; set; } = string.Empty;
        public string SourceId { get; set; } = string.Empty;
        public string ArtifactType { get; set; } = "LOG"; // LOG, DIFF, SCREENSHOT, TELEMETRY, BINARY
        public string FilePath { get; set; } = string.Empty;
        public string ContentHash { get; set; } = string.Empty;
        public string MetadataJson { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class ResearchJobRecord
    {
        public string Id { get; set; } = string.Empty;
        public string CaseId { get; set; } = string.Empty;
        public string CurrentState { get; set; } = "DISCOVER"; // 13-stage autonomous workflow state
        public string StatePayloadJson { get; set; } = string.Empty;
        public string Status { get; set; } = "RUNNING"; // RUNNING, PAUSED, COMPLETED, CANCELLED, FAILED
        public DateTime StartedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }

    public class CheckpointRecord
    {
        public string Id { get; set; } = string.Empty;
        public string JobId { get; set; } = string.Empty;
        public int StepIndex { get; set; }
        public string StateJson { get; set; } = string.Empty;
        public DateTime SavedAt { get; set; } = DateTime.Now;
    }

    // =========================================================================
    // KNOWLEDGE DATABASE ENGINE (VERSIONED SCHEMA & FTS5 ENGINE)
    // =========================================================================

    public class KnowledgeDatabase
    {
        private readonly string _dbPath;
        private readonly string _connectionString;
        private bool _fts5Available = false;

        public KnowledgeDatabase(string? dbPath = null)
        {
            if (string.IsNullOrEmpty(dbPath))
            {
                string aiLabDir = Path.Combine(Directory.GetCurrentDirectory(), ".ai-lab");
                if (!Directory.Exists(aiLabDir))
                {
                    Directory.CreateDirectory(aiLabDir);
                }
                _dbPath = Path.Combine(aiLabDir, "knowledge_base.db");
            }
            else
            {
                _dbPath = dbPath;
                string? dir = Path.GetDirectoryName(_dbPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }

            _connectionString = $"Data Source={_dbPath};";
            InitializeDatabase();
        }

        public string DbPath => _dbPath;
        public bool Fts5Available => _fts5Available;

        public void InitializeDatabase()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            // 1. Versioned Schema Migrations
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS SchemaMigrations (
                        Version INTEGER PRIMARY KEY,
                        AppliedAt TEXT NOT NULL
                    );
                ";
                cmd.ExecuteNonQuery();
            }

            // 2. Core Entities
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS KnowledgeItems (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Key TEXT UNIQUE NOT NULL,
                        Category TEXT NOT NULL,
                        Title TEXT NOT NULL,
                        Summary TEXT,
                        DetailedContent TEXT,
                        CodeSnippet TEXT,
                        VerifiedStatus TEXT NOT NULL,
                        CreatedAt TEXT NOT NULL,
                        Tags TEXT
                    );

                    CREATE TABLE IF NOT EXISTS Experiments (
                        Id TEXT PRIMARY KEY,
                        Title TEXT NOT NULL,
                        ProblemStatement TEXT NOT NULL,
                        Hypothesis TEXT,
                        ProposedModuleCode TEXT,
                        TestCode TEXT,
                        Status TEXT NOT NULL,
                        CompileOutput TEXT,
                        TestOutput TEXT,
                        FailureReason TEXT,
                        CreatedAt TEXT NOT NULL,
                        CompletedAt TEXT,
                        ExecutionDurationMs INTEGER DEFAULT 0,
                        PromotedToLibrary INTEGER DEFAULT 0
                    );

                    CREATE TABLE IF NOT EXISTS ModuleLibrary (
                        Id TEXT PRIMARY KEY,
                        Name TEXT NOT NULL,
                        Capability TEXT NOT NULL,
                        SourceCode TEXT NOT NULL,
                        UnitTestSourceCode TEXT,
                        Version TEXT NOT NULL,
                        Author TEXT NOT NULL,
                        VerificationEvidence TEXT,
                        CreatedAt TEXT NOT NULL,
                        IsActive INTEGER DEFAULT 1
                    );

                    CREATE TABLE IF NOT EXISTS ResearchCases (
                        Id TEXT PRIMARY KEY,
                        Title TEXT NOT NULL,
                        ProblemDescription TEXT,
                        GameTitle TEXT,
                        Status TEXT NOT NULL,
                        CreatedAt TEXT NOT NULL,
                        ResolvedAt TEXT
                    );

                    CREATE TABLE IF NOT EXISTS GameFingerprints (
                        Id TEXT PRIMARY KEY,
                        GameTitle TEXT NOT NULL,
                        EngineVersion TEXT,
                        Platform TEXT,
                        UnityFsVersion TEXT,
                        TextureFormat TEXT,
                        DetectedShaders TEXT,
                        AnalyzedAt TEXT NOT NULL
                    );

                    CREATE TABLE IF NOT EXISTS Observations (
                        Id TEXT PRIMARY KEY,
                        CaseId TEXT NOT NULL,
                        Category TEXT,
                        Description TEXT NOT NULL,
                        RawTelemetryJson TEXT,
                        ObservedAt TEXT NOT NULL
                    );

                    CREATE TABLE IF NOT EXISTS Hypotheses (
                        Id TEXT PRIMARY KEY,
                        CaseId TEXT NOT NULL,
                        Statement TEXT NOT NULL,
                        Rationale TEXT,
                        ConfidenceScore REAL DEFAULT 0.5,
                        Status TEXT NOT NULL,
                        CreatedAt TEXT NOT NULL
                    );

                    CREATE TABLE IF NOT EXISTS Strategies (
                        Id TEXT PRIMARY KEY,
                        HypothesisId TEXT NOT NULL,
                        PlanStepsJson TEXT,
                        EstimatedComplexity TEXT,
                        Status TEXT NOT NULL,
                        CreatedAt TEXT NOT NULL
                    );

                    CREATE TABLE IF NOT EXISTS CapabilityVersions (
                        Id TEXT PRIMARY KEY,
                        CapabilityId TEXT NOT NULL,
                        Name TEXT NOT NULL,
                        SemanticVersion TEXT NOT NULL,
                        InputContract TEXT,
                        OutputContract TEXT,
                        SourceCode TEXT NOT NULL,
                        Status TEXT NOT NULL,
                        ContentHash TEXT,
                        PromotedAt TEXT NOT NULL
                    );

                    CREATE TABLE IF NOT EXISTS TestRuns (
                        Id TEXT PRIMARY KEY,
                        ExperimentId TEXT,
                        CapabilityVersionId TEXT,
                        TotalTests INTEGER DEFAULT 0,
                        PassedTests INTEGER DEFAULT 0,
                        FailedTests INTEGER DEFAULT 0,
                        LogOutput TEXT,
                        DurationMs INTEGER DEFAULT 0,
                        Success INTEGER DEFAULT 0,
                        RunAt TEXT NOT NULL
                    );

                    CREATE TABLE IF NOT EXISTS EvidenceArtifacts (
                        Id TEXT PRIMARY KEY,
                        SourceId TEXT NOT NULL,
                        ArtifactType TEXT NOT NULL,
                        FilePath TEXT NOT NULL,
                        ContentHash TEXT,
                        MetadataJson TEXT,
                        CreatedAt TEXT NOT NULL
                    );

                    CREATE TABLE IF NOT EXISTS ResearchJobs (
                        Id TEXT PRIMARY KEY,
                        CaseId TEXT,
                        CurrentState TEXT NOT NULL,
                        StatePayloadJson TEXT,
                        Status TEXT NOT NULL,
                        StartedAt TEXT NOT NULL,
                        UpdatedAt TEXT NOT NULL
                    );

                    CREATE TABLE IF NOT EXISTS Checkpoints (
                        Id TEXT PRIMARY KEY,
                        JobId TEXT NOT NULL,
                        StepIndex INTEGER DEFAULT 0,
                        StateJson TEXT NOT NULL,
                        SavedAt TEXT NOT NULL
                    );
                ";
                cmd.ExecuteNonQuery();
            }

            // 3. FTS5 Virtual Table & Triggers
            try
            {
                using var ftsCmd = conn.CreateCommand();
                ftsCmd.CommandText = @"
                    CREATE VIRTUAL TABLE IF NOT EXISTS Knowledge_Fts USING fts5(
                        Key,
                        Category,
                        Title,
                        Summary,
                        DetailedContent,
                        Tags,
                        content='KnowledgeItems',
                        content_rowid='Id'
                    );

                    CREATE TRIGGER IF NOT EXISTS trg_ki_insert AFTER INSERT ON KnowledgeItems BEGIN
                        INSERT INTO Knowledge_Fts(rowid, Key, Category, Title, Summary, DetailedContent, Tags)
                        VALUES (new.Id, new.Key, new.Category, new.Title, new.Summary, new.DetailedContent, new.Tags);
                    END;

                    CREATE TRIGGER IF NOT EXISTS trg_ki_delete AFTER DELETE ON KnowledgeItems BEGIN
                        INSERT INTO Knowledge_Fts(Knowledge_Fts, rowid, Key, Category, Title, Summary, DetailedContent, Tags)
                        VALUES ('delete', old.Id, old.Key, old.Category, old.Title, old.Summary, old.DetailedContent, old.Tags);
                    END;

                    CREATE TRIGGER IF NOT EXISTS trg_ki_update AFTER UPDATE ON KnowledgeItems BEGIN
                        INSERT INTO Knowledge_Fts(Knowledge_Fts, rowid, Key, Category, Title, Summary, DetailedContent, Tags)
                        VALUES ('delete', old.Id, old.Key, old.Category, old.Title, old.Summary, old.DetailedContent, old.Tags);
                        INSERT INTO Knowledge_Fts(rowid, Key, Category, Title, Summary, DetailedContent, Tags)
                        VALUES (new.Id, new.Key, new.Category, new.Title, new.Summary, new.DetailedContent, new.Tags);
                    END;
                ";
                ftsCmd.ExecuteNonQuery();
                _fts5Available = true;
            }
            catch
            {
                _fts5Available = false;
            }

            // Record migration version
            using (var migCmd = conn.CreateCommand())
            {
                migCmd.CommandText = @"
                    INSERT OR IGNORE INTO SchemaMigrations (Version, AppliedAt)
                    VALUES (2, datetime('now'));
                ";
                migCmd.ExecuteNonQuery();
            }

            // Seed initial verified foundational knowledge if empty
            SeedFoundationalKnowledge(conn);
        }

        private void SeedFoundationalKnowledge(SqliteConnection conn)
        {
            using var checkCmd = conn.CreateCommand();
            checkCmd.CommandText = "SELECT COUNT(*) FROM KnowledgeItems;";
            long count = (long)(checkCmd.ExecuteScalar() ?? 0);
            if (count > 0) return;

            var seeds = new[]
            {
                new KnowledgeItem
                {
                    Key = "TMP_MATERIAL_PRESET_CLONER",
                    Category = "TMP_Shader",
                    Title = "Kỹ Thuật Nhân Bản Material Preset Cho TextMeshPro",
                    Summary = "Tạo bản sao Material Preset khi thay thế font 1-1, sao chép Outline, Underlay và Shader Keywords để bảo toàn hiệu ứng đồ họa gốc.",
                    DetailedContent = "Để tránh màn hình tím/hồng khi đổi SDF Font Asset, tạo Material mới kế thừa từ replacementFont.material, sau đó sao chép các properties: _OutlineColor, _OutlineWidth, _UnderlayColor, _UnderlaySoftness, _UnderlayOffset.",
                    CodeSnippet = "Material customMat = new Material(targetFont.material);\ncustomMat.CopyPropertiesFromMaterial(originalMaterial);\ncustomMat.EnableKeyword(\"OUTLINE_ON\");\ntextComponent.fontSharedMaterial = customMat;",
                    VerifiedStatus = "VERIFIED",
                    Tags = "TMP, Material, Outline, Underlay, PinkShader"
                },
                new KnowledgeItem
                {
                    Key = "UNITYFS_ASSETBUNDLE_HEADER",
                    Category = "AssetBundle",
                    Title = "Kiểm Định Header File UnityFS Của Font Bundle",
                    Summary = "Xác thực tệp font bundle có 7 bytes đầu là 'UnityFS' trước khi tải vào game để tránh crash engine.",
                    DetailedContent = "AssetBundle hợp lệ của Unity 5.3 trở lên luôn bắt đầu bằng Magic string 'UnityFS\\0'. Kiểm tra header trước khi AssetBundle.LoadFromFile sẽ loại trừ 100% lỗi bundle giả mạo hoặc hỏng.",
                    CodeSnippet = "byte[] header = new byte[7];\nusing var fs = File.OpenRead(bundlePath);\nfs.Read(header, 0, 7);\nbool isUnityFS = Encoding.ASCII.GetString(header) == \"UnityFS\";",
                    VerifiedStatus = "VERIFIED",
                    Tags = "AssetBundle, UnityFS, Binary, Validation"
                },
                new KnowledgeItem
                {
                    Key = "PROTECTED_ICON_FONT_ISOLATION",
                    Category = "Font_Protection",
                    Title = "Cách Ly Tuyệt Đối Font Icon & Emoji",
                    Summary = "Không bao giờ thay thế các font biểu tượng (Reference - Icons, NotoEmoji) để tránh mất icon phím bấm hoặc biểu cảm.",
                    DetailedContent = "Các thành phần UI dùng sprite asset hoặc ký tự biểu tượng Unicode đặc thù. Nếu bị gán font thường sẽ sinh lỗi ô vuông Tofu. Cần đưa vào danh sách ProtectedFonts.",
                    CodeSnippet = "if (fontName.Contains(\"Icon\", StringComparison.OrdinalIgnoreCase) || fontName.Contains(\"Emoji\", StringComparison.OrdinalIgnoreCase)) return false; // Không thay thế",
                    VerifiedStatus = "VERIFIED",
                    Tags = "Protection, Icons, Emoji, Tofu"
                },
                new KnowledgeItem
                {
                    Key = "VIETNAMESE_UNICODE_NFC_NFD_NORMALIZATION",
                    Category = "Unicode",
                    Title = "Chuẩn Hóa Ký Tự Tiếng Việt Tổ Hợp NFD Sang Dựng Sẵn NFC",
                    Summary = "Chuỗi tiếng Việt từ game engine có thể ở dạng NFD (ký tự gốc + dấu tách rời) gây trượt Dictionary Lookup. Cần chuẩn hóa sang NFC trước khi tra cứu.",
                    DetailedContent = "Trong C#, gọi `text.Normalize(NormalizationForm.FormC)` để gộp các ký tự tổ hợp như 'e' + '\\u0301' thành 'é'. Xử lý triệt để lỗi tra từ điển bản dịch không khớp do khác biệt Unicode decomposition.",
                    CodeSnippet = "string normalized = rawText.Normalize(NormalizationForm.FormC);",
                    VerifiedStatus = "VERIFIED",
                    Tags = "Unicode, NFC, NFD, Diacritics, Localization"
                }
            };

            foreach (var s in seeds)
            {
                AddOrUpdateKnowledgeItemInternal(conn, s);
            }
        }

        public void AddOrUpdateKnowledgeItem(KnowledgeItem item)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            AddOrUpdateKnowledgeItemInternal(conn, item);
        }

        private static void AddOrUpdateKnowledgeItemInternal(SqliteConnection conn, KnowledgeItem item)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO KnowledgeItems (Key, Category, Title, Summary, DetailedContent, CodeSnippet, VerifiedStatus, CreatedAt, Tags)
                VALUES (@Key, @Category, @Title, @Summary, @DetailedContent, @CodeSnippet, @VerifiedStatus, @CreatedAt, @Tags)
                ON CONFLICT(Key) DO UPDATE SET
                    Category = excluded.Category,
                    Title = excluded.Title,
                    Summary = excluded.Summary,
                    DetailedContent = excluded.DetailedContent,
                    CodeSnippet = excluded.CodeSnippet,
                    VerifiedStatus = excluded.VerifiedStatus,
                    Tags = excluded.Tags;
            ";
            cmd.Parameters.AddWithValue("@Key", item.Key);
            cmd.Parameters.AddWithValue("@Category", item.Category);
            cmd.Parameters.AddWithValue("@Title", item.Title);
            cmd.Parameters.AddWithValue("@Summary", item.Summary ?? string.Empty);
            cmd.Parameters.AddWithValue("@DetailedContent", item.DetailedContent ?? string.Empty);
            cmd.Parameters.AddWithValue("@CodeSnippet", item.CodeSnippet ?? string.Empty);
            cmd.Parameters.AddWithValue("@VerifiedStatus", item.VerifiedStatus);
            cmd.Parameters.AddWithValue("@CreatedAt", item.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@Tags", item.Tags ?? string.Empty);
            cmd.ExecuteNonQuery();
        }

        public List<KnowledgeItem> GetAllKnowledgeItems()
        {
            var list = new List<KnowledgeItem>();
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Key, Category, Title, Summary, DetailedContent, CodeSnippet, VerifiedStatus, CreatedAt, Tags FROM KnowledgeItems ORDER BY Id DESC;";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new KnowledgeItem
                {
                    Id = reader.GetInt64(0),
                    Key = reader.GetString(1),
                    Category = reader.GetString(2),
                    Title = reader.GetString(3),
                    Summary = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    DetailedContent = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    CodeSnippet = reader.IsDBNull(6) ? "" : reader.GetString(6),
                    VerifiedStatus = reader.GetString(7),
                    CreatedAt = DateTime.TryParse(reader.GetString(8), out var dt) ? dt : DateTime.Now,
                    Tags = reader.IsDBNull(9) ? "" : reader.GetString(9)
                });
            }
            return list;
        }

        /// <summary>
        /// Tìm kiếm Full-Text Search (FTS5) trên toàn bộ tri thức, tự động fallback về LIKE nếu FTS5 chưa kích hoạt.
        /// </summary>
        public List<KnowledgeItem> SearchKnowledgeFts(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return GetAllKnowledgeItems();
            }

            var list = new List<KnowledgeItem>();
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            if (_fts5Available)
            {
                try
                {
                    using var ftsCmd = conn.CreateCommand();
                    // Clean query string for FTS5 (remove special characters)
                    string sanitized = query.Replace("'", "''").Replace("\"", "").Trim();
                    ftsCmd.CommandText = @"
                        SELECT k.Id, k.Key, k.Category, k.Title, k.Summary, k.DetailedContent, k.CodeSnippet, k.VerifiedStatus, k.CreatedAt, k.Tags
                        FROM KnowledgeItems k
                        JOIN Knowledge_Fts f ON k.Id = f.rowid
                        WHERE Knowledge_Fts MATCH @Query
                        ORDER BY bm25(Knowledge_Fts);
                    ";
                    ftsCmd.Parameters.AddWithValue("@Query", sanitized);
                    using var reader = ftsCmd.ExecuteReader();
                    while (reader.Read())
                    {
                        list.Add(new KnowledgeItem
                        {
                            Id = reader.GetInt64(0),
                            Key = reader.GetString(1),
                            Category = reader.GetString(2),
                            Title = reader.GetString(3),
                            Summary = reader.IsDBNull(4) ? "" : reader.GetString(4),
                            DetailedContent = reader.IsDBNull(5) ? "" : reader.GetString(5),
                            CodeSnippet = reader.IsDBNull(6) ? "" : reader.GetString(6),
                            VerifiedStatus = reader.GetString(7),
                            CreatedAt = DateTime.TryParse(reader.GetString(8), out var dt) ? dt : DateTime.Now,
                            Tags = reader.IsDBNull(9) ? "" : reader.GetString(9)
                        });
                    }
                    if (list.Count > 0) return list;
                }
                catch
                {
                    // Fallback to LIKE
                }
            }

            // Fallback LIKE search
            using var fallbackCmd = conn.CreateCommand();
            fallbackCmd.CommandText = @"
                SELECT Id, Key, Category, Title, Summary, DetailedContent, CodeSnippet, VerifiedStatus, CreatedAt, Tags
                FROM KnowledgeItems
                WHERE Title LIKE @LikeQ OR Summary LIKE @LikeQ OR DetailedContent LIKE @LikeQ OR Tags LIKE @LikeQ OR Key LIKE @LikeQ
                ORDER BY Id DESC;
            ";
            fallbackCmd.Parameters.AddWithValue("@LikeQ", $"%{query}%");
            using var fbReader = fallbackCmd.ExecuteReader();
            while (fbReader.Read())
            {
                list.Add(new KnowledgeItem
                {
                    Id = fbReader.GetInt64(0),
                    Key = fbReader.GetString(1),
                    Category = fbReader.GetString(2),
                    Title = fbReader.GetString(3),
                    Summary = fbReader.IsDBNull(4) ? "" : fbReader.GetString(4),
                    DetailedContent = fbReader.IsDBNull(5) ? "" : fbReader.GetString(5),
                    CodeSnippet = fbReader.IsDBNull(6) ? "" : fbReader.GetString(6),
                    VerifiedStatus = fbReader.GetString(7),
                    CreatedAt = DateTime.TryParse(fbReader.GetString(8), out var dt) ? dt : DateTime.Now,
                    Tags = fbReader.IsDBNull(9) ? "" : fbReader.GetString(9)
                });
            }
            return list;
        }

        // =========================================================================
        // EXPERIMENT & MODULE REPOSITORY (SECTION IV & V)
        // =========================================================================

        public void RecordExperiment(ExperimentRecord exp)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Experiments (Id, Title, ProblemStatement, Hypothesis, ProposedModuleCode, TestCode, Status, CompileOutput, TestOutput, FailureReason, CreatedAt, CompletedAt, ExecutionDurationMs, PromotedToLibrary)
                VALUES (@Id, @Title, @ProblemStatement, @Hypothesis, @ProposedModuleCode, @TestCode, @Status, @CompileOutput, @TestOutput, @FailureReason, @CreatedAt, @CompletedAt, @ExecutionDurationMs, @PromotedToLibrary)
                ON CONFLICT(Id) DO UPDATE SET
                    Title = excluded.Title,
                    ProblemStatement = excluded.ProblemStatement,
                    Hypothesis = excluded.Hypothesis,
                    ProposedModuleCode = excluded.ProposedModuleCode,
                    TestCode = excluded.TestCode,
                    Status = excluded.Status,
                    CompileOutput = excluded.CompileOutput,
                    TestOutput = excluded.TestOutput,
                    FailureReason = excluded.FailureReason,
                    CompletedAt = excluded.CompletedAt,
                    ExecutionDurationMs = excluded.ExecutionDurationMs,
                    PromotedToLibrary = excluded.PromotedToLibrary;
            ";
            cmd.Parameters.AddWithValue("@Id", exp.Id);
            cmd.Parameters.AddWithValue("@Title", exp.Title);
            cmd.Parameters.AddWithValue("@ProblemStatement", exp.ProblemStatement);
            cmd.Parameters.AddWithValue("@Hypothesis", exp.Hypothesis ?? string.Empty);
            cmd.Parameters.AddWithValue("@ProposedModuleCode", exp.ProposedModuleCode ?? string.Empty);
            cmd.Parameters.AddWithValue("@TestCode", exp.TestCode ?? string.Empty);
            cmd.Parameters.AddWithValue("@Status", exp.Status);
            cmd.Parameters.AddWithValue("@CompileOutput", exp.CompileOutput ?? string.Empty);
            cmd.Parameters.AddWithValue("@TestOutput", exp.TestOutput ?? string.Empty);
            cmd.Parameters.AddWithValue("@FailureReason", exp.FailureReason ?? string.Empty);
            cmd.Parameters.AddWithValue("@CreatedAt", exp.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@CompletedAt", exp.CompletedAt.HasValue ? exp.CompletedAt.Value.ToString("yyyy-MM-dd HH:mm:ss") : (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ExecutionDurationMs", exp.ExecutionDurationMs);
            cmd.Parameters.AddWithValue("@PromotedToLibrary", exp.PromotedToLibrary ? 1 : 0);
            cmd.ExecuteNonQuery();
        }

        public List<ExperimentRecord> GetAllExperiments()
        {
            var list = new List<ExperimentRecord>();
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Title, ProblemStatement, Hypothesis, ProposedModuleCode, TestCode, Status, CompileOutput, TestOutput, FailureReason, CreatedAt, CompletedAt, ExecutionDurationMs, PromotedToLibrary FROM Experiments ORDER BY CreatedAt DESC;";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new ExperimentRecord
                {
                    Id = reader.GetString(0),
                    Title = reader.GetString(1),
                    ProblemStatement = reader.GetString(2),
                    Hypothesis = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    ProposedModuleCode = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    TestCode = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    Status = reader.GetString(6),
                    CompileOutput = reader.IsDBNull(7) ? "" : reader.GetString(7),
                    TestOutput = reader.IsDBNull(8) ? "" : reader.GetString(8),
                    FailureReason = reader.IsDBNull(9) ? "" : reader.GetString(9),
                    CreatedAt = DateTime.TryParse(reader.GetString(10), out var cdt) ? cdt : DateTime.Now,
                    CompletedAt = reader.IsDBNull(11) ? null : (DateTime.TryParse(reader.GetString(11), out var fdt) ? fdt : null),
                    ExecutionDurationMs = reader.GetInt64(12),
                    PromotedToLibrary = reader.GetInt32(13) == 1
                });
            }
            return list;
        }

        public void PromoteToModuleLibrary(ModuleLibraryItem mod)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO ModuleLibrary (Id, Name, Capability, SourceCode, UnitTestSourceCode, Version, Author, VerificationEvidence, CreatedAt, IsActive)
                VALUES (@Id, @Name, @Capability, @SourceCode, @UnitTestSourceCode, @Version, @Author, @VerificationEvidence, @CreatedAt, @IsActive)
                ON CONFLICT(Id) DO UPDATE SET
                    Name = excluded.Name,
                    Capability = excluded.Capability,
                    SourceCode = excluded.SourceCode,
                    UnitTestSourceCode = excluded.UnitTestSourceCode,
                    Version = excluded.Version,
                    Author = excluded.Author,
                    VerificationEvidence = excluded.VerificationEvidence,
                    IsActive = excluded.IsActive;
            ";
            cmd.Parameters.AddWithValue("@Id", mod.Id);
            cmd.Parameters.AddWithValue("@Name", mod.Name);
            cmd.Parameters.AddWithValue("@Capability", mod.Capability);
            cmd.Parameters.AddWithValue("@SourceCode", mod.SourceCode);
            cmd.Parameters.AddWithValue("@UnitTestSourceCode", mod.UnitTestSourceCode ?? string.Empty);
            cmd.Parameters.AddWithValue("@Version", mod.Version);
            cmd.Parameters.AddWithValue("@Author", mod.Author);
            cmd.Parameters.AddWithValue("@VerificationEvidence", mod.VerificationEvidence ?? string.Empty);
            cmd.Parameters.AddWithValue("@CreatedAt", mod.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@IsActive", mod.IsActive ? 1 : 0);
            cmd.ExecuteNonQuery();
        }

        public List<ModuleLibraryItem> GetAllModules()
        {
            var list = new List<ModuleLibraryItem>();
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Name, Capability, SourceCode, UnitTestSourceCode, Version, Author, VerificationEvidence, CreatedAt, IsActive FROM ModuleLibrary ORDER BY CreatedAt DESC;";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new ModuleLibraryItem
                {
                    Id = reader.GetString(0),
                    Name = reader.GetString(1),
                    Capability = reader.GetString(2),
                    SourceCode = reader.GetString(3),
                    UnitTestSourceCode = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    Version = reader.GetString(5),
                    Author = reader.GetString(6),
                    VerificationEvidence = reader.IsDBNull(7) ? "" : reader.GetString(7),
                    CreatedAt = DateTime.TryParse(reader.GetString(8), out var dt) ? dt : DateTime.Now,
                    IsActive = reader.GetInt32(9) == 1
                });
            }
            return list;
        }

        // =========================================================================
        // CAPABILITY VERSIONS & ROLLBACK (SECTION V)
        // =========================================================================

        public void SaveCapabilityVersion(CapabilityVersionRecord cap)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO CapabilityVersions (Id, CapabilityId, Name, SemanticVersion, InputContract, OutputContract, SourceCode, Status, ContentHash, PromotedAt)
                VALUES (@Id, @CapabilityId, @Name, @SemanticVersion, @InputContract, @OutputContract, @SourceCode, @Status, @ContentHash, @PromotedAt)
                ON CONFLICT(Id) DO UPDATE SET
                    Name = excluded.Name,
                    SemanticVersion = excluded.SemanticVersion,
                    InputContract = excluded.InputContract,
                    OutputContract = excluded.OutputContract,
                    SourceCode = excluded.SourceCode,
                    Status = excluded.Status,
                    ContentHash = excluded.ContentHash,
                    PromotedAt = excluded.PromotedAt;
            ";
            cmd.Parameters.AddWithValue("@Id", cap.Id);
            cmd.Parameters.AddWithValue("@CapabilityId", cap.CapabilityId);
            cmd.Parameters.AddWithValue("@Name", cap.Name);
            cmd.Parameters.AddWithValue("@SemanticVersion", cap.SemanticVersion);
            cmd.Parameters.AddWithValue("@InputContract", cap.InputContract ?? string.Empty);
            cmd.Parameters.AddWithValue("@OutputContract", cap.OutputContract ?? string.Empty);
            cmd.Parameters.AddWithValue("@SourceCode", cap.SourceCode);
            cmd.Parameters.AddWithValue("@Status", cap.Status);
            cmd.Parameters.AddWithValue("@ContentHash", cap.ContentHash ?? string.Empty);
            cmd.Parameters.AddWithValue("@PromotedAt", cap.PromotedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.ExecuteNonQuery();
        }

        public List<CapabilityVersionRecord> GetCapabilityHistory(string capabilityId)
        {
            var list = new List<CapabilityVersionRecord>();
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, CapabilityId, Name, SemanticVersion, InputContract, OutputContract, SourceCode, Status, ContentHash, PromotedAt FROM CapabilityVersions WHERE CapabilityId = @CapId ORDER BY PromotedAt DESC;";
            cmd.Parameters.AddWithValue("@CapId", capabilityId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new CapabilityVersionRecord
                {
                    Id = reader.GetString(0),
                    CapabilityId = reader.GetString(1),
                    Name = reader.GetString(2),
                    SemanticVersion = reader.GetString(3),
                    InputContract = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    OutputContract = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    SourceCode = reader.GetString(6),
                    Status = reader.GetString(7),
                    ContentHash = reader.IsDBNull(8) ? "" : reader.GetString(8),
                    PromotedAt = DateTime.TryParse(reader.GetString(9), out var dt) ? dt : DateTime.Now
                });
            }
            return list;
        }

        public bool RollbackCapability(string capabilityId, string targetVersion)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE CapabilityVersions
                SET Status = 'RELEASE_APPROVED'
                WHERE CapabilityId = @CapId AND SemanticVersion = @Ver;

                UPDATE CapabilityVersions
                SET Status = 'DEPRECATED'
                WHERE CapabilityId = @CapId AND SemanticVersion != @Ver;
            ";
            cmd.Parameters.AddWithValue("@CapId", capabilityId);
            cmd.Parameters.AddWithValue("@Ver", targetVersion);
            return cmd.ExecuteNonQuery() > 0;
        }

        // =========================================================================
        // RESEARCH CASES & WORKFLOW JOBS (SECTION VI)
        // =========================================================================

        public void SaveResearchCase(ResearchCase rc)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO ResearchCases (Id, Title, ProblemDescription, GameTitle, Status, CreatedAt, ResolvedAt)
                VALUES (@Id, @Title, @ProblemDescription, @GameTitle, @Status, @CreatedAt, @ResolvedAt)
                ON CONFLICT(Id) DO UPDATE SET
                    Title = excluded.Title,
                    ProblemDescription = excluded.ProblemDescription,
                    GameTitle = excluded.GameTitle,
                    Status = excluded.Status,
                    ResolvedAt = excluded.ResolvedAt;
            ";
            cmd.Parameters.AddWithValue("@Id", rc.Id);
            cmd.Parameters.AddWithValue("@Title", rc.Title);
            cmd.Parameters.AddWithValue("@ProblemDescription", rc.ProblemDescription ?? string.Empty);
            cmd.Parameters.AddWithValue("@GameTitle", rc.GameTitle ?? string.Empty);
            cmd.Parameters.AddWithValue("@Status", rc.Status);
            cmd.Parameters.AddWithValue("@CreatedAt", rc.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@ResolvedAt", rc.ResolvedAt.HasValue ? rc.ResolvedAt.Value.ToString("yyyy-MM-dd HH:mm:ss") : (object)DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        public void SaveResearchJob(ResearchJobRecord job)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO ResearchJobs (Id, CaseId, CurrentState, StatePayloadJson, Status, StartedAt, UpdatedAt)
                VALUES (@Id, @CaseId, @CurrentState, @StatePayloadJson, @Status, @StartedAt, @UpdatedAt)
                ON CONFLICT(Id) DO UPDATE SET
                    CurrentState = excluded.CurrentState,
                    StatePayloadJson = excluded.StatePayloadJson,
                    Status = excluded.Status,
                    UpdatedAt = excluded.UpdatedAt;
            ";
            cmd.Parameters.AddWithValue("@Id", job.Id);
            cmd.Parameters.AddWithValue("@CaseId", job.CaseId ?? string.Empty);
            cmd.Parameters.AddWithValue("@CurrentState", job.CurrentState);
            cmd.Parameters.AddWithValue("@StatePayloadJson", job.StatePayloadJson ?? string.Empty);
            cmd.Parameters.AddWithValue("@Status", job.Status);
            cmd.Parameters.AddWithValue("@StartedAt", job.StartedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@UpdatedAt", job.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.ExecuteNonQuery();
        }

        public void SaveCheckpoint(CheckpointRecord cp)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Checkpoints (Id, JobId, StepIndex, StateJson, SavedAt)
                VALUES (@Id, @JobId, @StepIndex, @StateJson, @SavedAt)
                ON CONFLICT(Id) DO UPDATE SET
                    StepIndex = excluded.StepIndex,
                    StateJson = excluded.StateJson,
                    SavedAt = excluded.SavedAt;
            ";
            cmd.Parameters.AddWithValue("@Id", cp.Id);
            cmd.Parameters.AddWithValue("@JobId", cp.JobId);
            cmd.Parameters.AddWithValue("@StepIndex", cp.StepIndex);
            cmd.Parameters.AddWithValue("@StateJson", cp.StateJson);
            cmd.Parameters.AddWithValue("@SavedAt", cp.SavedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.ExecuteNonQuery();
        }

        public CheckpointRecord? GetLatestCheckpoint(string jobId)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, JobId, StepIndex, StateJson, SavedAt FROM Checkpoints WHERE JobId = @JobId ORDER BY StepIndex DESC LIMIT 1;";
            cmd.Parameters.AddWithValue("@JobId", jobId);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new CheckpointRecord
                {
                    Id = reader.GetString(0),
                    JobId = reader.GetString(1),
                    StepIndex = reader.GetInt32(2),
                    StateJson = reader.GetString(3),
                    SavedAt = DateTime.TryParse(reader.GetString(4), out var dt) ? dt : DateTime.Now
                };
            }
            return null;
        }

        public void RecordTestRun(TestRunRecord tr)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO TestRuns (Id, ExperimentId, CapabilityVersionId, TotalTests, PassedTests, FailedTests, LogOutput, DurationMs, Success, RunAt)
                VALUES (@Id, @ExperimentId, @CapabilityVersionId, @TotalTests, @PassedTests, @FailedTests, @LogOutput, @DurationMs, @Success, @RunAt)
                ON CONFLICT(Id) DO UPDATE SET
                    TotalTests = excluded.TotalTests,
                    PassedTests = excluded.PassedTests,
                    FailedTests = excluded.FailedTests,
                    LogOutput = excluded.LogOutput,
                    DurationMs = excluded.DurationMs,
                    Success = excluded.Success;
            ";
            cmd.Parameters.AddWithValue("@Id", tr.Id);
            cmd.Parameters.AddWithValue("@ExperimentId", (object?)tr.ExperimentId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CapabilityVersionId", (object?)tr.CapabilityVersionId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@TotalTests", tr.TotalTests);
            cmd.Parameters.AddWithValue("@PassedTests", tr.PassedTests);
            cmd.Parameters.AddWithValue("@FailedTests", tr.FailedTests);
            cmd.Parameters.AddWithValue("@LogOutput", tr.LogOutput ?? string.Empty);
            cmd.Parameters.AddWithValue("@DurationMs", tr.DurationMs);
            cmd.Parameters.AddWithValue("@Success", tr.Success ? 1 : 0);
            cmd.Parameters.AddWithValue("@RunAt", tr.RunAt.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.ExecuteNonQuery();
        }

        public void SaveEvidenceArtifact(EvidenceArtifactRecord ea)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO EvidenceArtifacts (Id, SourceId, ArtifactType, FilePath, ContentHash, MetadataJson, CreatedAt)
                VALUES (@Id, @SourceId, @ArtifactType, @FilePath, @ContentHash, @MetadataJson, @CreatedAt)
                ON CONFLICT(Id) DO UPDATE SET
                    FilePath = excluded.FilePath,
                    ContentHash = excluded.ContentHash,
                    MetadataJson = excluded.MetadataJson;
            ";
            cmd.Parameters.AddWithValue("@Id", ea.Id);
            cmd.Parameters.AddWithValue("@SourceId", ea.SourceId);
            cmd.Parameters.AddWithValue("@ArtifactType", ea.ArtifactType);
            cmd.Parameters.AddWithValue("@FilePath", ea.FilePath);
            cmd.Parameters.AddWithValue("@ContentHash", ea.ContentHash ?? string.Empty);
            cmd.Parameters.AddWithValue("@MetadataJson", ea.MetadataJson ?? string.Empty);
            cmd.Parameters.AddWithValue("@CreatedAt", ea.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.ExecuteNonQuery();
        }

        public void SaveGameFingerprint(GameFingerprint gf)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO GameFingerprints (Id, GameTitle, EngineVersion, Platform, UnityFsVersion, TextureFormat, DetectedShaders, AnalyzedAt)
                VALUES (@Id, @GameTitle, @EngineVersion, @Platform, @UnityFsVersion, @TextureFormat, @DetectedShaders, @AnalyzedAt)
                ON CONFLICT(Id) DO UPDATE SET
                    EngineVersion = excluded.EngineVersion,
                    Platform = excluded.Platform,
                    UnityFsVersion = excluded.UnityFsVersion,
                    TextureFormat = excluded.TextureFormat,
                    DetectedShaders = excluded.DetectedShaders,
                    AnalyzedAt = excluded.AnalyzedAt;
            ";
            cmd.Parameters.AddWithValue("@Id", gf.Id);
            cmd.Parameters.AddWithValue("@GameTitle", gf.GameTitle);
            cmd.Parameters.AddWithValue("@EngineVersion", gf.EngineVersion ?? string.Empty);
            cmd.Parameters.AddWithValue("@Platform", gf.Platform ?? "Windows_x64");
            cmd.Parameters.AddWithValue("@UnityFsVersion", gf.UnityFsVersion ?? string.Empty);
            cmd.Parameters.AddWithValue("@TextureFormat", gf.TextureFormat ?? string.Empty);
            cmd.Parameters.AddWithValue("@DetectedShaders", gf.DetectedShaders ?? string.Empty);
            cmd.Parameters.AddWithValue("@AnalyzedAt", gf.AnalyzedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.ExecuteNonQuery();
        }

        public void SaveObservation(ObservationRecord ob)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Observations (Id, CaseId, Category, Description, RawTelemetryJson, ObservedAt)
                VALUES (@Id, @CaseId, @Category, @Description, @RawTelemetryJson, @ObservedAt)
                ON CONFLICT(Id) DO UPDATE SET
                    Description = excluded.Description,
                    RawTelemetryJson = excluded.RawTelemetryJson;
            ";
            cmd.Parameters.AddWithValue("@Id", ob.Id);
            cmd.Parameters.AddWithValue("@CaseId", ob.CaseId);
            cmd.Parameters.AddWithValue("@Category", ob.Category ?? string.Empty);
            cmd.Parameters.AddWithValue("@Description", ob.Description);
            cmd.Parameters.AddWithValue("@RawTelemetryJson", ob.RawTelemetryJson ?? string.Empty);
            cmd.Parameters.AddWithValue("@ObservedAt", ob.ObservedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.ExecuteNonQuery();
        }

        public void SaveHypothesis(HypothesisRecord hyp)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Hypotheses (Id, CaseId, Statement, Rationale, ConfidenceScore, Status, CreatedAt)
                VALUES (@Id, @CaseId, @Statement, @Rationale, @ConfidenceScore, @Status, @CreatedAt)
                ON CONFLICT(Id) DO UPDATE SET
                    Statement = excluded.Statement,
                    Rationale = excluded.Rationale,
                    ConfidenceScore = excluded.ConfidenceScore,
                    Status = excluded.Status;
            ";
            cmd.Parameters.AddWithValue("@Id", hyp.Id);
            cmd.Parameters.AddWithValue("@CaseId", hyp.CaseId);
            cmd.Parameters.AddWithValue("@Statement", hyp.Statement);
            cmd.Parameters.AddWithValue("@Rationale", hyp.Rationale ?? string.Empty);
            cmd.Parameters.AddWithValue("@ConfidenceScore", hyp.ConfidenceScore);
            cmd.Parameters.AddWithValue("@Status", hyp.Status);
            cmd.Parameters.AddWithValue("@CreatedAt", hyp.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.ExecuteNonQuery();
        }

        public void SaveStrategy(StrategyRecord st)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Strategies (Id, HypothesisId, PlanStepsJson, EstimatedComplexity, Status, CreatedAt)
                VALUES (@Id, @HypothesisId, @PlanStepsJson, @EstimatedComplexity, @Status, @CreatedAt)
                ON CONFLICT(Id) DO UPDATE SET
                    PlanStepsJson = excluded.PlanStepsJson,
                    EstimatedComplexity = excluded.EstimatedComplexity,
                    Status = excluded.Status;
            ";
            cmd.Parameters.AddWithValue("@Id", st.Id);
            cmd.Parameters.AddWithValue("@HypothesisId", st.HypothesisId);
            cmd.Parameters.AddWithValue("@PlanStepsJson", st.PlanStepsJson ?? string.Empty);
            cmd.Parameters.AddWithValue("@EstimatedComplexity", st.EstimatedComplexity);
            cmd.Parameters.AddWithValue("@Status", st.Status);
            cmd.Parameters.AddWithValue("@CreatedAt", st.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.ExecuteNonQuery();
        }
    }
}
