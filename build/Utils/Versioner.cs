/*
 * This file is largely based upon
 * https://github.com/adamralph/minver/blob/e17745c3a16e1b64a4863b4e780ca179b34ac38f/MinVer.Lib/Versioner.cs
 * Copyright (c) 2024 Adam Ralph
 * Adam Ralph licenses the referenced file to the Sigurd Team under the Apache-2.0 license.
 *
 * Copyright (c) 2024 Sigurd Team
 * The Sigurd Team licenses this file to you under the GPL-3.0-or-later license.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using LibGit2Sharp;
using NuGet.Versioning;

namespace Utils;

public class Versioner(string repositoryRootPath)
{
    private const string VersionTagPrefix = "v";

    private Repository? _repository;

    private VersionTagCandidate? _currentVersionTag;

    private VersionTagCandidate CurrentVersionTag => _currentVersionTag ??= ComputeVersionTagCandidate();

    public SemanticVersion Version => CurrentVersionTag.Version;

    public DateTimeOffset LastVersionChangeWhen => CurrentVersionTag.Commit.Committer.When;

    [MemberNotNull(nameof(_repository))]
    private void EnsureRepository()
    {
        if (_repository is null) throw new InvalidOperationException();
    }

    private VersionTagCandidate ComputeVersionTagCandidate()
    {
        _repository = new Repository(repositoryRootPath);
        using (_repository) {
            return GetVersionTagCandidates()
                .OrderDescending()
                .First();
        }
    }

    private LinkedList<VersionTagCandidate> GetVersionTagCandidates()
    {
        EnsureRepository();

        var versionTagsBySha = GetVersionTags()
            .GroupBy(versionTag => versionTag.Sha)
            .ToDictionary(
                group => group.Key,
                group => group.ToList()
            );

        var toConsider = new Stack<Commit>();
        toConsider.Push(_repository.Head.Tip);

        var consideredShas = new HashSet<string>();
        var candidates = new LinkedList<VersionTagCandidate>();

        while (toConsider.TryPop(out var commit)) {
            ConsiderCommit(commit);
        }

        return candidates;

        void ConsiderCommit(Commit commit)
        {
            if (!consideredShas.Add(commit.Sha)) return;
            AppendVersionTagCandidates(commit);
            PushParentsForConsideration(commit);
        }

        void AppendVersionTagCandidates(Commit commit)
        {
            if (!versionTagsBySha.TryGetValue(commit.Sha, out var commitVersionTags)) return;
            foreach (var versionTag in commitVersionTags) {
                var candidate = new VersionTagCandidate {
                    Commit = commit,
                    TagName = versionTag.Name,
                    Version = versionTag.Version,
                    Order = candidates.Count,
                };
                candidates.AddLast(candidate);
            }
        }

        void PushParentsForConsideration(Commit commit)
        {
            var hasParent = false;
            foreach (var parent in commit.Parents.Reverse()) {
                hasParent = true;
                toConsider.Push(parent);
            }

            if (hasParent) return;

            var candidate = new VersionTagCandidate {
                Commit = commit,
                TagName = "",
                Version = new SemanticVersion(0, 0, 0),
                Order = candidates.Count,
            };
            candidates.AddLast(candidate);
        }
    }

    private (string Name, string Sha, SemanticVersion Version)[] GetVersionTags()
    {
        EnsureRepository();

        var versionTags = new LinkedList<(string Name, string Sha, SemanticVersion Version)>();

        foreach (var tag in _repository.Tags) {
            var name = tag.FriendlyName;
            if (!name.StartsWith(VersionTagPrefix)) continue;
            var nameWithoutPrefix = name.Substring(VersionTagPrefix.Length);

            if (!SemanticVersion.TryParse(nameWithoutPrefix, out var version)) continue;

            versionTags.AddLast((name, tag.Target.Sha, version));
        }

        return versionTags
            .OrderBy(versionTag => versionTag.Version)
            .ThenBy(versionTag => versionTag.Name)
            .ToArray();
    }

    private sealed record VersionTagCandidate : IComparable<VersionTagCandidate>
    {
        public required Commit Commit { get; init; }
        public required string TagName { get; init; }
        public required SemanticVersion Version { get; init; }
        public required int Order { get; init; }

        public int CompareTo(VersionTagCandidate? other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            var versionComparison = Version.CompareTo(other.Version);
            if (versionComparison != 0) return versionComparison;
            return -Order.CompareTo(other.Order);
        }
    }
}