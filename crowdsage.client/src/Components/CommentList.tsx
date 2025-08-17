import { useQuery } from '@tanstack/react-query';
import React from 'react';
import { Loading } from './Loading';
import { ServerError } from './ServerError';

interface Answer {
  id: string,
  content: string,
  createdAt: Date,
  editedAt: Date,
  author: string,
}

export function CommentList({questionId : string}) {
    const { isPending, error, data: comments } = useQuery<Answer[]>({
      queryKey: ["comments_question", questionId],
      queryFn: () => 
        fetch("/api/question/1/comments").then((res) => res.json()),
    });

  if (isPending) return <Loading />;
  if (error) return <ServerError />;

  if (!comments.length) return <div className="text-sm text-gray-500 mt-2">No comments yet.</div>;
  return (
    <ul className="mt-2 space-y-2">
      {comments.map((c) => (
        <li key={c.id} className="flex items-start gap-3 text-sm">
          <div className="w-8 h-8 rounded-full bg-gray-100 flex items-center justify-center text-gray-700">{c.author[0]?.toUpperCase()}</div>
          <div>
            <div className="text-gray-800 font-medium">{c.author}</div>
            <div className="text-gray-600">{c.content}</div>
          </div>
        </li>
      ))}
    </ul>
  );
}