using CrowdSage.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace CrowdSage.Server.Services;

public class AnswersService(CrowdsageDbContext dbContext) : IAnswersService
{
    public async Task AddAnswerAsync(Answer answer) {
        if (answer == null)
        {
            throw new ArgumentNullException(nameof(answer), "Question cannot be null.");
        }
        answer.CreatedAt = DateTimeOffset.UtcNow;
        dbContext.Answers.Add(answer);
        await dbContext.SaveChangesAsync();
    }

    public async Task<IEnumerable<Answer>> GetAnswersForQuestion(Guid questionId)
    {
        return await dbContext.Answers
            .Where(a => a.QuestionId == questionId)
            .ToListAsync();
    }

    public async Task EditAnswer(Guid guid, Answer answer)
    {
        if (answer == null)
        {
            throw new ArgumentNullException(nameof(answer), "Answer cannot be null.");
        }
        var existingAnswer = await dbContext.Answers.FindAsync(guid)
            ?? throw new KeyNotFoundException($"Answer with ID {guid} not found.");
        existingAnswer.Content = answer.Content;
        existingAnswer.EditedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteAnswer(Guid id)
    {
        var answer = await dbContext.Answers.FindAsync(id)
            ?? throw new KeyNotFoundException($"Answer with ID {id} not found.");
        dbContext.Answers.Remove(answer);
        await dbContext.SaveChangesAsync();
    }
}

public interface IAnswersService
{
    public Task AddAnswerAsync(Answer answer);
    public Task<IEnumerable<Answer>> GetAnswersForQuestion(Guid questionId);
    public Task EditAnswer(Guid guid, Answer answer);
    public Task DeleteAnswer(Guid id);
}