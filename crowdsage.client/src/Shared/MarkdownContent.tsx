import ReactMarkdown, { type Components } from "react-markdown";

// There is no Tailwind typography plugin, so images need explicit sizing or
// large uploads overflow their container.
const components: Components = {
  img: ({ src, alt, title }) => (
    <img src={src} alt={alt} title={title} loading="lazy" className="max-w-full h-auto rounded my-2" />
  ),
};

export default function MarkdownContent({ children }: { children?: string | null }) {
  return <ReactMarkdown components={components}>{children}</ReactMarkdown>;
}
