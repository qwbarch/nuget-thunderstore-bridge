/*
 * Copyright (c) 2024 Sigurd Team
 * The Sigurd Team licenses this file to you under the GPL-3.0-OR-LATER license.
 */

using System.Text.Json.Serialization;
using Json.Schema;
using Json.Schema.Serialization;
using NuGet.Frameworks;

namespace Build.Schema;

[JsonSchema(typeof(CommunityConfiguration), nameof(CommunityConfigurationSchema))]
public class CommunityConfiguration
{
	public static JsonSchema CommunityConfigurationSchema = JsonSchema.FromFile("../assets/community.schema.json");

    [JsonPropertyName("communitySlug")]
    public required string CommunitySlug { get; init; }

    [JsonConverter(typeof(NuGetFrameworkJsonConverter))]
    [JsonPropertyName("runtimeFrameworkMoniker")]
    public required NuGetFramework RuntimeFramework { get; init; }

    [JsonPropertyName("packageNamespace")]
    public required string PackageNamespace { get; init; }
}
