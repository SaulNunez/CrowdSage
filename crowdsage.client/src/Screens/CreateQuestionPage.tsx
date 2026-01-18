import React, { useState } from "react";
import { useNavigate } from "react-router";
import { useAddQuestionMutation } from "../common/reducers";
import MarkdownEditor from "../Shared/MarkdownEditor";

export default function CreateQuestionPage() {
    const [title, setTitle] = useState("");
    const [content, setContent] = useState("");
    const [addQuestion, { isLoading }] = useAddQuestionMutation();
    const navigate = useNavigate();

    async function handleSubmit(e: React.FormEvent) {
        e.preventDefault();
        if (!title.trim() || !content.trim()) return;

        try {
            await addQuestion({ title, content, tags: [] }).unwrap();
            navigate("/");
        } catch (error) {
            console.error("Failed to post question:", error);
        }
    }

    return (
        <form onSubmit={handleSubmit} className="max-w-3xl mx-auto p-6 bg-white rounded-lg shadow-md">
            <h1 className="text-2xl font-bold mb-4">Create New Question</h1>
            <div className="mb-4">
                <label className="block text-gray-700 font-semibold mb-2" htmlFor="title">
                    Title
                </label>
                <input
                    type="text"
                    id="title"
                    value={title}
                    onChange={(e) => setTitle(e.target.value)}
                    className="w-full border rounded-lg p-3 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100 focus:ring-2 focus:ring-blue-500 focus:outline-none"
                    placeholder="Enter your question title"
                />
            </div>
            <div className="mb-4">
                <label className="block text-gray-700 font-semibold mb-2" htmlFor="content">
                    Content
                </label>
                <MarkdownEditor
                    textInputProps={{
                        id: "content",
                        rows: 10,
                        placeholder: "Write your question... (markdown supported)",
                        value: content,
                        onChange: (e: React.ChangeEvent<HTMLTextAreaElement>) => setContent(e.target.value)
                    }}
                    className="w-full border rounded-lg p-3 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100 focus:ring-2 focus:ring-blue-500 focus:outline-none"
                />
            </div>
            <div className="flex gap-4">
                <button
                    type="submit"
                    disabled={isLoading}
                    className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition"
                >
                    {isLoading ? "Posting..." : "Post Question"}
                </button>
                <button
                    type="button"
                    onClick={() => navigate(-1)}
                    className="px-4 py-2 border rounded-lg hover:bg-gray-100 transition"
                >
                    Cancel
                </button>
            </div>
        </form>
    );
}