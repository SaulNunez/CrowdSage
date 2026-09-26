import type { BaseComment } from '../types';
import { useTranslation } from 'react-i18next';
import MarkdownContent from '../Shared/MarkdownContent';

export function CommentList({ comments = [] }: { comments: BaseComment[] }) {
  const { t } = useTranslation();
  if (!comments.length) return <div className="text-sm text-gray-500 mt-2">{t('commentList.empty')}</div>;
  return (
    <ul className="mt-2 space-y-2">
      {comments.map((c) => (
        <li key={c.id} className="flex items-start gap-3 text-sm">
          <div className="w-8 h-8 rounded-full bg-gray-100 flex items-center justify-center text-gray-700">{c.author.userName[0]?.toUpperCase()}</div>
          <div className="min-w-0 flex-1">
            <div className="text-gray-800 font-medium">{c.author.userName}</div>
            <div className="text-gray-600"><MarkdownContent>{c.content}</MarkdownContent></div>
          </div>
        </li>
      ))}
    </ul>
  );
}