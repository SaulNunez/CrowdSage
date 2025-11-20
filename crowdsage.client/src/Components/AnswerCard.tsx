import { useState } from 'react';
import { CommentList } from './CommentList';
import { CommentForm } from './CommentForm';
import { useMutation, useQuery, useQueryClient, mutationOptions } from '@tanstack/react-query';
import ReactMarkdown from 'react-markdown';
import type { Answer, AnswerComment, AnswerCommentCreatePayload } from '../types';
import { ServerError } from './ServerError';
import { Loading } from './Loading';
import axios from 'axios';
import { useAddCommentForAnswerMutation, useGetCommentsForAnswerQuery } from '../common/reducers';

interface AnswerCardProps {
  answer: Answer;
  onUpvote: () => void;
  onBookmark: () => void;
}

export function AnswerCard({ answer, onUpvote, onBookmark }: AnswerCardProps) {

  const questionId = "";
  const answerId = "";

  const { data: answerComments, isLoading, error } = useGetCommentsForAnswerQuery({answerId, questionId});
  const [ addAnswerComment, { isLoading: addingComment } ] = useAddCommentForAnswerMutation();

  const createComment = (content: string) => {
    addAnswerComment({ data: { content }, questionId, answerId });
  };

  const [showCommentForm, setShowCommentForm] = useState<boolean>(false);

  if (isLoading) return <Loading />;
  if (error) return <ServerError />;
  
  return (
    <article className="bg-white rounded-lg shadow p-5 flex gap-4">
      <div className="w-16 flex flex-col items-center text-center">
        <button
          onClick={onUpvote}
          className="w-10 h-10 flex items-center justify-center rounded border text-sm"
          aria-label="Upvote"
        >
          ▲
        </button>
        <div className="mt-2 text-sm font-medium">{answer.votes}</div>
      </div>

      <div className="flex-1">
        <div className="flex items-start justify-between gap-4">
          <div>
            <div className="text-sm text-gray-700">
              Answered by <strong className="text-gray-900">{answer.author.userName}</strong>
              <span className="text-gray-500"> • {answer.createdAt.toLocaleDateString()}</span>
            </div>
          </div>
          <button
            onClick={onBookmark}
            className="text-sm px-2 py-1 border rounded"
          >
            {answer.bookmarked ? "Bookmarked" : "Bookmark"}
          </button>
        </div>

        <div className="mt-3 prose max-w-none">
          <ReactMarkdown>{answer.content}</ReactMarkdown>
        </div>

        <div className="mt-4 border-t pt-3">
          <h4 className="text-sm font-medium">Comments</h4>
          <CommentList comments={answerComments} />
          {showCommentForm ? (
            <CommentForm
              onSubmit={(text) => {
                createComment(text);
                setShowCommentForm(false);
              }}
            />
          ) : (
            <button
              disabled={addingComment}
              onClick={() => setShowCommentForm(true)}
              className="mt-2 text-sm text-blue-600"
            >
              Add a comment
            </button>
          )}
        </div>
      </div>
    </article>
  );
}