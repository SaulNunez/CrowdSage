using CrowdSage.Server.Models;

namespace CrowdSage.Server.Services;

public class QuestionsService(CrowdsageDbContext dbContext) : IQuestionsService
{
    public Question GetQuestionById(Guid id)
    {
        return dbContext.Questions.Find(id) ?? throw new KeyNotFoundException($"Question with ID {id} not found.");
    }

    public async Task AddQuestionAsync(Question question)
    {
        if (question == null)
        {
            throw new ArgumentNullException(nameof(question), "Question cannot be null.");
        }
        question.CreatedAt = DateTimeOffset.UtcNow;
        dbContext.Questions.Add(question);
        await dbContext.SaveChangesAsync();
    }

    public async Task EditQuestion(Guid guid, Question question)
    {
        if (question == null)
        {
            throw new ArgumentNullException(nameof(question), "Question cannot be null.");
        }
        var existingQuestion = dbContext.Questions.Find(guid) ?? throw new KeyNotFoundException($"Question with ID {guid} not found.");
        existingQuestion.Title = question.Title;
        existingQuestion.Content = question.Content;
        existingQuestion.EditedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteQuestion(Guid id)
    {
        var question = dbContext.Questions.Find(id) ?? throw new KeyNotFoundException($"Question with ID {id} not found.");
        dbContext.Questions.Remove(question);
        await dbContext.SaveChangesAsync();
    }
}

public interface IQuestionsService
{
    public Question GetQuestionById(Guid id);
    public Task AddQuestionAsync(Question question);
    public Task EditQuestion(Guid guid, Question question);
    public Task DeleteQuestion(Guid id);
}
