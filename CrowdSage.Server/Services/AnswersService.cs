﻿using CrowdSage.Server.Models;
using CrowdSage.Server.Models.InsertUpdate;
using CrowdSage.Server.Models.Outputs;
using Microsoft.EntityFrameworkCore;

namespace CrowdSage.Server.Services;

public class AnswersService(CrowdsageDbContext dbContext) : IAnswersService
{
    public async Task<AnswerDto> AddAnswerAsync(AnswerPayload answer, Guid questionId, string userId)
    {
        if (answer == null)
        {
            throw new ArgumentNullException(nameof(answer), "Question cannot be null.");
        }

        Answer answerEntity = new()
        {
            Content = answer.Content,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            AuthorId = userId,
            QuestionId = questionId,
        };

        answerEntity.Votes.Add(new AnswerVote
        {
            UserId = userId,
            Vote = Models.Enums.VoteValue.Upvote
        });

        dbContext.Answers.Add(answerEntity);
        await dbContext.SaveChangesAsync();

        return new AnswerDto
        {
            Id = answerEntity.Id,
            CreatedAt = answerEntity.CreatedAt,
            UpdatedAt = answerEntity.UpdatedAt,
            Content = answerEntity.Content,
            Author = new AuthorDto
            {
                Id = answerEntity.Author.Id,
                UserName = answerEntity.Author.UserName,
            },
            Bookmarked = false,
            Votes = 1
        };
    }

    public async Task<IEnumerable<AnswerDto>> GetAnswersForQuestion(Guid questionId)
    {
        var answers = await dbContext.Answers
            .Where(a => a.QuestionId == questionId)
            .Include(a => a.Author)
            .ToListAsync();

        return answers.Select(a => new AnswerDto
        {
            Id = a.Id,
            CreatedAt = a.CreatedAt,
            UpdatedAt = a.UpdatedAt,
            Content = a.Content,
            Author = new AuthorDto
            {
                Id = a.Author.Id,
                UserName = a.Author.UserName,
            }
        });
    }

    public async Task EditAnswer(Guid guid, AnswerPayload answer)
    {
        if (answer == null)
        {
            throw new ArgumentNullException(nameof(answer), "Answer cannot be null.");
        }
        var existingAnswer = await dbContext.Answers.FindAsync(guid)
            ?? throw new KeyNotFoundException($"Answer with ID {guid} not found.");
        existingAnswer.Content = answer.Content;
        existingAnswer.UpdatedAt = DateTimeOffset.UtcNow;
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
    public Task<AnswerDto> AddAnswerAsync(AnswerPayload answer, Guid questionId, string userId);
    public Task<IEnumerable<AnswerDto>> GetAnswersForQuestion(Guid questionId);
    public Task EditAnswer(Guid guid, AnswerPayload answer);
    public Task DeleteAnswer(Guid id);
}