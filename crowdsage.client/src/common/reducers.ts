import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';
import type { QuestionCreatePayload, Answer, AnswerComment, Question, QuestionComment, QuestionCommentCreatePayload, AnswerCommentCreatePayload, AnswerCreatePayload, UpvoteQuestionPayload, UpvoteAnswerPayload, BookmarkQuestionPayload, BookmarkAnswerPayload } from '../types';

export const questionsApi = createApi({
  reducerPath: 'questionsApi',
  baseQuery: fetchBaseQuery({ baseUrl: process.env.CROWDSAGE_BACKEND_URL }),
  tagTypes: ['Question', 'QuestionComment', 'Answer', 'AnswerComment'],
  endpoints: (build) => ({
    addQuestion: build.mutation<Question, QuestionCreatePayload>({
        query: (newQuestion) => ({
            url: 'questions',
            method: 'POST',
            body: newQuestion,
            providesTags: ['Question'],
        })
    }),
    upvoteQuestion: build.mutation<void, UpvoteQuestionPayload>({
        query: ({questionId, voteInput}) => ({
            url: `questions/${questionId}/vote`,
            method: 'POST',
            body: { voteInput },
            invalidatesTags: (result, error, {questionId}) => [{ type: 'Question', id: questionId }],
        }),
    }),
    bookmarkQuestion: build.mutation<void, BookmarkQuestionPayload>({
        query: ({questionId}) => ({
            url: `questions/${questionId}/bookmark`,
            method: 'POST',
            invalidatesTags: (result, error, {questionId}) => [{ type: 'Question', id: questionId }],
        }),
    }),
    removeBookmarkQuestion: build.mutation<void, BookmarkQuestionPayload>({
        query: ({questionId}) => ({
            url: `questions/${questionId}/bookmark`,
            method: 'DELETE',
            invalidatesTags: (result, error, {questionId}) => [{ type: 'Question', id: questionId }],
        }),
    }),
    editQuestion: build.mutation<Question, {data: QuestionCreatePayload, questionId: string }>({
        query: ({data, questionId}) => ({
            url: `questions/${questionId}`,
            method: 'PUT',
            body: data,
            invalidatesTag: ['Question']
        })
    }),
    getQuestionById: build.query<Question, string>({
      query: (questionId) => `questions/${questionId}`,
      providesTags: (result, error, questionId) => [{ type: 'Question', id: questionId }],
    }),
    getNewQuestions: build.query<Question[], {page: number, take: number}>({
        query: ({page, take}) => `questions/new/?page=${page}&take=${take}`,
    }),
    getCommentsForQuestion: build.query<QuestionComment[], string>({
        query: (questionId) => `question/${questionId}/comment`,
        providesTags: (result, error, questionId) => [
            'QuestionComment', 
            ...result.map(({ id }) => ({ type: 'QuestionComment' as const, id }))
        ],
    }),
    addQuestionComment: build.mutation<QuestionComment, { data: QuestionCommentCreatePayload, questionId: string }>({
        query: ({data, questionId}) => ({
            url: `question/${questionId}/comment`,
            method: 'POST',
            body: data
        })
    }),
    editQuestionComment: build.mutation<QuestionComment, {data: QuestionCommentCreatePayload, questionId: string}>({
        query: ({data, questionId}) => ({
            url: `questions/${questionId}`,
            method: 'PUT',
            body: data
        }),
        invalidatesTags: (result, error, {questionId}) => [{ type: 'QuestionComment', id: questionId }],
    }),
    getAnswersForQuestion: build.query<Answer[], string>({
        query: (questionId) => `question/${questionId}/answers`,
        providesTags: (result, error, questionId) => [
            'Answer', 
            ...result.map(({ id }) => ({ type: 'Answer' as const, id }) as const)
        ],
    }),
    addAnswer: build.mutation<AnswerComment, {data: AnswerCreatePayload, questionId: string}>({
        query: ({data, questionId}) => ({
            url: `api/questions/${questionId}/answers`,
            method: 'POST',
            body: data
        }),
        providesTags: ({id}, error, {questionId}) => [{ type: 'Answer', id: `${questionId}#${id}` }],
    }),
    editAnswer: build.mutation<AnswerComment, {data: AnswerCreatePayload, questionId: string, answerId: string}>({
        query: ({data, questionId, answerId}) => ({
            url: `api/questions/${questionId}/answers/${answerId}`,
            method: 'PUT',
            body: data,
        }),
        invalidatesTags: (result, error, {questionId, answerId}) => [{ type: 'AnswerComment', id: `${questionId}#${answerId}` }],
    }),
    upvoteAnswer: build.mutation<void, UpvoteAnswerPayload>({
        query: ({answerId, questionId, voteInput}) => ({
            url: `api/questions/${questionId}/answers/${answerId}/vote`,
            method: 'POST',
            body: { voteInput },
            invalidatesTags: (result, error, {questionId, answerId}) => [{ type: 'Answer', id: `${questionId}#${answerId}` }],
        }),
    }),
    bookmarkAnswer: build.mutation<void, BookmarkAnswerPayload>({
        query: ({answerId, questionId}) => ({
            url: `api/questions/${questionId}/answers/${answerId}/bookmark`,
            method: 'POST',
            invalidatesTags: (result, error, {questionId, answerId}) => [{ type: 'Answer', id: `${questionId}#${answerId}` }],
        }),
    }),
    removeBookmarkAnswer: build.mutation<void, BookmarkAnswerPayload>({
        query: ({answerId, questionId}) => ({
            url: `api/questions/${questionId}/answers/${answerId}/bookmark`,
            method: 'DELETE',
            invalidatesTags: (result, error, {questionId, answerId}) => [{ type: 'Answer', id: `${questionId}#${answerId}` }],
        }),
    }),
    getCommentsForAnswer: build.query<AnswerComment[], {answerId: string, questionId: string}>({
        query: ({answerId, questionId}) => `questions/${questionId}/answers/${answerId}/comments`,
        providesTags: (result, error, {answerId}) => [
            'AnswerComment', 
            ...result.map(({ id }) => ({ type: 'AnswerComment' as const, id: `${answerId}#${id}` }) as const)
        ],
    }),
    addCommentForAnswer: build.mutation<AnswerComment, {data: AnswerCommentCreatePayload, questionId: string, answerId: string}>({
        query: ({data, questionId, answerId}) => ({
            url: `api/questions/${questionId}/answers/${answerId}/comments`,
            method: 'POST',
            body: data
        }),
        providesTags: ({id}, error, {answerId}) => [{ type: 'AnswerComment', id: `${answerId}#${id}` }],
    }),
    editCommentForAnswer: build.mutation<AnswerComment, {data: AnswerCommentCreatePayload, questionId: string, answerId: string, answerCommentId: string}>({
        query: ({data, questionId, answerId, answerCommentId}) => ({
            url: `api/questions/${questionId}/answers/${answerId}/comments/${answerCommentId}`,
            method: 'PUT',
            body: data,
        }),
        invalidatesTags: (result, error, {answerId, answerCommentId}) => [{ type: 'AnswerComment', id: `${answerId}#${answerCommentId}` }],
    }),
    getBookmarkedQuestions: build.query<Question[], void>({
        query: () => `question/bookmark`,
    }),
    getBookmarkedAnswers: build.query<Answer[], void>({
        query: () => `answer/bookmark`,
    }),
  }),
});

export const {
    useAddQuestionMutation,
    useEditQuestionMutation,
    useUpvoteQuestionMutation,
    useGetQuestionByIdQuery,
    useGetNewQuestionsQuery,
    useGetCommentsForQuestionQuery,
    useAddQuestionCommentMutation,
    useEditQuestionCommentMutation,
    useGetAnswersForQuestionQuery,
    useAddAnswerMutation,
    useEditAnswerMutation,
    useUpvoteAnswerMutation,
    useGetCommentsForAnswerQuery,
    useAddCommentForAnswerMutation,
    useEditCommentForAnswerMutation,
    useGetBookmarkedQuestionsQuery,
    useGetBookmarkedAnswersQuery,
    useBookmarkQuestionMutation,
    useRemoveBookmarkQuestionMutation,
    useBookmarkAnswerMutation,
    useRemoveBookmarkAnswerMutation,
} = questionsApi;