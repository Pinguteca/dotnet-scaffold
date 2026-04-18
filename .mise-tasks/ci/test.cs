#!/usr/bin/env dotnet
//MISE description="Run tests in Release with TRX logger and XPlat coverage"
#:property PublishAot=false
#:package CliWrap@*

using CliWrap;

var repoRoot = Environment.CurrentDirectory;
var slnx = Path.Combine(repoRoot, "ScaffoldProjectName.slnx");

// Check if any test projects exist under src/, tests/, or test/
var testDirs = new[] { "src", "tests", "test" };
var hasTests = testDirs
    .Select(d => Path.Combine(repoRoot, d))
    .Where(Directory.Exists)
    .SelectMany(d => Directory.GetFiles(d, "*Tests*.csproj", SearchOption.AllDirectories)
        .Concat(Directory.GetFiles(d, "*Test*.csproj", SearchOption.AllDirectories)))
    .Any();

if (!hasTests)
{
    Info("No test projects found, skipping tests");
    return 0;
}

Info("Running tests in Release configuration");

var result = await Cli.Wrap("dotnet")
    .WithArguments([
        "test", slnx,
        "--configuration", "Release",
        "--no-build",
        "--logger", "trx;LogFileName=test-results.trx",
        "--collect:XPlat Code Coverage"
    ])
    .WithWorkingDirectory(repoRoot)
    .WithValidation(CommandResultValidation.None)
    .WithStandardOutputPipe(PipeTarget.ToStream(Console.OpenStandardOutput()))
    .WithStandardErrorPipe(PipeTarget.ToStream(Console.OpenStandardError()))
    .ExecuteAsync();

if (result.ExitCode != 0)
{
    Err($"Tests failed (exit code {result.ExitCode})");
    return result.ExitCode;
}

Ok("All tests passed");
return 0;

static void Info(string msg) => Console.WriteLine($"\x1b[1;34m==> {msg}\x1b[0m");
static void Ok(string msg) => Console.WriteLine($"\x1b[1;32m  + {msg}\x1b[0m");
static void Err(string msg) => Console.WriteLine($"\x1b[1;31m  x {msg}\x1b[0m");
