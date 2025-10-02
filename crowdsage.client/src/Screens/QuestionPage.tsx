import { useState } from "react";
import { AnswerCard } from "../Components/AnswerCard";
import { CommentList } from "../Components/CommentList";
import { CommentForm } from "../Components/CommentForm";
import { mutationOptions, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Loading } from "../Components/Loading";
import { ServerError } from "../Components/ServerError";
import ReactMarkdown from "react-markdown";
import type { Answer, AnswerCreatePayload, Question, QuestionComment, QuestionCommentCreatePayload } from "../types";
import axios from 'axios';
import { useParams } from "react-router";

function QuestionCommentSection({ questionId }: { questionId: string }) {
  const queryClient = useQueryClient();

  const { isPending, error, data } = useQuery<QuestionComment[]>({
    queryKey: ["question_comment", questionId],
    queryFn: () =>
      axios.get(`/api/questions/${questionId}/comments`),
  });

  const addCommentRequest = async (newAnswer: QuestionCommentCreatePayload) => {
    const { data } = await axios.post(`/api/questions/${questionId}/comments`, newAnswer);
    return data;
  };

function groupMutationOptions() {
  return mutationOptions({
    mutationKey: ['answer_comment'],
    mutationFn: addCommentRequest,
  });
}

const addCommentMutation = useMutation({
  ...groupMutationOptions(),
  onSuccess: () => {
    queryClient.invalidateQueries({ queryKey: ["question_comment", questionId] });
  },
});

const addComment = (comment: string) => addCommentMutation.mutate({ content: comment });

if (isPending) return <Loading />
if (error) return <ServerError />

return (
  <div className="mt-6 border-t pt-4">
    <h3 className="text-lg font-medium">Comments</h3>
    <CommentList comments={data} />

    <CommentForm onSubmit={(text) => addComment(text)} />
  </div>
);
}

function AnswerSection({ questionId }: { questionId: string }) {
  const queryClient = useQueryClient();
  function upvoteAnswer(id: string) {
    console.log(`Upvoted ${id}`);
  }

  const { isPending, error, data } = useQuery<Answer[]>({
    queryKey: ["answers", questionId],
    queryFn: () => axios.get(`/api/questions/${questionId}/answers`),
  });

  // UI state
  const [newAnswerText, setNewAnswerText] = useState("");

  const addAnswerRequest = async (newAnswer: AnswerCreatePayload) => {
    const { data } = await axios.post(`/api/questions/${questionId}/answers`, newAnswer);
    return data;
  };

  function groupMutationOptions() {
    return mutationOptions({
      mutationKey: ['add_answer'],
      mutationFn: addAnswerRequest,
    });
  }

  const addAnswerMutation = useMutation({
    ...groupMutationOptions(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["question", "1"] });
    },
  });

  function addAnswer() {
    if (!newAnswerText.trim()) return;

    addAnswerMutation.mutate({
      content: newAnswerText.trim()
    },
      {
        onSuccess: () => {
          setNewAnswerText("");
        }
      });
  }

  function toggleBookmarkAnswer(id: string) {
    //setAnswers((a) => a.map((ans) => (ans.id === id ? { ...ans, bookmarked: !ans.bookmarked } : ans)));
    console.log(`Bookmarked ${id}`);
  }

  if (isPending) return <Loading />
  if (error) return <ServerError />

  return (
    <section className="mt-8">
      {<h2 className="text-2xl font-semibold">{data.length} Answers</h2>}

      <div className="mt-4 space-y-4">
        {
          data.map((a) => (
            <AnswerCard
              key={a.id}
              answer={a}
              onUpvote={() => upvoteAnswer(a.id)}
              onBookmark={() => toggleBookmarkAnswer(a.id)}
            />
          ))}
      </div>

      <div className="mt-8 bg-white rounded-lg shadow-sm p-6">
        <h3 className="text-lg font-medium mb-2">Your Answer</h3>
        <textarea
          value={newAnswerText}
          onChange={(e) => setNewAnswerText(e.target.value)}
          rows={5}
          className="w-full border rounded p-3 focus:outline-none focus:ring"
          placeholder="Write your answer with markdown support..."
        />
        <div className="mt-3 flex gap-2">
          <button onClick={addAnswer} disabled={addAnswerMutation.isPending} className="px-4 py-2 bg-blue-600 text-white rounded">
            Post Your Answer
          </button>
          <button onClick={() => setNewAnswerText("")} className="px-4 py-2 border rounded">
            Cancel
          </button>
        </div>
      </div>
    </section>
  )
}

export default function QuestionPage() {
  const { questionId }  = useParams();

  const { isPending, error, data: question } = useQuery<Question>({
    queryKey: ["question", questionId],
    queryFn: async () => {
      const { data } = await axios.get(`/api/question/${questionId}`);
      return data;
    },
  });

  function toggleBookmarkQuestion() {
    //setQuestion((q) => ({ ...q, bookmarked: !q.bookmarked }));
  }

  if (isPending) return <Loading />;
  if (error) return <ServerError />;

  return (
    <div className="min-h-screen bg-gray-50 p-6">
      <div className="max-w-4xl mx-auto">
        <header className="mb-6">
          <h1 className="text-3xl font-bold text-gray-900">{question.title}</h1>
          <div className="mt-2 flex items-center text-sm text-gray-600 gap-3">
            <span>asked by <strong className="text-gray-800">{question.author.userName}</strong></span>
            <span>•</span>
            <span>{question.createdAt.toDateString()}</span>
            <div className="ml-auto flex gap-2">
              <button className="px-3 py-1 rounded-md border text-sm">Edit</button>
              <button className="px-3 py-1 rounded-md bg-red-50 text-red-700 text-sm">Follow</button>
              <button
                onClick={toggleBookmarkQuestion}
                className="px-3 py-1 rounded-md border text-sm"
              >
                {question.bookmarked ? "Bookmarked" : "Bookmark"}
              </button>
            </div>
          </div>
        </header>

        <main className="bg-white rounded-lg shadow-sm p-6">
          <section className="prose max-w-none">
            <ReactMarkdown>{question.content}</ReactMarkdown>
          </section>

          <div className="mt-4 flex flex-wrap gap-2">
            {question.tags.map((t) => (
              <span key={t} className="px-2 py-1 bg-blue-50 text-blue-700 rounded text-sm">
                {t}
              </span>
            ))}
          </div>

          <QuestionCommentSection questionId={questionId} />

          <AnswerSection questionId={questionId} />
        </main>
      </div>
    </div>
  );
}