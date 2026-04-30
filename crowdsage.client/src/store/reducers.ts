import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';
import type { QuestionCreatePayload, Answer, AnswerComment, Question, QuestionComment, QuestionCommentCreatePayload, AnswerCommentCreatePayload, AnswerCreatePayload, UpvoteQuestionPayload, UpvoteAnswerPayload, BookmarkQuestionPayload, BookmarkAnswerPayload, RegisterPayload } from '../types';
import type { RootState } from '../store';

export interface LoginRequest {
    username: string;
    password: string;
}

export interface LoginResponse {
    access_token: string;
    token_type: string;
    expires_in: number;
}

type CreateAnswerParams = {
    data: AnswerCreatePayload;
    questionId: string;
    answerId: string;
};

type EditQuestionCommentParams = {
    data: QuestionCommentCreatePayload;
    questionId: string;
};

interface ErrorDescriptionResponse {
    code: number;
    message: string;
}

type ErrorResponse = ErrorDescriptionResponse | void;

export const questionsApi = createApi({
  reducerPath: 'questionsApi',
  baseQuery: fetchBaseQuery({ 
    baseUrl: import.meta.env.VITE_CROWDSAGE_BACKEND_URL,
    prepareHeaders: (headers, { getState }) => {
        const state = getState() as RootState;
        const token = state.auth?.token;
        if (token) {
            headers.set('authorization', `Bearer ${token}`);
        }
        return headers;
    },
  }),
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
            invalidatesTags: (_result: void, _error: ErrorResponse, {questionId}: UpvoteQuestionPayload) => [{ type: 'Question', id: questionId }],
        }),
    }),
    bookmarkQuestion: build.mutation<void, BookmarkQuestionPayload>({
        query: ({questionId}) => ({
            url: `questions/${questionId}/bookmark`,
            method: 'POST',
            invalidatesTags: (_result: void, _error: ErrorResponse, {questionId}: BookmarkQuestionPayload) => [{ type: 'Question', id: questionId }],
        }),
    }),
    removeBookmarkQuestion: build.mutation<void, BookmarkQuestionPayload>({
        query: ({questionId}) => ({
            url: `questions/${questionId}/bookmark`,
            method: 'DELETE',
            invalidatesTags: (_result: void, _error: ErrorResponse, {questionId}: BookmarkQuestionPayload) => [{ type: 'Question', id: questionId }],
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
      providesTags: (_result, _error, questionId) => [{ type: 'Question', id: questionId }],
    }),
    getNewQuestions: build.query<Question[], {page: number, take: number}>({
        query: ({page, take}) => `questions/new/?page=${page}&take=${take}`,
    }),
    getCommentsForQuestion: build.query<QuestionComment[], string>({
        query: (questionId) => `question/${questionId}/comment`,
        providesTags: (result) => [
            'QuestionComment', 
            ...(result || []).map(({ id }) => ({ type: 'QuestionComment' as const, id }))
        ],
    }),
    addQuestionComment: build.mutation<QuestionComment, { data: QuestionCommentCreatePayload, questionId: string }>({
        query: ({data, questionId}) => ({
            url: `question/${questionId}/comment`,
            method: 'POST',
            body: data
        })
    }),
    editQuestionComment: build.mutation<QuestionComment, EditQuestionCommentParams>({
        query: ({data, questionId}) => ({
            url: `questions/${questionId}`,
            method: 'PUT',
            body: data
        }),
        invalidatesTags: (_result: QuestionComment | undefined, _error: ErrorResponse, {questionId}: EditQuestionCommentParams) => [{ type: 'QuestionComment', id: questionId }],
    }),
    getAnswersForQuestion: build.query<Answer[], string>({
        query: (questionId) => `question/${questionId}/answers`,
        providesTags: (result) => [
            'Answer', 
            ...(result || []).map(({ id }) => ({ type: 'Answer' as const, id }) as const)
        ],
    }),
    addAnswer: build.mutation<AnswerComment, {data: AnswerCreatePayload, questionId: string}>({
        query: ({data, questionId}) => ({
            url: `api/questions/${questionId}/answers`,
            method: 'POST',
            body: data
        }),
        invalidatesTags: (result, _error, {questionId}) => result ? [{ type: 'Answer', id: `${questionId}#${result.id}` }] : [],
    }),
    editAnswer: build.mutation<AnswerComment, CreateAnswerParams>({
        query: ({data, questionId, answerId}) => ({
            url: `api/questions/${questionId}/answers/${answerId}`,
            method: 'PUT',
            body: data,
        }),
        invalidatesTags: (_result: AnswerComment | undefined, _error: ErrorResponse, {questionId, answerId}: CreateAnswerParams) => [{ type: 'AnswerComment', id: `${questionId}#${answerId}` }],
    }),
    upvoteAnswer: build.mutation<void, UpvoteAnswerPayload>({
        query: ({answerId, questionId, voteInput}) => ({
            url: `api/questions/${questionId}/answers/${answerId}/vote`,
            method: 'POST',
            body: { voteInput },
            invalidatesTags: (_result: void, _error: ErrorResponse, {questionId, answerId}: UpvoteAnswerPayload) => [{ type: 'Answer', id: `${questionId}#${answerId}` }],
        }),
    }),
    bookmarkAnswer: build.mutation<void, BookmarkAnswerPayload>({
        query: ({answerId, questionId}) => ({
            url: `api/questions/${questionId}/answers/${answerId}/bookmark`,
            method: 'POST',
            invalidatesTags: (_result: void, _error: ErrorResponse, {questionId, answerId}: BookmarkAnswerPayload) => [{ type: 'Answer', id: `${questionId}#${answerId}` }],
        }),
    }),
    removeBookmarkAnswer: build.mutation<void, BookmarkAnswerPayload>({
        query: ({answerId, questionId}) => ({
            url: `api/questions/${questionId}/answers/${answerId}/bookmark`,
            method: 'DELETE',
            invalidatesTags: (_result: void, _error: ErrorResponse, {questionId, answerId}: BookmarkAnswerPayload) => [{ type: 'Answer', id: `${questionId}#${answerId}` }],
        }),
    }),
    getCommentsForAnswer: build.query<AnswerComment[], {answerId: string, questionId: string}>({
        query: ({answerId, questionId}) => `questions/${questionId}/answers/${answerId}/comments`,
        providesTags: (result: AnswerComment[] | undefined, _error, {answerId}) => [
            'AnswerComment', 
            ...(result || []).map(({ id }) => ({ type: 'AnswerComment' as const, id: `${answerId}#${id}` }) as const)
        ],
    }),
    addCommentForAnswer: build.mutation<AnswerComment, {data: AnswerCommentCreatePayload, questionId: string, answerId: string}>({
        query: ({data, questionId, answerId}) => ({
            url: `api/questions/${questionId}/answers/${answerId}/comments`,
            method: 'POST',
            body: data
        }),
        invalidatesTags: (result: AnswerComment | undefined, _error, {answerId}) => result ? [{ type: 'AnswerComment', id: `${answerId}#${result.id}` }] : [],
    }),
    editCommentForAnswer: build.mutation<AnswerComment, {data: AnswerCommentCreatePayload, questionId: string, answerId: string, answerCommentId: string}>({
        query: ({data, questionId, answerId, answerCommentId}) => ({
            url: `api/questions/${questionId}/answers/${answerId}/comments/${answerCommentId}`,
            method: 'PUT',
            body: data,
        }),
        invalidatesTags: (_result: AnswerComment | undefined, _error: ErrorResponse, {answerId, answerCommentId}: any) => [{ type: 'AnswerComment', id: `${answerId}#${answerCommentId}` }],
    }),
    getBookmarkedQuestions: build.query<Question[], void>({
        query: () => `question/bookmark`,
    }),
    getBookmarkedAnswers: build.query<Answer[], void>({
        query: () => `answer/bookmark`,
    }),
    registerUser: build.mutation<void, RegisterPayload>({
        query: (data) => ({
            url: `api/account/register`,
            method: 'POST',
            body: data
        }),
    }),
    login: build.mutation<LoginResponse, LoginRequest>({
        query: (credentials) => ({
            url: 'connect/token',
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: new URLSearchParams({
                grant_type: 'password',
                username: credentials.username,
                password: credentials.password,
                scope: 'offline_access'
            }).toString(),
        }),
    })
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
    useRegisterUserMutation,
    useLoginMutation
} = questionsApi;