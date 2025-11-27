# TextExpander 개발 가이드 (Cursor AI용)

이 문서는 TextExpander 프로젝트를 Cursor AI가 효율적으로 코딩·리팩토링할 수 있도록 구성된 전체 지침입니다.

## 📚 핵심 문서 위치

- `docs/10-prd/textexpander-prd.md` - 제품 정의 및 기능 명세
- `docs/20-design/textexpander-architecture.md` - 솔루션 구조 및 아키텍처
- `docs/20-design/textexpander-expander-engine.md` - ExpanderEngine 핵심 알고리즘
- `.cursor/rules/textexpander.mdc` - Cursor 규칙 파일

---

## 🎯 프로젝트 개요

**TextExpander**는 Windows 10/11에서 동작하는 전역 텍스트 확장 유틸입니다.

- **목표**: 짧은 키워드(`;addr`, `;sig`)를 입력 후 구분자(스페이스/엔터/구두점)를 누르면 미리 정의한 대체 텍스트로 자동 치환
- **기술 스택**: C# + .NET 8 + WPF (Windows 10/11)
- **배포**: 단일 EXE 또는 MSI 설치 프로그램
- **구조**: Core(도메인·엔진) + App(WPF UI·키보드 훅·트레이)

---

## 🏗️ 솔루션 구조

```
TextExpander.sln
├── TextExpander.Core/
│   ├── Domain/
│   │   ├── Category.cs
│   │   └── Snippet.cs
│   ├── Repositories/
│   │   ├── ISnippetRepository.cs
│   │   └── JsonSnippetRepository.cs
│   ├── Engine/
│   │   ├── IExpanderEngine.cs
│   │   └── ExpanderEngine.cs
│   └── Models/
│       └── [DTO/ValueObjects]
│
└── TextExpander.App/
    ├── Windows/
    │   └── MainWindow.xaml / .xaml.cs
    ├── ViewModels/
    │   ├── MainWindowViewModel.cs
    │   └── [Other VMs]
    ├── Services/
    │   ├── GlobalKeyboardHook.cs (Win32 API)
    │   ├── KeyboardOutputService.cs (SendInput)
    │   └── TrayIconService.cs
    └── App.xaml / .xaml.cs
```

### 핵심 원칙

1. **Core는 UI/OS API에 의존하지 않음** → 순수 도메인·비즈니스 로직 레이어
2. **App은 Core를 호출하여 실제 OS 작업 수행** → WPF UI + Win32 접근점
3. **의존성 방향**: App → Core (역 참조 금지)

---

## 🔧 개발 시 Cursor 사용법

### 1️⃣ 코드 생성 요청

```
"TextExpander.Core의 Category 모델을 MVVM 패턴으로 구현해줘.
docs/10-prd/textexpander-prd.md와 docs/20-design/textexpander-architecture.md를 참고해."
```

### 2️⃣ 기능 구현

```
"ExpanderEngine의 OnCharInput 메서드를 
docs/20-design/textexpander-expander-engine.md의 의사코드 기반으로 C# 구현해줘.
StringBuilder 버퍼, 구분자 검사, 스니펫 매칭 흐름을 포함."
```

### 3️⃣ 코드 검수

```
"위 코드를 검토해서:
- 단일 책임 원칙 준수 여부
- 구분자 처리 로직 정확성
- 버퍼 길이 제한(200자) 적용 여부
- 테스트 가능성 확인
을 체크해줘."
```

---

## 📋 주요 체크리스트

### Core 계층 (도메인/엔진)

- [ ] Category, Snippet 도메인 모델 구현
- [ ] ISnippetRepository 인터페이스 정의
- [ ] JsonSnippetRepository로 `%AppData%\TextExpander\snippets.json` 관리
- [ ] ExpanderEngine 의사코드를 C#으로 구현
- [ ] 구분자 정의 및 버퍼 관리 로직 완성
- [ ] 단위 테스트 작성 (Core는 UI 비의존이므로 테스트 용이)

### App 계층 (WPF UI)

- [ ] MainWindow XAML + ViewModel 바인딩
- [ ] GlobalKeyboardHook (Win32 SetWindowsHookEx) 구현
- [ ] KeyboardOutputService (SendInput로 Backspace/Replacement/Delimiter 입력)
- [ ] TrayIconService (NotifyIcon으로 열기/일시정지/종료)
- [ ] 상태 표시 (On/Off, 카테고리/스니펫 리스트)
- [ ] MVVM Toolkit 사용 (PropertyChanged, RelayCommand 등)

### 비기능

- [ ] 메모리 사용 최소화 (버퍼 길이 200자 제한)
- [ ] 백그라운드 상시 동작 안정성
- [ ] 치환 실패 시 원본 입력 보존
- [ ] 로컬 JSON 저장 (네트워크 전송 금지)

---

## 🎨 코드 품질 기준

### 네이밍

- 클래스: `ExpanderEngine`, `GlobalKeyboardHook`, `KeyboardOutputService`
- 메서드: `OnCharInput()`, `FindActiveSnippetByKeyword()`, `ReplaceWordWithSnippet()`
- 변수: `_buffer`, `_isSending`, `lastWord`, `activeSnippets`

### 구조

- 모든 로직은 Core에 도메인 모델로 정의
- App은 XAML + ViewModel로 UI 담당, 실제 동작은 Core 호출
- 인터페이스 의존 (ISnippetRepository, IExpanderEngine 등)

### 테스트

- Core의 각 도메인/서비스는 단위 테스트 필수
- ExpanderEngine의 OnCharInput, 버퍼 관리, 스니펫 매칭을 각각 테스트
- App의 Win32 호출은 E2E 테스트로 통합 검증

---

## 📖 참고 문서

1. **PRD** (`docs/10-prd/textexpander-prd.md`)
   - 사용자 시나리오, 기능 목록, 동작 방식, 비기능 요구사항

2. **아키텍처** (`docs/20-design/textexpander-architecture.md`)
   - Core/App 프로젝트 책임, 데이터 저장 구조, 의존성 방향

3. **ExpanderEngine 설계** (`docs/20-design/textexpander-expander-engine.md`)
   - 목적, 구분자 정의, 버퍼 관리 정책, 의사코드 구현

4. **Cursor 규칙** (`.cursor/rules/textexpander.mdc`)
   - 파일 생성 패턴, 네이밍 규칙, 라이어 책임, 체크리스트

---

## 💡 팁

- **컨텍스트 제공**: Cursor와 대화할 때 항상 위 3개 문서 중 관련된 것을 첨부해서 컨텍스트를 분명히 제공하세요.
- **단계별 구현**: Core부터 완성한 뒤 App을 구현하는 순서 추천 (의존성 관계 때문)
- **리팩터링**: 기능 추가할 때마다 Core의 도메인·서비스 인터페이스부터 수정한 뒤, App이 호출하는 부분을 업데이트하세요.

---

> 모든 Cursor 대화 시 이 가이드의 링크된 문서들을 함께 제공하면, AI가 더 정확한 코드를 생성합니다.
