

using System;
using System.IO;
using System.Collections.Generic;

namespace NO_Tactitools.Core;
public static class FileUtilities
{
    public static List<string> GetListFromConfigFile(string configFile)
    {
        var absolutePath = Path.Combine(Path.GetDirectoryName(typeof(Plugin).Assembly.Location), "config", configFile);
        var lines = File.ReadAllLines(absolutePath);
        return [.. lines];
    }
}