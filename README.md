# TextExpander

Windows 전역 텍스트 확장기 (Global Text Expander for Windows)

## 기능

- 사용자 정의 키워드로 긴 텍스트 자동 확장
- 카테고리별 스니펫 관리
- 한/영 모드 무관 트리거
- 메모장, Word 등 모든 애플리케이션에서 작동
- 시스템 트레이 아이콘으로 백그라운드 실행

## 최신 변경사항

### v1.2.0 (2025-11-29)

- **백스페이스 중복 실행 문제 해결**: 스니펫 확장 후 백스페이스가 1개 더 실행되던 버그 수정
  - `SendBackspaces` 호출 후 추가 딜레이 추가
  - `SendBackspaces`와 `SendText` 사이 대기 시간 추가하여 타이밍 충돌 방지
- **엔진 중복 처리 방지**: `HandleDelimiter`에서 이벤트 발생 전 엔진 비활성화
- **키보드 후크 개선**: 디바운스 시간 증가 (50ms → 100ms) 및 flags 체크 추가
- **버퍼 관리 강화**: 확장 완료 후 버퍼 명시적 클리어

### v1.1.0 (2025-11-28)

- **버퍼 리셋 자동화**: 컨텍스트 변경 시 자동으로 입력 버퍼 초기화
  - 방향키 (←, →, ↑, ↓): 커서 이동 감지
  - 탐색 키 (Home, End, Page Up, Page Down): 페이지/줄 이동 감지
  - 편집 단축키 (Ctrl+C/X/V/Z/Y): 복사, 잘라내기, 붙여넣기, 실행 취소/다시 실행
  - Delete 키: 문자 삭제 감지
  - Alt+Tab: 애플리케이션 전환 감지
  - 창 포커스 변경: 다른 창 클릭 시 자동 리셋
- **Backspace 동작 개선**: 버퍼에서 마지막 문자 1개만 삭제 (오타 수정 지원)
  - 예: `dd` 입력 → `ddff` (실수) → Backspace 2번 → `dd` → `ddzt` 입력 → `ddzt;` 스니펫 발동 ✨
- 코드 품질 개선
  - Dead code 제거 (`HandleBackspace` 메서드)
  - 입력 검증 추가 (음수 방지)
  - IDisposable 패턴 적용

### v1.0.0 (2025-11-28)

- 구분자 옵션 정리
  - 제거: 엔터, 스페이스, 백스페이스, 느낌표(?), 물음표(!)
  - 유지: 탭, 마침표, 쉼표, 세미콜론, 백틱, 작은따옴표, 슬래시
- 버퍼 리셋 로깅 개선
- 메모장 호환성 강화
- 설정 UI 업데이트

## 시스템 요구사항

- Windows 10 이상
- .NET 8.0 런타임

## 사용 방법

1. TextExpander 실행
2. 카테고리 추가
3. 스니펫 추가 (키워드 + 대체 텍스트)
4. 트리거 키 입력 시 자동 확장

## 개발

```bash
cd src
dotnet build
dotnet run --project TextExpander.App/TextExpander.App.csproj
```

## 라이선스

MIT License
