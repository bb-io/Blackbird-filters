namespace Blackbird.Filters.Shared;

/// <summary>
/// Expresses results of localization quality assessment in the form of aggregated ratings, either as scores or as voting results.
/// </summary>
/// <see cref="https://docs.oasis-open.org/xliff/xliff-core/v2.2/csd02/xliff-extended-v2.2-csd02-part2.html#Localization_Quality_Rating"/>
/// <param name="Score">A decimal number between 0.0 and 100.0. The higher the number the better the quality rating.</param>
/// <param name="ScoreThreshold">A decimal number between 0.0 and 100.0. Scores under the given threshold indicate a quality check fail.</param>
/// <param name="Votes">An integer number. This attribute provides the quality rating voting (crowd assessment) of target text, the higher the number the more positive votes or the better margin of positive votes over negative votes.</param>
/// <param name="VoteThreshold">An integer number. This attribute provides the minimum passing vote threshold. Votes under the given threshold indicate a quality check fail.</param>
/// <param name="ProfileReference">An Internationalized Resource Identifier (IRI). This attribute references a quality assessment model that has been used for the rating (either scoring or voting).</param>
public class Quality
{
    public double? Score { get; set; }
    public double? ScoreThreshold { get; set; }
    public int? Votes { get; set; }
    public int? VoteThreshold { get; set; }
    public string? ProfileReference { get; set; }

    internal bool IsEmpty()
    {
        return !Score.HasValue && !ScoreThreshold.HasValue && !Votes.HasValue && !VoteThreshold.HasValue && ProfileReference is null;
    }
}
