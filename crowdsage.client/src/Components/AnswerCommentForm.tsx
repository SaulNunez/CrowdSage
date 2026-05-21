import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useAddCommentForAnswerMutation } from '../store/reducers';

export 
function AnswerCommentForm({questionId, answerId}: {questionId: string, answerId: string}) {
  const [text, setText] = useState<string>("");
  const { t } = useTranslation();
  const [addComment, { isLoading: currentlyAddingComment }] = useAddCommentForAnswerMutation();

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        addComment({data: {content: text}, questionId, answerId});
        setText("");
      }}
      className="mt-3"
    >
      <div className="flex gap-2">
        <input
          className="flex-1 border rounded p-2 text-sm focus:outline-none"
          placeholder={t('commentForm.placeholder')}
          value={text}
          onChange={(e) => setText(e.target.value)}
        />
        <button disabled={currentlyAddingComment} type="submit" className="flex items-center justify-center px-3 py-2 bg-gray-100 border rounded text-sm disabled:opacity-50 disabled:cursor-not-allowed">
          {currentlyAddingComment && (
            <svg className="animate-spin -ml-1 mr-2 h-4 w-4 text-gray-700" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
              <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
              <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
            </svg>
          )}
          {t('commentForm.submit')}
        </button>
      </div>
    </form>
  );
}