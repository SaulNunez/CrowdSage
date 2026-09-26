using CrowdSage.Server.Models;
using CrowdSage.Server.Models.Enums;
using CrowdSage.Server.Models.Outputs;
using Microsoft.EntityFrameworkCore;

namespace CrowdSage.Server.Services;

public class UserProfileService(CrowdsageDbContext dbContext) : IUserProfileService
{
    public async Task<UserProfileDto> GetProfileAsync(string userId)
    {
        var user = await dbContext.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException($"User with ID {userId} not found.");

        var questionCount = await dbContext.Questions.CountAsync(q => q.AuthorId == userId);
        var answerCount = await dbContext.Answers.CountAsync(a => a.AuthorId == userId);
        var questionCommentCount = await dbContext.QuestionComments.CountAsync(c => c.AuthorId == userId);
        var answerCommentCount = await dbContext.AnswerComments.CountAsync(c => c.AuthorId == userId);

        return new UserProfileDto
        {
            Id = user.Id,
            UserName = user.UserName!,
            UrlPhoto = user.ProfilePicObjectKey,
            QuestionCount = questionCount,
            AnswerCount = answerCount,
            CommentCount = questionCommentCount + answerCommentCount
        };
    }

    public async Task<List<UserQuestionSummaryDto>> GetQuestionsAsync(string userId, int take = 20, int offset = 0)
    {
        await EnsureUserExistsAsync(userId);

        return await dbContext.Questions
            .Where(q => q.AuthorId == userId)
            .OrderByDescending(q => q.CreatedAt)
            .Skip(offset)
            .Take(take)
            .Select(q => new UserQuestionSummaryDto
            {
                Id = q.Id,
                Title = q.Title,
                Votes = q.Votes.Count(v => v.Vote == VoteValue.Upvote),
                AnswerCount = q.Answers.Count,
                CreatedAt = q.CreatedAt,
                UpdatedAt = q.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<List<UserAnswerSummaryDto>> GetAnswersAsync(string userId, int take = 20, int offset = 0)
    {
        await EnsureUserExistsAsync(userId);

        return await dbContext.Answers
            .Where(a => a.AuthorId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .Skip(offset)
            .Take(take)
            .Select(a => new UserAnswerSummaryDto
            {
                Id = a.Id,
                QuestionId = a.QuestionId,
                QuestionTitle = a.Question.Title,
                Content = a.Content,
                Votes = a.Votes.Count(v => v.Vote == VoteValue.Upvote),
                CreatedAt = a.CreatedAt,
                UpdatedAt = a.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<List<UserCommentSummaryDto>> GetCommentsAsync(string userId, int take = 20, int offset = 0)
    {
        await EnsureUserExistsAsync(userId);

        var questionComments = dbContext.QuestionComments
            .Where(c => c.AuthorId == userId)
            .Select(c => new UserCommentSummaryDto
            {
                Id = c.Id,
                Content = c.Content,
                QuestionId = c.QuestionId,
                QuestionTitle = c.Question.Title,
                AnswerId = (Guid?)null,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            });

        var answerComments = dbContext.AnswerComments
            .Where(c => c.AuthorId == userId)
            .Select(c => new UserCommentSummaryDto
            {
                Id = c.Id,
                Content = c.Content,
                QuestionId = c.Answer.QuestionId,
                QuestionTitle = c.Answer.Question.Title,
                AnswerId = (Guid?)c.AnswerId,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            });

        return await questionComments
            .Concat(answerComments)
            .OrderByDescending(c => c.CreatedAt)
            .Skip(offset)
            .Take(take)
            .ToListAsync();
    }

    private async Task EnsureUserExistsAsync(string userId)
    {
        if (!await dbContext.Users.AnyAsync(u => u.Id == userId))
        {
            throw new KeyNotFoundException($"User with ID {userId} not found.");
        }
    }
}

public interface IUserProfileService
{
    Task<UserProfileDto> GetProfileAsync(string userId);
    Task<List<UserQuestionSummaryDto>> GetQuestionsAsync(string userId, int take = 20, int offset = 0);
    Task<List<UserAnswerSummaryDto>> GetAnswersAsync(string userId, int take = 20, int offset = 0);
    Task<List<UserCommentSummaryDto>> GetCommentsAsync(string userId, int take = 20, int offset = 0);
}
