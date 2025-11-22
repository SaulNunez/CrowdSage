import { Link } from "react-router";
import { useState } from "react";
import { Avatar, AvatarImage, AvatarFallback } from "@/components/ui/avatar";

interface NavBarProps {
  isAuthenticated: boolean;
  userName?: string;
}

export default function NavBar({ isAuthenticated, userName }: NavBarProps) {
  const [search, setSearch] = useState("");

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    // TODO: Implement search logic
  };

  return (
    <nav className="w-full bg-white shadow-sm px-4 py-3 flex items-center justify-between">
      {/* Left section: Logo */}
      <Link to="/" className="text-2xl font-bold text-gray-800 hover:opacity-80 transition">
        CrowdSage
      </Link>

      {/* Center section: Search bar */}
      <form onSubmit={handleSearch} className="flex-1 px-6 max-w-xl w-full">
        <input
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Search questions..."
          className="w-full"
        />
      </form>

      {/* Right section: Actions */}
      <div className="flex items-center gap-4">
        <Link to="/question/new">
          <button className="rounded-2xl px-4 py-2">New Question</button>
        </Link>

        {isAuthenticated ? (
          userName? (
            <div className="flex items-center justify-center w-10 h-10 bg-gray-100 rounded-full">
                <span className="text-gray-600 font-medium">AB</span>
            </div>
          ) : (
          <img
            className="w-10 h-10 rounded-full"
            src="https://via.placeholder.com/150"
            alt="User Avatar"
            />)
        ) : (
          <Link to="/signin" className="text-gray-700 hover:underline">
            Sign In
          </Link>
        )}
      </div>
    </nav>
  );
}
