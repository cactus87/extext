using TextExpander.Core.Models;

namespace TextExpander.Core.Repositories;

/// <summary>
/// 애플리케이션 설정 저장소 인터페이스
/// </summary>
public interface ISettingsRepository
{
    /// <summary>
    /// 설정 로드
    /// </summary>
    Task<AppSettings> LoadAsync();

    /// <summary>
    /// 설정 저장
    /// </summary>
    Task SaveAsync(AppSettings settings);
}

