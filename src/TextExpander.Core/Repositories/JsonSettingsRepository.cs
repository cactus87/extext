using System.Text.Json;
using TextExpander.Core.Models;

namespace TextExpander.Core.Repositories;

/// <summary>
/// JSON 파일 기반 설정 저장소 구현
/// %AppData%\TextExpander\settings.json에 데이터 저장
/// </summary>
public class JsonSettingsRepository : ISettingsRepository
{
    private static readonly string AppDataPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "TextExpander"
    );
    
    private static readonly string FilePath = Path.Combine(AppDataPath, "settings.json");

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <inheritdoc/>
    public async Task<AppSettings> LoadAsync()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                // 파일이 없으면 기본 설정 반환
                return new AppSettings();
            }

            string json = await File.ReadAllTextAsync(FilePath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);

            return settings ?? new AppSettings();
        }
        catch (JsonException)
        {
            // JSON 파싱 실패 시 기본 설정 반환
            return new AppSettings();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("설정 로드 중 오류 발생", ex);
        }
    }

    /// <inheritdoc/>
    public async Task SaveAsync(AppSettings settings)
    {
        try
        {
            // 디렉터리가 없으면 생성
            Directory.CreateDirectory(AppDataPath);

            // 임시 파일에 먼저 쓰기
            string tempPath = FilePath + ".tmp";
            string json = JsonSerializer.Serialize(settings, _jsonOptions);
            await File.WriteAllTextAsync(tempPath, json);

            // 임시 파일을 정식 파일로 교체
            File.Move(tempPath, FilePath, overwrite: true);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("설정 저장 중 오류 발생", ex);
        }
    }
}

