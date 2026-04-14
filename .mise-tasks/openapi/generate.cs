#!/usr/bin/env dotnet
//MISE description="Generate OpenAPI spec from ApiService at out/openapi/"
//MISE alias="oag"
#:property PublishAot=false
#:package CliWrap@*

using CliWrap;

var repoRoot = Environment.CurrentDirectory;
var outDir = Path.Combine(repoRoot, "out", "openapi");
var project = Path.Combine(repoRoot, "src", "ScaffoldProjectName.ApiService", "ScaffoldProjectName.ApiService.csproj");

Info("Ensuring out/openapi/ exists...");
Directory.CreateDirectory(outDir);

Info("Building ApiService (Release)...");
var result = await Cli.Wrap("dotnet")
    .WithArguments(["build", project, "--configuration", "Release"])
    .WithWorkingDirectory(repoRoot)
    .WithValidation(CommandResultValidation.None)
    .WithStandardOutputPipe(PipeTarget.ToStream(Console.OpenStandardOutput()))
    .WithStandardErrorPipe(PipeTarget.ToStream(Console.OpenStandardError()))
    .ExecuteAsync();

if (result.ExitCode != 0)
{
    Err($"dotnet build failed (exit code {result.ExitCode})");
    return result.ExitCode;
}

Ok("OpenAPI spec generated at out/openapi/");
return 0;

static void Info(string msg) => Console.WriteLine($"\x1b[1;34m==> {msg}\x1b[0m");
static void Ok(string msg) => Console.WriteLine($"\x1b[1;32m  + {msg}\x1b[0m");
static void Err(string msg) => Console.WriteLine($"\x1b[1;31m  x {msg}\x1b[0m");
