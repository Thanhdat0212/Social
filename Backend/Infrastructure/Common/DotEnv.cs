namespace Infrastructure.Common;

/// <summary>
/// Trình tải file .env cho môi trường phát triển cục bộ và migration EF Core.
/// Tự động quét và nạp các biến môi trường từ file .env vào Environment Variables của tiến trình.
/// </summary>
public static class DotEnv
{
    public static void Load(string? filePath = null)
    {
        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
        {
            ReadFile(filePath);
            return;
        }

        // Tự động tìm kiếm file .env ở thư mục hiện tại và các cấp thư mục cha
        var candidates = new List<string>();

        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        for (int i = 0; i < 4 && dir != null; i++)
        {
            candidates.Add(Path.Combine(dir.FullName, ".env"));
            candidates.Add(Path.Combine(dir.FullName, "API", ".env"));
            candidates.Add(Path.Combine(dir.FullName, "Backend", ".env"));
            candidates.Add(Path.Combine(dir.FullName, "Backend", "API", ".env"));
            dir = dir.Parent;
        }

        var baseDir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        for (int i = 0; i < 4 && baseDir != null; i++)
        {
            candidates.Add(Path.Combine(baseDir.FullName, ".env"));
            candidates.Add(Path.Combine(baseDir.FullName, "API", ".env"));
            baseDir = baseDir.Parent;
        }

        foreach (var path in candidates)
        {
            if (File.Exists(path))
            {
                ReadFile(path);
                return;
            }
        }
    }

    private static void ReadFile(string filePath)
    {
        if (!File.Exists(filePath)) return;

        foreach (var rawLine in File.ReadAllLines(filePath))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#') || line.StartsWith(';'))
            {
                continue;
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();

            // Loại bỏ dấu nháy kép hoặc ngoặc đơn bao quanh nếu có
            if (value.Length >= 2)
            {
                if ((value.StartsWith('"') && value.EndsWith('"')) || 
                    (value.StartsWith('\'') && value.EndsWith('\'')))
                {
                    value = value[1..^1];
                }
            }

            // Gán biến môi trường hệ thống cho process hiện tại
            Environment.SetEnvironmentVariable(key, value);

            // Chuẩn hóa hỗ trợ cả cấu trúc phân cấp ":" và "__" của ASP.NET Core
            if (key.Contains(':'))
            {
                var dunderKey = key.Replace(":", "__");
                Environment.SetEnvironmentVariable(dunderKey, value);
            }
            else if (key.Contains("__"))
            {
                var colonKey = key.Replace("__", ":");
                Environment.SetEnvironmentVariable(colonKey, value);
            }
        }
    }
}
