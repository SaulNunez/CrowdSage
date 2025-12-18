using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using CrowdSage.Server.Models;
using CrowdSage.Server.Services;
using CrowdSage.Server.Models.InsertUpdate;

namespace CrowdSage.Server.Tests;

public class AnswerCommentServiceTests
{
    private static CrowdsageDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<CrowdsageDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CrowdsageDbContext(options);
    }

    [Fact]
    public async Task AddCommentAsync_NullPayload_ThrowsArgumentNullException()
    {
        await using var context = CreateInMemoryContext();
        var svc = new AnswerCommentService(context);

        await Assert.ThrowsAsync<ArgumentNullException>(() => svc.AddCommentAsync(null!, Guid.NewGuid(), "u1"));
    }

    [Fact]
    public async Task AddCommentAsync_AddsCommentAndReturnsDto_WithAuthor()
    {
        await using var context = CreateInMemoryContext();
        var user = new CrowdsageUser { Id = "acUser1", UserName = "ac1" };
        await context.Users.AddAsync(user);

        var question = new Question { Id = Guid.NewGuid(), Title = "Q", Content = "C", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, Tags = new System.Collections.Generic.List<string>(), Answers = new System.Collections.Generic.List<Answer>(), Votes = new System.Collections.Generic.List<QuestionVote>(), Comments = new System.Collections.Generic.List<QuestionComment>(), AuthorId = user.Id, Author = user };
        await context.Questions.AddAsync(question);

        var answer = new Answer { Content = "ans", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, Author = user, AuthorId = user.Id, Question = question, QuestionId = question.Id, Votes = new System.Collections.Generic.List<AnswerVote>(), Comments = new System.Collections.Generic.List<AnswerComment>() };
        await context.Answers.AddAsync(answer);
        await context.SaveChangesAsync();

        var svc = new AnswerCommentService(context);
        var payload = new AnswerCommentPayload { Content = "nice" };

        var dto = await svc.AddCommentAsync(payload, answer.Id, user.Id);

        Assert.NotNull(dto);
        Assert.Equal(payload.Content, dto.Content);
        Assert.NotNull(dto.Author);
        Assert.Equal(user.Id, dto.Author.Id);
        Assert.Equal(user.UserName, dto.Author.UserName);

        var inDb = await context.AnswerComments.FirstOrDefaultAsync(c => c.Content == payload.Content && c.AnswerId == answer.Id);
        Assert.NotNull(inDb);
    }

    [Fact]
    public async Task GetCommentByIdAsync_NonExistent_ThrowsKeyNotFoundException()
    {
        await using var context = CreateInMemoryContext();
        var svc = new AnswerCommentService(context);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.GetCommentByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetCommentByIdAsync_ReturnsDto_WithAuthor()
    {
        await using var context = CreateInMemoryContext();
        var user = new CrowdsageUser { Id = "acG1", UserName = "g1" };
        await context.Users.AddAsync(user);

        var question = new Question { Id = Guid.NewGuid(), Title = "Q", Content = "C", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, Tags = new System.Collections.Generic.List<string>(), Answers = new System.Collections.Generic.List<Answer>(), Votes = new System.Collections.Generic.List<QuestionVote>(), Comments = new System.Collections.Generic.List<QuestionComment>(), AuthorId = user.Id, Author = user };
        await context.Questions.AddAsync(question);

        var answer = new Answer { Content = "ans", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, Author = user, AuthorId = user.Id, Question = question, QuestionId = question.Id, Votes = new System.Collections.Generic.List<AnswerVote>(), Comments = new System.Collections.Generic.List<AnswerComment>() };
        await context.Answers.AddAsync(answer);

        var comment = new AnswerComment { Content = "c1", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, Answer = answer, AnswerId = answer.Id, Author = user, AuthorId = user.Id };
        await context.AnswerComments.AddAsync(comment);
        await context.SaveChangesAsync();

        var svc = new AnswerCommentService(context);
        var dto = await svc.GetCommentByIdAsync(comment.Id);

        Assert.NotNull(dto);
        Assert.Equal(comment.Content, dto.Content);
        Assert.Equal(user.Id, dto.Author.Id);
    }

    [Fact]
    public async Task EditCommentAsync_NullPayload_ThrowsArgumentNullException()
    {
        await using var context = CreateInMemoryContext();
        var svc = new AnswerCommentService(context);

        await Assert.ThrowsAsync<ArgumentNullException>(() => svc.EditCommentAsync(Guid.NewGuid(), null!));
    }

    [Fact]
    public async Task EditCommentAsync_NonExistent_ThrowsKeyNotFoundException()
    {
        await using var context = CreateInMemoryContext();
        var svc = new AnswerCommentService(context);
        var payload = new AnswerCommentPayload { Content = "x" };

        await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.EditCommentAsync(Guid.NewGuid(), payload));
    }

    [Fact]
    public async Task EditCommentAsync_UpdatesContentAndUpdatedAt_PreservesCreatedAndAuthor()
    {
        await using var context = CreateInMemoryContext();
        var user = new CrowdsageUser { Id = "acE1", UserName = "qe1" };
        await context.Users.AddAsync(user);

        var question = new Question { Id = Guid.NewGuid(), Title = "Q", Content = "C", CreatedAt = DateTimeOffset.UtcNow.AddHours(-2), UpdatedAt = DateTimeOffset.UtcNow.AddHours(-2), Tags = new System.Collections.Generic.List<string>(), Answers = new System.Collections.Generic.List<Answer>(), Votes = new System.Collections.Generic.List<QuestionVote>(), Comments = new System.Collections.Generic.List<QuestionComment>(), AuthorId = user.Id, Author = user };
        await context.Questions.AddAsync(question);

        var answer = new Answer { Content = "ans", CreatedAt = DateTimeOffset.UtcNow.AddHours(-1), UpdatedAt = DateTimeOffset.UtcNow.AddHours(-1), Author = user, AuthorId = user.Id, Question = question, QuestionId = question.Id, Votes = new System.Collections.Generic.List<AnswerVote>(), Comments = new System.Collections.Generic.List<AnswerComment>() };
        await context.Answers.AddAsync(answer);

        var comment = new AnswerComment { Content = "orig", CreatedAt = DateTimeOffset.UtcNow.AddHours(-1), UpdatedAt = DateTimeOffset.UtcNow.AddHours(-1), Answer = answer, AnswerId = answer.Id, Author = user, AuthorId = user.Id };
        await context.AnswerComments.AddAsync(comment);
        await context.SaveChangesAsync();

        var svc = new AnswerCommentService(context);
        var payload = new AnswerCommentPayload { Content = "edited" };

        var beforeCreated = comment.CreatedAt;
        var beforeUpdated = comment.UpdatedAt;

        await svc.EditCommentAsync(comment.Id, payload);

        var updated = await context.AnswerComments.FirstOrDefaultAsync(c => c.Id == comment.Id);
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
        var svc = new AnswerCommentService(context);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.DeleteCommentAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteCommentAsync_RemovesComment()
    {
        await using var context = CreateInMemoryContext();
        var user = new CrowdsageUser { Id = "acD1", UserName = "qd1" };
        await context.Users.AddAsync(user);

        var question = new Question { Id = Guid.NewGuid(), Title = "Q", Content = "C", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, Tags = new System.Collections.Generic.List<string>(), Answers = new System.Collections.Generic.List<Answer>(), Votes = new System.Collections.Generic.List<QuestionVote>(), Comments = new System.Collections.Generic.List<QuestionComment>(), AuthorId = user.Id, Author = user };
        await context.Questions.AddAsync(question);

        var answer = new Answer { Content = "ans", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, Author = user, AuthorId = user.Id, Question = question, QuestionId = question.Id, Votes = new System.Collections.Generic.List<AnswerVote>(), Comments = new System.Collections.Generic.List<AnswerComment>() };
        await context.Answers.AddAsync(answer);

        var comment = new AnswerComment { Content = "to del", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, Answer = answer, AnswerId = answer.Id, Author = user, AuthorId = user.Id };
        await context.AnswerComments.AddAsync(comment);
        await context.SaveChangesAsync();

        var svc = new AnswerCommentService(context);
        await svc.DeleteCommentAsync(comment.Id);

        var exists = await context.AnswerComments.AnyAsync(c => c.Id == comment.Id);
        Assert.False(exists);
    }

    [Fact]
    public async Task GetCommentsForAnswer_NoComments_ReturnsEmptyList()
    {
        await using var context = CreateInMemoryContext();
        var svc = new AnswerCommentService(context);

        var list = await svc.GetCommentsForAnswer(Guid.NewGuid());
        Assert.NotNull(list);
        Assert.Empty(list);
    }

    [Fact]
    public async Task GetCommentsForAnswer_ReturnsComments_WithAuthors()
    {
        await using var context = CreateInMemoryContext();
        var user = new CrowdsageUser { Id = "acL1", UserName = "ql1" };
        await context.Users.AddAsync(user);

        var question = new Question { Id = Guid.NewGuid(), Title = "Q", Content = "C", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, Tags = new System.Collections.Generic.List<string>(), Answers = new System.Collections.Generic.List<Answer>(), Votes = new System.Collections.Generic.List<QuestionVote>(), Comments = new System.Collections.Generic.List<QuestionComment>(), AuthorId = user.Id, Author = user };
        await context.Questions.AddAsync(question);

        var answer = new Answer { Content = "ans", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, Author = user, AuthorId = user.Id, Question = question, QuestionId = question.Id, Votes = new System.Collections.Generic.List<AnswerVote>(), Comments = new System.Collections.Generic.List<AnswerComment>() };
        await context.Answers.AddAsync(answer);

        var c1 = new AnswerComment { Content = "c1", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, Answer = answer, AnswerId = answer.Id, Author = user, AuthorId = user.Id };
        var c2 = new AnswerComment { Content = "c2", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, Answer = answer, AnswerId = answer.Id, Author = user, AuthorId = user.Id };
        await context.AnswerComments.AddAsync(c1);
        await context.AnswerComments.AddAsync(c2);
        await context.SaveChangesAsync();

        var svc = new AnswerCommentService(context);
        var list = await svc.GetCommentsForAnswer(answer.Id);

        Assert.Equal(2, list.Count);
        Assert.All(list, item => Assert.NotNull(item.Author));
    }
}
