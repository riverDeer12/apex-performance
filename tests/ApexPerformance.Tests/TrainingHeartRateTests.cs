using ApexPerformance.API.Constants;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Features.Trainings;

namespace ApexPerformance.Tests;

public class TrainingHeartRateTests
{
    private static readonly SaveTrainingHeartRateValidator Validator = new();

    private static Training Training(int? min = 60, int? max = 160, int? average = 120, string? source = null) => new()
    {
        Name = "Legs",
        Client = null!,
        HeartRateMin = min,
        HeartRateMax = max,
        HeartRateAverage = average,
        HeartRateSource = source
    };

    [Theory]
    [InlineData(null)]
    [InlineData(HeartRateSources.AppleHealth)]
    [InlineData(HeartRateSources.HealthConnect)]
    public void Known_or_missing_source_is_valid(string? source)
    {
        Assert.True(Validator.Validate(new SaveTrainingHeartRateRequest(60, 160, 120, source)).IsValid);
    }

    [Theory]
    [InlineData("google_fit")]
    [InlineData("")]
    [InlineData("Apple Health")]
    public void Unknown_source_is_not_valid(string source)
    {
        Assert.False(Validator.Validate(new SaveTrainingHeartRateRequest(60, 160, 120, source)).IsValid);
    }

    [Fact]
    public void Values_are_still_checked()
    {
        Assert.False(Validator.Validate(new SaveTrainingHeartRateRequest(130, 160, 120, HeartRateSources.HealthConnect)).IsValid);
        Assert.False(Validator.Validate(new SaveTrainingHeartRateRequest(20, 160, 120, HeartRateSources.HealthConnect)).IsValid);
    }

    [Fact]
    public void Missing_source_is_stored_as_apple_health()
    {
        // iPhone builds before the source was added.
        Assert.Equal(HeartRateSources.AppleHealth, HeartRateSources.Normalize(null));
        Assert.Equal(HeartRateSources.HealthConnect, HeartRateSources.Normalize(HeartRateSources.HealthConnect));
    }

    [Fact]
    public void Response_has_the_source()
    {
        Assert.Equal(new TrainingHeartRateResponse(60, 160, 120, HeartRateSources.HealthConnect),
            TrainingMapper.HeartRateOf(Training(source: HeartRateSources.HealthConnect)));
        // Saved before the source was kept: only iPhones sent heart rate then.
        Assert.Equal(HeartRateSources.AppleHealth, TrainingMapper.HeartRateOf(Training())?.Source);
        Assert.Null(TrainingMapper.HeartRateOf(Training(min: null)));
    }
}
