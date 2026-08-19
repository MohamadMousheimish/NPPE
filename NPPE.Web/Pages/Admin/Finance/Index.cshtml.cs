using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NPPE.Application.DTOs.Finance;
using NPPE.Application.Queries.Finance.GetFinancialSummary;
using NPPE.Application.Queries.Finance.GetThresholdStatus;

namespace NPPE.Web.Pages.Admin.Finance
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly IMediator _mediator;

        public IndexModel(IMediator mediator) => _mediator = mediator;

        [BindProperty(SupportsGet = true)] public string Period { get; set; } = "y";
        [BindProperty(SupportsGet = true)] public DateTime? From { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? To { get; set; }

        public FinancialSummaryDto Summary { get; private set; } = default!;
        public FinancialSummaryDto? Prior { get; private set; }
        public ThresholdStatusDto Threshold { get; private set; } = default!;
        public DateTime AsOf { get; private set; }

        public async Task OnGetAsync()
        {
            AsOf = DateTime.UtcNow;
            var (from, to, label) = ResolvePeriod(AsOf);
            Summary = await _mediator.Send(new GetFinancialSummaryQuery(from, to, label));
            Threshold = await _mediator.Send(new GetThresholdStatusQuery(AsOf));

            // Prior equivalent period (same length, immediately before) for deltas.
            if (from.HasValue)
            {
                var length = (to ?? AsOf) - from.Value;
                Prior = await _mediator.Send(
                    new GetFinancialSummaryQuery(from.Value - length, from.Value, "prior"));
            }
        }

        private (DateTime? from, DateTime? to, string label) ResolvePeriod(DateTime now)
        {
            var qStart = new DateTime(now.Year, ((now.Month - 1) / 3) * 3 + 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return Period switch
            {
                "q" => (qStart, null, "This quarter"),
                "lq" => (qStart.AddMonths(-3), qStart, "Last quarter"),
                "h" => (now.AddMonths(-6), null, "Last 6 months"),
                "all" => (null, null, "All time"),
                "custom" => (From?.ToUniversalTime(), To?.ToUniversalTime().AddDays(1), "Custom range"),
                _ => (new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc), null, "This year"),
            };
        }

        // Display helpers
        public string Money(decimal d) => (d < 0 ? "−CA$" : "CA$") + Math.Abs(d).ToString("N2");
        public string MoneyK(decimal d) => "CA$" + d.ToString("N0");
        public string ZoneClass => Threshold.Zone switch
        {
            Domain.Enums.ThresholdZone.RegisterNow => "danger",
            Domain.Enums.ThresholdZone.Approaching => "caution",
            _ => "safe"
        };
        public double Pct(decimal amount, decimal of) => of <= 0 ? 0 : Math.Min(100, (double)(amount / of) * 100);

        public record DeltaView(string Css, string Arrow, string Amount, string Kind);

        /// <summary>Percentage change vs the prior period, with good/bad colouring.</summary>
        public DeltaView Delta(decimal current, decimal? prior, bool higherIsBetter)
        {
            if (prior is null) return new("flat", "", "", "none");
            if (prior.Value == 0m)
            {
                if (current == 0m) return new("flat", "", "", "flat");
                var upNew = current > 0m;
                return new(upNew == higherIsBetter ? "up" : "down", upNew ? "▲" : "▼", "", "new");
            }
            var change = (current - prior.Value) / Math.Abs(prior.Value) * 100m;
            if (Math.Round(change, 1) == 0m) return new("flat", "", "", "flat");
            var up = change > 0m;
            return new(up == higherIsBetter ? "up" : "down", up ? "▲" : "▼",
                Math.Abs(change).ToString("N1") + "%", "pct");
        }
    }
}
