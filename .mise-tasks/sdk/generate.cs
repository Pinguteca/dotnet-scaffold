#!/usr/bin/env dotnet
//MISE description="Generate client SDK (usage: mise run sdk:generate -- <language|all>)"
//MISE depends=["openapi:generate"]
//MISE alias="sdkg"
#:property PublishAot=false
#:package CliWrap@*

using CliWrap;

var repoRoot = Environment.CurrentDirectory;
var inputSpec = Path.Combine(repoRoot, "out", "openapi", "ScaffoldProjectName.ApiService.json");

// Language config: generatorName, additionalProperties pairs
var languages = new Dictionary<string, (string GeneratorName, string[] AdditionalProps)>
{
    ["java"] = ("java", [
        "library=okhttp-gson",
        "dateLibrary=java8",
        "hideGenerationTimestamp=true",
        "disallowAdditionalPropertiesIfNotPresent=false",
        "groupId=ScaffoldOwner",
        "artifactId=ScaffoldProjectName-client",
        "apiPackage=ScaffoldProjectName.client.api",
        "modelPackage=ScaffoldProjectName.client.model"
    ]),
    ["kotlin"] = ("kotlin", [
        "library=jvm-okhttp4",
        "hideGenerationTimestamp=true",
        "groupId=ScaffoldOwner",
        "artifactId=ScaffoldProjectName-client",
        "packageName=ScaffoldProjectName.client"
    ]),
    ["go"] = ("go", [
        "packageName=client",
        "generateInterfaces=true",
        "structPrefix=true",
        "enumClassPrefix=true",
        "moduleName=github.com/ScaffoldOwner/ScaffoldTemplate-go"
    ]),
    ["rust"] = ("rust", [
        "library=reqwest",
        "supportAsync=true",
        "hideGenerationTimestamp=true",
        "packageName=ScaffoldProjectName-client"
    ]),
    ["python"] = ("python", [
        "library=urllib3",
        "hideGenerationTimestamp=true",
        "packageName=ScaffoldProjectName_client",
        "projectName=ScaffoldProjectName-client"
    ]),
    ["typescript"] = ("typescript-fetch", [
        "supportsES6=true",
        "useSingleRequestParameter=true",
        "enumUnknownDefaultCase=true",
        "withInterfaces=true",
        "npmName=@ScaffoldOwner/ScaffoldProjectName-client"
    ]),
    ["csharp"] = ("csharp", [
        "library=httpclient",
        "targetFramework=net10.0",
        "netCoreProjectFile=true",
        "hideGenerationTimestamp=true",
        "packageName=ScaffoldProjectName.Client"
    ]),
    ["dart"] = ("dart", [
        "pubName=scaffoldprojectname_client",
        "pubLibrary=scaffoldprojectname_client.api"
    ]),
};

var requestedLang = args.Length > 0 ? args[0].ToLowerInvariant() : "all";

IEnumerable<string> targets;
if (requestedLang == "all")
{
    targets = languages.Keys;
}
else if (languages.ContainsKey(requestedLang))
{
    targets = [requestedLang];
}
else
{
    Err($"Unknown language '{requestedLang}'. Valid options: all, {string.Join(", ", languages.Keys)}");
    return 1;
}

var targetList = targets.ToList();
Info($"Generating SDK(s): {string.Join(", ", targetList)}");

var failed = new List<string>();

foreach (var lang in targetList)
{
    var (generatorName, additionalProps) = languages[lang];
    var outDir = Path.Combine(repoRoot, "out", "sdk", lang);

    Directory.CreateDirectory(outDir);

    Info($"Generating {lang} ({generatorName})...");

    var cliArgs = new List<string>
    {
        "generate",
        "--input-spec", inputSpec,
        "--generator-name", generatorName,
        "--output", outDir,
        "--additional-properties", string.Join(",", additionalProps)
    };

    var result = await Cli.Wrap("openapi-generator-cli")
        .WithArguments(cliArgs)
        .WithWorkingDirectory(repoRoot)
        .WithValidation(CommandResultValidation.None)
        .WithStandardOutputPipe(PipeTarget.ToStream(Console.OpenStandardOutput()))
        .WithStandardErrorPipe(PipeTarget.ToStream(Console.OpenStandardError()))
        .ExecuteAsync();

    if (result.ExitCode != 0)
    {
        Err($"{lang}: generation failed (exit code {result.ExitCode})");
        failed.Add(lang);
        continue;
    }

    // Clean up junk files
    CleanJunk(outDir, lang);

    Ok($"{lang}: done -> out/sdk/{lang}/");
}

if (failed.Count > 0)
{
    Err($"{failed.Count} language(s) failed: {string.Join(", ", failed)}");
    return 1;
}

Ok($"All {targetList.Count} SDK(s) generated successfully");
return 0;

static void CleanJunk(string outDir, string lang)
{
    // Files to delete unconditionally
    string[] junkFiles = [".travis.yml", "git_push.sh", ".gitignore", ".openapi-generator-ignore"];
    foreach (var file in junkFiles)
    {
        var path = Path.Combine(outDir, file);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    // Directories to delete unconditionally
    var generatorMetaDir = Path.Combine(outDir, ".openapi-generator");
    if (Directory.Exists(generatorMetaDir))
    {
        Directory.Delete(generatorMetaDir, recursive: true);
    }

    // Generated docs and tests are usually noise - remove them
    var docsDir = Path.Combine(outDir, "docs");
    if (Directory.Exists(docsDir))
    {
        Directory.Delete(docsDir, recursive: true);
    }

    var testDir = Path.Combine(outDir, "test");
    if (Directory.Exists(testDir))
    {
        Directory.Delete(testDir, recursive: true);
    }
}

static void Info(string msg) => Console.WriteLine($"\x1b[1;34m==> {msg}\x1b[0m");
static void Ok(string msg) => Console.WriteLine($"\x1b[1;32m  + {msg}\x1b[0m");
static void Err(string msg) => Console.WriteLine($"\x1b[1;31m  x {msg}\x1b[0m");
