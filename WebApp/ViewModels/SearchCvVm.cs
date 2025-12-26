using System;
using System.Collections.Generic;

namespace WebApp.ViewModels;

public sealed class SearchCvVm
{
    public string NameQuery { get; init; } = string.Empty;
    public string SkillsQuery { get; init; } = string.Empty;

    public bool ShowLoginTip { get; init; }

    public List<CvCardVm> Cvs { get; init; } = new();

    public sealed class CvCardVm
    {
        public string UserId { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public string? Headline { get; init; }
        public string City { get; init; } = string.Empty;
        public bool IsPrivate { get; init; }
        public string? ProfileImagePath { get; init; }
        public string[] Skills { get; init; } = Array.Empty<string>();
        public int ProjectCount { get; init; }
    }
}
