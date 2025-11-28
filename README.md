# TextExpander

Windows 전역 텍스트 확장기 (Global Text Expander for Windows)

## 기능

- 사용자 정의 키워드로 긴 텍스트 자동 확장
- 카테고리별 스니펫 관리
- 한/영 모드 무관 트리거
- 메모장, Word 등 모든 애플리케이션에서 작동
- 시스템 트레이 아이콘으로 백그라운드 실행

## 최신 변경사항

### v1.1.0 (2025-11-28)

- **버퍼 리셋 자동화**: 컨텍스트 변경 시 자동으로 입력 버퍼 초기화
  - 방향키 (←, →, ↑, ↓): 커서 이동 감지
  - 탐색 키 (Home, End, Page Up, Page Down): 페이지/줄 이동 감지
  - 편집 단축키 (Ctrl+C/X/V/Z/Y): 복사, 잘라내기, 붙여넣기, 실행 취소/다시 실행
  - Delete 키: 문자 삭제 감지
  - Alt+Tab: 애플리케이션 전환 감지
  - 창 포커스 변경: 다른 창 클릭 시 자동 리셋
- 코드 품질 개선
  - Dead code 제거 (`HandleBackspace` 메서드)
  - 입력 검증 추가 (음수 방지)
  - IDisposable 패턴 적용

### v1.0.0 (2025-11-28)

- 구분자 옵션 정리
  - 제거: 엔터, 스페이스, 백스페이스, 느낼표(?), 물음표(!)
  - 유지: 탭, 마침표, 쉰표, 세미콜론, 백틱, 작은따옴표, 슬래시
- 버퍼 리셋 로직 개선
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

## 라이센스

MIT License
