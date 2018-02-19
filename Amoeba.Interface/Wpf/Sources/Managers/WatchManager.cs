using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using Amoeba.Messages;
using Amoeba.Service;
using Omnius.Base;
using Omnius.Wpf;

namespace Amoeba.Interface
{
    class WatchManager : ManagerBase
    {
        private ServiceManager _serviceManager;

        private DialogService _dialogService;

        private WatchTimer _checkUpdateTimer;
        private WatchTimer _checkDiskSpaceTimer;
        private WatchTimer _backupTimer;

        private readonly object _lockObject = new object();
        private volatile bool _isDisposed;

        public WatchManager(ServiceManager serviceManager, DialogService dialogService)
        {
            _serviceManager = serviceManager;
            _dialogService = dialogService;

            this.Setting_ChechUpdate();
            this.Setting_CheckDiskSpace();
            this.Setting_Backup();
        }

        private void Setting_ChechUpdate()
        {
            _checkUpdateTimer = new WatchTimer(() =>
            {
                try
                {
                    var updateInfo = SettingsManager.Instance.UpdateInfo;
                    if (!updateInfo.IsEnabled) return;

                    var store = _serviceManager.GetStore(updateInfo.Signature, CancellationToken.None).Result;
                    if (store == null) return;

                    var updateBox = store.Value.Boxes.FirstOrDefault(n => n.Name == "Update")?.Boxes.FirstOrDefault(n => n.Name == "Windows");
                    if (updateBox == null) return;

                    Seed targetSeed = null;

                    {
                        var map = new Dictionary<Seed, Version>();
                        var regex = new Regex(@"Amoeba.+?((\d*)\.(\d*)\.(\d*)).*?\.zip", RegexOptions.Compiled);

                        foreach (var seed in updateBox.Seeds)
                        {
                            var match = regex.Match(seed.Name);
                            if (!match.Success) continue;

                            var version = new Version(match.Groups[1].Value);
                            if (version <= AmoebaEnvironment.Version) continue;

                            map.Add(seed, version);
                        }

                        if (map.Count > 0)
                        {
                            var sortedList = map.ToList();
                            sortedList.Sort((x, y) => y.Value.CompareTo(x.Value));

                            targetSeed = sortedList.First().Key;
                        }
                    }

                    if (targetSeed == null) return;

                    string fullPath = Path.GetFullPath(Path.Combine(AmoebaEnvironment.Paths.UpdatePath, targetSeed.Name));
                    if (File.Exists(fullPath)) return;

                    var downloadItemInfo = new DownloadItemInfo(targetSeed, fullPath);
                    SettingsManager.Instance.DownloadItemInfos.Add(downloadItemInfo);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            });
            _checkUpdateTimer.Start(new TimeSpan(0, 0, 0), new TimeSpan(0, 3, 0));
        }

        private void Setting_CheckDiskSpace()
        {
            bool watchFlag = true;

            _checkDiskSpaceTimer = new WatchTimer(() =>
            {
                if (!watchFlag) return;

                try
                {
                    var paths = new List<string>();
                    paths.Add(AmoebaEnvironment.Config.Cache.BlocksPath);

                    bool flag = false;

                    foreach (string path in paths)
                    {
                        var drive = new DriveInfo(Path.GetFullPath(path));

                        if (drive.AvailableFreeSpace < NetworkConverter.FromSizeString("256MB"))
                        {
                            flag |= true;
                            break;
                        }
                    }

                    if (_serviceManager.Report.Core.Cache.FreeSpace < NetworkConverter.FromSizeString("10GB"))
                    {
                        flag |= true;
                    }

                    if (!flag)
                    {
                        if (_serviceManager.State == ManagerState.Stop)
                        {
                            _serviceManager.Start();
                            Log.Information("Start");
                        }
                    }
                    else
                    {
                        if (_serviceManager.State == ManagerState.Start)
                        {
                            _serviceManager.Stop();
                            Log.Information("Stop");

                            watchFlag = false;

                            App.Current.Dispatcher.InvokeAsync(() =>
                            {
                                if (_dialogService.ShowDialog(LanguagesManager.Instance.MainWindow_SpaceNotFound_Message,
                                    MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel) == MessageBoxResult.OK)
                                {
                                    watchFlag = true;
                                }
                            });
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            });
            _checkDiskSpaceTimer.Start(new TimeSpan(0, 0, 0), new TimeSpan(0, 3, 0));
        }

        private void Setting_Backup()
        {
            var sw = Stopwatch.StartNew();

            _backupTimer = new WatchTimer(() =>
            {
                if (sw.Elapsed.TotalMinutes > 30)
                {
                    sw.Restart();

                    try
                    {
                        Backup.Instance.Run();
                        this.GarbageCollect();
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                }
            });
            _backupTimer.Start(new TimeSpan(0, 0, 30));
        }

        private void GarbageCollect()
        {
            System.Runtime.GCSettings.LargeObjectHeapCompactionMode = System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect();
        }

        protected override void Dispose(bool isDisposing)
        {
            if (_isDisposed) return;
            _isDisposed = true;

            if (isDisposing)
            {
                _checkUpdateTimer.Stop();
                _checkUpdateTimer.Dispose();

                _checkDiskSpaceTimer.Stop();
                _checkDiskSpaceTimer.Dispose();

                _backupTimer.Stop();
                _backupTimer.Dispose();
            }
        }
    }
}
