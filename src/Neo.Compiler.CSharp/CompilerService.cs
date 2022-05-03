// Copyright (C) 2015-2021 The Neo Project.
// 
// The Neo.Compiler.CSharp is free software distributed under the MIT 
// software license, see the accompanying file LICENSE in the main directory 
// of the project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.CodeAnalysis;
using Neo.IO;
using Neo.IO.Json;
using Neo.SmartContract;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using Diagnostic = Microsoft.CodeAnalysis.Diagnostic;

namespace Neo.Compiler
{
    public static class CompilerService
    {
        public static void Compile(string[] srcPaths, string outDir, string contractName, bool generateDebugInfo, bool generateAssembly, bool noOptimize, bool noInline, byte addressVersion)
        {
            var options = new Options
            {
                Output = outDir,
                ContractName = contractName,
                Debug = generateDebugInfo,
                Assembly = generateAssembly,
                NoOptimize = noOptimize,
                NoInline = noInline,
                AddressVersion = addressVersion
            };

            Handle(options, srcPaths);
        }

        public static CompileResult Compile(string codeStr)
        {
            var options = new Options
            {
                AddressVersion = ProtocolSettings.Default.AddressVersion
            };
            return ProcessSourceCode(options, codeStr);
        }

        private static int Handle(Options options, string[] paths)
        {
            if (paths is null || paths.Length == 0)
            {
                var ret = ProcessDirectory(options, Environment.CurrentDirectory);
                return ret;
            }
            paths = paths.Select(p => Path.GetFullPath(p)).ToArray();
            if (paths.Length == 1)
            {
                string path = paths[0];
                if (Directory.Exists(path))
                    return ProcessDirectory(options, path);
                if (File.Exists(path) && Path.GetExtension(path).ToLowerInvariant() == ".csproj")
                    return ProcessCsproj(options, path);
            }
            foreach (string path in paths)
            {
                if (Path.GetExtension(path).ToLowerInvariant() != ".cs")
                {
                    Console.Error.WriteLine("The files must have a .cs extension.");
                    return 1;
                }
                if (!File.Exists(path))
                {
                    Console.Error.WriteLine($"The file \"{path}\" doesn't exist.");
                    return 1;
                }
            }
            return ProcessSources(options, Path.GetDirectoryName(paths[0])!, paths);
        }

        private static int ProcessDirectory(Options options, string path)
        {
            string? csproj = Directory.EnumerateFiles(path, "*.csproj", SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (csproj is null)
            {
                string obj = Path.Combine(path, "obj");
                string[] sourceFiles = Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories).Where(p => !p.StartsWith(obj)).ToArray();
                if (sourceFiles.Length == 0)
                {
                    Console.Error.WriteLine($"No .cs file is found in \"{path}\".");
                    return 2;
                }
                return ProcessSources(options, path, sourceFiles);
            }
            else
            {
                return ProcessCsproj(options, csproj);
            }
        }

        private static int ProcessCsproj(Options options, string path)
        {
            return ProcessOutputs(options, Path.GetDirectoryName(path)!, CompilationContext.CompileProject(path, options));
        }

        private static int ProcessSources(Options options, string folder, string[] sourceFiles)
        {
            return ProcessOutputs(options, folder, CompilationContext.CompileSources(sourceFiles, options));
        }
        private static CompileResult ProcessSourceCode(Options options, string srcCode)
        {
            var compileRes = CompilationContext.CompileCodeStr(srcCode, options);
            var nefFile = compileRes.CreateExecutable();
            var manifest = compileRes.CreateManifest();
            return new CompileResult
            {
                Nef = nefFile,
                Manifest = manifest,
                Diagnostics = compileRes.Diagnostics
            };
        }

        private static int ProcessOutputs(Options options, string folder, CompilationContext context)
        {
            foreach (Diagnostic diagnostic in context.Diagnostics)
            {
                if (diagnostic.Severity == DiagnosticSeverity.Error)
                    Console.Error.WriteLine(diagnostic.ToString());
                else
                    Console.WriteLine(diagnostic.ToString());
            }
            if (context.Success)
            {
                folder = options.Output ?? Path.Combine(folder, "bin", "sc");
                Directory.CreateDirectory(folder);
                string path = Path.Combine(folder, $"{context.ContractName}.nef");
                File.WriteAllBytes(path, context.CreateExecutable().ToArray());
                Console.WriteLine($"Created {path}");
                path = Path.Combine(folder, $"{context.ContractName}.manifest.json");
                File.WriteAllBytes(path, context.CreateManifest().ToByteArray(false));
                Console.WriteLine($"Created {path}");
                if (options.Debug)
                {
                    path = Path.Combine(folder, $"{context.ContractName}.nefdbgnfo");
                    using FileStream fs = new(path, FileMode.Create, FileAccess.Write);
                    using ZipArchive archive = new(fs, ZipArchiveMode.Create);
                    using Stream stream = archive.CreateEntry($"{context.ContractName}.debug.json").Open();
                    stream.Write(context.CreateDebugInformation().ToByteArray(false));
                    Console.WriteLine($"Created {path}");
                }
                if (options.Assembly)
                {
                    path = Path.Combine(folder, $"{context.ContractName}.asm");
                    File.WriteAllText(path, context.CreateAssembly());
                    Console.WriteLine($"Created {path}");
                }
                Console.WriteLine("Compilation completed successfully.");
                return 0;
            }
            else
            {
                Console.Error.WriteLine("Compilation failed.");
                return 1;
            }
        }
    }
    public class CompileResult
    {
        public NefFile Nef { get; set; } = new NefFile();
        public JObject Manifest { get; set; } = new JObject();
        public IEnumerable<Diagnostic> Diagnostics { get; set; } = new List<Diagnostic>();

    }
}