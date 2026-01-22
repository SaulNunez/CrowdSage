using CrowdSage.Server.Models;
using CrowdSage.Server.Models.InsertUpdate;
using CrowdSage.Server.Models.Outputs;
using Microsoft.EntityFrameworkCore;

namespace CrowdSage.Server.Services;

public class QuestionsService(CrowdsageDbContext dbContext) : IQuestionsService
{
    public QuestionDto GetQuestionById(Guid id, string? userId)
    {
        var question = dbContext.Questions.Find(id) ?? throw new KeyNotFoundException($"Question with ID {id} not found.");

        return new QuestionDto
        {
            Id = question.Id,
            Content = question.Content,
            CreatedAt = question.CreatedAt,
            UpdatedAt = question.UpdatedAt,
            Bookmarked = question.UserBookmarks.Any(ub => ub.UserId == userId),
            Votes = question.Votes.Count(v => v.Vote == Models.Enums.VoteValue.Upvote),
            CurrentUserVote = question.Votes
                    .Where(v => v.UserId == userId)
                    .Select(v => v.Vote)
                    .FirstOrDefault(),
            Author = new AuthorDto
            {
                Id = question.Author.Id,
                UserName = question.Author.UserName,
                UrlPhoto = question.Author.ProfilePicObjectKey
            }
        };
    }

    public async Task<List<QuestionDto>> GetNewQuestionsAsync(string? userId, int take = 10, int offset = 0)
    {
        var questions = dbContext.Questions
            .OrderByDescending(q => q.CreatedAt)
            .Take(take)
            .Skip(offset);
        
        return questions.Select(q => new QuestionDto
            {
                Id = q.Id,
                Content = q.Content,
                CreatedAt = q.CreatedAt,
                UpdatedAt = q.UpdatedAt,
                Bookmarked = q.UserBookmarks.Any(ub => ub.UserId == userId),
                Votes = q.Votes.Count(v => v.Vote == Models.Enums.VoteValue.Upvote),
                CurrentUserVote = q.Votes
                    .Where(v => v.UserId == userId)
                    .Select(v => v.Vote)
                    .FirstOrDefault(),
                Author = new AuthorDto
                {
                    Id = q.Author.Id,
                    UserName = q.Author.UserName,
                    UrlPhoto = q.Author.ProfilePicObjectKey
                }
            })
            .ToList();
    }

    public async Task<QuestionDto> AddQuestionAsync(QuestionPayload question, string userId)
    {
        if (question == null)
        {
            throw new ArgumentNullException(nameof(question), "Question cannot be null.");
        }
        // Add upvote from question author
        var questionEntity = new Question
        {
            Title = question.Title,
            Content = question.Content,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            AuthorId = userId,
            Tags = [],
            Answers = [],
            Votes = [],
            Comments = []
        };

        questionEntity.Votes.Add(new QuestionVote
        {
            Vote = Models.Enums.VoteValue.Upvote,
            UserId = userId
        });

        dbContext.Questions.Add(questionEntity);
        await dbContext.SaveChangesAsync();

        return new QuestionDto
        {
            Id = questionEntity.Id,
            Content = questionEntity.Content,
            CreatedAt = questionEntity.CreatedAt,
            UpdatedAt = questionEntity.UpdatedAt,
            Bookmarked = questionEntity.UserBookmarks.Any(ub => ub.UserId == userId),
            Votes = questionEntity.Votes.Count(v => v.Vote == Models.Enums.VoteValue.Upvote),
            CurrentUserVote = questionEntity.Votes
                    .Where(v => v.UserId == userId)
                    .Select(v => v.Vote)
                    .FirstOrDefault(),
            Author = new AuthorDto
            {
                Id = questionEntity.Author.Id ?? userId,
                UserName = questionEntity.Author.UserName,
                UrlPhoto = questionEntity.Author.ProfilePicObjectKey
            }
        };
    }

    public async Task EditQuestion(Guid guid, QuestionPayload question)
    {
        if (question == null)
        {
            throw new ArgumentNullException(nameof(question), "Question cannot be null.");
        }
        var existingQuestion = dbContext.Questions.Find(guid) ?? throw new KeyNotFoundException($"Question with ID {guid} not found.");
        existingQuestion.Title = question.Title;
        existingQuestion.Content = question.Content;
        existingQuestion.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteQuestion(Guid id)
    {
        var question = dbContext.Questions.Find(id) ?? throw new KeyNotFoundException($"Question with ID {id} not found.");
        dbContext.Questions.Remove(question);
        await dbContext.SaveChangesAsync();
    }

    public void BookmarkQuestion(Guid questionId, string userId)
    {
        var bookmark = new QuestionBookmark
        {
            QuestionId = questionId,
            UserId = userId
        };

        dbContext.QuestionBookmarks.Add(bookmark);
        dbContext.SaveChanges();
    }

    public void RemoveBookmarkFromQuestion(Guid questionId, string userId)
    {
        var bookmark = dbContext.QuestionBookmarks.Where(b => b.QuestionId == questionId && b.UserId == userId).FirstOrDefault() ?? throw new KeyNotFoundException($"Bookmark for Question ID {questionId} and User ID {userId} not found.");
        dbContext.QuestionBookmarks.Remove(bookmark);
        dbContext.SaveChanges();
    }

    public Task VoteOnQuestion(Guid answerId, string userId, VoteInput voteInput)
    {
        var vote = dbContext.QuestionVotes.Where(v => v.QuestionId == answerId && v.UserId == userId).FirstOrDefault();
        if (vote == null)
        {
            var question = dbContext.Questions.Find(answerId) ?? throw new KeyNotFoundException($"Question with ID {answerId} not found.");
            var newVote = new QuestionVote
            {
                Question = question,
                UserId = userId,
                Vote = voteInput.Vote
            };
            dbContext.QuestionVotes.Add(newVote);
        }
        else
        {
            vote.Vote = voteInput.Vote;
        }
        return dbContext.SaveChangesAsync();
    }

    public async Task<List<QuestionDto>> GetBookmarkedQuestions(string userId, int take = 50, int offset = 0)
    {
        var bookmarkedQuestions = await dbContext.QuestionBookmarks
            .Where(b => b.UserId == userId)
            .Select(b => b.Question)
            .OrderByDescending(q => q.CreatedAt)
            .Take(take)
            .Skip(offset)
            .ToListAsync();

        return bookmarkedQuestions.Select(q => new QuestionDto
            {
                Id = q.Id,
                Content = q.Content,
                CreatedAt = q.CreatedAt,
                UpdatedAt = q.UpdatedAt,
                Bookmarked = q.UserBookmarks.Any(ub => ub.UserId == userId),
                Votes = q.Votes.Count(v => v.Vote == Models.Enums.VoteValue.Upvote),
                CurrentUserVote = q.Votes
                    .Where(v => v.UserId == userId)
                    .Select(v => v.Vote)
                    .FirstOrDefault(),
                Author = new AuthorDto
                {
                    Id = q.Author.Id,
                    UserName = q.Author.UserName,
                    UrlPhoto = q.Author.ProfilePicObjectKey
                }
            })
            .ToList();
    }
}

public interface IQuestionsService
{
    public QuestionDto GetQuestionById(Guid id, string? userId);
    public Task<QuestionDto> AddQuestionAsync(QuestionPayload question, string userId);
    public Task EditQuestion(Guid guid, QuestionPayload question);
    public Task DeleteQuestion(Guid id);
    Task<List<QuestionDto>> GetNewQuestionsAsync(string? userId, int take = 10, int offset = 0);
    void BookmarkQuestion(Guid questionId, string userId);
    void RemoveBookmarkFromQuestion(Guid questionId, string userId);
    Task VoteOnQuestion(Guid answerId, string userId, VoteInput voteInput);
    Task<List<QuestionDto>> GetBookmarkedQuestions(string userId, int take = 50, int offset = 0);
}
