using System.Collections.Generic;
using Omnius.Collections;

namespace Amoeba.Service
{
    sealed class UnicastMetadataCollection : FilteredList<UnicastMetadata>
    {
        public UnicastMetadataCollection() : base() { }
        public UnicastMetadataCollection(int capacity) : base(capacity) { }
        public UnicastMetadataCollection(IEnumerable<UnicastMetadata> collections) : base(collections) { }

        protected override bool Filter(UnicastMetadata item)
        {
            return (item != null);
        }
    }
}
