import { useTranslation } from 'react-i18next';

export function Loading() {
  const { t } = useTranslation();
  return (
    <div className="flex items-center justify-center h-screen">
      <div className="animate-spin rounded-full h-16 w-16 border-t-2 border-blue-500"></div>
      <p className="ml-4 text-gray-700">{t('loading')}</p>
    </div>
  );
}