using System;
using System.IO;
using System.Collections.Generic;

namespace NO_Tactitools.Core;
public static class FileUtilities
{
    public static List<string> GetListFromConfigFile(string configFile)
    {
        var assemblyDir = Path.GetDirectoryName(typeof(Plugin).Assembly.Location) ?? Environment.CurrentDirectory;
        var absolutePath = Path.Combine(assemblyDir, "config", configFile);
        if (!File.Exists(absolutePath)) return [];

        var lines = File.ReadAllLines(absolutePath);
        var result = new List<string>(lines.Length);
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var trimmedStart = line.TrimStart();
            if (trimmedStart.StartsWith("//")) continue;
            result.Add(trimmedStart.TrimEnd());
        }
        Plugin.Log("Loading config file " + configFile + " with " + result.Count + " entries.");
        return result;
    }
}