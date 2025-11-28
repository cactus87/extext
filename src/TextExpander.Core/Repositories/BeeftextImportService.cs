using System.Text.Json;
using System.Text.Json.Serialization;
using TextExpander.Core.Domain;

namespace TextExpander.Core.Repositories;

/// <summary>
/// Beeftext JSON 형식을 TextExpander 형식으로 변환
/// </summary>
public class BeeftextImportService
{
    // Beeftext 구조 (그룹 UUID → 카테고리명 맵핑)
    private static readonly Dictionary<string, string> BeeftextGroupMapping = new()
    {
        // 기존 카테고리 매핑
        { "{3de718cf-d81d-4983-8d73-9ca87ad888fa}", "일반" },
        { "{8040f460-168e-429a-a76e-9505f7302d2f}", "개발" },
        { "{869421f2-b48a-41bc-a8ee-204be64bcd03}", "개인정보" },
        { "{db8fd60d-5ed3-4b3d-8038-195b58389603}", "건강/의료" },
        { "{bb1531b1-33f1-4cfd-b2bc-e32a49cce6a7}", "기술" },
        { "{23ef3d18-dd5a-4972-95f3-3ea06399ac96}", "지식관리" },
        { "{3f8da907-379d-4482-8175-e77a536ba9c9}", "언어/표현" },
        { "{cd386186-2d55-45c9-9343-ea2a9d0c53a8}", "버그해결" },
    };

    /// <summary>
    /// Beeftext JSON 파일 내용을 TextExpander 형식으로 변환
    /// </summary>
    public static (List<Category> categories, List<Snippet> snippets) ConvertFromBeeftext(
        string beeftextJson)
    {
        var categories = new List<Category>();
        var snippets = new List<Snippet>();

        try
        {
            using var doc = JsonDocument.Parse(beeftextJson);
            var root = doc.RootElement;

            // 초기 카테고리 생성
            var categoryMap = CreateRefinedCategories();
            categories.AddRange(categoryMap.Values);

            // Combos를 스니펫으로 변환
            if (root.TryGetProperty("combos", out var combosElement))
            {
                foreach (var combo in combosElement.EnumerateArray())
                {
                    var snippet = ConvertComboToSnippet(combo, categoryMap);
                    if (snippet != null)
                    {
                        snippets.Add(snippet);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Beeftext JSON 변환 중 오류 발생", ex);
        }

        return (categories, snippets);
    }

    /// <summary>
    /// 15개의 세분화된 카테고리 생성
    /// </summary>
    private static Dictionary<string, Category> CreateRefinedCategories()
    {
        var categories = new Dictionary<string, Category>
        {
            // 1. 일반 (자주 쓰는 표현)
            ["일반"] = new()
            {
                Id = Guid.NewGuid().ToString(),
                Name = "일반",
                Description = "자주 사용하는 일반적인 표현",
                BackgroundColor = "#E3F2FD",
                ForegroundColor = "#1565C0",
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            // 2. 개인정보
            ["개인정보"] = new()
            {
                Id = Guid.NewGuid().ToString(),
                Name = "개인정보",
                Description = "개인 연락처 및 정보",
                BackgroundColor = "#FFF3E0",
                ForegroundColor = "#E65100",
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            // 3. 건강/의료
            ["건강/의료"] = new()
            {
                Id = Guid.NewGuid().ToString(),
                Name = "건강/의료",
                Description = "건강 관련 용어 및 정보",
                BackgroundColor = "#F3E5F5",
                ForegroundColor = "#6A1B9A",
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            // 4. 개발/프로그래밍
            ["개발"] = new()
            {
                Id = Guid.NewGuid().ToString(),
                Name = "개발/프로그래밍",
                Description = "코딩 관련 용어 및 명령어",
                BackgroundColor = "#E8F5E9",
                ForegroundColor = "#1B5E20",
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            // 5. AI/LLM
            ["AI/LLM"] = new()
            {
                Id = Guid.NewGuid().ToString(),
                Name = "AI/LLM",
                Description = "AI 및 대언어모델 관련 용어",
                BackgroundColor = "#FCE4EC",
                ForegroundColor = "#C2185B",
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            // 6. 기술/도구
            ["기술/도구"] = new()
            {
                Id = Guid.NewGuid().ToString(),
                Name = "기술/도구",
                Description = "기술 스택 및 개발 도구",
                BackgroundColor = "#ECEFF1",
                ForegroundColor = "#37474F",
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            // 7. 옵시디언
            ["옵시디언"] = new()
            {
                Id = Guid.NewGuid().ToString(),
                Name = "옵시디언",
                Description = "옵시디언 관련 용어",
                BackgroundColor = "#E0F2F1",
                ForegroundColor = "#004D40",
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            // 8. 접속부사/연결표현
            ["접속부사"] = new()
            {
                Id = Guid.NewGuid().ToString(),
                Name = "접속부사/연결표현",
                Description = "글쓰기 위한 접속부사 모음",
                BackgroundColor = "#FFF8E1",
                ForegroundColor = "#F57F17",
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            // 9. 소셜미디어
            ["소셜미디어"] = new()
            {
                Id = Guid.NewGuid().ToString(),
                Name = "소셜미디어",
                Description = "소셜미디어 관련 용어",
                BackgroundColor = "#F1F8E9",
                ForegroundColor = "#33691E",
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            // 10. 조직/기관
            ["조직/기관"] = new()
            {
                Id = Guid.NewGuid().ToString(),
                Name = "조직/기관",
                Description = "기관 및 조직 이름",
                BackgroundColor = "#E0F2F1",
                ForegroundColor = "#00695C",
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            // 11. 문서/작성
            ["문서/작성"] = new()
            {
                Id = Guid.NewGuid().ToString(),
                Name = "문서/작성",
                Description = "문서 작성 관련 용어",
                BackgroundColor = "#FCE4EC",
                ForegroundColor = "#880E4F",
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            // 12. 지식관리
            ["지식관리"] = new()
            {
                Id = Guid.NewGuid().ToString(),
                Name = "지식관리",
                Description = "지식 및 정보 관리 관련",
                BackgroundColor = "#EDE7F6",
                ForegroundColor = "#3F2C70",
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            // 13. 장비/기기
            ["장비/기기"] = new()
            {
                Id = Guid.NewGuid().ToString(),
                Name = "장비/기기",
                Description = "의료 및 IT 장비",
                BackgroundColor = "#F3E5F5",
                ForegroundColor = "#7B1FA2",
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            // 14. 업무/프로젝트
            ["업무/프로젝트"] = new()
            {
                Id = Guid.NewGuid().ToString(),
                Name = "업무/프로젝트",
                Description = "업무 및 프로젝트 관련",
                BackgroundColor = "#FFF3E0",
                ForegroundColor = "#E65100",
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            // 15. 기타
            ["기타"] = new()
            {
                Id = Guid.NewGuid().ToString(),
                Name = "기타",
                Description = "분류되지 않은 스니펫",
                BackgroundColor = "#EEEEEE",
                ForegroundColor = "#212121",
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
        };

        return categories;
    }

    /// <summary>
    /// Beeftext combo를 TextExpander Snippet으로 변환
    /// </summary>
    private static Snippet? ConvertComboToSnippet(JsonElement combo, Dictionary<string, Category> categories)
    {
        try
        {
            var keyword = combo.GetProperty("keyword").GetString();
            var snippet = combo.GetProperty("snippet").GetString();
            var groupUuid = combo.GetProperty("group").GetString();
            var enabled = combo.GetProperty("enabled").GetBoolean();

            if (string.IsNullOrWhiteSpace(keyword) || string.IsNullOrWhiteSpace(snippet))
            {
                return null;
            }

            // 카테고리 결정
            var categoryName = DetermineCategoryFromContent(keyword, snippet, groupUuid);
            if (!categories.TryGetValue(categoryName, out var category))
            {
                categoryName = "기타";
                category = categories[categoryName];
            }

            return new Snippet
            {
                Id = Guid.NewGuid().ToString(),
                CategoryId = category.Id,
                Keyword = keyword,
                Replacement = snippet.TrimEnd(),
                IsEnabled = enabled,
                Note = "",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 콘텐츠를 기반으로 적절한 카테고리 결정
    /// </summary>
    private static string DetermineCategoryFromContent(string keyword, string snippet, string? groupUuid)
    {
        // 기존 그룹 UUID 기반 맵핑 사용 (우선순위 높음)
        if (groupUuid != null && BeeftextGroupMapping.TryGetValue(groupUuid, out var mappedCategory))
        {
            return mappedCategory;
        }

        // 내용 기반 분류
        var lowerKeyword = keyword.ToLower();
        var lowerSnippet = snippet.ToLower();

        // 개인정보
        if (lowerSnippet.Contains("@naver") || lowerSnippet.Contains("@gmail") || 
            lowerSnippet.Contains("010-") || lowerSnippet.Contains("휴대폰") ||
            lowerSnippet.Contains("주소") || lowerSnippet.Contains("이메일"))
        {
            return "개인정보";
        }

        // 건강/의료
        if (lowerSnippet.Contains("근육") || lowerSnippet.Contains("병") ||
            lowerSnippet.Contains("호흡") || lowerSnippet.Contains("장애") ||
            lowerSnippet.Contains("의료") || lowerSnippet.Contains("협회"))
        {
            return "건강/의료";
        }

        // 개발/프로그래밍
        if (lowerSnippet.Contains("code") || lowerSnippet.Contains("claude") ||
            lowerSnippet.Contains("api") || lowerSnippet.Contains("프로그램"))
        {
            return "개발";
        }

        // AI/LLM
        if (lowerSnippet.Contains("claude") || lowerSnippet.Contains("ai") ||
            lowerSnippet.Contains("llm") || lowerSnippet.Contains("프롬프트"))
        {
            return "AI/LLM";
        }

        // 기술/도구
        if (lowerSnippet.Contains("vs code") || lowerSnippet.Contains("cursor") ||
            lowerSnippet.Contains("설치") || lowerSnippet.Contains("plugin"))
        {
            return "기술/도구";
        }

        // 옵시디언
        if (lowerSnippet.Contains("옵시디언") || lowerSnippet.Contains("obsidian") ||
            lowerSnippet.Contains("원자 노트"))
        {
            return "옵시디언";
        }

        // 접속부사
        if (lowerSnippet.Contains("따라서") || lowerSnippet.Contains("그렇지만") ||
            lowerSnippet.Contains("하지만") || lowerSnippet.Contains("그런데") ||
            lowerSnippet.Contains("왜냐하면") || lowerSnippet.Contains("그러므로"))
        {
            return "접속부사";
        }

        // 소셜미디어
        if (lowerSnippet.Contains("카카오") || lowerSnippet.Contains("인스타") ||
            lowerSnippet.Contains("유튜브") || lowerSnippet.Contains("넷플릭스") ||
            lowerSnippet.Contains("디스코드"))
        {
            return "소셜미디어";
        }

        // 조직/기관
        if (lowerSnippet.Contains("광주") || lowerSnippet.Contains("협회") ||
            lowerSnippet.Contains("기관") || lowerSnippet.Contains("재단"))
        {
            return "조직/기관";
        }

        // 문서/작성
        if (lowerSnippet.Contains("계획") || lowerSnippet.Contains("템플릿") ||
            lowerSnippet.Contains("가이드") || lowerSnippet.Contains("참고자료"))
        {
            return "문서/작성";
        }

        // 장비/기기
        if (lowerSnippet.Contains("데스크탑") || lowerSnippet.Contains("키보드") ||
            lowerSnippet.Contains("마우스") || lowerSnippet.Contains("호흡기"))
        {
            return "장비/기기";
        }

        // 기타
        return "기타";
    }
}

