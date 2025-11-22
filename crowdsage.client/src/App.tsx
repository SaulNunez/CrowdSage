import { useEffect, useMemo, useState } from 'react';
import './App.css';
import { Loading } from './Components/Loading';
import { ServerError } from './Components/ServerError';
import { useNavigate } from 'react-router';
import { useGetNewQuestionsQuery } from './common/reducers';
import type { Question } from './types';
import { QuestionCard } from './Components/QuestionCard';


// LandingPage Component
export default function App() {
  const [search, setSearch] = useState<string>("");
  const [selectedTag, setSelectedTag] = useState<string>("All");
  const navigate = useNavigate();
  const [darkMode, setDarkMode] = useState<boolean>(false);
  const [page] = useState(1);

  const {data: questions, isLoading, error} = useGetNewQuestionsQuery({page: page, take: 10});

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
    return questions?.filter((q) => {
      const matchesSearch =
        q.title.toLowerCase().includes(search.toLowerCase()) ||
        q.content.toLowerCase().includes(search.toLowerCase());
      const matchesTag = selectedTag === "All" || q.tags.includes(selectedTag);
      return matchesSearch && matchesTag;
    });
  }, [questions, search, selectedTag]);

  if (isLoading) return <Loading />
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
            <QuestionCard question={q} id={q.id} />
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
