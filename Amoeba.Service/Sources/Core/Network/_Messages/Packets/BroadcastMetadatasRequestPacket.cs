using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Omnius.Base;
using Omnius.Security;
using Omnius.Serialization;

namespace Amoeba.Service
{
    partial class NetworkManager
    {
        sealed class BroadcastMetadatasRequestPacket : ItemBase<BroadcastMetadatasRequestPacket>
        {
            private enum SerializeId
            {
                Signatures = 0,
            }

            private SignatureCollection _signatures;

            public BroadcastMetadatasRequestPacket(IEnumerable<Signature> signatures)
            {
                if (signatures != null) this.ProtectedSignatures.AddRange(signatures);
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

                        if (id == (int)SerializeId.Signatures)
                        {
                            for (int i = (int)reader.GetUInt32() - 1; i >= 0; i--)
                            {
                                this.ProtectedSignatures.Add(Signature.Import(reader.GetStream(), bufferManager));
                            }
                        }
                    }
                }
            }

            protected override Stream Export(BufferManager bufferManager, int depth)
            {
                using (var writer = new ItemStreamWriter(bufferManager))
                {
                    // Signatures
                    if (this.ProtectedSignatures.Count > 0)
                    {
                        writer.Write((uint)SerializeId.Signatures);
                        writer.Write((uint)this.ProtectedSignatures.Count);

                        foreach (var item in this.ProtectedSignatures)
                        {
                            writer.Write(item.Export(bufferManager));
                        }
                    }

                    return writer.GetStream();
                }
            }

            private volatile ReadOnlyCollection<Signature> _readOnlySignatures;

            public IEnumerable<Signature> Signatures
            {
                get
                {
                    if (_readOnlySignatures == null)
                        _readOnlySignatures = new ReadOnlyCollection<Signature>(this.ProtectedSignatures);

                    return _readOnlySignatures;
                }
            }

            private SignatureCollection ProtectedSignatures
            {
                get
                {
                    if (_signatures == null)
                        _signatures = new SignatureCollection();

                    return _signatures;
                }
            }
        }
    }
}
