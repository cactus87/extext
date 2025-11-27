# TextExpander

Windows 전역 텍스트 확장기 (Global Text Expander for Windows)

## 기능

- 사용자 정의 키워드로 긴 텍스트 자동 확장
- 카테고리별 스니펫 관리
- 한/영 모드 무관 트리거
- 메모장, Word 등 모든 애플리케이션에서 작동
- 시스템 트레이 아이콘으로 백그라운드 실행

## 최신 변경사항

### v1.0.0 (2025-11-28)

- 구분자 옵션 정리
  - 제거: 엔터, 스페이스, 백스페이스, 느낌표(?), 물음표(!)
  - 유지: 탭, 마침표, 쉼표, 세미콜론, 백틱, 작은따옴표, 슬래시
- 버퍼 리셋 로직 개선
- 프로그램 호환성 강화
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

