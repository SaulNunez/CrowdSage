using CrowdSage.Server.Models;
using CrowdSage.Server.Models.InsertUpdate;
using Microsoft.EntityFrameworkCore;

namespace CrowdSage.Server.Services;

public interface IQuestionCommentService
{
    Task<QuestionComment> AddCommnentAsync(QuestionCommentDto comment, Guid questionId);
    Task DeleteCommentAsync(Guid id);
    Task EditCommentAsync(Guid id, QuestionCommentDto updatedComment);
    Task<QuestionComment> GetCommentByIdAsync(Guid id);
    Task<List<QuestionComment>> GetCommentsForQuestion(Guid questionId);
}

public class QuestionCommentService(CrowdsageDbContext dbContext) : IQuestionCommentService
{
    public async Task<QuestionComment> AddCommnentAsync(QuestionCommentDto comment, Guid questionId)
    {
        if (comment == null)
        {
            throw new ArgumentNullException(nameof(comment), "Comment cannot be null.");
        }

        var questionCommentEntity = new QuestionComment
        {
            Content = comment.Content,
            CreatedAt = DateTime.UtcNow,
        };

        dbContext.QuestionComments.Add(questionCommentEntity);
        await dbContext.SaveChangesAsync();

        return questionCommentEntity;
    }

    public async Task<QuestionComment> GetCommentByIdAsync(Guid id)
    {
        var comment = await dbContext.QuestionComments.FindAsync(id);
        if (comment == null)
        {
            throw new KeyNotFoundException($"Comment with ID {id} not found.");
        }
        return comment;
    }

    public async Task EditCommentAsync(Guid id, QuestionCommentDto updatedComment)
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

    public async Task<List<QuestionComment>> GetCommentsForQuestion(Guid questionId)
    {
        var comments = await dbContext.QuestionComments
            .Where(c => c.QuestionId == questionId)
            .ToListAsync();

        return comments;
    }
}