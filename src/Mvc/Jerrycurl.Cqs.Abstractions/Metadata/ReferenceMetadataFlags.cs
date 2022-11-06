﻿using System;

namespace Jerrycurl.Cqs.Metadata
{
    [Flags]
    public enum ReferenceMetadataFlags
    {
        None = 0,
        CandidateKey = 1,
        ForeignKey = 2,
        PrimaryKey = 4,
        Key = ForeignKey | CandidateKey | PrimaryKey,
    }
}
