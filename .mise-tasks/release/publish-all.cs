#!/usr/bin/env dotnet
//MISE description="Publish .NET binaries for all target platforms"
//MISE alias="rpa"
#:property PublishAot=false
#:package CliWrap@*

using CliWrap;

var repoRoot = Environment.CurrentDirectory;
var publishDir = Path.Combine(repoRoot, "out", "publish");
var project = Path.Combine(repoRoot, "src", "ScaffoldProjectName.ApiService", "ScaffoldProjectName.ApiService.csproj");
var version = ResolveVersion(args);

string[] rids = ["linux-x64", "linux-arm64", "win-x64", "win-arm64", "osx-x64", "osx-arm64"];

Info($"Publishing v{version} for {rids.Length} platforms...");

if (Directory.Exists(publishDir))
    Directory.Delete(publishDir, true);
Directory.CreateDirectory(publishDir);

// Parallel publish for all RIDs
var tasks = rids.Select(rid => Task.Run(async () =>
{
    Info($"  dotnet publish --runtime {rid}");
    var result = await Cli.Wrap("dotnet")
        .WithArguments([
            "publish", project,
            "--configuration", "Release",
            "--runtime", rid,
            "--output", Path.Combine(publishDir, rid),
            $"-p:RELEASE_VERSION={version}",
            "--no-self-contained",
            "-p:PublishReadyToRun=true",
            "--verbosity",
            "quiet"
        ])
        .WithValidation(CommandResultValidation.None)
        .WithStandardErrorPipe(PipeTarget.ToStream(Console.OpenStandardError()))
        .ExecuteAsync();

    if (result.ExitCode != 0)
    {
        Err($"  {rid} failed (exit code {result.ExitCode})");
        return (rid, success: false);
    }
    Ok($"  {rid} done");
    return (rid, success: true);
})).ToArray();

var results = await Task.WhenAll(tasks);
var failed = results.Where(r => !r.success).ToArray();

if (failed.Length > 0)
{
    Err($"{failed.Length} platform build(s) failed: {string.Join(", ", failed.Select(r => r.rid))}");
    return 1;
}

Ok($"All {rids.Length} platforms published to out/publish/");
foreach (var rid in rids)
{
    var count = Directory.GetFiles(Path.Combine(publishDir, rid), "*", SearchOption.AllDirectories).Length;
    Ok($"  {rid}: {count} files");
}
return 0;

// Helpers
static string Env(string name, string fallback) =>
    Environment.GetEnvironmentVariable(name) ?? fallback;

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
