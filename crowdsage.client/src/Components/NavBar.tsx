import { Link } from "react-router";
import { useState } from "react";
import { useTranslation } from "react-i18next";

interface NavBarProps {
  isAuthenticated: boolean;
  userName?: string;
}

export default function NavBar({ isAuthenticated, userName }: NavBarProps) {
  const [search, setSearch] = useState("");
  const { t } = useTranslation();

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    // TODO: Implement search logic
  };

  return (
    <nav className="w-full bg-white dark:bg-gray-900 shadow-sm px-4 py-3 flex items-center justify-between">
      {/* Left section: Logo */}
      <Link to="/" className="text-2xl font-bold text-gray-800 dark:text-white hover:opacity-80 transition">
        CrowdSage
      </Link>

      {/* Center section: Search bar */}
      <form onSubmit={handleSearch} className="flex-1 px-6 max-w-xl w-full">
        <input
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder={t('navBar.searchPlaceholder')}
          className="w-full bg-gray-100 dark:bg-gray-800 dark:text-white px-4 py-2 rounded-full border border-transparent dark:border-gray-700 focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
      </form>

      {/* Right section: Actions */}
      <div className="flex items-center gap-4">
        <Link to="/question/new">
          <button className="rounded-2xl px-4 py-2 dark:text-white dark:bg-gray-800 dark:hover:bg-gray-700">{t('navBar.newQuestion')}</button>
        </Link>

        {isAuthenticated ? (
          userName? (
            <div className="flex items-center justify-center w-10 h-10 bg-gray-100 dark:bg-gray-800 rounded-full">
                <span className="text-gray-600 dark:text-gray-300 font-medium">AB</span>
            </div>
          ) : (
          <img
            className="w-10 h-10 rounded-full"
            src="https://via.placeholder.com/150"
            alt="User Avatar"
            />)
        ) : (
          <Link to="/signin" className="text-gray-700 dark:text-gray-300 hover:underline">
            {t('navBar.signIn')}
          </Link>
        )}
      </div>
    </nav>
  );
}
