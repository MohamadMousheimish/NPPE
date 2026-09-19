using NPPE.Application.Common;

namespace NPPE.Web.StudyContent;

/// <summary>
/// The three fixed, curated questions shown on the public free-trial page (/Try).
/// Always the same three, so a repeat visitor can't harvest new questions. Bilingual —
/// text is picked for the current culture via <see cref="LocalizedText"/>.
/// </summary>
public static class SampleQuestions
{
    public sealed record Option(string Label, string En, string Fr)
    {
        public string Text => LocalizedText.Pick(En, Fr);
    }

    public sealed record Question(string StemEn, string StemFr, string Correct,
        IReadOnlyList<Option> Options, string ExplEn, string ExplFr)
    {
        public string Stem => LocalizedText.Pick(StemEn, StemFr);
        public string Explanation => LocalizedText.Pick(ExplEn, ExplFr);
    }

    public static readonly IReadOnlyList<Question> All = new[]
    {
        new Question(
            "An engineer discovers that their employer is disposing of waste into a river. Although no specific regulation appears to prohibit the disposal of this particular material, the engineer believes the practice may pose a risk to the environment and public safety. What should the professional do first?",
            "Un ingénieur découvre que son employeur déverse des déchets dans une rivière. Bien qu'aucune réglementation précise ne semble interdire l'élimination de ce matériau en particulier, l'ingénieur estime que cette pratique pourrait présenter un risque pour l'environnement et la sécurité du public. Que devrait faire le professionnel en premier lieu?",
            "B",
            new[]
            {
                new Option("A", "Report the employer's actions to the media.", "Signaler les agissements de l'employeur aux médias."),
                new Option("B", "Raise concern with the appropriate department manager.", "Soulever la préoccupation auprès du directeur de service approprié."),
                new Option("C", "Immediately file a complaint with the professional regulator.", "Déposer immédiatement une plainte auprès de l'organisme de réglementation de la profession."),
                new Option("D", "Report the employer directly to the appropriate government authority.", "Signaler l'employeur directement à l'autorité gouvernementale compétente."),
            },
            "A professional's first step should generally be to raise the concern through the appropriate internal organizational channel, unless there is an immediate danger requiring urgent external intervention.",
            "La première démarche d'un professionnel devrait généralement consister à soulever la préoccupation par la voie interne appropriée de l'organisation, sauf en cas de danger immédiat exigeant une intervention externe urgente."),

        new Question(
            "Under what circumstances may an engineer disclose confidential or proprietary information obtained through professional employment?",
            "Dans quelles circonstances un ingénieur peut-il divulguer des renseignements confidentiels ou exclusifs obtenus dans le cadre d'un emploi professionnel?",
            "B",
            new[]
            {
                new Option("A", "Whenever the new employer is not a competitor", "Chaque fois que le nouvel employeur n'est pas un concurrent"),
                new Option("B", "When overriding ethical obligations or the public interest require disclosure", "Lorsque des obligations éthiques supérieures ou l'intérêt public exigent la divulgation"),
                new Option("C", "Whenever the engineer has completed a specified period of employment", "Chaque fois que l'ingénieur a accompli une période d'emploi déterminée"),
                new Option("D", "Automatically after employment ends", "Automatiquement après la fin de l'emploi"),
            },
            "Professional confidentiality is important but not absolute. Disclosure can be required or justified by law or by an overriding professional duty — particularly where serious risks to public safety or welfare are involved.",
            "La confidentialité professionnelle est importante, mais non absolue. La divulgation peut être exigée ou justifiée par la loi ou par un devoir professionnel supérieur, en particulier lorsque des risques graves pour la sécurité ou le bien-être du public sont en jeu."),

        new Question(
            "What is the primary purpose of requiring individuals to be licensed before they can independently practise engineering or geoscience?",
            "Quelle est la finalité première de l'exigence selon laquelle une personne doit être titulaire d'un permis avant de pouvoir exercer de façon autonome le génie ou la géoscience?",
            "C",
            new[]
            {
                new Option("A", "To reduce competition and increase the income of licensed professionals", "Réduire la concurrence et augmenter le revenu des professionnels titulaires d'un permis"),
                new Option("B", "To allow the profession to arbitrarily select who may practise", "Permettre à la profession de choisir arbitrairement qui peut exercer"),
                new Option("C", "To protect the public by ensuring that only qualified and accountable professionals are authorized to practise", "Protéger le public en veillant à ce que seuls des professionnels qualifiés et responsables soient autorisés à exercer"),
                new Option("D", "All of the above", "Toutes ces réponses"),
            },
            "The fundamental purpose of professional regulation is public protection — ensuring only qualified, accountable professionals practise.",
            "La finalité fondamentale de la réglementation professionnelle est la protection du public — veiller à ce que seuls des professionnels qualifiés et responsables exercent."),
    };
}
