using NPPE.Application.Common;
using Xunit;

namespace NPPE.Tests;

public class AnswerOptionOrderTests
{
    [Theory]
    // Normal answers → rank 0 (stay where they are).
    [InlineData("A licensed engineer must seal the drawings", 0)]
    [InlineData("Report the issue to the regulator", 0)]
    // Catch-all / meta answers → rank 1 (sort last), across the real variants.
    [InlineData("All of the above", 1)]
    [InlineData("All of the above.", 1)]
    [InlineData("all the above", 1)]
    [InlineData("All of the above, where applicable and subject to the duties", 1)]
    [InlineData("None of the above", 1)]
    [InlineData("None of the above.", 1)]
    [InlineData("None of these answers", 1)]
    [InlineData("None of these", 1)]
    [InlineData("None of these could potentially provide coverage", 1)]
    [InlineData("  none of the above  ", 1)]   // whitespace-insensitive
    public void MetaRank_flags_catch_all_options(string text, int expected)
        => Assert.Equal(expected, AnswerOptionOrder.MetaRank(text));

    [Fact]
    public void Ordering_pushes_meta_options_to_the_bottom_without_touching_normal_order()
    {
        // Labels A..D with a catch-all sitting at B in the source data.
        var options = new[]
        {
            (Label: 'A', Text: "Notify the client in writing"),
            (Label: 'B', Text: "All of the above"),
            (Label: 'C', Text: "Stop work until it is resolved"),
            (Label: 'D', Text: "Escalate to a senior engineer"),
        };

        var ordered = options
            .OrderBy(o => AnswerOptionOrder.MetaRank(o.Text))
            .ThenBy(o => o.Label)
            .Select(o => o.Label)
            .ToArray();

        // Non-meta options keep their A/C/D order; the catch-all moves to the end.
        Assert.Equal(new[] { 'A', 'C', 'D', 'B' }, ordered);
    }
}
