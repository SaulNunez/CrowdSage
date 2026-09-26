using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using CrowdSage.Server.Models;
using CrowdSage.Server.Models.Enums;
using CrowdSage.Server.Services;

namespace CrowdSage.Server.Tests;

public class UserProfileServiceTests
{
    private static readonly DateTimeOffset BaseTime = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static CrowdsageDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<CrowdsageDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CrowdsageDbContext(options);
    }

    private static Question NewQuestion(CrowdsageUser author, string title, int minutes) => new()
    {
        Id = Guid.NewGuid(),
        Title = title,
        Content = $"{title} content",
        CreatedAt = BaseTime.AddMinutes(minutes),
        UpdatedAt = BaseTime.AddMinutes(minutes),
        AuthorId = author.Id,
        Author = author,
        Tags = new List<string>(),
        Answers = new List<Answer>(),
        Votes = new List<QuestionVote>(),
        Comments = new List<QuestionComment>()
    };

    private static Answer NewAnswer(CrowdsageUser author, Question question, string content, int minutes) => new()
    {
        Id = Guid.NewGuid(),
        Content = content,
        CreatedAt = BaseTime.AddMinutes(minutes),
        UpdatedAt = BaseTime.AddMinutes(minutes),
        AuthorId = author.Id,
        Author = author,
        Question = question,
        QuestionId = question.Id,
        Votes = new List<AnswerVote>(),
        Comments = new List<AnswerComment>()
    };

    private static QuestionComment NewQuestionComment(CrowdsageUser author, Question question, string content, int minutes) => new()
    {
        Id = Guid.NewGuid(),
        Content = content,
        CreatedAt = BaseTime.AddMinutes(minutes),
        UpdatedAt = BaseTime.AddMinutes(minutes),
        AuthorId = author.Id,
        Author = author,
        Question = question,
        QuestionId = question.Id
    };

    private static AnswerComment NewAnswerComment(CrowdsageUser author, Answer answer, string content, int minutes) => new()
    {
        Id = Guid.NewGuid(),
        Content = content,
        CreatedAt = BaseTime.AddMinutes(minutes),
        UpdatedAt = BaseTime.AddMinutes(minutes),
        AuthorId = author.Id,
        Author = author,
        Answer = answer,
        AnswerId = answer.Id
    };

    [Fact]
    public async Task GetProfileAsync_UnknownUser_ThrowsKeyNotFoundException()
    {
        await using var context = CreateInMemoryContext();
        var svc = new UserProfileService(context);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.GetProfileAsync("missing"));
    }

    [Fact]
    public async Task ListMethods_UnknownUser_ThrowKeyNotFoundException()
    {
        await using var context = CreateInMemoryContext();
        var svc = new UserProfileService(context);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.GetQuestionsAsync("missing"));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.GetAnswersAsync("missing"));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.GetCommentsAsync("missing"));
    }

    [Fact]
    public async Task GetProfileAsync_ReturnsCounts_OnlyForThatUser()
    {
        await using var context = CreateInMemoryContext();
        var user = new CrowdsageUser { Id = "pUser", UserName = "profile" };
        var other = new CrowdsageUser { Id = "pOther", UserName = "other" };
        await context.Users.AddRangeAsync(user, other);

        var q1 = NewQuestion(user, "q1", 0);
        var q2 = NewQuestion(user, "q2", 1);
        var otherQ = NewQuestion(other, "oq", 2);
        await context.Questions.AddRangeAsync(q1, q2, otherQ);

        var a1 = NewAnswer(user, otherQ, "a1", 3);
        var otherA = NewAnswer(other, q1, "oa", 4);
        await context.Answers.AddRangeAsync(a1, otherA);

        await context.QuestionComments.AddRangeAsync(
            NewQuestionComment(user, otherQ, "qc", 5),
            NewQuestionComment(other, q1, "oqc", 6));
        await context.AnswerComments.AddRangeAsync(
            NewAnswerComment(user, otherA, "ac", 7),
            NewAnswerComment(other, a1, "oac", 8));
        await context.SaveChangesAsync();

        var svc = new UserProfileService(context);
        var profile = await svc.GetProfileAsync(user.Id);

        Assert.Equal(user.Id, profile.Id);
        Assert.Equal(user.UserName, profile.UserName);
        Assert.Equal(2, profile.QuestionCount);
        Assert.Equal(1, profile.AnswerCount);
        Assert.Equal(2, profile.CommentCount);
    }

    [Fact]
    public async Task GetQuestionsAsync_ReturnsOwnQuestions_NewestFirst_WithVotesAndAnswerCount()
    {
        await using var context = CreateInMemoryContext();
        var user = new CrowdsageUser { Id = "qUser", UserName = "q" };
        var other = new CrowdsageUser { Id = "qOther", UserName = "o" };
        await context.Users.AddRangeAsync(user, other);

        var older = NewQuestion(user, "older", 0);
        var newer = NewQuestion(user, "newer", 10);
        await context.Questions.AddRangeAsync(older, newer, NewQuestion(other, "not mine", 5));
        await context.Answers.AddRangeAsync(NewAnswer(other, newer, "a", 11), NewAnswer(other, newer, "b", 12));
        await context.QuestionVotes.AddRangeAsync(
            new QuestionVote { QuestionId = newer.Id, UserId = user.Id, Vote = VoteValue.Upvote },
            new QuestionVote { QuestionId = newer.Id, UserId = other.Id, Vote = VoteValue.Upvote },
            new QuestionVote { QuestionId = older.Id, UserId = other.Id, Vote = VoteValue.Neutral });
        await context.SaveChangesAsync();

        var svc = new UserProfileService(context);
        var list = await svc.GetQuestionsAsync(user.Id);

        Assert.Equal(new[] { "newer", "older" }, list.Select(q => q.Title));
        Assert.Equal(2, list[0].Votes);
        Assert.Equal(2, list[0].AnswerCount);
        Assert.Equal(0, list[1].Votes);
        Assert.Equal(0, list[1].AnswerCount);
    }

    [Fact]
    public async Task GetAnswersAsync_ReturnsParentQuestionIdAndTitle()
    {
        await using var context = CreateInMemoryContext();
        var user = new CrowdsageUser { Id = "aUser", UserName = "a" };
        var other = new CrowdsageUser { Id = "aOther", UserName = "o" };
        await context.Users.AddRangeAsync(user, other);

        var question = NewQuestion(other, "Parent question", 0);
        await context.Questions.AddAsync(question);
        var answer = NewAnswer(user, question, "my answer", 1);
        await context.Answers.AddRangeAsync(answer, NewAnswer(other, question, "not mine", 2));
        await context.AnswerVotes.AddAsync(new AnswerVote { AnswerId = answer.Id, UserId = other.Id, Vote = VoteValue.Upvote });
        await context.SaveChangesAsync();

        var svc = new UserProfileService(context);
        var list = await svc.GetAnswersAsync(user.Id);

        var dto = Assert.Single(list);
        Assert.Equal(answer.Id, dto.Id);
        Assert.Equal(question.Id, dto.QuestionId);
        Assert.Equal("Parent question", dto.QuestionTitle);
        Assert.Equal("my answer", dto.Content);
        Assert.Equal(1, dto.Votes);
    }

    [Fact]
    public async Task GetCommentsAsync_MergesQuestionAndAnswerComments_NewestFirst()
    {
        await using var context = CreateInMemoryContext();
        var user = new CrowdsageUser { Id = "cUser", UserName = "c" };
        var other = new CrowdsageUser { Id = "cOther", UserName = "o" };
        await context.Users.AddRangeAsync(user, other);

        var question = NewQuestion(other, "Commented question", 0);
        await context.Questions.AddAsync(question);
        var answer = NewAnswer(other, question, "answer", 1);
        await context.Answers.AddAsync(answer);

        var onQuestion = NewQuestionComment(user, question, "on question", 2);
        var onAnswer = NewAnswerComment(user, answer, "on answer", 3);
        await context.QuestionComments.AddRangeAsync(onQuestion, NewQuestionComment(other, question, "not mine", 4));
        await context.AnswerComments.AddAsync(onAnswer);
        await context.SaveChangesAsync();

        var svc = new UserProfileService(context);
        var list = await svc.GetCommentsAsync(user.Id);

        Assert.Equal(new[] { onAnswer.Id, onQuestion.Id }, list.Select(c => c.Id));

        Assert.Equal(answer.Id, list[0].AnswerId);
        Assert.Equal(question.Id, list[0].QuestionId);
        Assert.Equal("Commented question", list[0].QuestionTitle);

        Assert.Null(list[1].AnswerId);
        Assert.Equal(question.Id, list[1].QuestionId);
        Assert.Equal("Commented question", list[1].QuestionTitle);
    }

    [Fact]
    public async Task ListMethods_ApplyOffsetAndTake_AfterOrdering()
    {
        await using var context = CreateInMemoryContext();
        var user = new CrowdsageUser { Id = "pgUser", UserName = "pg" };
        await context.Users.AddAsync(user);

        var questions = Enumerable.Range(0, 5).Select(i => NewQuestion(user, $"q{i}", i)).ToList();
        await context.Questions.AddRangeAsync(questions);
        await context.QuestionComments.AddRangeAsync(
            Enumerable.Range(0, 3).Select(i => NewQuestionComment(user, questions[0], $"qc{i}", i * 2)));
        var answer = NewAnswer(user, questions[0], "a", 0);
        await context.Answers.AddAsync(answer);
        await context.AnswerComments.AddRangeAsync(
            Enumerable.Range(0, 3).Select(i => NewAnswerComment(user, answer, $"ac{i}", i * 2 + 1)));
        await context.SaveChangesAsync();

        var svc = new UserProfileService(context);

        var questionPage = await svc.GetQuestionsAsync(user.Id, take: 2, offset: 2);
        Assert.Equal(new[] { "q2", "q1" }, questionPage.Select(q => q.Title));

        var commentPage = await svc.GetCommentsAsync(user.Id, take: 3, offset: 2);
        Assert.Equal(new[] { "ac1", "qc1", "ac0" }, commentPage.Select(c => c.Content));
    }
}
