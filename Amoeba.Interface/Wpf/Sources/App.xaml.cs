using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Ionic.Zip;
using Omnius.Base;
using Omnius.Security;
using Omnius.Wpf;

namespace Amoeba.Interface
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private Mutex _mutex;

        private List<Process> _processList = new List<Process>();

        public App()
        {
            Process.GetCurrentProcess().SetMemoryPriority(5);

            NativeMethods.SetThreadExecutionState(NativeMethods.ExecutionState.Continuous);

            CryptoConfig.AddAlgorithm(typeof(SHA256Cng),
                "SHA256",
                "SHA256Cng",
                "System.Security.Cryptography.SHA256",
                "System.Security.Cryptography.SHA256Cng");

            Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));

            Thread.GetDomain().UnhandledException += this.App_UnhandledException;

            this.Setting_Log();
        }

        private void App_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            if (exception == null) return;

            Log.Error(exception);
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Error(e.Exception);
        }

        private void Setting_Log()
        {
            string logPath = null;
            bool isHeaderWrite = true;

            for (int i = 0; i < 1024; i++)
            {
                if (i == 0)
                {
                    logPath = Path.Combine(AmoebaEnvironment.Paths.LogPath, string.Format("{0}.txt", DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", System.Globalization.DateTimeFormatInfo.InvariantInfo)));
                }
                else
                {
                    logPath = Path.Combine(AmoebaEnvironment.Paths.LogPath, string.Format("{0}.({1}).txt", DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", System.Globalization.DateTimeFormatInfo.InvariantInfo), i));
                }

                if (!File.Exists(logPath)) break;
            }

            if (logPath == null) return;

            Log.MessageEvent += (sender, e) =>
            {
                if (e.Level == LogMessageLevel.Information) return;
#if !DEBUG
                if (e.Level == LogMessageLevel.Debug) return;
#endif

                lock (logPath)
                {
                    try
                    {
                        using (var writer = new StreamWriter(logPath, true, new UTF8Encoding(false)))
                        {
                            if (isHeaderWrite)
                            {
                                writer.WriteLine(this.GetMachineInfomation());
                                isHeaderWrite = false;
                            }

                            writer.WriteLine(MessageToString(e));
                            writer.Flush();
                        }
                    }
                    catch (Exception)
                    {

                    }
                }
            };

            Log.ExceptionEvent += (sender, e) =>
            {
                if (e.Level == LogMessageLevel.Information) return;
#if !DEBUG
                if (e.Level == LogMessageLevel.Debug) return;
#endif

                lock (logPath)
                {
                    try
                    {
                        using (var writer = new StreamWriter(logPath, true, new UTF8Encoding(false)))
                        {
                            if (isHeaderWrite)
                            {
                                writer.WriteLine(this.GetMachineInfomation());
                                isHeaderWrite = false;
                            }

                            writer.WriteLine(ExceptionToString(e));
                            writer.Flush();
                        }
                    }
                    catch (Exception)
                    {

                    }
                }
            };

            string MessageToString(LogMessageEventArgs e)
            {
                var sb = new StringBuilder();
                sb.AppendLine();
                sb.AppendLine("--------------------------------------------------------------------------------");
                sb.AppendLine();
                sb.AppendLine(string.Format("Time:\t\t{0}", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")));
                sb.AppendLine(string.Format("Level:\t\t{0}", e.Level));
                sb.AppendLine(string.Format("Message:\t\t{0}", e.Message));

                return sb.ToString();
            }

            string ExceptionToString(LogExceptionEventArgs e)
            {
                var sb = new StringBuilder();
                sb.AppendLine();
                sb.AppendLine("--------------------------------------------------------------------------------");
                sb.AppendLine();
                sb.AppendLine(string.Format("Time:\t\t{0}", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")));
                sb.AppendLine(string.Format("Level:\t\t{0}", e.Level));

                Exception exception = e.Exception;

                while (exception != null)
                {
                    sb.AppendLine(string.Format("Exception:\t\t{0}", exception.GetType().ToString()));
                    if (!string.IsNullOrWhiteSpace(exception.Message)) sb.AppendLine(string.Format("Message:\t\t{0}", exception.Message));
                    if (!string.IsNullOrWhiteSpace(exception.StackTrace)) sb.AppendLine(string.Format("StackTrace:\t\t{0}", exception.StackTrace));

                    exception = exception.InnerException;

                    if (exception != null)
                    {
                        sb.AppendLine();
                    }
                }

                return sb.ToString();
            }
        }

        private string GetMachineInfomation()
        {
            var osInfo = Environment.OSVersion;
            string osName = "";

            if (osInfo.Platform == PlatformID.Win32NT)
            {
                if (osInfo.Version.Major == 4)
                {
                    osName = "Windows NT 4.0";
                }
                else if (osInfo.Version.Major == 5)
                {
                    switch (osInfo.Version.Minor)
                    {
                        case 0:
                            osName = "Windows 2000";
                            break;

                        case 1:
                            osName = "Windows XP";
                            break;

                        case 2:
                            osName = "Windows Server 2003";
                            break;
                    }
                }
                else if (osInfo.Version.Major == 6)
                {
                    switch (osInfo.Version.Minor)
                    {
                        case 0:
                            osName = "Windows Vista";
                            break;

                        case 1:
                            osName = "Windows 7";
                            break;

                        case 2:
                            osName = "Windows 8";
                            break;

                        case 3:
                            osName = "Windows 8.1";
                            break;
                    }
                }
                else if (osInfo.Version.Major == 10)
                {
                    osName = "Windows 10";
                }
            }
            else if (osInfo.Platform == PlatformID.WinCE)
            {
                osName = "Windows CE";
            }
            else if (osInfo.Platform == PlatformID.MacOSX)
            {
                osName = "MacOSX";
            }
            else if (osInfo.Platform == PlatformID.Unix)
            {
                osName = "Unix";
            }

            return string.Format(
                "Amoeba:\t\t{0}\r\n" +
                "OS:\t\t{1} ({2})\r\n" +
                ".NET Framework:\t{3}", AmoebaEnvironment.Version.ToString(3), osName, osInfo.VersionString, Environment.Version);
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                string sessionId = NetworkConverter.ToHexString(Sha256.Compute(Path.GetFullPath(Assembly.GetEntryAssembly().Location)));

                // ���d�N���h�~
                {
                    _mutex = new Mutex(false, sessionId);

                    if (!_mutex.WaitOne(0))
                    {
                        this.Shutdown();

                        return;
                    }
                }

                // �A�b�v�f�[�g
                {
                    // �ꎞ�I�ɍ쐬���ꂽ"Amoeba.Update.exe"���폜����B
                    {
                        string tempUpdateExeFilePath = Path.Combine(AmoebaEnvironment.Paths.WorkPath, "Amoeba.Update.exe");

                        if (File.Exists(tempUpdateExeFilePath))
                        {
                            File.Delete(tempUpdateExeFilePath);
                        }
                    }

                    if (Directory.Exists(AmoebaEnvironment.Paths.UpdatePath))
                    {
                        string zipFilePath = null;

                        // �ŐV�̃o�[�W������zip�������B
                        {
                            var map = new Dictionary<string, Version>();
                            var regex = new Regex(@"Amoeba.+?((\d*)\.(\d*)\.(\d*)).*?\.zip", RegexOptions.Compiled);

                            foreach (string path in Directory.GetFiles(AmoebaEnvironment.Paths.UpdatePath))
                            {
                                var match = regex.Match(Path.GetFileName(path));
                                if (!match.Success) continue;

                                var version = new Version(match.Groups[1].Value);
                                if (version < AmoebaEnvironment.Version) continue;

                                map.Add(path, version);
                            }

                            if (map.Count > 0)
                            {
                                var sortedList = map.ToList();
                                sortedList.Sort((x, y) => y.Value.CompareTo(x.Value));

                                zipFilePath = sortedList.First().Key;
                            }
                        }

                        if (zipFilePath != null)
                        {
                            string tempUpdateDirectoryPath = Path.Combine(AmoebaEnvironment.Paths.WorkPath, "Update");

                            if (Directory.Exists(tempUpdateDirectoryPath))
                            {
                                Directory.Delete(tempUpdateDirectoryPath, true);
                            }

                            using (var zipfile = new ZipFile(zipFilePath))
                            {
                                zipfile.ExtractExistingFile = ExtractExistingFileAction.OverwriteSilently;
                                zipfile.ExtractAll(tempUpdateDirectoryPath);
                            }

                            if (File.Exists(zipFilePath))
                            {
                                File.Delete(zipFilePath);
                            }

                            string tempUpdateExeFilePath = Path.Combine(AmoebaEnvironment.Paths.WorkPath, "Amoeba.Update.exe");

                            File.Copy("Amoeba.Update.exe", tempUpdateExeFilePath);

                            var startInfo = new ProcessStartInfo();
                            startInfo.FileName = Path.GetFullPath(tempUpdateExeFilePath);
                            startInfo.Arguments = string.Format("\"{0}\" \"{1}\" \"{2}\" \"{3}\"",
                                sessionId,
                                Path.Combine(tempUpdateDirectoryPath, "Core"),
                                Directory.GetCurrentDirectory(),
                                Path.Combine(Directory.GetCurrentDirectory(), "Amoeba.Interface.exe"));
                            startInfo.WorkingDirectory = Path.GetFullPath(Path.GetDirectoryName(tempUpdateExeFilePath));

                            Process.Start(startInfo);

                            this.Shutdown();

                            return;
                        }
                    }
                }

                // ����̃t�H���_���쐬����B
                {
                    foreach (var propertyInfo in typeof(AmoebaEnvironment.EnvironmentPaths).GetProperties())
                    {
                        string path = propertyInfo.GetValue(AmoebaEnvironment.Paths) as string;
                        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                    }
                }

                // Temp�t�H���_�����ϐ��ɓo�^�B
                {
                    // Temp�t�H���_����|���B
                    try
                    {
                        foreach (string path in Directory.GetFiles(AmoebaEnvironment.Paths.TempPath, "*", SearchOption.AllDirectories))
                        {
                            File.Delete(path);
                        }

                        foreach (string path in Directory.GetDirectories(AmoebaEnvironment.Paths.TempPath, "*", SearchOption.AllDirectories))
                        {
                            Directory.Delete(path, true);
                        }
                    }
                    catch (Exception)
                    {

                    }

                    Environment.SetEnvironmentVariable("TMP", Path.GetFullPath(AmoebaEnvironment.Paths.TempPath), EnvironmentVariableTarget.Process);
                    Environment.SetEnvironmentVariable("TEMP", Path.GetFullPath(AmoebaEnvironment.Paths.TempPath), EnvironmentVariableTarget.Process);
                }

                // �A�b�v�O���[�h�����B
                {
                    if (AmoebaEnvironment.Config.Version <= new Version(5, 0, 60))
                    {
                        var basePath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, @"Service\Core\Cache");
                        Directory.CreateDirectory(Path.Combine(basePath, "Blocks"));

                        var renameList = new List<(string oldPath, string newPath)>();
                        renameList.Add((@"CacheInfos.json.gz", @"ContentInfos.json.gz"));
                        renameList.Add((@"Size.json.gz", @"Blocks\Size.json.gz"));
                        renameList.Add((@"ClusterIndex.json.gz", @"Blocks\ClusterIndex.json.gz"));

                        foreach (var (oldPath, newPath) in renameList)
                        {
                            File.Copy(Path.Combine(basePath, oldPath), Path.Combine(basePath, newPath));
                        }
                    }
                }

                this.StartupUri = new Uri("Mvvm/Windows/Main/MainWindow.xaml", UriKind.Relative);
            }
            catch (Exception ex)
            {
                Log.Error(ex);

                this.Shutdown();

                return;
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            Parallel.ForEach(_processList, new ParallelOptions() { MaxDegreeOfParallelism = 8 }, process =>
            {
                try
                {
                    process.Kill();
                    process.WaitForExit();
                }
                catch (Exception)
                {

                }
            });
        }

        static class NativeMethods
        {
            [Flags]
            public enum ExecutionState : uint
            {
                Null = 0,
                SystemRequired = 1,
                DisplayRequired = 2,
                Continuous = 0x80000000,
            }

            [DllImport("kernel32.dll")]
            public extern static ExecutionState SetThreadExecutionState(ExecutionState esFlags);
        }
    }
}
