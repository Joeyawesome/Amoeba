﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Amoeba.Core;
using Omnius.Base;
using Omnius.Collections;
using Omnius.Configuration;
using Omnius.Security;
using Omnius.Utilities;
using Omnius.Serialization;
using System.Collections.Concurrent;

namespace Amoeba.Service
{
    class MessageDownloadManager : StateManagerBase, ISettings
    {
        private BufferManager _bufferManager;
        private CoreManager _coreManager;

        private Settings _settings;

        private HashSet<Signature> _searchSignatures = new HashSet<Signature>();

        private Dictionary<Type, VolatileHashDictionary<Metadata, object>> _cachedMessages;

        private WatchTimer _watchTimer;

        private Random _random = new Random();

        private ManagerState _state = ManagerState.Stop;

        private readonly object _thisLock = new object();
        private volatile bool _disposed;

        private const int _maxMessageSize = 1024 * 1024 * 256;

        public MessageDownloadManager(string configPath, CoreManager coreManager, BufferManager bufferManager)
        {
            _bufferManager = bufferManager;
            _coreManager = coreManager;

            _settings = new Settings(configPath);

            _cachedMessages = new Dictionary<Type, VolatileHashDictionary<Metadata, object>>();

            _watchTimer = new WatchTimer(this.WatchTimer, new TimeSpan(0, 0, 30));
        }

        private void WatchTimer()
        {
            lock (_thisLock)
            {
                foreach (var dic in _cachedMessages.Values)
                {
                    dic.Update();
                }
            }
        }

        public IEnumerable<Signature> SearchSignatures
        {
            get
            {
                lock (_thisLock)
                {
                    return _searchSignatures.ToArray();
                }
            }
        }

        public void SetSearchSignatures(IEnumerable<Signature> signatures)
        {
            lock (_thisLock)
            {
                _searchSignatures.Clear();
                _searchSignatures.UnionWith(signatures);
            }
        }

        private Task<BroadcastMessage<T>> GetBroadcastMessage<T>(int version, Signature signature)
            where T : ItemBase<T>
        {
            if (signature == null) throw new ArgumentNullException(nameof(signature));

            var type = typeof(T);

            lock (_thisLock)
            {
                var broadcastMetadata = _coreManager.GetBroadcastMetadata(signature, type.Name);
                if (broadcastMetadata == null) return Task.FromResult<BroadcastMessage<T>>(null);

                var dic = _cachedMessages.GetOrAdd(type, (_) => new VolatileHashDictionary<Metadata, object>(new TimeSpan(0, 30, 0)));

                // Cache
                {
                    object cachedResult;

                    if (dic.TryGetValue(broadcastMetadata.Metadata, out cachedResult))
                    {
                        return Task.FromResult((BroadcastMessage<T>)cachedResult);
                    }
                }

                var task = _coreManager.GetStream(broadcastMetadata.Metadata, _maxMessageSize)
                    .ContinueWith((t) =>
                    {
                        var stream = t.Result;
                        if (stream == null) return null;

                        var result = new BroadcastMessage<T>(broadcastMetadata.Certificate.GetSignature(),
                            broadcastMetadata.CreationTime, ContentConverter.FromStream<T>(version, stream));

                        dic[broadcastMetadata.Metadata] = result;

                        return result;
                    });

                return task;
            }
        }

        private Task<IEnumerable<T>> GetUnicastMessages<T>(int version, Signature signature, IExchangeDecrypt exchangePrivateKey, int limit)
            where T : ItemBase<T>
        {
            if (signature == null) throw new ArgumentNullException(nameof(signature));
            if (exchangePrivateKey == null) throw new ArgumentNullException(nameof(exchangePrivateKey));

            var type = typeof(T);

            return Task.Run(() =>
            {
                VolatileHashDictionary<Metadata, object> dic;

                lock (_thisLock)
                {
                    dic = _cachedMessages.GetOrAdd(type, (_) => new VolatileHashDictionary<Metadata, object>(new TimeSpan(0, 30, 0)));
                }

                foreach (var unicastMetadata in _coreManager.GetUnicastMetadatas(signature, type.Name))
                {
                    if (!_searchSignatures.Contains(unicastMetadata.Certificate.GetSignature()) && unicastMetadata.Cost.Value < limit) continue;

                    // Cache
                    {
                        object cachedResult;

                        if (dic.TryGetValue(broadcastMetadata.Metadata, out cachedResult))
                        {
                            return Task.FromResult((T)cachedResult);
                        }
                    }
                }

                var task = _coreManager.GetStream(broadcastMetadata.Metadata, _maxMessageSize)
                    .ContinueWith((t) =>
                    {
                        var stream = t.Result;
                        if (stream == null) return null;

                        var result = ContentConverter.FromStream<T>(version, stream);
                        dic[broadcastMetadata.Metadata] = result;

                        return result;
                    });

                return task;
            });
        }

        public Task<Profile> GetProfile(Signature signature)
        {
            lock (_thisLock)
            {
                return this.GetBroadcastMessage<Profile>(0, signature);
            }
        }

        public Task<Store> GetStore(Signature signature)
        {
            lock (_thisLock)
            {
                return this.GetBroadcastMessage<Store>(0, signature);
            }
        }

        public Task<MailMessage> GetMail(Signature signature)
        {
            lock (_thisLock)
            {
                return this.GetBroadcastMessage<MailMessage>(0, signature);
            }
        }

        public override ManagerState State
        {
            get
            {
                if (_disposed) throw new ObjectDisposedException(this.GetType().FullName);

                return _state;
            }
        }

        private readonly object _stateLock = new object();

        public override void Start()
        {
            if (_disposed) throw new ObjectDisposedException(this.GetType().FullName);

            lock (_stateLock)
            {
                lock (_thisLock)
                {
                    if (this.State == ManagerState.Start) return;
                    _state = ManagerState.Start;
                }
            }
        }

        public override void Stop()
        {
            if (_disposed) throw new ObjectDisposedException(this.GetType().FullName);

            lock (_stateLock)
            {
                lock (_thisLock)
                {
                    if (this.State == ManagerState.Stop) return;
                    _state = ManagerState.Stop;
                }
            }
        }

        #region ISettings

        public void Load()
        {
            lock (_thisLock)
            {
                int version = _settings.Load("Version", () => 0);

                this.SetSearchSignatures(_settings.Load("SearchSignatures", () => new Signature[0]));
            }
        }

        public void Save()
        {
            lock (_thisLock)
            {
                _settings.Save("Version", 0);

                _settings.Save("SearchSignatures", this.SearchSignatures);
            }
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {

            }
        }
    }

    [Serializable]
    class MessageDownloadManagerException : ManagerException
    {
        public MessageDownloadManagerException() : base() { }
        public MessageDownloadManagerException(string message) : base(message) { }
        public MessageDownloadManagerException(string message, Exception innerException) : base(message, innerException) { }
    }
}