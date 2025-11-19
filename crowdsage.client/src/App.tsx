import { useEffect, useMemo, useState } from 'react';
import './App.css';
import type { Question } from './types';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import axios from 'axios';
import { Loading } from './Components/Loading';
import { ServerError } from './Components/ServerError';
import { useNavigate } from 'react-router';


// LandingPage Component
export default function App() {
  const [search, setSearch] = useState<string>("");
  const [selectedTag, setSelectedTag] = useState<string>("All");
  const navigate = useNavigate();
  const [darkMode, setDarkMode] = useState<boolean>(false);
  const [page] = useState(1);

  const newQuestionsQuery = (page: number, resultsPerPage: number): Promise<Question[]> => 
      axios.get(`/api/questions/new/?page=${page}&take=${resultsPerPage}`).then(res => res.data);

    const { isPending, error, data: questions } = useQuery({
    queryKey: ["question_new", page],
    queryFn: () => newQuestionsQuery(page, 10),
    placeholderData: keepPreviousData
  });

  // Toggle dark mode
  useEffect(() => {
    if (darkMode) document.documentElement.classList.add("dark");
    else document.documentElement.classList.remove("dark");
  }, [darkMode]);


  // Extract all unique tags
  const allTags = useMemo(() => {
    const tags = Array.from(new Set(questions?.flatMap((q) => q.tags)));
    return ["All", ...tags];
  }, [questions]);

    // Filtered questions
  const filteredQuestions = useMemo(() => {
    return questions.filter((q) => {
      const matchesSearch =
        q.title.toLowerCase().includes(search.toLowerCase()) ||
        q.content.toLowerCase().includes(search.toLowerCase());
      const matchesTag = selectedTag === "All" || q.tags.includes(selectedTag);
      return matchesSearch && matchesTag;
    });
  }, [questions, search, selectedTag]);

  if (isPending) return <Loading />
  if (error) return <ServerError />

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900 transition-colors">
      <div className="max-w-5xl mx-auto px-6 py-10">
        {/* Header */}
        <header className="flex flex-col sm:flex-row sm:items-center sm:justify-between mb-8 gap-4">
          <h1 className="text-3xl font-bold text-gray-900 dark:text-gray-100">
            Latest Questions
          </h1>
          <button
            onClick={() => setDarkMode((prev) => !prev)}
            className="px-4 py-2 rounded-lg border text-sm dark:border-gray-600 dark:text-gray-200"
          >
            {darkMode ? "☀️ Light Mode" : "🌙 Dark Mode"}
          </button>
        </header>

        {/* Search and Filter */}
        <div className="flex flex-col sm:flex-row items-center gap-4 mb-8">
          <input
            type="text"
            placeholder="Search questions..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="flex-1 border rounded-lg p-3 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100 focus:ring-2 focus:ring-blue-500 focus:outline-none"
          />
          <select
            value={selectedTag}
            onChange={(e) => setSelectedTag(e.target.value)}
            className="border rounded-lg p-3 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100 focus:ring-2 focus:ring-blue-500 focus:outline-none"
          >
            {allTags.map((tag) => (
              <option key={tag} value={tag}>
                {tag}
              </option>
            ))}
          </select>
        </div>

        {/* Question List */}
        <div className="space-y-6">
          {filteredQuestions.map((q) => (
            <article
              key={q.id}
              onClick={() => navigate(`/questions/${q.id}`)}
              className="bg-white dark:bg-gray-800 rounded-2xl shadow p-6 flex flex-col gap-4 transition hover:shadow-md hover:-translate-y-1 cursor-pointer border dark:border-gray-700"
            >
              {/* Title + bookmark */}
              <div className="flex justify-between items-start">
                <h2 className="text-xl font-semibold text-blue-600 dark:text-blue-400 hover:underline">
                  {q.title}
                </h2>
                <button
                  onClick={(e) => {
                    e.stopPropagation();
                    /*setQuestions((prev) =>
                      prev.map((x) =>
                        x.id === q.id ? { ...x, bookmarked: !x.bookmarked } : x
                      )
                    );*/
                  }}
                  className={`p-2 rounded-full border ${
                    q.bookmarked
                      ? "text-yellow-500 border-yellow-400"
                      : "text-gray-500 border-gray-300 dark:border-gray-600"
                  } hover:bg-gray-100 dark:hover:bg-gray-700`}
                  aria-label="Bookmark question"
                >
                  {q.bookmarked ? "★" : "☆"}
                </button>
              </div>

              {/* Content preview */}
              <p className="text-gray-700 dark:text-gray-300 text-sm line-clamp-2">
                {q.content}
              </p>

              {/* Tags */}
              <div className="flex flex-wrap gap-2">
                {q.tags.map((tag) => (
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
                    {q.author.userName}
                  </strong>
                </div>
                <div>{q.createdAt.toLocaleDateString()}</div>
              </div>
            </article>
          ))}

          {filteredQuestions.length === 0 && (
            <p className="text-center text-gray-500 dark:text-gray-400 mt-10">
              No questions found.
            </p>
          )}
        </div>
      </div>
    </div>
  );
}
