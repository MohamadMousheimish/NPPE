using NPPE.Application.Common;
using NPPE.Domain.Entities;
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

    [Fact]
    public void DisplayLabel_renumbers_sequentially_with_the_catch_all_shown_last()
    {
        // Stored labels: the catch-all sits at 'B' in the source data.
        var notify  = new AnswerOption { Id = Guid.NewGuid(), Text = "Notify the client in writing", Label = 'A' };
        var allAbove = new AnswerOption { Id = Guid.NewGuid(), Text = "All of the above", Label = 'B', IsCorrect = true };
        var stop    = new AnswerOption { Id = Guid.NewGuid(), Text = "Stop work until it is resolved", Label = 'C' };
        var escalate = new AnswerOption { Id = Guid.NewGuid(), Text = "Escalate to a senior engineer", Label = 'D' };
        var options = new[] { notify, allAbove, stop, escalate };

        // Display order is A, C, D, then the catch-all — re-lettered A, B, C, D.
        Assert.Equal('A', AnswerOptionOrder.DisplayLabel(options, notify));
        Assert.Equal('B', AnswerOptionOrder.DisplayLabel(options, stop));
        Assert.Equal('C', AnswerOptionOrder.DisplayLabel(options, escalate));
        Assert.Equal('D', AnswerOptionOrder.DisplayLabel(options, allAbove)); // catch-all shown last, as "D"
    }
}
