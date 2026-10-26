namespace Risk.Api.Data.Import;

public interface ICountrySource
{
    IReadOnlyList<CountryRow> Read();
}