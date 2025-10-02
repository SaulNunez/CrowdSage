import React, { useState } from 'react';

export 
function CommentForm({ onSubmit }: { onSubmit: (text: string) => void }) {
  const [text, setText] = useState<string>("");
  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        onSubmit(text);
        setText("");
      }}
      className="mt-3"
    >
      <div className="flex gap-2">
        <input
          className="flex-1 border rounded p-2 text-sm focus:outline-none"
          placeholder="Add a comment..."
          value={text}
          onChange={(e) => setText(e.target.value)}
        />
        <button type="submit" className="px-3 py-2 bg-gray-100 border rounded text-sm">Comment</button>
      </div>
    </form>
  );
}