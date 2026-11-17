using ApexPerformance.API.Features.Profile;

namespace ApexPerformance.Tests;

public class UserPicturesTests
{
    private static readonly Guid UserId = Guid.Parse("0f8b6a1e-2c3d-4e5f-8a9b-0c1d2e3f4a5b");
    private static readonly DateTimeOffset UpdatedAt = new(2026, 11, 17, 10, 30, 0, 250, TimeSpan.Zero);

    [Fact]
    public void Url_is_relative_to_the_api_and_carries_the_version()
    {
        Assert.Equal($"users/{UserId}/picture?v={UpdatedAt.ToUnixTimeMilliseconds()}",
            UserPictures.Url(UserId, UpdatedAt));
    }

    [Fact]
    public void No_url_without_a_picture()
    {
        Assert.Null(UserPictures.Url(UserId, null));
        Assert.Null(UserPictures.None.UrlFor(UserId));
        Assert.Null(UserPictures.None.UrlFor(null));
    }

    [Fact]
    public void New_picture_gets_a_new_url()
    {
        Assert.NotEqual(UserPictures.Url(UserId, UpdatedAt), UserPictures.Url(UserId, UpdatedAt.AddSeconds(1)));
    }

    [Fact]
    public void Lookup_returns_the_url_only_for_users_with_a_picture()
    {
        var other = Guid.NewGuid();
        var pictures = new UserPictures(new Dictionary<Guid, DateTimeOffset> { [UserId] = UpdatedAt });

        Assert.Equal(UserPictures.Url(UserId, UpdatedAt), pictures.UrlFor(UserId));
        Assert.Null(pictures.UrlFor(other));
    }
}
