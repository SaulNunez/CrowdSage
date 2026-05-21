import { useTranslation } from 'react-i18next';

export function ServerError() {
  const { t } = useTranslation();
  return (
    <div className="flex items-center justify-center h-screen bg-red-50 dark:bg-gray-900">
      <div className="max-w-md mx-auto bg-white dark:bg-gray-800 rounded-lg shadow-lg p-6">
        <h1 className="text-2xl font-bold text-red-600 dark:text-red-400 mb-4">{t('serverError.title')}</h1>
        <p className="text-gray-700 dark:text-gray-300 mb-4">
          {t('serverError.message1')}
        </p>
        <p className="text-sm text-gray-500 dark:text-gray-400">
          {t('serverError.message2')}
        </p>
      </div>
    </div>
  );
}