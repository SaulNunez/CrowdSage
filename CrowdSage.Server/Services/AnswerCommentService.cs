using CrowdSage.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace CrowdSage.Server.Services;

public interface IAnswerCommentService
{
    Task<AnswerComment> AddCommentAsync(AnswerComment comment, Guid answerId);
    Task DeleteCommentAsync(Guid id);
    Task EditCommentAsync(Guid id, AnswerComment updatedComment);
    Task<AnswerComment> GetCommentByIdAsync(Guid id);
    Task<List<AnswerComment>> GetCommentsForAnswer(Guid answerId);
}

public class AnswerCommentService(CrowdsageDbContext dbContext) : IAnswerCommentService
{
    public async Task<AnswerComment> AddCommentAsync(AnswerComment comment, Guid answerId)
    {
        if (comment == null)
        {
            throw new ArgumentNullException(nameof(comment), "Comment cannot be null.");
        }

        // Simulate adding the comment to a data store
        comment.CreatedAt = DateTime.UtcNow;
        comment.AnswerId = answerId;
        dbContext.AnswerComments.Add(comment);
        await dbContext.SaveChangesAsync();

        return comment;
    }

    public async Task<AnswerComment> GetCommentByIdAsync(Guid id)
    {
        var comment = await dbContext.AnswerComments.FindAsync(id) ?? throw new KeyNotFoundException($"Comment with ID {id} not found.");
        return comment;
    }

    public async Task EditCommentAsync(Guid id, AnswerComment updatedComment)
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

    public async Task<List<AnswerComment>> GetCommentsForAnswer(Guid answerId)
    {
        var comments = await dbContext.AnswerComments
            .Where(c => c.AnswerId == answerId)
            .ToListAsync();

        return comments;
    }
}