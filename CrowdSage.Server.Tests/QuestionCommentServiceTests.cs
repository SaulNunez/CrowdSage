using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using CrowdSage.Server.Models;
using CrowdSage.Server.Services;
using CrowdSage.Server.Models.InsertUpdate;

namespace CrowdSage.Server.Tests;

public class QuestionCommentServiceTests
{
    private static CrowdsageDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<CrowdsageDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CrowdsageDbContext(options);
    }

    [Fact]
    public async Task AddCommnentAsync_NullPayload_ThrowsArgumentNullException()
    {
        await using var context = CreateInMemoryContext();
        var svc = new QuestionCommentService(context);

        await Assert.ThrowsAsync<ArgumentNullException>(() => svc.AddCommnentAsync(null!, Guid.NewGuid(), "u1"));
    }

    [Fact]
    public async Task AddCommnentAsync_MissingQuestion_ThrowsKeyNotFoundException()
    {
        await using var context = CreateInMemoryContext();
        var user = new CrowdsageUser { Id = "qcUser1", UserName = "qc1" };
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();

        var svc = new QuestionCommentService(context);
        var payload = new QuestionCommentPayload { Content = "cmt" };

        await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.AddCommnentAsync(payload, Guid.NewGuid(), user.Id));
    }

    [Fact]
    public async Task AddCommnentAsync_AddsCommentAndReturnsDto_WithAuthor()
    {
        await using var context = CreateInMemoryContext();
        var user = new CrowdsageUser { Id = "qcUser2", UserName = "qc2" };
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

        var svc = new QuestionCommentService(context);
        var payload = new QuestionCommentPayload { Content = "nice comment" };

        var dto = await svc.AddCommnentAsync(payload, question.Id, user.Id);

        Assert.NotNull(dto);
        Assert.Equal(payload.Content, dto.Content);
        Assert.NotNull(dto.Author);
        Assert.Equal(user.Id, dto.Author.Id);
        Assert.Equal(user.UserName, dto.Author.UserName);

        var inDb = await context.QuestionComments.FirstOrDefaultAsync(c => c.Content == payload.Content && c.QuestionId == question.Id);
        Assert.NotNull(inDb);
    }
}
