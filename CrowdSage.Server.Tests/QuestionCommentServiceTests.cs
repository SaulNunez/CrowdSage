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

    [Fact]
    public async Task GetCommentByIdAsync_NonExistent_ThrowsKeyNotFoundException()
    {
        await using var context = CreateInMemoryContext();
        var svc = new QuestionCommentService(context);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.GetCommentByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetCommentByIdAsync_ReturnsDto_WithAuthor()
    {
        await using var context = CreateInMemoryContext();
        var user = new CrowdsageUser { Id = "qcG1", UserName = "g1" };
        await context.Users.AddAsync(user);

        var question = new Question { Id = Guid.NewGuid(), Title = "Q", Content = "C", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, Tags = new System.Collections.Generic.List<string>(), Answers = new System.Collections.Generic.List<Answer>(), Votes = new System.Collections.Generic.List<QuestionVote>(), Comments = new System.Collections.Generic.List<QuestionComment>(), AuthorId = user.Id, Author = user };
        await context.Questions.AddAsync(question);

        var comment = new QuestionComment { Content = "c1", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, Question = question, QuestionId = question.Id, Author = user, AuthorId = user.Id };
        await context.QuestionComments.AddAsync(comment);
        await context.SaveChangesAsync();

        var svc = new QuestionCommentService(context);
        var dto = await svc.GetCommentByIdAsync(comment.Id);

        Assert.NotNull(dto);
        Assert.Equal(comment.Content, dto.Content);
        Assert.Equal(user.Id, dto.Author.Id);
    }

    [Fact]
    public async Task EditCommentAsync_NullPayload_ThrowsArgumentNullException()
    {
        await using var context = CreateInMemoryContext();
        var svc = new QuestionCommentService(context);

        await Assert.ThrowsAsync<ArgumentNullException>(() => svc.EditCommentAsync(Guid.NewGuid(), null!));
    }

    [Fact]
    public async Task EditCommentAsync_NonExistent_ThrowsKeyNotFoundException()
    {
        await using var context = CreateInMemoryContext();
        var svc = new QuestionCommentService(context);
        var payload = new QuestionCommentPayload { Content = "x" };

        await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.EditCommentAsync(Guid.NewGuid(), payload));
    }

    [Fact]
    public async Task EditCommentAsync_UpdatesContentAndUpdatedAt_PreservesCreatedAndAuthor()
    {
        await using var context = CreateInMemoryContext();
        var user = new CrowdsageUser { Id = "qcE1", UserName = "qe1" };
        await context.Users.AddAsync(user);

        var question = new Question { Id = Guid.NewGuid(), Title = "Q", Content = "C", CreatedAt = DateTimeOffset.UtcNow.AddHours(-2), UpdatedAt = DateTimeOffset.UtcNow.AddHours(-2), Tags = new System.Collections.Generic.List<string>(), Answers = new System.Collections.Generic.List<Answer>(), Votes = new System.Collections.Generic.List<QuestionVote>(), Comments = new System.Collections.Generic.List<QuestionComment>(), AuthorId = user.Id, Author = user };
        await context.Questions.AddAsync(question);

        var comment = new QuestionComment { Content = "orig", CreatedAt = DateTimeOffset.UtcNow.AddHours(-1), UpdatedAt = DateTimeOffset.UtcNow.AddHours(-1), Question = question, QuestionId = question.Id, Author = user, AuthorId = user.Id };
        await context.QuestionComments.AddAsync(comment);
        await context.SaveChangesAsync();

        var svc = new QuestionCommentService(context);
        var payload = new QuestionCommentPayload { Content = "edited" };

        var beforeCreated = comment.CreatedAt;
        var beforeUpdated = comment.UpdatedAt;

        await svc.EditCommentAsync(comment.Id, payload);

        var updated = await context.QuestionComments.FirstOrDefaultAsync(c => c.Id == comment.Id);
        Assert.NotNull(updated);
        Assert.Equal(payload.Content, updated!.Content);
        Assert.Equal(beforeCreated, updated.CreatedAt);
        Assert.True(updated.UpdatedAt > beforeUpdated);
        Assert.Equal(user.Id, updated.AuthorId);
    }

    [Fact]
    public async Task DeleteCommentAsync_NonExistent_ThrowsKeyNotFoundException()
    {
        await using var context = CreateInMemoryContext();
        var svc = new QuestionCommentService(context);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.DeleteCommentAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteCommentAsync_RemovesComment()
    {
        await using var context = CreateInMemoryContext();
        var user = new CrowdsageUser { Id = "qcD1", UserName = "qd1" };
        await context.Users.AddAsync(user);

        var question = new Question { Id = Guid.NewGuid(), Title = "Q", Content = "C", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, Tags = new System.Collections.Generic.List<string>(), Answers = new System.Collections.Generic.List<Answer>(), Votes = new System.Collections.Generic.List<QuestionVote>(), Comments = new System.Collections.Generic.List<QuestionComment>(), AuthorId = user.Id, Author = user };
        await context.Questions.AddAsync(question);

        var comment = new QuestionComment { Content = "to del", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, Question = question, QuestionId = question.Id, Author = user, AuthorId = user.Id };
        await context.QuestionComments.AddAsync(comment);
        await context.SaveChangesAsync();

        var svc = new QuestionCommentService(context);
        await svc.DeleteCommentAsync(comment.Id);

        var exists = await context.QuestionComments.AnyAsync(c => c.Id == comment.Id);
        Assert.False(exists);
    }

    [Fact]
    public async Task GetCommentsForQuestion_NoComments_ReturnsEmptyList()
    {
        await using var context = CreateInMemoryContext();
        var svc = new QuestionCommentService(context);

        var list = await svc.GetCommentsForQuestion(Guid.NewGuid());
        Assert.NotNull(list);
        Assert.Empty(list);
    }

    [Fact]
    public async Task GetCommentsForQuestion_ReturnsComments_WithAuthors()
    {
        await using var context = CreateInMemoryContext();
        var user = new CrowdsageUser { Id = "qcL1", UserName = "ql1" };
        await context.Users.AddAsync(user);

        var question = new Question { Id = Guid.NewGuid(), Title = "Q", Content = "C", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, Tags = new System.Collections.Generic.List<string>(), Answers = new System.Collections.Generic.List<Answer>(), Votes = new System.Collections.Generic.List<QuestionVote>(), Comments = new System.Collections.Generic.List<QuestionComment>(), AuthorId = user.Id, Author = user };
        await context.Questions.AddAsync(question);

        var c1 = new QuestionComment { Content = "c1", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, Question = question, QuestionId = question.Id, Author = user, AuthorId = user.Id };
        var c2 = new QuestionComment { Content = "c2", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, Question = question, QuestionId = question.Id, Author = user, AuthorId = user.Id };
        await context.QuestionComments.AddAsync(c1);
        await context.QuestionComments.AddAsync(c2);
        await context.SaveChangesAsync();

        var svc = new QuestionCommentService(context);
        var list = await svc.GetCommentsForQuestion(question.Id);

        Assert.Equal(2, list.Count);
        Assert.All(list, item => Assert.NotNull(item.Author));
    }
}
