using CrowdSage.Server.Models;
using CrowdSage.Server.Models.InsertUpdate;
using Microsoft.EntityFrameworkCore;

namespace CrowdSage.Server.Services;

public class AnswersService(CrowdsageDbContext dbContext) : IAnswersService
{
    public async Task<Answer> AddAnswerAsync(AnswerDto answer)
    {
        if (answer == null)
        {
            throw new ArgumentNullException(nameof(answer), "Question cannot be null.");
        }

        //TODO: Vote question from the user that posted it
        Answer answerEntity = new()
        {
            Content = answer.Content,
            CreatedAt = DateTime.UtcNow,
            EditedAt = DateTime.UtcNow,
            //Votes = new List<AnswerVote>(),
        };

        dbContext.Answers.Add(answerEntity);
        await dbContext.SaveChangesAsync();

        return answerEntity;
    }

    public async Task<IEnumerable<Answer>> GetAnswersForQuestion(Guid questionId)
    {
        return await dbContext.Answers
            .Where(a => a.QuestionId == questionId)
            .ToListAsync();
    }

    public async Task EditAnswer(Guid guid, AnswerDto answer)
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
    public Task<Answer> AddAnswerAsync(AnswerDto answer);
    public Task<IEnumerable<Answer>> GetAnswersForQuestion(Guid questionId);
    public Task EditAnswer(Guid guid, AnswerDto answer);
    public Task DeleteAnswer(Guid id);
}