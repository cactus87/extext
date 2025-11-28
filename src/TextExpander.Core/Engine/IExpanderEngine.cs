using TextExpander.Core.Models;

namespace TextExpander.Core.Engine;

/// <summary>
/// 텍스트 확장 엔진 인터페이스
/// 키보드 입력 스트림을 처리하여 스니펫 매칭 및 확장 신호 제공
/// </summary>
public interface IExpanderEngine
{
    /// <summary>
    /// 텍스트 확장이 필요할 때 발생하는 이벤트
    /// App 레이어에서 구독하여 실제 키 입력을 처리
    /// </summary>
    event Action<ExpanderResult>? OnExpansionNeeded;

    /// <summary>
    /// 글로벌 키보드 후크에서 받은 문자 입력 처리
    /// </summary>
    /// <param name="ch">입력된 문자 (\b는 Backspace)</param>
    void OnCharInput(char ch);

    /// <summary>
    /// 스니펫 저장소 설정
    /// </summary>
    void SetRepository(Repositories.ISnippetRepository repository);

    /// <summary>
    /// 애플리케이션 설정 적용
    /// </summary>
    void SetSettings(AppSettings settings);

    /// <summary>
    /// 엔진 활성화
    /// </summary>
    void Enable();

    /// <summary>
    /// 엔진 비활성화 (일시 정지)
    /// </summary>
    void Disable();

    /// <summary>
    /// 프로그램이 키 입력을 송신 중인지 플래그 설정
    /// 재귀 방지용 (우리가 보낸 입력은 처리 안 함)
    /// </summary>
    void SetSendingFlag(bool sending);

    /// <summary>
    /// 입력 버퍼 강제 리셋 (컨텍스트 변경 시 호출)
    /// </summary>
    void ResetBuffer();
}

