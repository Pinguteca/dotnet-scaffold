#!/usr/bin/env dotnet
//MISE description="Fail if there are breaking changes vs the committed baseline"
//MISE depends=["openapi:generate"]
//MISE alias="oab"
#:property PublishAot=false
#:package CliWrap@*

using CliWrap;

var repoRoot = Environment.CurrentDirectory;
var baseline = Path.Combine(repoRoot, "openapi-baseline.json");
var current = Path.Combine(repoRoot, "out", "openapi", "ScaffoldProjectName.ApiService.json");

if (!File.Exists(baseline))
{
    Info("No baseline at openapi-baseline.json - skipping breaking change detection");
    return 0;
}

Info("Checking for breaking changes against openapi-baseline.json...");
var result = await Cli.Wrap("oasdiff")
    .WithArguments(["breaking", baseline, current, "--fail-on", "ERR", "--format", "text"])
    .WithWorkingDirectory(repoRoot)
    .WithValidation(CommandResultValidation.None)
    .WithStandardOutputPipe(PipeTarget.ToStream(Console.OpenStandardOutput()))
    .WithStandardErrorPipe(PipeTarget.ToStream(Console.OpenStandardError()))
    .ExecuteAsync();

if (result.ExitCode != 0)
{
    Err($"Breaking changes detected (exit code {result.ExitCode})");
    return result.ExitCode;
}

Ok("No breaking changes detected");
return 0;

static void Info(string msg) => Console.WriteLine($"\x1b[1;34m==> {msg}\x1b[0m");
static void Ok(string msg) => Console.WriteLine($"\x1b[1;32m  + {msg}\x1b[0m");
static void Err(string msg) => Console.WriteLine($"\x1b[1;31m  x {msg}\x1b[0m");
