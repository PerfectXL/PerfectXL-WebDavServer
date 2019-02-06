using System.Collections.Generic;
using System.Configuration;

namespace PerfectXL.WebDavServer
{
    internal static class KeyValueConfigurationCollectionExtensions
    {
        internal static void AddOrUpdateSettings(this KeyValueConfigurationCollection settings, Dictionary<string, string> newSettings)
        {
            foreach (var kvp in newSettings)
            {
                if (settings[kvp.Key] == null)
                {
                    settings.Add(kvp.Key, kvp.Value);
                }
                else
                {
                    settings[kvp.Key].Value = kvp.Value;
                }
            }
        }
    }
}