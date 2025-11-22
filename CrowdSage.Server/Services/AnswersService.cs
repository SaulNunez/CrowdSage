using CrowdSage.Server.Models;
using CrowdSage.Server.Models.Enums;
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

    public void BookmarkAnswer(Guid answerId, string userId)
    {
        var bookmark = new AnswerBookmark
        {
            AnswerId = answerId,
            UserId = userId!,
        };
        dbContext.AnswerBookmarks.Add(bookmark);
        dbContext.SaveChanges();
    }

    public void RemoveBookmarkFromAnswer(Guid answerId, string userId)
    {
        var bookmark = dbContext.AnswerBookmarks.Where(b => b.AnswerId == answerId && b.UserId == userId).FirstOrDefault() ?? throw new KeyNotFoundException($"Bookmark for Answer ID {answerId} and User ID {userId} not found.");
        dbContext.AnswerBookmarks.Remove(bookmark);
        dbContext.SaveChanges();
    }

    public async Task VoteOnAnswer(Guid answerId, string userId, VoteInput vote)
    {
        var voteDb = dbContext.AnswerVotes.Where(v => v.AnswerId == answerId && v.UserId == userId).FirstOrDefault();
        if (voteDb == null)
        {
            var answer = await dbContext.Answers.FindAsync(answerId)
                ?? throw new KeyNotFoundException($"Answer with ID {answerId} not found.");
            var newVote = new AnswerVote
            {
                Answer = answer,
                UserId = userId,
                Vote = vote.Vote
            };
            dbContext.AnswerVotes.Add(newVote);
        }
        else
        {
            voteDb.Vote = vote.Vote;
            dbContext.AnswerVotes.Update(voteDb);
        }
        await dbContext.SaveChangesAsync();
    }

    public async Task<List<AnswerDto>> GetBookmarkedAnswers(string userId)
    {
        var bookmarkedAnswers = await dbContext.AnswerBookmarks
            .Where(b => b.UserId == userId)
            .Select(b => b.Answer)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        return bookmarkedAnswers.Select(a => new AnswerDto
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
        }).ToList();
    }
}

public interface IAnswersService
{
    public Task<AnswerDto> AddAnswerAsync(AnswerPayload answer, Guid questionId, string userId);
    public Task<IEnumerable<AnswerDto>> GetAnswersForQuestion(Guid questionId);
    public Task EditAnswer(Guid guid, AnswerPayload answer);
    public Task DeleteAnswer(Guid id);
    void BookmarkAnswer(Guid answerId, string userId);
    void RemoveBookmarkFromAnswer(Guid answerId, string userId);
    Task VoteOnAnswer(Guid answerId, string userId, VoteInput vote);
    Task<List<AnswerDto>> GetBookmarkedAnswers(string userId);
}