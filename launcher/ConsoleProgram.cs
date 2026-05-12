using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace CircuitLangLauncher
{
    internal static class ConsoleProgram
    {
        private const string AppName = "GokottaElecCLI";
        private const string AppVersion = "V1.5";

        private static int Main(string[] args)
        {
            string repoRoot = FindRepoRoot(AppContext.BaseDirectory);
            string cliPath = Path.Combine(repoRoot, "scripts", "elec-cli.mjs");

            if (!File.Exists(cliPath))
            {
                Console.Error.WriteLine(AppName + " " + AppVersion);
                Console.Error.WriteLine("ERROR: scripts\\elec-cli.mjs was not found.");
                Console.Error.WriteLine("repoRoot=" + repoRoot);
                return 1;
            }

            return RunNode(repoRoot, BuildForwardedArgs(cliPath, args));
        }

        private static string[] BuildForwardedArgs(string cliPath, string[] args)
        {
            List<string> forwarded = new List<string>();
            forwarded.Add(cliPath);

            if (args.Length == 0)
            {
                forwarded.Add("--help");
                return forwarded.ToArray();
            }

            string first = CleanArg(args[0]);
            string ext = Path.GetExtension(first);
            if (ext.Equals(".txt", StringComparison.OrdinalIgnoreCase))
            {
                forwarded.Add("paste");
            }
            else if (ext.Equals(".cnl", StringComparison.OrdinalIgnoreCase))
            {
                forwarded.Add("build");
            }

            foreach (string arg in args)
            {
                forwarded.Add(arg);
            }

            return forwarded.ToArray();
        }

        private static int RunNode(string repoRoot, string[] forwardedArgs)
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = FindNode();
            psi.Arguments = JoinArguments(forwardedArgs);
            psi.WorkingDirectory = repoRoot;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.CreateNoWindow = true;

            try
            {
                using (Process process = Process.Start(psi))
                {
                    if (process == null)
                    {
                        Console.Error.WriteLine("ERROR: failed to start Node.js.");
                        return 1;
                    }

                    string stdout = process.StandardOutput.ReadToEnd();
                    string stderr = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (!string.IsNullOrEmpty(stdout)) Console.Out.Write(stdout);
                    if (!string.IsNullOrEmpty(stderr)) Console.Error.Write(stderr);

                    return process.ExitCode;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.GetType().Name + ": " + ex.Message);
                return 1;
            }
        }

        private static string FindRepoRoot(string exePath)
        {
            DirectoryInfo directory = Directory.Exists(exePath)
                ? new DirectoryInfo(exePath)
                : new DirectoryInfo(Path.GetDirectoryName(exePath) ?? Directory.GetCurrentDirectory());

            for (DirectoryInfo current = directory; current != null; current = current.Parent)
            {
                if (File.Exists(Path.Combine(current.FullName, "scripts", "elec-cli.mjs")) &&
                    File.Exists(Path.Combine(current.FullName, "package.json")))
                {
                    return current.FullName;
                }
            }

            return Directory.GetCurrentDirectory();
        }

        private static string FindNode()
        {
            string[] candidates = { "node.exe", "node" };
            foreach (string candidate in candidates)
            {
                try
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.FileName = candidate;
                    startInfo.Arguments = "--version";
                    startInfo.UseShellExecute = false;
                    startInfo.RedirectStandardOutput = true;
                    startInfo.RedirectStandardError = true;
                    startInfo.CreateNoWindow = true;
                    using (Process process = Process.Start(startInfo))
                    {
                        if (process == null) continue;
                        process.WaitForExit(3000);
                        if (process.ExitCode == 0) return candidate;
                    }
                }
                catch
                {
                }
            }

            return "node.exe";
        }

        private static string JoinArguments(string[] values)
        {
            List<string> result = new List<string>();
            foreach (string value in values)
            {
                result.Add(Quote(value));
            }
            return string.Join(" ", result.ToArray());
        }

        private static string Quote(string value)
        {
            string cleaned = value ?? "";
            if (cleaned.Length == 0) return "\"\"";
            if (cleaned.IndexOfAny(new[] { ' ', '\t', '\r', '\n', '"' }) < 0) return cleaned;
            return "\"" + cleaned.Replace("\"", "\\\"") + "\"";
        }

        private static string CleanArg(string value)
        {
            return (value ?? "").Trim().Trim('"', '\'');
        }
    }
}
