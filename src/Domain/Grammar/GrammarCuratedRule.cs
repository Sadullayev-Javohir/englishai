using Domain.Common;

namespace Domain.Grammar;

public sealed class GrammarCuratedRule
{
    private GrammarCuratedRule() { HeadingUz = null!; BodyUz = null!; }
    private GrammarCuratedRule(string headingUz, string bodyUz)
    {
        Id = Guid.NewGuid();
        HeadingUz = headingUz;
        BodyUz = bodyUz;
    }

    public Guid Id { get; private set; }
    public string HeadingUz { get; private set; }
    public string BodyUz { get; private set; }

    public static GrammarCuratedRule Create(string headingUz, string bodyUz)
    {
        if (string.IsNullOrWhiteSpace(headingUz)) throw new DomainException("Rule heading must not be empty.");
        if (string.IsNullOrWhiteSpace(bodyUz)) throw new DomainException("Rule body must not be empty.");
        return new GrammarCuratedRule(headingUz.Trim(), bodyUz.Trim());
    }
}
