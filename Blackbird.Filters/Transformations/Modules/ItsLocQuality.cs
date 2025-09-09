using System.Xml.Linq;

namespace Blackbird.Filters.Transformations.Modules;

/// <summary>
/// Expresses results of localization quality assessment in the form of aggregated ratings, either as scores or as voting results.
/// </summary>
/// <see cref="https://docs.oasis-open.org/xliff/xliff-core/v2.2/csd02/xliff-extended-v2.2-csd02-part2.html#Localization_Quality_Rating"/>
/// <param name="LocQualityRatingScore">A decimal number between 0.0 and 100.0. The higher the number the better the quality rating.</param>
/// <param name="LocQualityRatingScoreThreshold">A decimal number between 0.0 and 100.0. Scores under the given threshold indicate a quality check fail.</param>
/// <param name="LocQualityRatingVote">An integer number. This attribute provides the quality rating voting (crowd assessment) of target text, the higher the number the more positive votes or the better margin of positive votes over negative votes.</param>
/// <param name="LocQualityRatingVoteThreshold">An integer number. This attribute provides the minimum passing vote threshold. Votes under the given threshold indicate a quality check fail.</param>
/// <param name="LocQualityRatingProfileRef">An Internationalized Resource Identifier (IRI). This attribute references a quality assessment model that has been used for the rating (either scoring or voting).</param>
public sealed record class ItsLocQuality(
    double? LocQualityRatingScore,
    double? LocQualityRatingScoreThreshold,
    int? LocQualityRatingVote,
    int? LocQualityRatingVoteThreshold,
    string? LocQualityRatingProfileRef
)
{
    public static readonly XNamespace ItsXNamespace = "http://www.w3.org/2005/11/its";
}
