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
        Assert.Equal(Models.Enums.VoteValue.Upvote, dto.CurrentUserVote);

        // Assert DB state
        var answersInDb = context.Answers.Include(a => a.Votes).Where(a => a.QuestionId == question.Id).ToList();
        Assert.Single(answersInDb);
        var added = answersInDb.Single();
        Assert.Equal(payload.Content, added.Content);
        Assert.Single(added.Votes);
        Assert.Equal(user.Id, added.Votes.First().UserId);
    }

    [Fact]
    public async Task AddAnswerAsync_MissingQuestion_ThrowsKeyNotFoundException()
    {
        await using var context = CreateInMemoryContext();
        var user = new CrowdsageUser { Id = "user2", UserName = "u2" };
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();

        var svc = new AnswersService(context);
        var payload = new AnswerPayload { Content = "x" };

        await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.AddAnswerAsync(payload, Guid.NewGuid(), user.Id));
    }

    [Fact]
    public async Task GetAnswersForQuestion_ReturnsDtos_WithCorrectCurrentUserVote()
    {
        await using var context = CreateInMemoryContext();
        var author = new CrowdsageUser { Id = "author", UserName = "author" };
        var voter = new CrowdsageUser { Id = "voter", UserName = "voter" };
        var nonVoter = new CrowdsageUser { Id = "nonVoter", UserName = "nonVoter" };
        await context.Users.AddRangeAsync(author, voter, nonVoter);

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
            AuthorId = author.Id,
            Author = author
        };
        await context.Questions.AddAsync(question);

        var answer = new Answer
        {
            Content = "Ans",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            AuthorId = author.Id,
            Author = author,
            QuestionId = question.Id,
            Question = question,
            Votes = new System.Collections.Generic.List<AnswerVote>(),
            Comments = new System.Collections.Generic.List<AnswerComment>()
        };
        await context.Answers.AddAsync(answer);

        var vote = new AnswerVote { AnswerId = answer.Id, UserId = voter.Id, Vote = Models.Enums.VoteValue.Upvote };
        await context.AnswerVotes.AddAsync(vote);
        await context.SaveChangesAsync();

        var svc = new AnswersService(context);

        // Case 1: No user logged in
        var answersNoUser = await svc.GetAnswersForQuestion(question.Id, null);
        var dtoNoUser = answersNoUser.First();
        Assert.Equal(Models.Enums.VoteValue.Neutral, dtoNoUser.CurrentUserVote);

        // Case 2: Voter logged in
        var answersVoter = await svc.GetAnswersForQuestion(question.Id, voter.Id);
        var dtoVoter = answersVoter.First();
        Assert.Equal(Models.Enums.VoteValue.Upvote, dtoVoter.CurrentUserVote);

        // Case 3: Non-voter logged in
        var answersNonVoter = await svc.GetAnswersForQuestion(question.Id, nonVoter.Id);
        var dtoNonVoter = answersNonVoter.First();
        Assert.Equal(Models.Enums.VoteValue.Neutral, dtoNonVoter.CurrentUserVote);
    }

    [Fact]
    public async Task GetBookmarkedAnswers_ReturnsDtosWithBookmarkedTrue()
    {
        await using var context = CreateInMemoryContext();
        var user = new CrowdsageUser { Id = "user3", UserName = "u3" };
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

        var answer = new Answer
        {
            Content = "ans",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Author = user,
            AuthorId = user.Id,
            Question = question,
            QuestionId = question.Id,
            Votes = new System.Collections.Generic.List<AnswerVote>(),
            Comments = new System.Collections.Generic.List<AnswerComment>()
        };
        await context.Answers.AddAsync(answer);
        await context.SaveChangesAsync();

        // Add bookmark
        var bookmark = new AnswerBookmark { AnswerId = answer.Id, UserId = user.Id };
        await context.AnswerBookmarks.AddAsync(bookmark);
        await context.SaveChangesAsync();

        var svc = new AnswersService(context);
        var bookmarked = await svc.GetBookmarkedAnswers(user.Id);

        Assert.Single(bookmarked);
        var dto = bookmarked.First();
        Assert.True(dto.Bookmarked);
        Assert.Equal(answer.Content, dto.Content);
        Assert.Equal(Models.Enums.VoteValue.Neutral, dto.CurrentUserVote);
    }

    [Fact]
    public async Task EditAnswer_NullPayload_ThrowsArgumentNullException()
    {
        await using var context = CreateInMemoryContext();
        var svc = new AnswersService(context);

        await Assert.ThrowsAsync<ArgumentNullException>(() => svc.EditAnswer(Guid.NewGuid(), null!));
    }

    [Fact]
    public async Task EditAnswer_NonExistentAnswer_ThrowsKeyNotFoundException()
    {
        await using var context = CreateInMemoryContext();
        var svc = new AnswersService(context);
        var payload = new AnswerPayload { Content = "new" };

        await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.EditAnswer(Guid.NewGuid(), payload));
    }

    [Fact]
    public async Task EditAnswer_UpdatesContentAndUpdatedAt_PreservesOtherFields()
    {
        await using var context = CreateInMemoryContext();

        var user = new CrowdsageUser { Id = "editUser", UserName = "editor" };
        await context.Users.AddAsync(user);

        var question = new Question
        {
            Id = Guid.NewGuid(),
            Title = "Q",
            Content = "C",
            CreatedAt = DateTimeOffset.UtcNow.AddHours(-2),
            UpdatedAt = DateTimeOffset.UtcNow.AddHours(-2),
            Tags = new System.Collections.Generic.List<string>(),
            Answers = new System.Collections.Generic.List<Answer>(),
            Votes = new System.Collections.Generic.List<QuestionVote>(),
            Comments = new System.Collections.Generic.List<QuestionComment>(),
            AuthorId = user.Id,
            Author = user
        };
        await context.Questions.AddAsync(question);

        var answer = new Answer
        {
            Content = "original",
            CreatedAt = DateTimeOffset.UtcNow.AddHours(-1),
            UpdatedAt = DateTimeOffset.UtcNow.AddHours(-1),
            Author = user,
            AuthorId = user.Id,
            Question = question,
            QuestionId = question.Id,
            Votes = new System.Collections.Generic.List<AnswerVote> { new AnswerVote { UserId = user.Id, Vote = Models.Enums.VoteValue.Upvote } },
            Comments = new System.Collections.Generic.List<AnswerComment>()
        };
        await context.Answers.AddAsync(answer);
        await context.SaveChangesAsync();

        var svc = new AnswersService(context);
        var payload = new AnswerPayload { Content = "updated content" };

        var beforeCreated = answer.CreatedAt;
        var beforeUpdated = answer.UpdatedAt;

        await svc.EditAnswer(answer.Id, payload);

        var updated = await context.Answers.Include(a => a.Votes).FirstOrDefaultAsync(a => a.Id == answer.Id);
        Assert.NotNull(updated);
        Assert.Equal(payload.Content, updated!.Content);
        Assert.Equal(beforeCreated, updated.CreatedAt);
        Assert.True(updated.UpdatedAt > beforeUpdated);
        // Ensure author and votes not changed
        Assert.Equal(user.Id, updated.AuthorId);
        Assert.Single(updated.Votes);
        Assert.Equal(Models.Enums.VoteValue.Upvote, updated.Votes.First().Vote);
    }

    [Fact]
    public async Task DeleteAnswer_NonExistent_ThrowsKeyNotFoundException()
    {
        await using var context = CreateInMemoryContext();
        var svc = new AnswersService(context);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.DeleteAnswer(Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteAnswer_RemovesAnswerAndRelatedVotesAndBookmarks()
    {
        await using var context = CreateInMemoryContext();

        var user = new CrowdsageUser { Id = "delUser", UserName = "deleter" };
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

        var answerToDelete = new Answer
        {
            Content = "to be deleted",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Author = user,
            AuthorId = user.Id,
            Question = question,
            QuestionId = question.Id,
            Votes = new System.Collections.Generic.List<AnswerVote>(),
            Comments = new System.Collections.Generic.List<AnswerComment>()
        };
        var answerToKeep = new Answer
        {
            Content = "keep",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Author = user,
            AuthorId = user.Id,
            Question = question,
            QuestionId = question.Id,
            Votes = new System.Collections.Generic.List<AnswerVote>(),
            Comments = new System.Collections.Generic.List<AnswerComment>()
        };

        await context.Answers.AddAsync(answerToDelete);
        await context.Answers.AddAsync(answerToKeep);
        await context.SaveChangesAsync();

        // Add a vote and a bookmark for the answer to delete
        var vote = new AnswerVote { AnswerId = answerToDelete.Id, UserId = user.Id, Vote = Models.Enums.VoteValue.Upvote };
        await context.AnswerVotes.AddAsync(vote);
        var bookmark = new AnswerBookmark { AnswerId = answerToDelete.Id, UserId = user.Id };
        await context.AnswerBookmarks.AddAsync(bookmark);
        await context.SaveChangesAsync();

        var svc = new AnswersService(context);

        // Act
        await svc.DeleteAnswer(answerToDelete.Id);

        // Assert: answer removed
        Assert.False(await context.Answers.AnyAsync(a => a.Id == answerToDelete.Id));
        // votes and bookmarks removed
        Assert.False(await context.AnswerVotes.AnyAsync(v => v.AnswerId == answerToDelete.Id));
        Assert.False(await context.AnswerBookmarks.AnyAsync(b => b.AnswerId == answerToDelete.Id));
        // ensure other answer remains
        Assert.True(await context.Answers.AnyAsync(a => a.Id == answerToKeep.Id));
    }

    [Fact]
    public async Task BookmarkAnswer_AddsBookmark_WhenAnswerExists()
    {
        await using var context = CreateInMemoryContext();
        var user = new CrowdsageUser { Id = "bmUser1", UserName = "bm1" };
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

        var answer = new Answer
        {
            Content = "ans",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Author = user,
            AuthorId = user.Id,
            Question = question,
            QuestionId = question.Id,
            Votes = new System.Collections.Generic.List<AnswerVote>(),
            Comments = new System.Collections.Generic.List<AnswerComment>()
        };
        await context.Answers.AddAsync(answer);
        await context.SaveChangesAsync();

        var svc = new AnswersService(context);
        svc.BookmarkAnswer(answer.Id, user.Id);

        var saved = await context.AnswerBookmarks.FirstOrDefaultAsync(b => b.AnswerId == answer.Id && b.UserId == user.Id);
        Assert.NotNull(saved);
        Assert.Equal(answer.Id, saved!.AnswerId);
        Assert.Equal(user.Id, saved.UserId);
    }

    [Fact]
    public async Task BookmarkAnswer_AllowsDuplicateBookmarks()
    {
        await using var context = CreateInMemoryContext();
        var user = new CrowdsageUser { Id = "bmUser2", UserName = "bm2" };
        await context.Users.AddAsync(user);

        var question = new Question { Id = Guid.NewGuid(), Title = "Q", Content = "C", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, Tags = new System.Collections.Generic.List<string>(), Answers = new System.Collections.Generic.List<Answer>(), Votes = new System.Collections.Generic.List<QuestionVote>(), Comments = new System.Collections.Generic.List<QuestionComment>(), AuthorId = user.Id, Author = user };
        await context.Questions.AddAsync(question);

        var answer = new Answer { Content = "ans", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, Author = user, AuthorId = user.Id, Question = question, QuestionId = question.Id, Votes = new System.Collections.Generic.List<AnswerVote>(), Comments = new System.Collections.Generic.List<AnswerComment>() };
        await context.Answers.AddAsync(answer);
        await context.SaveChangesAsync();

        var svc = new AnswersService(context);
        svc.BookmarkAnswer(answer.Id, user.Id);
        svc.BookmarkAnswer(answer.Id, user.Id);

        var bookmarks = await context.AnswerBookmarks.Where(b => b.AnswerId == answer.Id && b.UserId == user.Id).ToListAsync();
        Assert.Equal(2, bookmarks.Count);
    }

    [Fact]
    public async Task BookmarkAnswer_NonExistentAnswer_CreatesBookmarkRecord()
    {
        await using var context = CreateInMemoryContext();
        var user = new CrowdsageUser { Id = "bmUser3", UserName = "bm3" };
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();

        var svc = new AnswersService(context);
        var randomAnswerId = Guid.NewGuid();
        svc.BookmarkAnswer(randomAnswerId, user.Id);

        var saved = await context.AnswerBookmarks.FirstOrDefaultAsync(b => b.AnswerId == randomAnswerId && b.UserId == user.Id);
        Assert.NotNull(saved);
        Assert.Equal(randomAnswerId, saved!.AnswerId);
    }

    [Fact]
    public async Task RemoveBookmarkFromAnswer_NonExistent_ThrowsKeyNotFoundException()
    {
        await using var context = CreateInMemoryContext();
        var svc = new AnswersService(context);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => Task.Run(() => svc.RemoveBookmarkFromAnswer(Guid.NewGuid(), "no-user")));
    }

    [Fact]
    public async Task RemoveBookmarkFromAnswer_RemovesExistingBookmark()
    {
        await using var context = CreateInMemoryContext();
        var user = new CrowdsageUser { Id = "rbUser1", UserName = "rb1" };
        await context.Users.AddAsync(user);

        var question = new Question { Id = Guid.NewGuid(), Title = "Q", Content = "C", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, Tags = new System.Collections.Generic.List<string>(), Answers = new System.Collections.Generic.List<Answer>(), Votes = new System.Collections.Generic.List<QuestionVote>(), Comments = new System.Collections.Generic.List<QuestionComment>(), AuthorId = user.Id, Author = user };
        await context.Questions.AddAsync(question);

        var answer = new Answer { Content = "ans", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, Author = user, AuthorId = user.Id, Question = question, QuestionId = question.Id, Votes = new System.Collections.Generic.List<AnswerVote>(), Comments = new System.Collections.Generic.List<AnswerComment>() };
        await context.Answers.AddAsync(answer);
        await context.SaveChangesAsync();

        var bookmark = new AnswerBookmark { AnswerId = answer.Id, UserId = user.Id };
        await context.AnswerBookmarks.AddAsync(bookmark);
        await context.SaveChangesAsync();

        var svc = new AnswersService(context);
        svc.RemoveBookmarkFromAnswer(answer.Id, user.Id);

        var exists = await context.AnswerBookmarks.AnyAsync(b => b.AnswerId == answer.Id && b.UserId == user.Id);
        Assert.False(exists);
    }

    [Fact]
    public async Task RemoveBookmarkFromAnswer_WithDuplicates_RemovesOnlyOne()
    {
        await using var context = CreateInMemoryContext();
        var user = new CrowdsageUser { Id = "rbUser2", UserName = "rb2" };
        await context.Users.AddAsync(user);

        var question = new Question { Id = Guid.NewGuid(), Title = "Q", Content = "C", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, Tags = new System.Collections.Generic.List<string>(), Answers = new System.Collections.Generic.List<Answer>(), Votes = new System.Collections.Generic.List<QuestionVote>(), Comments = new System.Collections.Generic.List<QuestionComment>(), AuthorId = user.Id, Author = user };
        await context.Questions.AddAsync(question);

        var answer = new Answer { Content = "ans", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, Author = user, AuthorId = user.Id, Question = question, QuestionId = question.Id, Votes = new System.Collections.Generic.List<AnswerVote>(), Comments = new System.Collections.Generic.List<AnswerComment>() };
        await context.Answers.AddAsync(answer);
        await context.SaveChangesAsync();

        // insert duplicate bookmarks
        var bm1 = new AnswerBookmark { AnswerId = answer.Id, UserId = user.Id };
        var bm2 = new AnswerBookmark { AnswerId = answer.Id, UserId = user.Id };
        await context.AnswerBookmarks.AddAsync(bm1);
        await context.AnswerBookmarks.AddAsync(bm2);
        await context.SaveChangesAsync();

        var svc = new AnswersService(context);
        svc.RemoveBookmarkFromAnswer(answer.Id, user.Id);

        var remaining = await context.AnswerBookmarks.Where(b => b.AnswerId == answer.Id && b.UserId == user.Id).ToListAsync();
        Assert.Single(remaining);
    }

    [Fact]
    public async Task VoteOnAnswer_NonExistentAnswer_ThrowsKeyNotFoundException()
    {
        await using var context = CreateInMemoryContext();
        var svc = new AnswersService(context);

        var input = new CrowdSage.Server.Models.InsertUpdate.VoteInput { Vote = Models.Enums.VoteValue.Upvote };
        await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.VoteOnAnswer(Guid.NewGuid(), "u-x", input));
    }

    [Fact]
    public async Task VoteOnAnswer_AddsVote_WhenNoExistingVote()
    {
        await using var context = CreateInMemoryContext();
        var user = new CrowdsageUser { Id = "vUser1", UserName = "v1" };
        await context.Users.AddAsync(user);

        var question = new Question { Id = Guid.NewGuid(), Title = "Q", Content = "C", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, Tags = new System.Collections.Generic.List<string>(), Answers = new System.Collections.Generic.List<Answer>(), Votes = new System.Collections.Generic.List<QuestionVote>(), Comments = new System.Collections.Generic.List<QuestionComment>(), AuthorId = user.Id, Author = user };
        await context.Questions.AddAsync(question);

        var answer = new Answer { Content = "ans", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, Author = user, AuthorId = user.Id, Question = question, QuestionId = question.Id, Votes = new System.Collections.Generic.List<AnswerVote>(), Comments = new System.Collections.Generic.List<AnswerComment>() };
        await context.Answers.AddAsync(answer);
        await context.SaveChangesAsync();

        var svc = new AnswersService(context);
        var input = new CrowdSage.Server.Models.InsertUpdate.VoteInput { Vote = Models.Enums.VoteValue.Upvote };
        await svc.VoteOnAnswer(answer.Id, user.Id, input);

        var saved = await context.AnswerVotes.FirstOrDefaultAsync(v => v.AnswerId == answer.Id && v.UserId == user.Id);
        Assert.NotNull(saved);
        Assert.Equal(Models.Enums.VoteValue.Upvote, saved!.Vote);
    }

    [Fact]
    public async Task VoteOnAnswer_UpdatesExistingVote()
    {
        await using var context = CreateInMemoryContext();
        var user = new CrowdsageUser { Id = "vUser2", UserName = "v2" };
        await context.Users.AddAsync(user);

        var question = new Question { Id = Guid.NewGuid(), Title = "Q", Content = "C", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, Tags = new System.Collections.Generic.List<string>(), Answers = new System.Collections.Generic.List<Answer>(), Votes = new System.Collections.Generic.List<QuestionVote>(), Comments = new System.Collections.Generic.List<QuestionComment>(), AuthorId = user.Id, Author = user };
        await context.Questions.AddAsync(question);

        var answer = new Answer { Content = "ans", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, Author = user, AuthorId = user.Id, Question = question, QuestionId = question.Id, Votes = new System.Collections.Generic.List<AnswerVote>(), Comments = new System.Collections.Generic.List<AnswerComment>() };
        await context.Answers.AddAsync(answer);
        await context.SaveChangesAsync();

        // add initial vote
        var vote = new AnswerVote { AnswerId = answer.Id, UserId = user.Id, Vote = Models.Enums.VoteValue.Upvote };
        await context.AnswerVotes.AddAsync(vote);
        await context.SaveChangesAsync();

        var svc = new AnswersService(context);
        var input = new CrowdSage.Server.Models.InsertUpdate.VoteInput { Vote = Models.Enums.VoteValue.Neutral };
        await svc.VoteOnAnswer(answer.Id, user.Id, input);

        var saved = await context.AnswerVotes.FirstOrDefaultAsync(v => v.AnswerId == answer.Id && v.UserId == user.Id);
        Assert.NotNull(saved);
        Assert.Equal(Models.Enums.VoteValue.Neutral, saved!.Vote);

        // ensure only one vote record exists for that user/answer
        var count = await context.AnswerVotes.CountAsync(v => v.AnswerId == answer.Id && v.UserId == user.Id);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task VoteOnAnswer_MultipleUsersVotes_PreservedWhenUpdatingOne()
    {
        await using var context = CreateInMemoryContext();
        var user1 = new CrowdsageUser { Id = "vA", UserName = "va" };
        var user2 = new CrowdsageUser { Id = "vB", UserName = "vb" };
        await context.Users.AddAsync(user1);
        await context.Users.AddAsync(user2);

        var question = new Question { Id = Guid.NewGuid(), Title = "Q", Content = "C", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, Tags = new System.Collections.Generic.List<string>(), Answers = new System.Collections.Generic.List<Answer>(), Votes = new System.Collections.Generic.List<QuestionVote>(), Comments = new System.Collections.Generic.List<QuestionComment>(), AuthorId = user1.Id, Author = user1 };
        await context.Questions.AddAsync(question);

        var answer = new Answer { Content = "ans", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, Author = user1, AuthorId = user1.Id, Question = question, QuestionId = question.Id, Votes = new System.Collections.Generic.List<AnswerVote>(), Comments = new System.Collections.Generic.List<AnswerComment>() };
        await context.Answers.AddAsync(answer);
        await context.SaveChangesAsync();

        await context.AnswerVotes.AddAsync(new AnswerVote { AnswerId = answer.Id, UserId = user1.Id, Vote = Models.Enums.VoteValue.Upvote });
        await context.AnswerVotes.AddAsync(new AnswerVote { AnswerId = answer.Id, UserId = user2.Id, Vote = Models.Enums.VoteValue.Upvote });
        await context.SaveChangesAsync();

        var svc = new AnswersService(context);
        var input = new CrowdSage.Server.Models.InsertUpdate.VoteInput { Vote = Models.Enums.VoteValue.Neutral };
        await svc.VoteOnAnswer(answer.Id, user1.Id, input);

        var v1 = await context.AnswerVotes.FirstOrDefaultAsync(v => v.AnswerId == answer.Id && v.UserId == user1.Id);
        var v2 = await context.AnswerVotes.FirstOrDefaultAsync(v => v.AnswerId == answer.Id && v.UserId == user2.Id);
        Assert.Equal(Models.Enums.VoteValue.Neutral, v1!.Vote);
        Assert.Equal(Models.Enums.VoteValue.Upvote, v2!.Vote);
    }
}
