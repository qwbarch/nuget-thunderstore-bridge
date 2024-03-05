/*
 * This file is largely based upon
 * https://github.com/thunderstore-io/thunderstore-cli/blob/10b73c843f2dd1a9ed9c6cb687dbbaa555626052/ThunderstoreCLI/Configuration/ProjectFileConfig.cs
 * thunderstore-cli Copyright (c) 2021 Thunderstore
 * Thunderstore expressly permits Lordfirespeed to use and redistribute the source of thunderstore-cli as Lordfirespeed sees fit.
 * Lordfirespeed licenses the referenced file to the Sigurd team under the MIT license.
 *
 * Copyright (c) 2024 Sigurd Team
 * The Sigurd Team licenses this file to you under the LGPL-3.0-OR-LATER license.
 */

using System.IO;
using System.Linq;
using ThunderstoreCLI.Models;
using static Crayon.Output;

namespace ThunderstoreCLI.Configuration;

internal class ProjectFileConfig : EmptyConfig
{
    private string SourcePath = null!;
    private ThunderstoreProject Project = null!;

    public override void Parse(Config currentConfig)
    {
        SourcePath = currentConfig.GetProjectConfigPath();
        if (!File.Exists(SourcePath))
        {
            Utils.Write.Warn(
                "Unable to find project configuration file",
                $"Looked from {Dim(SourcePath)}"
            );
            Project = new ThunderstoreProject(false);
            return;
        }
        Project = ThunderstoreProject.Deserialize(File.ReadAllText(SourcePath))!;
    }

    public override GeneralConfig GetGeneralConfig()
    {
        return new GeneralConfig
        {
            Repository = Project.Publish?.Repository!
        };
    }

    public override PackageConfig GetPackageMeta()
    {
        return new PackageConfig
        {
            Namespace = Project.Package?.Namespace,
            Name = Project.Package?.Name,
            VersionNumber = Project.Package?.VersionNumber,
            ProjectConfigPath = SourcePath,
            Description = Project.Package?.Description,
            Dependencies = Project.Package?.Dependencies,
            ContainsNsfwContent = Project.Package?.ContainsNsfwContent,
            WebsiteUrl = Project.Package?.WebsiteUrl
        };
    }

    public override BuildConfig GetBuildConfig()
    {
        return new BuildConfig
        {
            CopyPaths = Project.Build?.CopyPaths
                .Select(static path => new CopyPathMap(path.Source, path.Target))
                .ToList(),
            IconPath = Project.Build?.Icon,
            OutDir = Project.Build?.OutDir,
            ReadmePath = Project.Build?.Readme
        };
    }

    public override PublishConfig GetPublishConfig()
    {
        return new PublishConfig
        {
            Categories = Project.Publish?.Categories.Categories,
            Communities = Project.Publish?.Communities
        };
    }

    public override InstallConfig GetInstallConfig()
    {
        return new InstallConfig
        {
            InstallerDeclarations = Project.Install?.InstallerDeclarations
                .Select(static path => new InstallerDeclaration(path.Identifier))
                .ToList()
        };
    }

    public static void Write(Config config, string path)
    {
        File.WriteAllText(path, new ThunderstoreProject(config).Serialize());
    }
}