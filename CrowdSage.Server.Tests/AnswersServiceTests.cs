using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using CrowdSage.Server.Models;
using CrowdSage.Server.Services;
using CrowdSage.Server.Models.InsertUpdate;

namespace CrowdSage.Server.Tests;

public class AnswersServiceTests
{
    private static CrowdsageDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<CrowdsageDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CrowdsageDbContext(options);
    }

    [Fact]
    public async Task AddAnswerAsync_NullPayload_ThrowsArgumentNullException()
    {
        await using var context = CreateInMemoryContext();
        var svc = new AnswersService(context);

        await Assert.ThrowsAsync<ArgumentNullException>(() => svc.AddAnswerAsync(null!, Guid.NewGuid(), "user1"));
    }

    [Fact]
    public async Task AddAnswerAsync_AddsAnswerAndReturnsDto_WithAuthorAndVote()
    {
        await using var context = CreateInMemoryContext();

        // Arrange: create a user and a question
        var user = new CrowdsageUser { Id = "user1", UserName = "testuser" };
        await context.Users.AddAsync(user);

        var question = new Question
        {
            Id = Guid.NewGuid(),
            Title = "Q",
            Content = "C",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Tags = new System.Collections.Generic.List<string>(),
            Answers = new System.Collections.Generic.List<Answer>(),
            Votes = new System.Collections.Generic.List<QuestionVote>(),
            Comments = new System.Collections.Generic.List<QuestionComment>(),
            AuthorId = user.Id,
            Author = user
        };
        await context.Questions.AddAsync(question);
        await context.SaveChangesAsync();

        var svc = new AnswersService(context);

        var payload = new AnswerPayload { Content = "answer content" };

        // Act
        var dto = await svc.AddAnswerAsync(payload, question.Id, user.Id);

        // Assert DTO
        Assert.NotNull(dto);
        Assert.Equal(payload.Content, dto.Content);
        Assert.NotNull(dto.Author);
        Assert.Equal(user.Id, dto.Author.Id);
        Assert.Equal(user.UserName, dto.Author.UserName);
        Assert.Equal(1, dto.Votes);
        Assert.False(dto.Bookmarked);

        // Assert DB state
        var answersInDb = context.Answers.Include(a => a.Votes).Where(a => a.QuestionId == question.Id).ToList();
        Assert.Single(answersInDb);
        var added = answersInDb.Single();
        Assert.Equal(payload.Content, added.Content);
        Assert.Single(added.Votes);
        Assert.Equal(user.Id, added.Votes.First().UserId);
    }
}
