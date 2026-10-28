namespace Vestigium.Helpers.Network;

public sealed record InferenceEvidence(
    InferenceEvidenceKind Kind,
    string Detail,
    int? Port = null);
