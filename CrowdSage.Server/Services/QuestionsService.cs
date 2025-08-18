using CrowdSage.Server.Models;
using CrowdSage.Server.Models.InsertUpdate;

namespace CrowdSage.Server.Services;

public class QuestionsService(CrowdsageDbContext dbContext) : IQuestionsService
{
    public Question GetQuestionById(Guid id)
    {
        return dbContext.Questions.Find(id) ?? throw new KeyNotFoundException($"Question with ID {id} not found.");
    }

    public async Task<Question> AddQuestionAsync(QuestionDto question)
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
            EditedAt = DateTimeOffset.UtcNow,
            //Votes = new List<QuestionVote>(),
        };

        dbContext.Questions.Add(questionEntity);
        await dbContext.SaveChangesAsync();

        return questionEntity;
    }

    public async Task EditQuestion(Guid guid, QuestionDto question)
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
    public Task<Question> AddQuestionAsync(QuestionDto question);
    public Task EditQuestion(Guid guid, QuestionDto question);
    public Task DeleteQuestion(Guid id);
}
