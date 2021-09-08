using Playnite.SDK;
using System;
using System.IO;
using System.Windows;
using CommonPluginsPlaynite.Common;
using System.Threading.Tasks;
using System.Threading;

namespace CommonPluginsShared
{
    public class PluginLocalization
    {
        private static ILogger logger = LogManager.GetLogger();


        /// <summary>
        /// Load common localization
        /// </summary>
        /// <param name="pluginFolder"></param>
        /// <param name="language"></param>
        /// <param name="DefaultLoad"></param>
        internal static void SetPluginLanguage(string pluginFolder, string language, bool DefaultLoad = false)
        {
            var dictionaries = Application.Current.Resources.MergedDictionaries;

            // Load default for missing
            if (!DefaultLoad)
            {
#if DEBUG
                // Force development localization
                Task.Run(() =>
                {
                    Thread.Sleep(2000);
                    SetPluginLanguage(pluginFolder, "LocSource", true);
                });
#endif
            }

#if DEBUG
            if (DefaultLoad)
            {
                // Load default localization 
                var langFile = Path.Combine(pluginFolder, "localization\\" + language + ".xaml");

                if (File.Exists(langFile))
                {
                    ResourceDictionary res = null;
                    try
                    {
                        res = Xaml.FromFile<ResourceDictionary>(langFile);
                        res.Source = new Uri(langFile, UriKind.Absolute);

                        foreach (var key in res.Keys)
                        {
                            if (res[key] is string locString && locString.IsNullOrEmpty())
                            {
                                res.Remove(key);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, $"Failed to integrate localization file {langFile}");
                        return;
                    }

                    dictionaries.Add(res);
                }
                else
                {
                    logger.Warn($"File {langFile} not found");
                }
            }
#endif

            // Load common localization 
            var langFileCommon = Path.Combine(pluginFolder, "localization\\Common\\" + language + ".xaml");

            if (File.Exists(langFileCommon))
            {
                ResourceDictionary res = null;
                try
                {
                    res = Xaml.FromFile<ResourceDictionary>(langFileCommon);
                    res.Source = new Uri(langFileCommon, UriKind.Absolute);

                    foreach (var key in res.Keys)
                    {
                        if (res[key] is string locString && locString.IsNullOrEmpty())
                        {
                            res.Remove(key);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"Failed to integrate localization file {langFileCommon}");
                    return;
                }

                dictionaries.Add(res);
            }
            else
            {
                logger.Warn($"File {langFileCommon} not found");
            }
        }
    }
}
