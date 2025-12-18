using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using CrowdSage.Server.Models;
using CrowdSage.Server.Services;
using CrowdSage.Server.Models.InsertUpdate;

namespace CrowdSage.Server.Tests;

public class QuestionsServiceTests
{
    private static CrowdsageDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<CrowdsageDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CrowdsageDbContext(options);
    }

    [Fact]
    public async Task AddQuestionAsync_NullPayload_ThrowsArgumentNullException()
    {
        await using var context = CreateInMemoryContext();
        var svc = new QuestionsService(context);

        await Assert.ThrowsAsync<ArgumentNullException>(() => svc.AddQuestionAsync(null!, "u1"));
    }

    [Fact]
    public async Task AddQuestionAsync_AddsQuestionAndReturnsDto_WithAuthorAndVote()
    {
        await using var context = CreateInMemoryContext();
        var user = new CrowdsageUser { Id = "qUser1", UserName = "q1" };
        await context.Users.AddAsync(user);

        var svc = new QuestionsService(context);
        var payload = new QuestionPayload { Title = "T", Content = "C" };

        var dto = await svc.AddQuestionAsync(payload, user.Id);

        Assert.NotNull(dto);
        Assert.Equal(payload.Content, dto.Content);
        Assert.NotNull(dto.Author);
        Assert.Equal(user.Id, dto.Author.Id);
        Assert.Equal(1, dto.Votes);

        var inDb = await context.Questions.Include(q => q.Votes).FirstOrDefaultAsync(q => q.Id == dto.Id);
        Assert.NotNull(inDb);
        Assert.Single(inDb!.Votes);
        Assert.Equal(user.Id, inDb.Votes.First().UserId);
    }

    [Fact]
    public async Task EditQuestion_NullPayload_ThrowsArgumentNullException()
    {
        await using var context = CreateInMemoryContext();
        var svc = new QuestionsService(context);

        await Assert.ThrowsAsync<ArgumentNullException>(() => svc.EditQuestion(Guid.NewGuid(), null!));
    }

    [Fact]
    public async Task EditQuestion_NonExistent_ThrowsKeyNotFoundException()
    {
        await using var context = CreateInMemoryContext();
        var svc = new QuestionsService(context);
        var payload = new QuestionPayload { Title = "X", Content = "Y" };

        await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.EditQuestion(Guid.NewGuid(), payload));
    }

    [Fact]
    public async Task EditQuestion_UpdatesContentAndUpdatedAt()
    {
        await using var context = CreateInMemoryContext();
        var user = new CrowdsageUser { Id = "qeUser", UserName = "qe" };
        await context.Users.AddAsync(user);

        var question = new Question { Title = "old", Content = "oldc", CreatedAt = DateTimeOffset.UtcNow.AddHours(-2), UpdatedAt = DateTimeOffset.UtcNow.AddHours(-2), AuthorId = user.Id, Author = user, Tags = new System.Collections.Generic.List<string>(), Answers = new System.Collections.Generic.List<Answer>(), Votes = new System.Collections.Generic.List<QuestionVote>(), Comments = new System.Collections.Generic.List<QuestionComment>() };
        await context.Questions.AddAsync(question);
        await context.SaveChangesAsync();

        var svc = new QuestionsService(context);
        var payload = new QuestionPayload { Title = "newt", Content = "newc" };

        var beforeUpdated = question.UpdatedAt;
        await svc.EditQuestion(question.Id, payload);

        var updated = await context.Questions.FindAsync(question.Id);
        Assert.NotNull(updated);
        Assert.Equal(payload.Content, updated!.Content);
        Assert.True(updated.UpdatedAt > beforeUpdated);
    }

    [Fact]
    public async Task DeleteQuestion_NonExistent_ThrowsKeyNotFoundException()
    {
        await using var context = CreateInMemoryContext();
        var svc = new QuestionsService(context);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.DeleteQuestion(Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteQuestion_RemovesQuestion()
    {
        await using var context = CreateInMemoryContext();
        var user = new CrowdsageUser { Id = "qdUser", UserName = "qd" };
        await context.Users.AddAsync(user);

        var question = new Question { Title = "t", Content = "c", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, AuthorId = user.Id, Author = user, Tags = new System.Collections.Generic.List<string>(), Answers = new System.Collections.Generic.List<Answer>(), Votes = new System.Collections.Generic.List<QuestionVote>(), Comments = new System.Collections.Generic.List<QuestionComment>() };
        await context.Questions.AddAsync(question);
        await context.SaveChangesAsync();

        var svc = new QuestionsService(context);
        await svc.DeleteQuestion(question.Id);

        var exists = await context.Questions.AnyAsync(q => q.Id == question.Id);
        Assert.False(exists);
    }

    [Fact]
    public async Task BookmarkQuestion_AddsBookmark()
    {
        await using var context = CreateInMemoryContext();
        var user = new CrowdsageUser { Id = "qbUser", UserName = "qb" };
        await context.Users.AddAsync(user);

        var question = new Question { Title = "t", Content = "c", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, AuthorId = user.Id, Author = user, Tags = new System.Collections.Generic.List<string>(), Answers = new System.Collections.Generic.List<Answer>(), Votes = new System.Collections.Generic.List<QuestionVote>(), Comments = new System.Collections.Generic.List<QuestionComment>() };
        await context.Questions.AddAsync(question);
        await context.SaveChangesAsync();

        var svc = new QuestionsService(context);
        svc.BookmarkQuestion(question.Id, user.Id);

        var saved = await context.QuestionBookmarks.FirstOrDefaultAsync(b => b.QuestionId == question.Id && b.UserId == user.Id);
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task BookmarkQuestion_AllowsDuplicateBookmarks()
    {
        await using var context = CreateInMemoryContext();
        var user = new CrowdsageUser { Id = "qbUser2", UserName = "qb2" };
        await context.Users.AddAsync(user);

        var question = new Question { Title = "t", Content = "c", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, AuthorId = user.Id, Author = user, Tags = new System.Collections.Generic.List<string>(), Answers = new System.Collections.Generic.List<Answer>(), Votes = new System.Collections.Generic.List<QuestionVote>(), Comments = new System.Collections.Generic.List<QuestionComment>() };
        await context.Questions.AddAsync(question);
        await context.SaveChangesAsync();

        var svc = new QuestionsService(context);
        svc.BookmarkQuestion(question.Id, user.Id);
        svc.BookmarkQuestion(question.Id, user.Id);

        var bookmarks = await context.QuestionBookmarks.Where(b => b.QuestionId == question.Id && b.UserId == user.Id).ToListAsync();
        Assert.Equal(2, bookmarks.Count);
    }

    [Fact]
    public async Task RemoveBookmarkFromQuestion_NonExistent_ThrowsKeyNotFoundException()
    {
        await using var context = CreateInMemoryContext();
        var svc = new QuestionsService(context);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => Task.Run(() => svc.RemoveBookmarkFromQuestion(Guid.NewGuid(), "no-user")));
    }

    [Fact]
    public async Task RemoveBookmarkFromQuestion_RemovesExistingBookmark()
    {
        await using var context = CreateInMemoryContext();
        var user = new CrowdsageUser { Id = "rbQ1", UserName = "rbq1" };
        await context.Users.AddAsync(user);

        var question = new Question { Title = "t", Content = "c", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, AuthorId = user.Id, Author = user, Tags = new System.Collections.Generic.List<string>(), Answers = new System.Collections.Generic.List<Answer>(), Votes = new System.Collections.Generic.List<QuestionVote>(), Comments = new System.Collections.Generic.List<QuestionComment>() };
        await context.Questions.AddAsync(question);
        await context.SaveChangesAsync();

        var bookmark = new QuestionBookmark { QuestionId = question.Id, UserId = user.Id };
        await context.QuestionBookmarks.AddAsync(bookmark);
        await context.SaveChangesAsync();

        var svc = new QuestionsService(context);
        svc.RemoveBookmarkFromQuestion(question.Id, user.Id);

        var exists = await context.QuestionBookmarks.AnyAsync(b => b.QuestionId == question.Id && b.UserId == user.Id);
        Assert.False(exists);
    }

    [Fact]
    public async Task VoteOnQuestion_NonExistent_ThrowsKeyNotFoundException()
    {
        await using var context = CreateInMemoryContext();
        var svc = new QuestionsService(context);
        var input = new VoteInput { Vote = Models.Enums.VoteValue.Upvote };

        await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.VoteOnQuestion(Guid.NewGuid(), "u-x", input));
    }

    [Fact]
    public async Task VoteOnQuestion_AddsVote_WhenNoExistingVote()
    {
        await using var context = CreateInMemoryContext();
        var user = new CrowdsageUser { Id = "vQUser1", UserName = "vqu1" };
        await context.Users.AddAsync(user);

        var question = new Question { Title = "t", Content = "c", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, AuthorId = user.Id, Author = user, Tags = new System.Collections.Generic.List<string>(), Answers = new System.Collections.Generic.List<Answer>(), Votes = new System.Collections.Generic.List<QuestionVote>(), Comments = new System.Collections.Generic.List<QuestionComment>() };
        await context.Questions.AddAsync(question);
        await context.SaveChangesAsync();

        var svc = new QuestionsService(context);
        var input = new VoteInput { Vote = Models.Enums.VoteValue.Upvote };
        await svc.VoteOnQuestion(question.Id, user.Id, input);

        var saved = await context.QuestionVotes.FirstOrDefaultAsync(v => v.QuestionId == question.Id && v.UserId == user.Id);
        Assert.NotNull(saved);
        Assert.Equal(Models.Enums.VoteValue.Upvote, saved!.Vote);
    }

    [Fact]
    public async Task VoteOnQuestion_UpdatesExistingVote()
    {
        await using var context = CreateInMemoryContext();
        var user = new CrowdsageUser { Id = "vQUser2", UserName = "vqu2" };
        await context.Users.AddAsync(user);

        var question = new Question { Title = "t", Content = "c", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, AuthorId = user.Id, Author = user, Tags = new System.Collections.Generic.List<string>(), Answers = new System.Collections.Generic.List<Answer>(), Votes = new System.Collections.Generic.List<QuestionVote>(), Comments = new System.Collections.Generic.List<QuestionComment>() };
        await context.Questions.AddAsync(question);
        await context.SaveChangesAsync();

        var initial = new QuestionVote { QuestionId = question.Id, UserId = user.Id, Vote = Models.Enums.VoteValue.Upvote };
        await context.QuestionVotes.AddAsync(initial);
        await context.SaveChangesAsync();

        var svc = new QuestionsService(context);
        var input = new VoteInput { Vote = Models.Enums.VoteValue.Neutral };
        await svc.VoteOnQuestion(question.Id, user.Id, input);

        var saved = await context.QuestionVotes.FirstOrDefaultAsync(v => v.QuestionId == question.Id && v.UserId == user.Id);
        Assert.NotNull(saved);
        Assert.Equal(Models.Enums.VoteValue.Neutral, saved!.Vote);

        var count = await context.QuestionVotes.CountAsync(v => v.QuestionId == question.Id && v.UserId == user.Id);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task GetBookmarkedQuestions_NoBookmarks_ReturnsEmptyList()
    {
        await using var context = CreateInMemoryContext();
        var svc = new QuestionsService(context);

        var list = await svc.GetBookmarkedQuestions("no-user");
        Assert.NotNull(list);
        Assert.Empty(list);
    }

    [Fact]
    public async Task GetBookmarkedQuestions_ReturnsBookmarked_WithAuthors()
    {
        await using var context = CreateInMemoryContext();
        var user = new CrowdsageUser { Id = "bqU1", UserName = "bq1" };
        await context.Users.AddAsync(user);

        var question = new Question { Title = "t", Content = "c", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, AuthorId = user.Id, Author = user, Tags = new System.Collections.Generic.List<string>(), Answers = new System.Collections.Generic.List<Answer>(), Votes = new System.Collections.Generic.List<QuestionVote>(), Comments = new System.Collections.Generic.List<QuestionComment>() };
        await context.Questions.AddAsync(question);
        await context.SaveChangesAsync();

        var bm = new QuestionBookmark { QuestionId = question.Id, UserId = user.Id };
        await context.QuestionBookmarks.AddAsync(bm);
        await context.SaveChangesAsync();

        var svc = new QuestionsService(context);
        var list = await svc.GetBookmarkedQuestions(user.Id);

        Assert.Single(list);
        Assert.True(list.First().Bookmarked);
        Assert.NotNull(list.First().Author);
    }
}
