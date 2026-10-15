namespace JobTracker.Core.Enums;

/// <summary>The kind of interview being scheduled.</summary>
public enum InterviewType
{
    /// <summary>An interview that does not fit another category.</summary>
    Other = 0,
    /// <summary>An initial screening conversation.</summary>
    Screening = 1,
    /// <summary>An interview assessing technical skills.</summary>
    Technical = 2,
    /// <summary>An interview assessing experience and behaviours.</summary>
    Behavioural = 3,
    /// <summary>An assessment-centre session.</summary>
    AssessmentCentre = 4,
    /// <summary>A final-round interview.</summary>
    FinalRound = 5
}
