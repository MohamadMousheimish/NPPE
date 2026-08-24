namespace NPPE.Web.StudyContent;

/// <summary>
/// Metadata for one NPPE syllabus section. The rich reading content itself lives in
/// the matching Razor partial (Pages/Student/Study/_Section{Number}.cshtml); this keeps
/// the lightweight, reusable data (cards, headers, weightings) separate from the prose.
/// </summary>
public sealed record StudySection(
    int Number,
    string Roman,
    string Slug,
    string Title,
    int WeightPercent,
    string QuestionRange,
    string Summary,
    string[] Topics);

/// <summary>The six weighted sections of the NPPE syllabus (weights sum to 100%).</summary>
public static class StudyLibrary
{
    public static readonly IReadOnlyList<StudySection> Sections = new StudySection[]
    {
        new(1, "I", "professionalism", "Professionalism", 10, "7–10 questions",
            "What it means to hold professional status — the profession's relationship with society, self-regulation, and why engineering and geoscience matter to the public.",
            new[]
            {
                "Definition & interpretation of professional status",
                "Roles & responsibilities of professionals in society",
                "Professions in Canada — definitions & scopes of practice",
                "The value of the professions to society",
            }),

        new(2, "II", "ethics", "Ethics", 20, "17–21 questions",
            "How professionals reason about right and wrong — the role of ethics, the classical theories, the Code of Ethics, and a repeatable method for resolving dilemmas.",
            new[]
            {
                "The role of ethics in society; cultures & customs",
                "Ethical theories and principles",
                "Codes of Ethics of engineers & geoscientists",
                "Common dilemmas & making ethical decisions",
            }),

        new(3, "III", "professional-practice", "Professional Practice", 30, "27–32 questions",
            "The largest section: accountability and standards of practice, duties to employers and clients, risk and insurance, the environment, software liability, stamping, and whistleblowing.",
            new[]
            {
                "Accountability, workplace issues & standards of practice",
                "Responsibilities to employers & clients; conflict of interest",
                "Relations with others; statutory & non-statutory standards",
                "Risk management, insurance & due diligence",
                "Environmental responsibility & software liability",
                "Document authentication; duty to inform & whistleblowing",
            }),

        new(4, "IV", "law-for-professional-practice", "Law for Professional Practice", 20, "23–28 questions",
            "The legal foundations every professional needs: the Canadian legal system, contracts and torts, business and employment law, dispute resolution, intellectual property, and more.",
            new[]
            {
                "The Canadian legal system",
                "Contract law — elements, principles & applications",
                "Tort law — negligence & misrepresentation",
                "Business, employment & labour law",
                "Dispute resolution",
                "Intellectual property; expert witness; liens; OH&S; privacy",
            }),

        new(5, "V", "professional-law", "Professional Law", 10, "7–10 questions",
            "The Acts, regulations and bylaws that govern the professions — admission and licensing, illegal practice, misuse of title, and the role of technical societies.",
            new[]
            {
                "The Act, regulations & bylaws",
                "Admission to the professions",
                "Illegal practice & misuse of title",
                "Professional & technical societies",
            }),

        new(6, "VI", "regulation-discipline", "Regulation & Discipline Processes", 10, "7–10 questions",
            "How members and firms are held to account: complaints and discipline procedures, practice review of individuals and firms, and continuing professional development.",
            new[]
            {
                "Discipline procedures",
                "Practice review of individuals",
                "Practice review of firms",
                "Continuing professional development (CPD)",
            }),
    };

    public static StudySection? Get(int number) => Sections.FirstOrDefault(s => s.Number == number);
}
