namespace Risk.Api.DesignPatterns.Strategy;

public sealed record NpcAttackChoice(string FromCountryId, string ToCountryId, double WinChance, string Justification);

public sealed record NpcPlacement(string CountryId, int Soldiers, string Justification);

public sealed record NpcContinueDecision(bool Continue, string Justification);

public sealed record NpcTransferChoice(string FromCountryId, string ToCountryId, int Soldiers, string Justification);