#!/usr/bin/env dotnet
//MISE description="Update the OpenAPI baseline (commit the result to version the contract)"
//MISE depends=["openapi:generate"]
//MISE alias="oabl"
#:property PublishAot=false
#:package CliWrap@*

var repoRoot = Environment.CurrentDirectory;
var source = Path.Combine(repoRoot, "out", "openapi", "ScaffoldProjectName.ApiService.json");
var dest = Path.Combine(repoRoot, "openapi-baseline.json");

Info($"Copying {source} -> {dest}");
File.Copy(source, dest, overwrite: true);
Ok("openapi-baseline.json updated - commit this file to version the contract");
return 0;

static void Info(string msg) => Console.WriteLine($"\x1b[1;34m==> {msg}\x1b[0m");
static void Ok(string msg) => Console.WriteLine($"\x1b[1;32m  + {msg}\x1b[0m");
static void Err(string msg) => Console.WriteLine($"\x1b[1;31m  x {msg}\x1b[0m");
