#!/usr/bin/env dotnet
//MISE description="Run JReleaser full-release"
//MISE depends=["release:publish-all", "release:sbom"]
//MISE alias="rjr"
#:property PublishAot=false
#:package CliWrap@*

using CliWrap;

var repoRoot = Environment.CurrentDirectory;
var version = ResolveVersion(args);

Info($"Running JReleaser full-release for v{version}...");

// Set version env var for JReleaser
Environment.SetEnvironmentVariable("JRELEASER_PROJECT_VERSION", version);

// Collect extra args (e.g., --dry-run)
var extraArgs = args.Length > 0 && args[0] == version
    ? args.Skip(1).ToArray()
    : args;

var jreleaserArgs = new List<string> { "full-release" };
jreleaserArgs.AddRange(extraArgs);

var result = await Cli.Wrap("jreleaser")
    .WithArguments(jreleaserArgs)
    .WithWorkingDirectory(repoRoot)
    .WithValidation(CommandResultValidation.None)
    .WithStandardOutputPipe(PipeTarget.ToStream(Console.OpenStandardOutput()))
    .WithStandardErrorPipe(PipeTarget.ToStream(Console.OpenStandardError()))
    .ExecuteAsync();

if (result.ExitCode != 0)
{
    Err($"JReleaser failed (exit code {result.ExitCode})");
    return result.ExitCode;
}

Ok($"JReleaser full-release complete for v{version}");
return 0;

// Helpers
static string ResolveVersion(string[] args)
{
    var version = Environment.GetEnvironmentVariable("RELEASE_VERSION")
        ?? (args.Length > 0 ? args[0] : null);
    if (string.IsNullOrEmpty(version))
    {
        Err("No version specified. Set RELEASE_VERSION or pass as argument.");
        Environment.Exit(1);
    }
    return version!;
}

static void Info(string msg) => Console.WriteLine($"\x1b[1;34m==> {msg}\x1b[0m");
static void Ok(string msg) => Console.WriteLine($"\x1b[1;32m  + {msg}\x1b[0m");
static void Err(string msg) => Console.WriteLine($"\x1b[1;31m  x {msg}\x1b[0m");
