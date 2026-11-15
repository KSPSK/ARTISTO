using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ARTISTO.Data;
using ARTISTO.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ARTISTO.Tests;

public class ChatApiTests
{
    [Theory]
    [InlineData("/api/conversations", "GET")]
    [InlineData("/api/conversations", "POST")]
    [InlineData("/api/conversations/1", "GET")]
    [InlineData("/api/conversations/1/messages", "GET")]
    [InlineData("/api/conversations/1/messages", "POST")]
    public async Task Chat_routes_require_authentication(string url, string method)
    {
        using var app = new SocialApp();
        await app.Seed();
        using var anonymous = app.Client();

        using var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (method == "POST")
        {
            request.Content = JsonContent.Create(new { participantId = 1, content = "Hello" });
        }

        using var response = await anonymous.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task User_cannot_start_a_conversation_with_themselves()
    {
        using var app = new SocialApp();
        await app.Seed();
        using var user = app.Client();
        var userId = await app.Register(user);

        using var response = await user.PostAsJsonAsync("/api/conversations", new
        {
            participantId = userId,
            initialMessage = "Hello me"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Starting_conversation_with_missing_user_or_artwork_returns_not_found()
    {
        using var app = new SocialApp();
        await app.Seed();
        using var alice = app.Client();
        using var bob = app.Client();
        await app.Register(alice);
        var bobId = await app.Register(bob);

        using var missingUser = await alice.PostAsJsonAsync("/api/conversations", new
        {
            participantId = 999999
        });
        Assert.Equal(HttpStatusCode.NotFound, missingUser.StatusCode);

        using var missingArtwork = await alice.PostAsJsonAsync("/api/conversations", new
        {
            participantId = bobId,
            artworkListingId = 999999
        });
        Assert.Equal(HttpStatusCode.NotFound, missingArtwork.StatusCode);
    }

    [Fact]
    public async Task Artwork_conversation_requires_one_participant_to_own_the_artwork()
    {
        using var app = new SocialApp();
        await app.Seed();
        using var alice = app.Client();
        using var bob = app.Client();
        using var charlie = app.Client();
        await app.Register(alice);
        var bobId = await app.Register(bob);
        await app.Register(charlie);
        var charlieListingId = await app.Artwork(charlie);

        // Neither Alice nor Bob owns Charlie's artwork
        using var unrelatedArtwork = await alice.PostAsJsonAsync("/api/conversations", new
        {
            participantId = bobId,
            artworkListingId = charlieListingId,
            initialMessage = "Talking about Charlie's art"
        });
        Assert.Equal(HttpStatusCode.BadRequest, unrelatedArtwork.StatusCode);

        // Create an ownerless listing in the database and verify it is rejected
        int ownerlessListingId;
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ArtistoDbContext>();
            var ownerless = new ArtworkListing
            {
                Title = "Ownerless",
                Description = "Seeded",
                Price = 50,
                CreatorName = "Unknown",
                Category = ArtworkCategory.Painting,
                UserId = null
            };
            db.ArtworkListings.Add(ownerless);
            await db.SaveChangesAsync();
            ownerlessListingId = ownerless.Id;
        }

        using var ownerlessResponse = await alice.PostAsJsonAsync("/api/conversations", new
        {
            participantId = bobId,
            artworkListingId = ownerlessListingId
        });
        Assert.Equal(HttpStatusCode.BadRequest, ownerlessResponse.StatusCode);
    }

    [Fact]
    public async Task Accessing_or_messaging_missing_conversation_returns_not_found()
    {
        using var app = new SocialApp();
        await app.Seed();
        using var alice = app.Client();
        await app.Register(alice);

        Assert.Equal(HttpStatusCode.NotFound, (await alice.GetAsync("/api/conversations/999999")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await alice.GetAsync("/api/conversations/999999/messages")).StatusCode);
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await alice.PostAsJsonAsync("/api/conversations/999999/messages", new { content = "Hello?" })).StatusCode);
    }

    [Fact]
    public async Task Participants_can_exchange_messages_and_strangers_are_forbidden_even_with_blank_payload()
    {
        using var app = new SocialApp();
        await app.Seed();
        using var seller = app.Client();
        using var buyer = app.Client();
        using var stranger = app.Client();
        var sellerId = await app.Register(seller);
        var buyerId = await app.Register(buyer);
        await app.Register(stranger);
        var listingId = await app.Artwork(seller, localPickupAvailable: true);

        using var started = await buyer.PostAsJsonAsync("/api/conversations", new
        {
            participantId = sellerId,
            artworkListingId = listingId,
            initialMessage = "  Is this painting still available for pickup?  "
        });

        Assert.Equal(HttpStatusCode.Created, started.StatusCode);
        var conversation = await started.Content.ReadFromJsonAsync<JsonElement>();
        var conversationId = conversation.GetProperty("id").GetInt32();
        Assert.Equal(sellerId, conversation.GetProperty("otherUserId").GetInt32());
        Assert.Equal(listingId, conversation.GetProperty("artworkListingId").GetInt32());

        var initialMessages = conversation.GetProperty("messages");
        Assert.Equal(1, initialMessages.GetArrayLength());
        Assert.Equal("Is this painting still available for pickup?", initialMessages[0].GetProperty("content").GetString());
        Assert.Equal(buyerId, initialMessages[0].GetProperty("senderId").GetInt32());

        // Seller replies in the conversation
        using var reply = await seller.PostAsJsonAsync($"/api/conversations/{conversationId}/messages", new
        {
            content = "  Yes, we can meet in Vilnius tomorrow!  "
        });
        Assert.Equal(HttpStatusCode.Created, reply.StatusCode);
        var replyJson = await reply.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(sellerId, replyJson.GetProperty("senderId").GetInt32());
        Assert.Equal("Yes, we can meet in Vilnius tomorrow!", replyJson.GetProperty("content").GetString());

        // Both buyer and seller can read the conversation and messages
        using var sellerView = await seller.GetAsync($"/api/conversations/{conversationId}");
        Assert.Equal(HttpStatusCode.OK, sellerView.StatusCode);
        var sellerDetails = await sellerView.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(buyerId, sellerDetails.GetProperty("otherUserId").GetInt32());
        Assert.Equal(2, sellerDetails.GetProperty("messages").GetArrayLength());

        using var buyerMessages = await buyer.GetAsync($"/api/conversations/{conversationId}/messages");
        Assert.Equal(HttpStatusCode.OK, buyerMessages.StatusCode);
        Assert.Equal(2, (await buyerMessages.Content.ReadFromJsonAsync<JsonElement>()).GetArrayLength());

        // Non-participant (stranger) is forbidden from reading or sending messages (even if content is blank)
        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.GetAsync($"/api/conversations/{conversationId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.GetAsync($"/api/conversations/{conversationId}/messages")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await stranger.PostAsJsonAsync($"/api/conversations/{conversationId}/messages", new { content = "Intruder" })).StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await stranger.PostAsJsonAsync($"/api/conversations/{conversationId}/messages", new { content = "   " })).StatusCode);
    }

    [Fact]
    public async Task Starting_existing_conversation_reuses_same_thread_regardless_of_initiator_order()
    {
        using var app = new SocialApp();
        await app.Seed();
        using var alice = app.Client();
        using var bob = app.Client();
        var aliceId = await app.Register(alice);
        var bobId = await app.Register(bob);
        var listingId = await app.Artwork(bob);

        using var first = await alice.PostAsJsonAsync("/api/conversations", new
        {
            participantId = bobId,
            initialMessage = "Hi Bob!"
        });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var firstId = (await first.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        // Bob starts conversation with Alice (reversed order) -> reuses the same conversation ID
        using var second = await bob.PostAsJsonAsync("/api/conversations", new
        {
            participantId = aliceId,
            initialMessage = "Hi Alice!"
        });
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        var secondJson = await second.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(firstId, secondJson.GetProperty("id").GetInt32());
        Assert.Equal(2, secondJson.GetProperty("messages").GetArrayLength());

        // Starting a conversation about a specific artwork creates a separate thread
        using var artworkChat = await alice.PostAsJsonAsync("/api/conversations", new
        {
            participantId = bobId,
            artworkListingId = listingId,
            initialMessage = "Question about your artwork"
        });
        Assert.Equal(HttpStatusCode.Created, artworkChat.StatusCode);
        var artworkChatId = (await artworkChat.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        Assert.NotEqual(firstId, artworkChatId);
    }

    [Fact]
    public async Task Inbox_lists_only_user_conversations_ordered_by_most_recent_message()
    {
        using var app = new SocialApp();
        await app.Seed();
        using var alice = app.Client();
        using var bob = app.Client();
        using var charlie = app.Client();
        await app.Register(alice);
        var bobId = await app.Register(bob);
        var charlieId = await app.Register(charlie);

        using var convWithBob = await alice.PostAsJsonAsync("/api/conversations", new
        {
            participantId = bobId,
            initialMessage = "First thread"
        });
        var bobConvId = (await convWithBob.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        using var convWithCharlie = await alice.PostAsJsonAsync("/api/conversations", new
        {
            participantId = charlieId,
            initialMessage = "Second thread"
        });
        var charlieConvId = (await convWithCharlie.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        // Bob sends a new message, making bobConvId the most recently active conversation
        Assert.Equal(
            HttpStatusCode.Created,
            (await bob.PostAsJsonAsync($"/api/conversations/{bobConvId}/messages", new { content = "Newest reply" })).StatusCode);

        var aliceInbox = await (await alice.GetAsync("/api/conversations")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, aliceInbox.GetArrayLength());
        Assert.Equal(bobConvId, aliceInbox[0].GetProperty("id").GetInt32());
        Assert.Equal("Newest reply", aliceInbox[0].GetProperty("lastMessage").GetProperty("content").GetString());
        Assert.Equal(charlieConvId, aliceInbox[1].GetProperty("id").GetInt32());

        // Bob only sees his own conversation with Alice, not Alice's conversation with Charlie
        var bobInbox = await (await bob.GetAsync("/api/conversations")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, bobInbox.GetArrayLength());
        Assert.Equal(bobConvId, bobInbox[0].GetProperty("id").GetInt32());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Blank_or_overlong_messages_are_rejected(string invalidContent)
    {
        using var app = new SocialApp();
        await app.Seed();
        using var alice = app.Client();
        using var bob = app.Client();
        await app.Register(alice);
        var bobId = await app.Register(bob);
        var overlong = new string('x', 2001);

        using var badInitial = await alice.PostAsJsonAsync("/api/conversations", new
        {
            participantId = bobId,
            initialMessage = invalidContent
        });
        Assert.Equal(HttpStatusCode.BadRequest, badInitial.StatusCode);

        using var overlongInitial = await alice.PostAsJsonAsync("/api/conversations", new
        {
            participantId = bobId,
            initialMessage = overlong
        });
        Assert.Equal(HttpStatusCode.BadRequest, overlongInitial.StatusCode);

        using var created = await alice.PostAsJsonAsync("/api/conversations", new
        {
            participantId = bobId
        });
        var convId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        using var badMessage = await alice.PostAsJsonAsync($"/api/conversations/{convId}/messages", new
        {
            content = invalidContent
        });
        Assert.Equal(HttpStatusCode.BadRequest, badMessage.StatusCode);

        using var overlongMessage = await alice.PostAsJsonAsync($"/api/conversations/{convId}/messages", new
        {
            content = overlong
        });
        Assert.Equal(HttpStatusCode.BadRequest, overlongMessage.StatusCode);
    }
}
