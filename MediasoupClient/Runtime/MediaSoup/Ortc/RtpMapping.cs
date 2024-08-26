using System;
using System.Collections.Generic;

[Serializable]
public class RtpMapping
{
    public List<RtpMappingCodec> Codecs { get; set; }

    public List<RtpMappingEncoding> Encodings { get; set; }
}

[Serializable]
public class RtpMappingCodec
{
    public int PayloadType { get; set; }

    public int MappedPayloadType { get; set; }
}

[Serializable]
public class RtpMappingEncoding
{
    public uint? Ssrc { get; set; }

    public string? Rid { get; set; }

    public string? ScalabilityMode { get; set; }

    public uint MappedSsrc { get; set; }
}
