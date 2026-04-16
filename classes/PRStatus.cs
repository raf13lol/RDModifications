namespace RDModifications;

public enum PRStatus : sbyte
{
    Unknown = sbyte.MinValue,
    NonRefereed = -1,
    Pending = 0,
    Mixed = 3,
    PeerReviewed = 10,

    UN = Unknown,
    NR = NonRefereed,
    PD = Pending,
    MX = Mixed,
    PR = PeerReviewed,
}