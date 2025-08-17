import React, { useState } from "react";
import { AnswerCard } from "../Components/AnswerCard";
import { CommentList } from "../Components/CommentList";
import { CommentForm } from "../Components/CommentForm";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Loading } from "../Components/Loading";
import { ServerError } from "../Components/ServerError";

interface Question {
  title: string,
  content: string,
  createdAt: Date,
  editedAt: Date
  tags: string[],
  author: string,
  comments: { id: number, author: string, content: string }[]
}


export default function QuestionPage() {
  const [answers, setAnswers] = useState([
    {
      id: 11,
      author: "carol",
      postedAt: "Aug 7, 2025",
      body:
        "Use a custom useDebounce hook that returns the debounced value, then watch that with useEffect to call the API.",
      comments: [
        { id: 201, author: "dave", text: "Careful with stale closures — include deps." },
      ],
      votes: 12,
    },
    {
      id: 12,
      author: "erin",
      postedAt: "Aug 7, 2025",
      body:
        "Alternatively, use lodash.debounce directly inside useEffect and cleanup on unmount.",
      comments: [],
      votes: 3,
    },
  ]);

  const queryClient = useQueryClient();

  const { isPending, error, data: question } = useQuery<Question>({
    queryKey: ["question", "1"],
    queryFn: () => 
      fetch("/api/question/1").then((res) => res.json()),
  });

  const addAnswerMutation = useMutation({
    mutationFn: (newAnswer) => 
      fetch("/api/question/1/answers", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(newAnswer),
      }).then((res) => res.json()),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["question", "1"] });
    },
  })

  // UI state
  const [newAnswerText, setNewAnswerText] = useState("");

  // Generic helper to add comment to either question or a specific answer
  function addComment({ targetType, targetId, author = "you", text }) {
    if (!text || !text.trim()) return;
    const comment = { id: Date.now(), author, text: text.trim() };

    if (targetType === "question") {
      setQuestion((q) => ({ ...q, comments: [...q.comments, comment] }));
    } else if (targetType === "answer") {
      setAnswers((ans) =>
        ans.map((a) => (a.id === targetId ? { ...a, comments: [...a.comments, comment] } : a))
      );
    }
  }

  function addAnswer() {
    if (!newAnswerText.trim()) return;
    
    addAnswerMutation.mutate({
      content: newAnswerText.trim() 
    });

    setNewAnswerText("");
  }

  function upvoteAnswer(id) {
    setAnswers((a) => a.map((ans) => (ans.id === id ? { ...ans, votes: ans.votes + 1 } : ans)));
  }

  if (isPending) return <Loading />;
  if (error) return <ServerError />;

  return (
    <div className="min-h-screen bg-gray-50 p-6">
      <div className="max-w-4xl mx-auto">
        <header className="mb-6">
          <h1 className="text-3xl font-bold text-gray-900">{question.title}</h1>
          <div className="mt-2 flex items-center text-sm text-gray-600 gap-3">
            <span>asked by <strong className="text-gray-800">{question.author}</strong></span>
            <span>•</span>
            <span>{question.createdAt.toDateString()}</span>
            <div className="ml-auto flex gap-2">
              <button className="px-3 py-1 rounded-md border text-sm">Edit</button>
              <button className="px-3 py-1 rounded-md bg-red-50 text-red-700 text-sm">Follow</button>
            </div>
          </div>
        </header>

        <main className="bg-white rounded-lg shadow-sm p-6">
          <section className="prose max-w-none">
            <p>{question.content}</p>
          </section>

          <div className="mt-4 flex flex-wrap gap-2">
            {question.tags.map((t) => (
              <span key={t} className="px-2 py-1 bg-blue-50 text-blue-700 rounded text-sm">
                {t}
              </span>
            ))}
          </div>

          <div className="mt-6 border-t pt-4">
            <h3 className="text-lg font-medium">Comments</h3>
            <CommentList questionId="1"/>
            <CommentForm onSubmit={(text) => addComment({ targetType: "question", text })} />
          </div>
        </main>

        <section className="mt-8">
          <h2 className="text-2xl font-semibold">{answers.length} Answers</h2>

          <div className="mt-4 space-y-4">
            {answers.map((a) => (
              <AnswerCard
                key={a.id}
                answer={a}
                onComment={(text) => addComment({ targetType: "answer", targetId: a.id, text })}
                onUpvote={() => upvoteAnswer(a.id)}
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
              placeholder="Write your answer..."
            />
            <div className="mt-3 flex gap-2">
              <button onClick={addAnswer} className="px-4 py-2 bg-blue-600 text-white rounded">
                Post Your Answer
              </button>
              <button onClick={() => setNewAnswerText("")} className="px-4 py-2 border rounded">
                Cancel
              </button>
            </div>
          </div>
        </section>
      </div>
    </div>
  );
}