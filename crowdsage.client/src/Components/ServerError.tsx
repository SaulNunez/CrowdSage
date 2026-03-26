import { useTranslation } from 'react-i18next';

export function ServerError() {
  const { t } = useTranslation();
  return (
    <div className="flex items-center justify-center h-screen bg-red-50">
      <div className="max-w-md mx-auto bg-white rounded-lg shadow-lg p-6">
        <h1 className="text-2xl font-bold text-red-600 mb-4">{t('serverError.title')}</h1>
        <p className="text-gray-700 mb-4">
          {t('serverError.message1')}
        </p>
        <p className="text-sm text-gray-500">
          {t('serverError.message2')}
        </p>
      </div>
    </div>
  );
}