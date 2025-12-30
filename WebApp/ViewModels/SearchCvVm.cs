using System;
using System.Collections.Generic;

namespace WebApp.ViewModels;

/// <summary>
/// ViewModel för CV-sökresultat.
/// Innehåller sökparametrar och en lista med CV-kort för presentation.
/// </summary>
public sealed class SearchCvVm
{
    public string NameQuery { get; init; } = string.Empty;
    public string CityQuery { get; init; } = string.Empty;

    // Mode: "normal" innebär AND över sökord (kräver alla tokens),
    // "similar" innebär matchning mot en användarprofilens kompetenser.
    public string Mode { get; init; } = "normal";

    public string Sort { get; init; } = string.Empty;
    public bool SortLockedToMatch { get; init; }

    public bool ShowLoginTip { get; init; }

    public int[] SelectedSkillIds { get; init; } = Array.Empty<int>();
    public string[] SelectedSkillNames { get; init; } = Array.Empty<string>();

    public bool IsSimilarMode => string.Equals(Mode, "similar", StringComparison.OrdinalIgnoreCase);
    public int SimilarSourceTotal { get; init; }
    public string SimilarHint { get; init; } = string.Empty;
    public string? Source { get; init; }
    public string? SourceUserId { get; init; }

    public List<CompetenceItemVm> Competences { get; init; } = new();

    // Statisk lista med alla tillgängliga färdigheter (krävs för vy).
    public List<SkillItemVm> AllSkills { get; init; } = new();

    // Lista med CV-kort som används för vy-rendering.
    public List<CvCardVm> Cvs { get; init; } = new();

    public sealed class CompetenceItemVm
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Category { get; init; } = string.Empty;
        public bool IsTopList { get; init; }
        public int SortOrder { get; init; }
    }

    public sealed class SkillItemVm
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
    }

    public sealed class CvCardVm
    {
        public string UserId { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public string? Headline { get; init; }
        public string City { get; init; } = string.Empty;
        public bool IsPrivate { get; init; }
        public string? ProfileImagePath { get; init; }

        public string? AboutMe { get; init; }

        // Färdigheter som array av unika strängar.
        public string[] Skills { get; init; } = Array.Empty<string>();

        // Utbildningar/erfarenheter som redan formaterade strängar för vy.
        public string[] Educations { get; init; } = Array.Empty<string>();
        public string[] Experiences { get; init; } = Array.Empty<string>();

        public int ProjectCount { get; init; }

        public int? MatchCount { get; init; }
        public int? SourceTotal { get; init; }

        public DateTimeOffset CreatedUtc { get; init; }
    }
}
