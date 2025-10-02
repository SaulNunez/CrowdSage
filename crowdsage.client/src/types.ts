interface IDateable
{
    createdAt: Date,
    updatedAt: Date
}

export interface Answer extends IDateable {
    id: string,
    content: string,
    votes: number,
    bookmarked: boolean,
    author: Author
}

export interface BaseComment extends IDateable {
    id: string,
    content: string,
    author: Author
}

export type AnswerComment = BaseComment;

export type QuestionComment = BaseComment;

export interface Question extends IDateable {
    id: string,
    title: string,
    content: string
    tags: string[],
    bookmarked: boolean,
    author: Author
}

export interface Author {
    id: string,
    urlPhoto: string | null ,
    userName: string
}

export interface AnswerCommentCreatePayload {
    content: string
}

export interface QuestionCommentCreatePayload {
    content: string
}

export interface AnswerCreatePayload {
    content: string
}

export interface QuestionCreatePayload {
    title: string,
    content: string
}