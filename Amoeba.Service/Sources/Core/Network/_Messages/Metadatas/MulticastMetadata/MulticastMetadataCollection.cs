using System.Collections.Generic;
using Omnius.Collections;

namespace Amoeba.Service
{
    sealed class MulticastMetadataCollection : FilteredList<MulticastMetadata>
    {
        public MulticastMetadataCollection() : base() { }
        public MulticastMetadataCollection(int capacity) : base(capacity) { }
        public MulticastMetadataCollection(IEnumerable<MulticastMetadata> collections) : base(collections) { }

        protected override bool Filter(MulticastMetadata item)
        {
            return (item != null);
        }
    }
}
