using System;
using System.IO;
using System.Runtime.Serialization;
using Omnius.Base;
using Omnius.Serialization;

namespace Amoeba.Messages
{
    [DataContract(Name = nameof(Seed))]
    public sealed class Seed : ItemBase<Seed>, ISeed
    {
        private enum SerializeId
        {
            Name = 0,
            Length = 1,
            CreationTime = 2,
            Metadata = 3,
        }

        private string _name;
        private long _length;
        private DateTime _creationTime;
        private Metadata _metadata;

        public static readonly int MaxNameLength = 256;

        public Seed(string name, long length, DateTime creationTime, Metadata metadata)
        {
            this.Name = name;
            this.Length = length;
            this.CreationTime = creationTime;
            this.Metadata = metadata;
        }

        protected override void Initialize()
        {

        }

        protected override void ProtectedImport(Stream stream, BufferManager bufferManager, int depth)
        {
            using (var reader = new ItemStreamReader(stream, bufferManager))
            {
                while (reader.Available > 0)
                {
                    int id = (int)reader.GetUInt32();

                    if (id == (int)SerializeId.Name)
                    {
                        this.Name = reader.GetString();
                    }
                    else if (id == (int)SerializeId.Length)
                    {
                        this.Length = (long)reader.GetUInt64();
                    }
                    else if (id == (int)SerializeId.CreationTime)
                    {
                        this.CreationTime = reader.GetDateTime();
                    }
                    else if (id == (int)SerializeId.Metadata)
                    {
                        this.Metadata = Metadata.Import(reader.GetStream(), bufferManager);
                    }
                }
            }
        }

        protected override Stream Export(BufferManager bufferManager, int depth)
        {
            using (var writer = new ItemStreamWriter(bufferManager))
            {
                // Name
                if (this.Name != null)
                {
                    writer.Write((uint)SerializeId.Name);
                    writer.Write(this.Name);
                }
                // Length
                if (this.Length != 0)
                {
                    writer.Write((uint)SerializeId.Length);
                    writer.Write((ulong)this.Length);
                }
                // CreationTime
                if (this.CreationTime != DateTime.MinValue)
                {
                    writer.Write((uint)SerializeId.CreationTime);
                    writer.Write(this.CreationTime);
                }
                // Metadata
                if (this.Metadata != null)
                {
                    writer.Write((uint)SerializeId.Metadata);
                    writer.Write(this.Metadata.Export(bufferManager));
                }

                return writer.GetStream();
            }
        }

        public override int GetHashCode()
        {
            if (this.Metadata == null) return 0;
            else return this.Metadata.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || !(obj is Seed)) return false;

            return this.Equals((Seed)obj);
        }

        public override bool Equals(Seed other)
        {
            if ((object)other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;

            if (this.Name != other.Name
                || this.Length != other.Length
                || this.CreationTime != other.CreationTime
                || this.Metadata != other.Metadata)
            {
                return false;
            }

            return true;
        }

        public override string ToString()
        {
            return this.Name;
        }

        #region ISeed

        [DataMember(Name = nameof(Name))]
        public string Name
        {
            get
            {
                return _name;
            }
            private set
            {
                if (value != null && value.Length > Seed.MaxNameLength)
                {
                    throw new ArgumentException();
                }

                _name = value;
            }
        }

        [DataMember(Name = nameof(Length))]
        public long Length
        {
            get
            {
                return _length;
            }
            private set
            {
                _length = value;
            }
        }

        [DataMember(Name = nameof(CreationTime))]
        public DateTime CreationTime
        {
            get
            {
                return _creationTime;
            }
            private set
            {
                var utc = value.ToUniversalTime();
                _creationTime = utc.AddTicks(-(utc.Ticks % TimeSpan.TicksPerSecond));
            }
        }

        [DataMember(Name = nameof(Metadata))]
        public Metadata Metadata
        {
            get
            {
                return _metadata;
            }
            private set
            {
                _metadata = value;
            }
        }

        #endregion
    }
}
