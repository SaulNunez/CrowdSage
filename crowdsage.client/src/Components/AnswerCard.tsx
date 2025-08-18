import React, { useState } from 'react';
import { CommentList } from './CommentList';
import { CommentForm } from './CommentForm';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

interface AnswerComment {

}

export function AnswerCard({ answer, onUpvote }) {
  const queryClient = useQueryClient();

  const { isPending, error, data: answerCommentData } = useQuery<AnswerComment[]>({
    queryKey: ["answer_comment", "1"],
    queryFn: () =>
      fetch("/api/questions/1/answers/1/comments").then((res) => res.json()),
  });

  const addCommentMutation = useMutation({
    mutationFn: (newAnswer) =>
      fetch("/api/questions/1/answers/1/comments", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(newAnswer),
      }).then((res) => res.json()),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["answer_comment", "1"] });
    },
  });

  const createComment = (text) => {
    addCommentMutation.mutate({ content: text });
  };

  const [showCommentForm, setShowCommentForm] = useState(false);

  return (
    <article className="bg-white rounded-lg shadow p-5 flex gap-4">
      <div className="w-16 flex flex-col items-center text-center">
        <button
          onClick={onUpvote}
          className="w-10 h-10 flex items-center justify-center rounded border text-sm"
          aria-label="Upvote"
        >
        </button>
        <div className="mt-2 text-sm font-medium">{answer.votes}</div>
      </div>

      <div className="flex-1">
        <div className="flex items-start justify-between gap-4">
          <div>
            <div className="text-sm text-gray-700">
              Answered by <strong className="text-gray-900">{answer.author}</strong>
              <span className="text-gray-500"> • {answer.postedAt}</span>
            </div>
          </div>
        </div>

        <div className="mt-3 prose max-w-none">{answer.body}</div>

        <div className="mt-4 border-t pt-3">
          <h4 className="text-sm font-medium">Comments</h4>
          <CommentList comments={answerCommentData} />
          {showCommentForm ? (
            <CommentForm
              onSubmit={(text) => {
                createComment(text);
                setShowCommentForm(false);
              }}
            />
          ) : (
            <button
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