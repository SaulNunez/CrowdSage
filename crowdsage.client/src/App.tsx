import { useEffect, useState } from 'react';
import './App.css';
import type { Question } from './types';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import axios from 'axios';
import { Loading } from './Components/Loading';
import { ServerError } from './Components/ServerError';


// LandingPage Component
export default function App() {
  const [darkMode, setDarkMode] = useState<boolean>(false);
  const [page] = useState(1);

  const newQuestionsQuery = (page: number, resultsPerPage: number): Promise<Question[]> => 
      axios.get(`/api/questions/new/?page=${page}&take=${resultsPerPage}`).then(res => res.data);

    const { isPending, error, data: questions } = useQuery({
    queryKey: ["question_new", page],
    queryFn: () => newQuestionsQuery(page, 10),
    placeholderData: keepPreviousData
  });

  // Toggle system mode
  useEffect(() => {
    if (darkMode) {
      document.documentElement.classList.add("dark");
    } else {
      document.documentElement.classList.remove("dark");
    }
  }, [darkMode]);

  if (isPending) return <Loading />
  if (error) return <ServerError />

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900 transition-colors">
      <div className="max-w-5xl mx-auto px-6 py-10">
        <header className="flex justify-between items-center mb-8">
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

        <div className="space-y-6">
          {questions.map((q) => (
            <article
              key={q.id}
              className="p-6 bg-white dark:bg-gray-800 rounded-2xl shadow-sm border dark:border-gray-700 transition"
            >
              <h2 className="text-xl font-semibold text-blue-600 dark:text-blue-400 hover:underline cursor-pointer">
                {q.title}
              </h2>
              <div className="mt-2 flex flex-wrap gap-2">
                {q.tags.map((tag) => (
                  <span
                    key={tag}
                    className="px-2 py-1 bg-blue-50 dark:bg-blue-900/40 text-blue-700 dark:text-blue-300 rounded text-xs"
                  >
                    {tag}
                  </span>
                ))}
              </div>
              <div className="mt-3 text-sm text-gray-600 dark:text-gray-400 flex items-center gap-2">
                <span>
                  Asked by <strong className="text-gray-800 dark:text-gray-200">{q.author.userName}</strong>
                </span>
                <span>•</span>
                <span>{q.createdAt.toLocaleDateString()}</span>
              </div>
            </article>
          ))}
        </div>
      </div>
    </div>
  );
}