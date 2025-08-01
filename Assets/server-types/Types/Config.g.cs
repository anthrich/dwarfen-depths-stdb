// THIS FILE IS AUTOMATICALLY GENERATED BY SPACETIMEDB. EDITS TO THIS FILE
// WILL NOT BE SAVED. MODIFY TABLES IN YOUR MODULE SOURCE CODE INSTEAD.

// This was generated using spacetimedb cli version 1.2.0 (commit fb41e50eb73573b70eea532aeb6158eaac06fae0).

#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SpacetimeDB.Types
{
    [SpacetimeDB.Type]
    [DataContract]
    public sealed partial class Config
    {
        [DataMember(Name = "Id")]
        public uint Id;
        [DataMember(Name = "WorldSize")]
        public ulong WorldSize;
        [DataMember(Name = "UpdateEntityInterval")]
        public float UpdateEntityInterval;

        public Config(
            uint Id,
            ulong WorldSize,
            float UpdateEntityInterval
        )
        {
            this.Id = Id;
            this.WorldSize = WorldSize;
            this.UpdateEntityInterval = UpdateEntityInterval;
        }

        public Config()
        {
        }
    }
}
