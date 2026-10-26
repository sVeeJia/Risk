using Risk.Api.Dtos.Requests;
using Risk.Api.Models;
using Risk.Api.Services;
using Risk.Tests.Fakes;

namespace Risk.Tests.Services;

public class CountryServiceTests
{
    private static CountryService Create()
    {
        var repo = new FakeCountryRepository();
        repo.Items.AddRange(new[]
        {
            new Country { Id = "IN", Name = "India", Population = 1_476_625_576, Rank = 1 },
            new Country { Id = "ZA", Name = "South Africa", Population = 64_000_000, Rank = 25 },
            new Country { Id = "VA", Name = "Holy See", Population = 506, Rank = 234 },
        });
        return new CountryService(repo);
    }

    [Fact]
    public async Task Search_ByName_IsCaseInsensitive()
    {
        var result = await Create().SearchAsync(new CountryQuery { Search = "south" });
        Assert.Single(result.Items);
        Assert.Equal("ZA", result.Items[0].Id);
    }

    [Fact]
    public async Task Filter_BySoldiers_ReturnsOnlyMatching()
    {
        var result = await Create().SearchAsync(new CountryQuery { Soldiers = 9 });
        Assert.Equal("IN", Assert.Single(result.Items).Id);
    }

    [Fact]
    public async Task Sort_ByPopulationAscending_PutsSmallestFirst()
    {
        var result = await Create().SearchAsync(new CountryQuery { Sort = "population" });
        Assert.Equal("VA", result.Items[0].Id);
    }

    [Fact]
    public async Task Paging_ReportsTotalAndSlices()
    {
        var result = await Create().SearchAsync(new CountryQuery { PageSize = 2, Page = 2 });
        Assert.Equal(3, result.Total);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetById_Unknown_ReturnsNull() =>
        Assert.Null(await Create().GetByIdAsync("XX"));
}