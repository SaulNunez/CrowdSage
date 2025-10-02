using CrowdSage.Server.Models;
using CrowdSage.Server.Models.InsertUpdate;
using CrowdSage.Server.Models.Outputs;
using Microsoft.EntityFrameworkCore;

namespace CrowdSage.Server.Services;

public interface IQuestionCommentService
{
    Task<QuestionCommentDto> AddCommnentAsync(QuestionCommentPayload comment, Guid questionId);
    Task DeleteCommentAsync(Guid id);
    Task EditCommentAsync(Guid id, QuestionCommentPayload updatedComment);
    Task<QuestionCommentDto> GetCommentByIdAsync(Guid id);
    Task<List<QuestionCommentDto>> GetCommentsForQuestion(Guid questionId);
}

public class QuestionCommentService(CrowdsageDbContext dbContext) : IQuestionCommentService
{
    public async Task<QuestionCommentDto> AddCommnentAsync(QuestionCommentPayload comment, Guid questionId, string userId)
    {
        if (comment == null)
        {
            throw new ArgumentNullException(nameof(comment), "Comment cannot be null.");
        }

        var questionCommentEntity = new QuestionComment
        {
            Content = comment.Content,
            CreatedAt = DateTime.UtcNow,
            AuthorId = userId
        };

        dbContext.QuestionComments.Add(questionCommentEntity);
        await dbContext.SaveChangesAsync();

        return new QuestionCommentDto
        {
            Content = questionCommentEntity.Content,
            CreatedAt = questionCommentEntity.CreatedAt,
            UpdatedAt = questionCommentEntity.UpdatedAt,
            Author = new AuthorDto
            {
                Id = questionCommentEntity.Author.Id,
                UserName = questionCommentEntity.Author.UserName,
            }
        };
    }

    public async Task<QuestionCommentDto> GetCommentByIdAsync(Guid id)
    {
        var comment = await dbContext.QuestionComments.FindAsync(id) ?? throw new KeyNotFoundException($"Comment with ID {id} not found.");
        return new QuestionCommentDto
        {
            Content = comment.Content,
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt,
            Author = new AuthorDto
            {
                Id = comment.Author.Id,
                UserName = comment.Author.UserName,
            }
        };
    }

    public async Task EditCommentAsync(Guid id, QuestionCommentPayload updatedComment)
    {
        if (updatedComment == null)
        {
            throw new ArgumentNullException(nameof(updatedComment), "Updated comment cannot be null.");
        }

        var existingComment = await dbContext.QuestionComments.FindAsync(id) ?? throw new KeyNotFoundException($"Comment with ID {id} not found.");
        existingComment.Content = updatedComment.Content;
        existingComment.UpdatedAt = DateTime.UtcNow;

        dbContext.QuestionComments.Update(existingComment);
        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteCommentAsync(Guid id)
    {
        var comment = await dbContext.QuestionComments.FindAsync(id) ?? throw new KeyNotFoundException($"Comment with ID {id} not found.");
        dbContext.QuestionComments.Remove(comment);
        await dbContext.SaveChangesAsync();
    }

    public async Task<List<QuestionCommentDto>> GetCommentsForQuestion(Guid questionId)
    {
        var comments = await dbContext.QuestionComments
            .Where(c => c.QuestionId == questionId)
            .ToListAsync();

        return [.. comments.Select(comment => new QuestionCommentDto
        {
            Content = comment.Content,
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt,
            Author = new AuthorDto
            {
                Id = comment.Author.Id,
                UserName = comment.Author.UserName,
            }
        })];
    }
}