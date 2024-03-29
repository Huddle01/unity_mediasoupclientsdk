using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mediasoup.RtpParameter;
using Unity.WebRTC;
using Utilme.SdpTransform;
using System;
using System.Linq;

public class UnifiedPlanUtils
{
    public static List<RtpEncodingParameters> GetRtpEncodingParameters(MediaDescription offerMediaObject)
    {
        List<uint> ssrcs = new List<uint>();

        if (offerMediaObject.Attributes.Ssrcs!=null && offerMediaObject.Attributes.Ssrcs.Count > 0) 
        {
            foreach (var line in offerMediaObject.Attributes.Ssrcs) 
            {
                ssrcs.Add(line.Id);
            }
        }

        if (ssrcs.Count==0) 
        {
            throw new Exception("no a=ssrc lines found");
        }

        Dictionary<uint, uint> ssrcToRtxSsrc = new Dictionary<uint, uint>();

        foreach (var line in offerMediaObject.Attributes.SsrcGroups ?? Enumerable.Empty<SsrcGroup>())
        {
            if (line.Semantics != "FID")
            {
                continue;
            }

            var ssrcRtxSsrc = line.SsrcIds.Select(uint.Parse).ToList();

            if (ssrcRtxSsrc.Count == 2 && ssrcs.Contains(ssrcRtxSsrc[0]))
            {
                // Remove both the SSRC and RTX SSRC from the set so later we know
                // that they are already handled.
                ssrcs.Remove(ssrcRtxSsrc[0]);
                ssrcs.Remove(ssrcRtxSsrc[1]);

                // Add to the dictionary.
                ssrcToRtxSsrc.Add(ssrcRtxSsrc[0], ssrcRtxSsrc[1]);
            }
        }

        foreach (var ssrc in ssrcs)
        {
            if (ssrcToRtxSsrc.ContainsKey(ssrc)) ssrcToRtxSsrc.Remove(ssrc);
            ssrcToRtxSsrc.Add(ssrc, uint.MaxValue);
        }

        List<RtpEncodingParameters> encodings = new List<RtpEncodingParameters>();

        foreach (var kvp in ssrcToRtxSsrc)
        {
            var ssrc = kvp.Key;
            var rtxSsrc = kvp.Value;

            RtpEncodingParameters encoding = new RtpEncodingParameters { Ssrc = ssrc };

            if (rtxSsrc != uint.MaxValue)
            {
                encoding.Rtx.Ssrc = rtxSsrc;
            }

            encodings.Add(encoding);
        }

        return encodings;


    }

    public void AddLegacySimulcast(MediaDescription offerMediaObject, int numStreams)
    {
        if (numStreams <= 1)
        {
            throw new InvalidCastException("numStreams must be greater than 1");
        }

        // Get the SSRC.
        Ssrc ssrcMsidLine = (offerMediaObject.Attributes.Ssrcs ?? Enumerable.Empty<Ssrc>())
            .FirstOrDefault(line => line.Attribute == "msid");

        if (ssrcMsidLine == null)
        {
            throw new Exception("a=ssrc line with msid information not found");
        }


        var msidValues = ssrcMsidLine.Value.Split(' ');
        var streamId = msidValues[0];
        var trackId = msidValues[1];
        int firstSsrc = (int)ssrcMsidLine.Id;
        int? firstRtxSsrc = null;

        // Get the SSRC for RTX.
        var fidLine = (offerMediaObject.Attributes.SsrcGroups ?? Enumerable.Empty<SsrcGroup>())
            .FirstOrDefault(line => line.Semantics == "FID" && line.SsrcIds.Select(int.Parse).First() == firstSsrc);

        if (fidLine != null)
        {
            var ssrcs1 = fidLine.SsrcIds.Select(int.Parse).ToList();
            firstRtxSsrc = ssrcs1.Count > 1 ? ssrcs1[1] : (int?)null;
        }

        var ssrcCnameLine = (offerMediaObject.Attributes.Ssrcs ?? Enumerable.Empty<Ssrc>())
            .FirstOrDefault(line => line.Attribute == "cname");

        if (ssrcCnameLine == null)
        {
            throw new Exception("a=ssrc line with cname information not found");
        }

        var cname = ssrcCnameLine.Value;
        var ssrcs = new List<int>();
        var rtxSsrcs = new List<int>();

        for (int i = 0; i < numStreams; ++i)
        {
            ssrcs.Add(firstSsrc + i);

            if (firstRtxSsrc.HasValue)
            {
                rtxSsrcs.Add(firstRtxSsrc.Value + i);
            }
        }

        offerMediaObject.Attributes.SsrcGroups = new List<SsrcGroup>();
        offerMediaObject.Attributes.Ssrcs = new List<Ssrc>();

        offerMediaObject.Attributes.SsrcGroups.Add(new SsrcGroup
        {
            Semantics = "SIM",
            SsrcIds = ssrcs.Select(x => x.ToString()).ToArray(),
        });

        for (int i = 0; i < ssrcs.Count; ++i)
        {
            var ssrc = ssrcs[i];

            offerMediaObject.Attributes.Ssrcs.Add(new Ssrc
            {
                Id = (uint)ssrc,
                Attribute = "cname",
                Value = cname,
            });

            offerMediaObject.Attributes.Ssrcs.Add(new Ssrc
            {
                Id = (uint)ssrc,
                Attribute = "msid",
                Value = $"{streamId} {trackId}",
            });
        }

        for (int i = 0; i < rtxSsrcs.Count; ++i)
        {
            var ssrc = ssrcs[i];
            var rtxSsrc = rtxSsrcs[i];

            offerMediaObject.Attributes.Ssrcs.Add(new Ssrc
            {
                Id = (uint)rtxSsrc,
                Attribute = "cname",
                Value = cname,
            });

            offerMediaObject.Attributes.Ssrcs.Add(new Ssrc
            {
                Id = (uint)rtxSsrc,
                Attribute = "msid",
                Value = $"{streamId} {trackId}",
            });

            offerMediaObject.Attributes.SsrcGroups.Add(new SsrcGroup
            {
                Semantics = "FID",
                SsrcIds = new string[] { ssrc.ToString(), rtxSsrc.ToString() },
            });

        }

    }

}
