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
        Assert.Equal(Models.Enums.VoteValue.Upvote, dto.CurrentUserVote);

        var inDb = await context.Questions.Include(q => q.Votes).FirstOrDefaultAsync(q => q.Id == dto.Id);
        Assert.NotNull(inDb);
        Assert.Single(inDb!.Votes);
        Assert.Equal(user.Id, inDb.Votes.First().UserId);
    }

    [Fact]
    public async Task GetQuestionById_ReturnsDto_WithCorrectCurrentUserVote()
    {
        await using var context = CreateInMemoryContext();
        var author = new CrowdsageUser { Id = "author", UserName = "author" };
        var voter = new CrowdsageUser { Id = "voter", UserName = "voter" };
        var nonVoter = new CrowdsageUser { Id = "nonVoter", UserName = "nonVoter" };
        await context.Users.AddRangeAsync(author, voter, nonVoter);

        var question = new Question
        {
            Id = Guid.NewGuid(),
            Title = "Title",
            Content = "Content",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            AuthorId = author.Id,
            Author = author,
            Tags = new System.Collections.Generic.List<string>(),
            Answers = new System.Collections.Generic.List<Answer>(),
            Votes = new System.Collections.Generic.List<QuestionVote>(),
            Comments = new System.Collections.Generic.List<QuestionComment>()
        };
        await context.Questions.AddAsync(question);

        var vote = new QuestionVote { QuestionId = question.Id, UserId = voter.Id, Vote = Models.Enums.VoteValue.Upvote };
        await context.QuestionVotes.AddAsync(vote);
        await context.SaveChangesAsync();

        var svc = new QuestionsService(context);

        // Case 1: No user logged in (userId is null) -> Expect Neutral
        var dtoNoUser = svc.GetQuestionById(question.Id, null);
        Assert.Equal(Models.Enums.VoteValue.Neutral, dtoNoUser.CurrentUserVote);

        // Case 2: User logged in and voted (voter) -> Expect Upvote
        var dtoVoter = svc.GetQuestionById(question.Id, voter.Id);
        Assert.Equal(Models.Enums.VoteValue.Upvote, dtoVoter.CurrentUserVote);

        // Case 3: User logged in but hasn't voted (nonVoter) -> Expect Neutral
        var dtoNonVoter = svc.GetQuestionById(question.Id, nonVoter.Id);
        Assert.Equal(Models.Enums.VoteValue.Neutral, dtoNonVoter.CurrentUserVote);
    }

    [Fact]
    public async Task GetQuestionById_ReturnsDto_WithCorrectBookmarkedStatus()
    {
        await using var context = CreateInMemoryContext();
        var author = new CrowdsageUser { Id = "author", UserName = "author" };
        var bookmarker = new CrowdsageUser { Id = "bookmarker", UserName = "bookmarker" };
        var nonBookmarker = new CrowdsageUser { Id = "nonBookmarker", UserName = "nonBookmarker" };
        await context.Users.AddRangeAsync(author, bookmarker, nonBookmarker);

        var question = new Question
        {
            Id = Guid.NewGuid(),
            Title = "Title",
            Content = "Content",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            AuthorId = author.Id,
            Author = author,
            Tags = new System.Collections.Generic.List<string>(),
            Answers = new System.Collections.Generic.List<Answer>(),
            Votes = new System.Collections.Generic.List<QuestionVote>(),
            Comments = new System.Collections.Generic.List<QuestionComment>(),
            UserBookmarks = new System.Collections.Generic.List<QuestionBookmark>()
        };
        await context.Questions.AddAsync(question);
        await context.SaveChangesAsync();

        var bookmark = new QuestionBookmark { QuestionId = question.Id, UserId = bookmarker.Id };
        await context.QuestionBookmarks.AddAsync(bookmark);
        await context.SaveChangesAsync();

        var svc = new QuestionsService(context);

        // Case 1: Bookmarker logged in
        var dtoBookmarker = svc.GetQuestionById(question.Id, bookmarker.Id);
        Assert.True(dtoBookmarker.Bookmarked);

        // Case 2: Non-bookmarker logged in
        var dtoNonBookmarker = svc.GetQuestionById(question.Id, nonBookmarker.Id);
        Assert.False(dtoNonBookmarker.Bookmarked);

        // Case 3: No user logged in
        var dtoNoUser = svc.GetQuestionById(question.Id, null);
        Assert.False(dtoNoUser.Bookmarked);
    }

    [Fact]
    public async Task GetNewQuestionsAsync_ReturnsDtos_WithCorrectCurrentUserVote()
    {
        await using var context = CreateInMemoryContext();
        var author = new CrowdsageUser { Id = "author", UserName = "author" };
        var voter = new CrowdsageUser { Id = "voter", UserName = "voter" };
        var nonVoter = new CrowdsageUser { Id = "nonVoter", UserName = "nonVoter" };
        await context.Users.AddRangeAsync(author, voter, nonVoter);

        var q1 = new Question
        {
            Id = Guid.NewGuid(),
            Title = "Q1",
            Content = "C1",
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-10),
            UpdatedAt = DateTimeOffset.UtcNow,
            AuthorId = author.Id,
            Author = author,
            Tags = new System.Collections.Generic.List<string>(),
            Answers = new System.Collections.Generic.List<Answer>(),
            Votes = new System.Collections.Generic.List<QuestionVote>(),
            Comments = new System.Collections.Generic.List<QuestionComment>()
        };
        var q2 = new Question
        {
            Id = Guid.NewGuid(),
            Title = "Q2",
            Content = "C2",
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            UpdatedAt = DateTimeOffset.UtcNow,
            AuthorId = author.Id,
            Author = author,
            Tags = new System.Collections.Generic.List<string>(),
            Answers = new System.Collections.Generic.List<Answer>(),
            Votes = new System.Collections.Generic.List<QuestionVote>(),
            Comments = new System.Collections.Generic.List<QuestionComment>()
        };
        await context.Questions.AddRangeAsync(q1, q2);

        var vote = new QuestionVote { QuestionId = q1.Id, UserId = voter.Id, Vote = Models.Enums.VoteValue.Upvote };
        await context.QuestionVotes.AddAsync(vote);
        await context.SaveChangesAsync();

        var svc = new QuestionsService(context);

        // Case 1: No user logged in
        var listNoUser = await svc.GetNewQuestionsAsync(null);
        var dtoQ1_NoUser = listNoUser.First(q => q.Id == q1.Id);
        var dtoQ2_NoUser = listNoUser.First(q => q.Id == q2.Id);
        
        Assert.Equal(Models.Enums.VoteValue.Neutral, dtoQ1_NoUser.CurrentUserVote);
        Assert.Equal(Models.Enums.VoteValue.Neutral, dtoQ2_NoUser.CurrentUserVote);

        // Case 2: Voter logged in
        var listVoter = await svc.GetNewQuestionsAsync(voter.Id);
        var dtoQ1_Voter = listVoter.First(q => q.Id == q1.Id);
        var dtoQ2_Voter = listVoter.First(q => q.Id == q2.Id);

        Assert.Equal(Models.Enums.VoteValue.Upvote, dtoQ1_Voter.CurrentUserVote);
        Assert.Equal(Models.Enums.VoteValue.Neutral, dtoQ2_Voter.CurrentUserVote);

        // Case 3: Non-voter logged in
        var listNonVoter = await svc.GetNewQuestionsAsync(nonVoter.Id);
        var dtoQ1_NonVoter = listNonVoter.First(q => q.Id == q1.Id);
        var dtoQ2_NonVoter = listNonVoter.First(q => q.Id == q2.Id);

        Assert.Equal(Models.Enums.VoteValue.Neutral, dtoQ1_NonVoter.CurrentUserVote);
        Assert.Equal(Models.Enums.VoteValue.Neutral, dtoQ2_NonVoter.CurrentUserVote);
    }

    [Fact]
    public async Task GetNewQuestionsAsync_ReturnsDtos_WithCorrectBookmarkedStatus()
    {
        await using var context = CreateInMemoryContext();
        var author = new CrowdsageUser { Id = "author", UserName = "author" };
        var bookmarker = new CrowdsageUser { Id = "bookmarker", UserName = "bookmarker" };
        var nonBookmarker = new CrowdsageUser { Id = "nonBookmarker", UserName = "nonBookmarker" };
        await context.Users.AddRangeAsync(author, bookmarker, nonBookmarker);

        var q1 = new Question
        {
            Id = Guid.NewGuid(),
            Title = "Q1",
            Content = "C1",
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-10),
            UpdatedAt = DateTimeOffset.UtcNow,
            AuthorId = author.Id,
            Author = author,
            Tags = new System.Collections.Generic.List<string>(),
            Answers = new System.Collections.Generic.List<Answer>(),
            Votes = new System.Collections.Generic.List<QuestionVote>(),
            Comments = new System.Collections.Generic.List<QuestionComment>(),
            UserBookmarks = new System.Collections.Generic.List<QuestionBookmark>()
        };
        await context.Questions.AddAsync(q1);

        var bookmark = new QuestionBookmark { QuestionId = q1.Id, UserId = bookmarker.Id };
        await context.QuestionBookmarks.AddAsync(bookmark);
        await context.SaveChangesAsync();

        var svc = new QuestionsService(context);

        // Case 1: Bookmarker logged in
        var listBookmarker = await svc.GetNewQuestionsAsync(bookmarker.Id);
        var dtoQ1_Bookmarker = listBookmarker.First(q => q.Id == q1.Id);
        Assert.True(dtoQ1_Bookmarker.Bookmarked);

        // Case 2: Non-bookmarker logged in
        var listNonBookmarker = await svc.GetNewQuestionsAsync(nonBookmarker.Id);
        var dtoQ1_NonBookmarker = listNonBookmarker.First(q => q.Id == q1.Id);
        Assert.False(dtoQ1_NonBookmarker.Bookmarked);

        // Case 3: No user logged in
        var listNoUser = await svc.GetNewQuestionsAsync(null);
        var dtoQ1_NoUser = listNoUser.First(q => q.Id == q1.Id);
        Assert.False(dtoQ1_NoUser.Bookmarked);
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
    public async Task DeleteQuestion_RemovesAllAssociatedBookmarks()
    {
        await using var context = CreateInMemoryContext();
        var user1 = new CrowdsageUser { Id = "bmDelQ1", UserName = "u1" };
        var user2 = new CrowdsageUser { Id = "bmDelQ2", UserName = "u2" };
        await context.Users.AddRangeAsync(user1, user2);

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
            AuthorId = user1.Id,
            Author = user1
        };
        await context.Questions.AddAsync(question);
        await context.SaveChangesAsync();

        // Add bookmarks from multiple users
        var bm1 = new QuestionBookmark { QuestionId = question.Id, UserId = user1.Id };
        var bm2 = new QuestionBookmark { QuestionId = question.Id, UserId = user2.Id };
        await context.QuestionBookmarks.AddRangeAsync(bm1, bm2);
        await context.SaveChangesAsync();

        var svc = new QuestionsService(context);
        await svc.DeleteQuestion(question.Id);

        var bookmarks = await context.QuestionBookmarks.Where(b => b.QuestionId == question.Id).ToListAsync();
        Assert.Empty(bookmarks);
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
