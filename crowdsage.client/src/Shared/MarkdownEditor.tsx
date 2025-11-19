import { useRef, useState, useEffect } from "react";

type Props = {
  value?: string;
  onChange?: (val: string) => void;
  uploadImage?: (file: File) => Promise<string>;
  className?: string;
};

export default function MarkdownEditor({ value = "", onChange, uploadImage, className = "" }: Props) {
  const [text, setText] = useState<string>(value);
  const textareaRef = useRef<HTMLTextAreaElement | null>(null);

  // modals
  const [showLinkModal, setShowLinkModal] = useState(false);
  const [linkTitle, setLinkTitle] = useState("");
  const [linkUrl, setLinkUrl] = useState("");

  const [showImageModal, setShowImageModal] = useState(false);
  const [imageTitle, setImageTitle] = useState("");
  const [imageUrl, setImageUrl] = useState("");
  const [imageFile, setImageFile] = useState<File | null>(null);
  const [uploading, setUploading] = useState(false);

  useEffect(() => {
    setText(value);
  }, [value]);

  function updateText(newText: string, newSelection?: { start: number; end: number }) {
    setText(newText);
    onChange?.(newText);
    if (typeof newSelection !== "undefined" && textareaRef.current) {
      const t = textareaRef.current;
      // need a tiny timeout to ensure DOM updated
      setTimeout(() => {
        t.focus();
        t.setSelectionRange(newSelection.start, newSelection.end);
      }, 0);
    }
  }

  function getLineStart(position: number) {
    return text.lastIndexOf("\n", position - 1) + 1; // returns 0 when not found
  }

  function applyToSelectedLines(transform: (line: string, indexInSelection: number) => string) {
    const ta = textareaRef.current;
    if (!ta) return;
    const start = ta.selectionStart;
    const end = ta.selectionEnd;
    const full = text;
    const before = full.slice(0, start);
    const selected = full.slice(start, end);
    const after = full.slice(end);

    // figure lines that are affected: lines that intersect selection
    const selStartLine = full.slice(0, start).split("\n");
    const startLineIndex = selStartLine.length - 1;

    const between = full.slice(start, end).split("\n");

    // compute the absolute line array for whole document
    const allLines = full.split("\n");

    // We'll replace lines from startLineIndex to startLineIndex + between.length - 1
    const newLines = [...allLines];
    for (let i = 0; i < between.length; i++) {
      const li = startLineIndex + i;
      newLines[li] = transform(newLines[li], i);
    }

    const newText = newLines.join("\n");

    // compute new selection range: we want to include transformed lines
    // find new selection start: index of startLineIndex-th line start
    let newStart = 0;
    for (let i = 0; i < startLineIndex; i++) newStart += newLines[i].length + 1;
    // new end: include the number of lines we've transformed
    let newEnd = newStart;
    for (let i = 0; i < between.length; i++) newEnd += newLines[startLineIndex + i].length + 1;
    // if last line, remove trailing +1
    if (between.length > 0) newEnd -= 1;

    updateText(newText, { start: newStart, end: newEnd });
  }

  function insertWrapping(open: string, close: string) {
    const ta = textareaRef.current;
    if (!ta) return;
    const start = ta.selectionStart;
    const end = ta.selectionEnd;

    if (start !== end) {
      // wrap selected text
      const newText = text.slice(0, start) + open + text.slice(start, end) + close + text.slice(end);
      updateText(newText, { start: start + open.length, end: end + open.length });
    } else {
      // cursor only: insert open+close and place cursor between
      const newText = text.slice(0, start) + open + close + text.slice(end);
      const caret = start + open.length;
      updateText(newText, { start: caret, end: caret });
    }
  }

  function onBold() {
    insertWrapping("**", "**");
  }
  function onItalic() {
    insertWrapping("*", "*");
  }
  function onInlineCode() {
    insertWrapping("`", "`");
  }

  function onHeading(level: number) {
    // apply to each line that intersects selection; if no selection, use current line
    const ta = textareaRef.current;
    if (!ta) return;
    const start = ta.selectionStart;
    const end = ta.selectionEnd;
    const lines = text.split("\n");

    applyToSelectedLines((line) => {
      const trimmed = line.replace(/^#{1,6}\s*/, "");
      return "#".repeat(level) + (trimmed.length ? " " + trimmed : "");
    });
  }

  function onBlockquote() {
    applyToSelectedLines((line) => {
      if (/^>\s?/.test(line)) return line; // don't double
      return `> ${line}`;
    });
  }

  function onUnorderedList() {
    applyToSelectedLines((line) => {
      if (/^(-|\*|\+)\s/.test(line)) return line;
      return `- ${line}`;
    });
  }

  function onOrderedList() {
    const ta = textareaRef.current;
    if (!ta) return;
    const start = ta.selectionStart;
    const end = ta.selectionEnd;
    const full = text;

    const selStartLine = full.slice(0, start).split("\n");
    const startLineIndex = selStartLine.length - 1;
    const between = full.slice(start, end).split("\n");

    applyToSelectedLines((line, idx) => {
      // if already ordered, leave
      if (/^\d+\.\s/.test(line)) return line;
      return `${idx + 1}. ${line}`;
    });
  }

  function onTaskList() {
    applyToSelectedLines((line) => {
      if (/^- \[.\]\s/.test(line)) return line;
      return `- [ ] ${line}`;
    });
  }

  async function onLinkInsert() {
    setShowLinkModal(true);
    setLinkTitle("");
    setLinkUrl("");
  }

  function confirmLink() {
    const ta = textareaRef.current;
    if (!ta) return;
    const pos = ta.selectionStart;
    const md = `[${linkTitle || "link"}](${linkUrl || ""})`;
    const newText = text.slice(0, pos) + md + text.slice(pos);
    updateText(newText, { start: pos + md.length, end: pos + md.length });
    setShowLinkModal(false);
  }

  async function onImageInsert() {
    setShowImageModal(true);
    setImageTitle("");
    setImageUrl("");
    setImageFile(null);
  }

  async function confirmImage() {
    const ta = textareaRef.current;
    if (!ta) return;
    let finalUrl = imageUrl;
    if (imageFile && uploadImage) {
      setUploading(true);
      try {
        finalUrl = await uploadImage(imageFile);
      } catch (e) {
        // failed upload - keep provided url
        console.error(e);
      } finally {
        setUploading(false);
      }
    }
    const pos = ta.selectionStart;
    const md = `![${imageTitle || "img"}](${finalUrl || ""})`;
    const newText = text.slice(0, pos) + md + text.slice(pos);
    updateText(newText, { start: pos + md.length, end: pos + md.length });
    setShowImageModal(false);
  }

  function onCodeBlock() {
    const ta = textareaRef.current;
    if (!ta) return;
    const start = ta.selectionStart;
    const end = ta.selectionEnd;

    const lineStart = getLineStart(start);
    const lineEnd = text.indexOf("\n", start) === -1 ? text.length : text.indexOf("\n", start);
    const isStartOfLine = start === lineStart;
    const hasTextInLine = lineEnd > lineStart && text.slice(lineStart, lineEnd).trim().length > 0;

    let insertPos = start;
    let newText = text;

    if (!isStartOfLine) {
      // insert a newline at cursor position then create block
      newText = text.slice(0, start) + "\n" + text.slice(start);
      insertPos = start + 1; // after the new line
    } else if (hasTextInLine) {
      // move existing text down one line
      const rest = text.slice(lineStart, text.length);
      // find where this line ends
      const thisLineEnd = lineEnd;
      newText = text.slice(0, thisLineEnd) + "\n" + text.slice(thisLineEnd);
      insertPos = thisLineEnd + 1;
    }

    // insert code block at insertPos
    const block = "```\n\n```\n";
    newText = newText.slice(0, insertPos) + block + newText.slice(insertPos);
    // place cursor between the newlines inside the codeblock
    const caret = insertPos + 4; // after ```\n
    updateText(newText, { start: caret, end: caret });
  }

  // toolbar
  return (
    <div className={`markdown-editor ${className}`}>
      <div className="bg-gray-50 border rounded-t p-2 flex gap-2 items-center">
        <div className="flex gap-1">
          <button onClick={onBold} className="px-2 py-1 rounded hover:bg-gray-100">B</button>
          <button onClick={onItalic} className="px-2 py-1 rounded hover:bg-gray-100">I</button>
          <button onClick={onInlineCode} className="px-2 py-1 rounded hover:bg-gray-100">`code`</button>
        </div>

        <div className="ml-2">
          <label className="mr-1">Heading:</label>
          <select
            onChange={(e) => onHeading(Number(e.target.value))}
            defaultValue={0}
            className="p-1 rounded border"
          >
            <option value={0}>Select</option>
            <option value={1}>H1</option>
            <option value={2}>H2</option>
            <option value={3}>H3</option>
            <option value={4}>H4</option>
            <option value={5}>H5</option>
            <option value={6}>H6</option>
          </select>
        </div>

        <div className="flex gap-1 ml-2">
          <button onClick={onLinkInsert} className="px-2 py-1 rounded hover:bg-gray-100">Link</button>
          <button onClick={onImageInsert} className="px-2 py-1 rounded hover:bg-gray-100">Image</button>
        </div>

        <div className="flex gap-1 ml-2">
          <button onClick={onOrderedList} className="px-2 py-1 rounded hover:bg-gray-100">OL</button>
          <button onClick={onUnorderedList} className="px-2 py-1 rounded hover:bg-gray-100">UL</button>
          <button onClick={onTaskList} className="px-2 py-1 rounded hover:bg-gray-100">Task</button>
        </div>

        <div className="ml-auto">
          <button onClick={onBlockquote} className="px-2 py-1 rounded hover:bg-gray-100">Quote</button>
          <button onClick={onCodeBlock} className="px-2 py-1 rounded hover:bg-gray-100">Code Block</button>
        </div>
      </div>

      <textarea
        ref={textareaRef}
        value={text}
        onChange={(e) => updateText(e.target.value)}
        className="w-full min-h-[240px] p-4 border rounded-b font-mono text-sm focus:outline-none"
      />

      {/* Link Modal */}
      {showLinkModal && (
        <div className="fixed inset-0 flex items-center justify-center bg-black/40">
          <div className="bg-white p-4 rounded shadow w-96">
            <h3 className="font-bold mb-2">Insert Link</h3>
            <input value={linkTitle} onChange={(e) => setLinkTitle(e.target.value)} placeholder="Title" className="w-full p-2 border rounded mb-2" />
            <input value={linkUrl} onChange={(e) => setLinkUrl(e.target.value)} placeholder="https://..." className="w-full p-2 border rounded mb-2" />
            <div className="flex justify-end gap-2">
              <button onClick={() => setShowLinkModal(false)} className="px-3 py-1 rounded">Cancel</button>
              <button onClick={confirmLink} className="px-3 py-1 rounded bg-blue-600 text-white">Insert</button>
            </div>
          </div>
        </div>
      )}

      {/* Image Modal */}
      {showImageModal && (
        <div className="fixed inset-0 flex items-center justify-center bg-black/40">
          <div className="bg-white p-4 rounded shadow w-96">
            <h3 className="font-bold mb-2">Insert Image</h3>
            <input value={imageTitle} onChange={(e) => setImageTitle(e.target.value)} placeholder="Alt text / title" className="w-full p-2 border rounded mb-2" />
            <input value={imageUrl} onChange={(e) => setImageUrl(e.target.value)} placeholder="https://..." className="w-full p-2 border rounded mb-2" />
            <div className="mb-2">
              <label className="block text-sm mb-1">Or upload a file</label>
              <input type="file" accept="image/*" onChange={(e) => setImageFile(e.target.files ? e.target.files[0] : null)} />
            </div>
            <div className="flex justify-end gap-2">
              <button onClick={() => setShowImageModal(false)} className="px-3 py-1 rounded">Cancel</button>
              <button onClick={confirmImage} disabled={uploading} className="px-3 py-1 rounded bg-blue-600 text-white">{uploading ? 'Uploading...' : 'Insert'}</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
