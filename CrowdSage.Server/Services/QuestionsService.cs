using CrowdSage.Server.Models;
using CrowdSage.Server.Models.InsertUpdate;
using CrowdSage.Server.Models.Outputs;
using Microsoft.EntityFrameworkCore;

namespace CrowdSage.Server.Services;

public class QuestionsService(CrowdsageDbContext dbContext) : IQuestionsService
{
    public QuestionDto GetQuestionById(Guid id)
    {
        var question = dbContext.Questions.Find(id) ?? throw new KeyNotFoundException($"Question with ID {id} not found.");

        return new QuestionDto
        {
            Id = question.Id,
            Content = question.Content,
            CreatedAt = question.CreatedAt,
            UpdatedAt = question.UpdatedAt,
            Bookmarked = false,
            Votes = 1,
            Author = new AuthorDto
            {
                Id = question.Author.Id,
                UserName = question.Author.UserName
            }
        };
    }

    public async Task<List<QuestionDto>> GetNewQuestionsAsync(int take = 10, int offset = 0)
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
                Bookmarked = false,
                Votes = q.Votes.Count(v => v.Vote == Models.Enums.VoteValue.Upvote),
                Author = new AuthorDto
                {
                    Id = q.Author.Id,
                    UserName = q.Author.UserName
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
            AuthorId = userId
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
            Bookmarked = false,
            Votes = 1,
            Author = new AuthorDto
            {
                Id = questionEntity.Author.Id,
                UserName = questionEntity.Author.UserName
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
}

public interface IQuestionsService
{
    public QuestionDto GetQuestionById(Guid id);
    public Task<QuestionDto> AddQuestionAsync(QuestionPayload question, string userId);
    public Task EditQuestion(Guid guid, QuestionPayload question);
    public Task DeleteQuestion(Guid id);
    Task<List<QuestionDto>> GetNewQuestionsAsync(int take = 10, int offset = 0);
    void BookmarkQuestion(Guid questionId, string userId);
    void RemoveBookmarkFromQuestion(Guid questionId, string userId);
}
