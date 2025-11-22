import { useNavigate } from "react-router";
import type { Question } from "../types";

export function QuestionCard({question} : {question: Question}) {
  const navigate = useNavigate();
  return (
  <article
    key={question.id}
    onClick={() => navigate(`/questions/${question.id}`)}
    className="bg-white dark:bg-gray-800 rounded-2xl shadow p-6 flex flex-col gap-4 transition hover:shadow-md hover:-translate-y-1 cursor-pointer border dark:border-gray-700"
  >
    {/* Title + bookmark */}
    <div className="flex justify-between items-start">
      <h2 className="text-xl font-semibold text-blue-600 dark:text-blue-400 hover:underline">
        {question.title}
      </h2>
      <button
        onClick={(e) => {
          e.stopPropagation();
          /*setQuestions((prev) =>
            prev.map((x) =>
              x.id === q.id ? { ...x, bookmarked: !x.bookmarked } : x
            )
          );*/
        } }
        className={`p-2 rounded-full border ${question.bookmarked
            ? "text-yellow-500 border-yellow-400"
            : "text-gray-500 border-gray-300 dark:border-gray-600"} hover:bg-gray-100 dark:hover:bg-gray-700`}
        aria-label="Bookmark question"
      >
        {question.bookmarked ? "★" : "☆"}
      </button>
    </div>

    {/* Content preview */}
    <p className="text-gray-700 dark:text-gray-300 text-sm line-clamp-2">
      {question.content}
    </p>

    {/* Tags */}
    <div className="flex flex-wrap gap-2">
      {question.tags.map((tag) => (
        <span
          key={tag}
          className="px-2 py-1 bg-blue-50 dark:bg-blue-900/40 text-blue-700 dark:text-blue-300 rounded text-xs"
        >
          {tag}
        </span>
      ))}
    </div>

    {/* Author + Date */}
    <div className="flex items-center justify-between text-sm text-gray-600 dark:text-gray-400 pt-3 border-t dark:border-gray-700">
      <div>
        Asked by{" "}
        <strong className="text-gray-800 dark:text-gray-200">
          {question.author.userName}
        </strong>
      </div>
      <div>{question.createdAt.toLocaleDateString()}</div>
    </div>
  </article>
  );
}