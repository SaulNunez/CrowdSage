using CrowdSage.Server.Models;
using CrowdSage.Server.Models.InsertUpdate;
using CrowdSage.Server.Models.Outputs;
using Microsoft.EntityFrameworkCore;

namespace CrowdSage.Server.Services;

public interface IAnswerCommentService
{
    Task<AnswerCommentDto> AddCommentAsync(AnswerCommentPayload comment, Guid answerId, string userId);
    Task DeleteCommentAsync(Guid id);
    Task EditCommentAsync(Guid id, AnswerCommentPayload updatedComment);
    Task<AnswerCommentDto> GetCommentByIdAsync(Guid id);
    Task<List<AnswerCommentDto>> GetCommentsForAnswer(Guid answerId);
}

public class AnswerCommentService(CrowdsageDbContext dbContext) : IAnswerCommentService
{
    public async Task<AnswerCommentDto> AddCommentAsync(AnswerCommentPayload comment, Guid answerId, string userId)
    {
        if (comment == null)
        {
            throw new ArgumentNullException(nameof(comment), "Comment cannot be null.");
        }

        var answerCommentEntity = new AnswerComment
        {
            Content = comment.Content,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            AuthorId = userId,
            AnswerId = answerId
        };

        dbContext.AnswerComments.Add(answerCommentEntity);
        await dbContext.SaveChangesAsync();

        return new AnswerCommentDto
        {
            Id = answerCommentEntity.Id,
            Content = answerCommentEntity.Content,
            CreatedAt = answerCommentEntity.CreatedAt,
            UpdatedAt = answerCommentEntity.UpdatedAt,
            Author = new AuthorDto
            {
                Id = answerCommentEntity.Author.Id,
                UserName = answerCommentEntity.Author.UserName
            }
        };
    }

    public async Task<AnswerCommentDto> GetCommentByIdAsync(Guid id)
    {
        var comment = await dbContext.AnswerComments.FindAsync(id) ?? throw new KeyNotFoundException($"Comment with ID {id} not found.");

        return new AnswerCommentDto
        {
            Id = comment.Id,
            Content = comment.Content,
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt,
            Author = new AuthorDto
            {
                Id = comment.Author.Id,
                UserName = comment.Author.UserName
            }
        };
    }

    public async Task EditCommentAsync(Guid id, AnswerCommentPayload updatedComment)
    {
        if (updatedComment == null)
        {
            throw new ArgumentNullException(nameof(updatedComment), "Updated comment cannot be null.");
        }

        var existingComment = await dbContext.AnswerComments.FindAsync(id) ?? throw new KeyNotFoundException($"Comment with ID {id} not found.");
        existingComment.Content = updatedComment.Content;
        existingComment.UpdatedAt = DateTime.UtcNow;

        dbContext.AnswerComments.Update(existingComment);
        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteCommentAsync(Guid id)
    {
        var comment = await dbContext.AnswerComments.FindAsync(id) ?? throw new KeyNotFoundException($"Comment with ID {id} not found.");
        dbContext.AnswerComments.Remove(comment);
        await dbContext.SaveChangesAsync();
    }

    public async Task<List<AnswerCommentDto>> GetCommentsForAnswer(Guid answerId)
    {
        var comments = await dbContext.AnswerComments
            .Where(c => c.AnswerId == answerId)
            .Include(c => c.Author)
            .ToListAsync();

        return [.. comments.Select(comment => new AnswerCommentDto
            {
                Id = comment.Id,
                Content = comment.Content,
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt,
                Author = new AuthorDto
                {
                    Id = comment.Author.Id,
                    UserName = comment.Author.UserName
                }
            })];
    }
}