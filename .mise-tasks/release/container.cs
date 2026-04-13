#!/usr/bin/env dotnet
//MISE description="Build and push multi-arch container image"
//MISE alias="rctr"
#:property PublishAot=false
#:package CliWrap@*

using CliWrap;

var repoRoot = Environment.CurrentDirectory;
var version = ResolveVersion(args);
var containerfile = Path.Combine(repoRoot, "src", "ScaffoldProjectName.ApiService", "Containerfile.release");
var registry = Env("CONTAINER_REGISTRY", "ghcr.io");
var owner = Env("CONTAINER_OWNER", "ScaffoldOwner").ToLowerInvariant();
var imageName = Env("CONTAINER_NAME", "ScaffoldProjectName").ToLowerInvariant();
var image = $"{registry}/{owner}/{imageName}";
var platforms = Env("CONTAINER_PLATFORMS", "linux/amd64,linux/arm64");

Info($"Building multi-arch container {image}:{version} for platforms [{platforms}]");

if (!File.Exists(containerfile))
{
    Err($"Containerfile not found: {containerfile}");
    return 1;
}

var buildArgs = new List<string>
{
    "buildx", "build",
    "--platform", platforms,
    "--file", containerfile,
    "--tag", $"{image}:{version}",
    "--tag", $"{image}:latest",
    "--push",
    "."
};

var result = await Cli.Wrap("docker")
    .WithArguments(buildArgs)
    .WithWorkingDirectory(repoRoot)
    .WithValidation(CommandResultValidation.None)
    .WithStandardOutputPipe(PipeTarget.ToStream(Console.OpenStandardOutput()))
    .WithStandardErrorPipe(PipeTarget.ToStream(Console.OpenStandardError()))
    .ExecuteAsync();

if (result.ExitCode != 0)
{
    Err($"Container build failed (exit code {result.ExitCode})");
    return result.ExitCode;
}

Ok($"Pushed {image}:{version} and {image}:latest");
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
