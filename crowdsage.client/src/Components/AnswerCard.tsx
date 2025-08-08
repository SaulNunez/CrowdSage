import React, { useState } from 'react';

export function AnswerCard({ answer, onComment, onUpvote }) {
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
          <CommentList comments={answer.comments} />
          {showCommentForm ? (
            <CommentForm
              onSubmit={(text) => {
                onComment(text);
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