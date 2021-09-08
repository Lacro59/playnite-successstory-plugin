using Playnite.SDK;
using CommonPluginsPlaynite.Common;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using CommonPluginsShared.Models;
using System.Windows.Automation;
using System.Windows.Media;
using Playnite.SDK.Data;

namespace CommonPluginsShared
{
    public class Common
    {
        private static ILogger logger = LogManager.GetLogger();


        /// <summary>
        /// Load the common ressources
        /// </summary>
        /// <param name="pluginFolder"></param>
        public static void Load(string pluginFolder, string language)
        {
            // Common localization
            PluginLocalization.SetPluginLanguage(pluginFolder, language);

            #region Common xaml
            List<string> ListCommonFiles = new List<string>
            {
                Path.Combine(pluginFolder, "Resources\\Common.xaml"),
                Path.Combine(pluginFolder, "Resources\\LiveChartsCommon\\Common.xaml"),
                Path.Combine(pluginFolder, "Resources\\Controls\\ListExtendStyle.xaml")
            };

            foreach (string CommonFile in ListCommonFiles)
            {
                if (File.Exists(CommonFile))
                {
                    Common.LogDebug(true, $"Load {CommonFile}");

                    ResourceDictionary res = null;
                    try
                    {
                        res = Xaml.FromFile<ResourceDictionary>(CommonFile);
                        res.Source = new Uri(CommonFile, UriKind.Absolute);

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
                        LogError(ex, false, $"Failed to integrate file {CommonFile}");
                        return;
                    }

                    Common.LogDebug(true, $"res: {Serialization.ToJson(res)}");

                    Application.Current.Resources.MergedDictionaries.Add(res);
                }
                else
                {
                    logger.Warn($"File {CommonFile} not find");
                    return;
                }
            }
            #endregion

            #region Common font
            string FontFile = Path.Combine(pluginFolder, "Resources\\font.ttf");
            if (File.Exists(FontFile))
            {
                long fileSize = 0;
                if (Application.Current.Resources.FindName("CommonFontSize") != null)
                {
                    fileSize = (long)Application.Current.Resources.FindName("CommonFontSize");
                }

                // Load only the newest
                if (fileSize <= new FileInfo(FontFile).Length)
                {
                    Application.Current.Resources.Remove("CommonFontSize");
                    Application.Current.Resources.Add("CommonFontSize", new FileInfo(FontFile).Length);

                    FontFamily fontFamily = new FontFamily(new Uri(FontFile), "./#font");
                    Application.Current.Resources.Remove("CommonFont");
                    Application.Current.Resources.Add("CommonFont", fontFamily);
                }
            }
            else
            {
                logger.Warn($"File {FontFile} not find");
            }
            #endregion
        }


        /// <summary>
        /// Load common event
        /// </summary>
        /// <param name="PlayniteAPI"></param>
        public static void SetEvent(IPlayniteAPI PlayniteAPI)
        {
            if (PlayniteAPI.ApplicationInfo.Mode == ApplicationMode.Desktop)
            {
                EventManager.RegisterClassHandler(typeof(Window), Window.LoadedEvent, new RoutedEventHandler(WindowBase_LoadedEvent));
            }
        }

        #region Common event
        private static void WindowBase_LoadedEvent(object sender, System.EventArgs e)
        {
            string WinIdProperty = string.Empty;
            string WinName = string.Empty;

            try
            {
                WinIdProperty = ((Window)sender).GetValue(AutomationProperties.AutomationIdProperty).ToString();
                WinName = ((Window)sender).Name;

                if (WinIdProperty == "WindowSettings")
                {
                    ((Window)sender).Width = 860;
                }
                else if (((Window)sender).DataContext.GetType().GetProperty("SettingsView") != null 
                    && (((dynamic)(Window)sender).DataContext).SettingsView.DataContext is ISettings)
                {
                    ((Window)sender).Width = 700;
                    ((Window)sender).Height = 500;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Error on WindowBase_LoadedEvent for {WinName} - {WinIdProperty}");
            }
        }
        #endregion


        #region Logs
        /// <summary>
        /// Debug log with ignore when no debug mode
        /// </summary>
        /// <param name="IsIgnored"></param>
        /// <param name="Message"></param>
        public static void LogDebug(bool IsIgnored, string Message)
        {
            if (IsIgnored)
            {
                Message = $"[Ignored] {Message}";
            }

#if DEBUG
            logger.Debug(Message);
#else
            if (!IsIgnored) 
            {            
                logger.Debug(Message); 
            }
#endif
        }

        /// <summary>
        /// Error log with ignore when no debug mode
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="IsIgnored"></param>
        public static void LogError(Exception ex, bool IsIgnored)
        {
            TraceInfos traceInfos = new TraceInfos(ex);
            string Message = string.Empty;

            if (IsIgnored)
            {
                Message = $"[Ignored] ";
            }
            
            if (!traceInfos.InitialCaller.IsNullOrEmpty())
            {
                Message += $"Error on {traceInfos.InitialCaller}()";
            }

            Message += $"|{traceInfos.FileName}|{traceInfos.LineNumber}";

#if DEBUG
            logger.Error(ex, $"{Message}");
#else
            if (!IsIgnored) 
            {
                logger.Error(ex, $"{Message}");
            }
#endif
        }

        /// <summary>
        /// Error log with ignore when no debug mode
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="IsIgnored"></param>
        /// <param name="Message"></param>
        public static void LogError(Exception ex, bool IsIgnored, string Message)
        {
            TraceInfos traceInfos = new TraceInfos(ex);
            
            if (IsIgnored)
            {
                Message = $"[Ignored] {Message}";
            }

            Message = $"{Message}|{traceInfos.FileName}|{traceInfos.LineNumber}";

#if DEBUG
            logger.Error(ex, $"{Message}");
#else
            if (!IsIgnored) 
            {
                logger.Error(ex, $"{Message}");
            }
#endif
        }
        #endregion
    }
}
