using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using GzipS3Client;
using GzipS3Client.Configuration;
using GzipS3Client.Extensions;
using Nancy.Authentication.Token.Storage;

namespace Nancy.Authentication.Token.S3Storage
{
    public class S3TokenKeyStorage : ITokenKeyStore
    {
        private readonly string _keyName;
        private static readonly object SyncLock = new object();
        private readonly AmazonStorageService _amazonStorageService;
        private readonly BinaryFormatter _binaryFormatter;

        public S3TokenKeyStorage(string keyName)
        {
            _keyName = keyName;
            _binaryFormatter = new BinaryFormatter();
            _amazonStorageService = new AmazonStorageService(new AppSettingsAmazonS3Configuration());
        }

        public IDictionary<DateTime, byte[]> Retrieve()
        {
            lock (SyncLock)
            {
                if (!_amazonStorageService.ContainsFile(_keyName))
                {
                    return new Dictionary<DateTime, byte[]>();
                }

                var fileContent = _amazonStorageService.Get(_keyName);

                using (var stream = new MemoryStream(fileContent.Content))
                {
                    return (Dictionary<DateTime, byte[]>)_binaryFormatter.Deserialize(stream);
                }
            }
        }

        public void Store(IDictionary<DateTime, byte[]> keys)
        {
            lock (SyncLock)
            {
                var keyChain = new Dictionary<DateTime, byte[]>(keys);

                using (var stream = new MemoryStream())
                {
                    _binaryFormatter.Serialize(stream, keyChain);
                    var content = new FileContent(_keyName, stream.ReadBytes());
                    _amazonStorageService.Save(content);
                }
            }
        }

        public void Purge()
        {
            lock (SyncLock)
            {
                if (_amazonStorageService.ContainsFile(_keyName))
                {
                    _amazonStorageService.Delete(_keyName);
                }
            }
        }
    }
}