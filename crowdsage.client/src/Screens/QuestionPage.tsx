import { useState } from "react";
import { AnswerCard } from "../Components/AnswerCard";
import { CommentList } from "../Components/CommentList";
import { CommentForm } from "../Components/CommentForm";
import { Loading } from "../Components/Loading";
import { ServerError } from "../Components/ServerError";
import ReactMarkdown from "react-markdown";
import { useParams } from "react-router";
import { useAddAnswerMutation, useAddQuestionCommentMutation, useGetAnswersForQuestionQuery, useGetCommentsForQuestionQuery, useGetQuestionByIdQuery, useUpvoteQuestionMutation } from "../common/reducers";

function QuestionCommentSection({ questionId }: { questionId: string }) {
  const { data, isLoading, error} = useGetCommentsForQuestionQuery(questionId);

  const [addComment, { isLoading: currentlyAddingComment }] = useAddQuestionCommentMutation();

  if (isLoading) return <Loading />
  if (error) return <ServerError />

  return (
    <div className="mt-6 border-t pt-4">
      <h3 className="text-lg font-medium">Comments</h3>
      {data && data.length > 0 ? (
        <CommentList comments={data} />
      ) : (
        <div className="text-sm text-gray-500 mt-2">No comments yet.</div>
      )}

      <CommentForm onSubmit={(content) => addComment({data: {content}, questionId})} />
    </div>
  );
}

function AnswerSection({ questionId }: { questionId: string }) {
  const { data, isLoading, error} = useGetAnswersForQuestionQuery(questionId);

  // UI state
  const [newAnswerText, setNewAnswerText] = useState("");

  const [createAnswer, { isLoading: addingAnswer}] = useAddAnswerMutation();

  async function addAnswer() {
    if (!newAnswerText.trim()) return;

    try {
      await createAnswer({data: {content: newAnswerText.trim()}, questionId});
      setNewAnswerText("");
    } catch (error) {
      console.error("Error adding answer:", error);
    }
  }

  if (isLoading) return <Loading />
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
              questionId={questionId}
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
          <button onClick={addAnswer} disabled={addingAnswer} className="px-4 py-2 bg-blue-600 text-white rounded">
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

  const {data: question, isLoading, error} = useGetQuestionByIdQuery(questionId!);
  const [upvoteQuestion] = useUpvoteQuestionMutation();

  function toggleBookmarkQuestion() {
    //setQuestion((q) => ({ ...q, bookmarked: !q.bookmarked }));
  }

  function handleUpvote() {
    if (questionId) {
      upvoteQuestion({ questionId, voteInput: 'Upvote' });
    }
  }

  if (isLoading) return <Loading />;
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
          <div className="flex gap-4">
            <div className="w-16 flex flex-col items-center text-center">
              <button
                onClick={handleUpvote}
                className="w-10 h-10 flex items-center justify-center rounded border text-sm hover:bg-gray-100"
                aria-label="Upvote"
              >
                ▲
              </button>
              <div className="mt-2 text-sm font-medium">{question.votes}</div>
            </div>
            <div className="flex-1">
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

              {questionId? <QuestionCommentSection questionId={questionId} /> : null}
            </div>
          </div>

          {questionId? <AnswerSection questionId={questionId} /> : null}
        </main>
      </div>
    </div>
  );
}