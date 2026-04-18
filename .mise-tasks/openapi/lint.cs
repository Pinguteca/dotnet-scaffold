#!/usr/bin/env dotnet
//MISE description="Lint OpenAPI spec with CI-friendly output format"
//MISE depends=["openapi:generate"]
#:property PublishAot=false
#:package CliWrap@*

using CliWrap;

var repoRoot = Environment.CurrentDirectory;
var spec = Path.Combine(repoRoot, "out", "openapi", "ScaffoldProjectName.ApiService.json");
var ruleset = Path.Combine(repoRoot, ".spectral.yml");

if (!File.Exists(spec))
{
    Err($"OpenAPI spec not found at {spec}");
    Err("Run 'mise run openapi:generate' first");
    return 1;
}

Info("Linting OpenAPI spec");

// Use github-actions format if running in CI, pretty otherwise
var isCI = Environment.GetEnvironmentVariable("CI") == "true"
    || Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true";
var format = isCI ? "github-actions" : "pretty";

var result = await Cli.Wrap("spectral")
    .WithArguments([
        "lint", spec,
        "--ruleset", ruleset,
        "--format", format
    ])
    .WithWorkingDirectory(repoRoot)
    .WithValidation(CommandResultValidation.None)
    .WithStandardOutputPipe(PipeTarget.ToStream(Console.OpenStandardOutput()))
    .WithStandardErrorPipe(PipeTarget.ToStream(Console.OpenStandardError()))
    .ExecuteAsync();

if (result.ExitCode != 0)
{
    Err($"Spectral lint failed (exit code {result.ExitCode})");
    return result.ExitCode;
}

Ok("OpenAPI spec passed lint checks");
return 0;

static void Info(string msg) => Console.WriteLine($"\x1b[1;34m==> {msg}\x1b[0m");
static void Ok(string msg) => Console.WriteLine($"\x1b[1;32m  + {msg}\x1b[0m");
static void Err(string msg) => Console.WriteLine($"\x1b[1;31m  x {msg}\x1b[0m");
