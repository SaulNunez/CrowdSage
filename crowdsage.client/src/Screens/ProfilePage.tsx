import { useState, type ReactNode } from 'react';
import { Link, Navigate, useParams } from 'react-router';
import { useSelector } from 'react-redux';
import { useTranslation } from 'react-i18next';
import type { RootState } from '../store';
import {
    useGetMyProfileQuery,
    useGetUserAnswersQuery,
    useGetUserCommentsQuery,
    useGetUserProfileQuery,
    useGetUserQuestionsQuery,
} from '../store/reducers';
import { Loading } from '../Components/Loading';
import { ServerError } from '../Components/ServerError';

const PAGE_SIZE = 20;

function formatDate(value: string) {
    return new Date(value).toLocaleDateString();
}

export default function ProfilePage() {
    const { userId: routeUserId } = useParams();
    const token = useSelector((state: RootState) => state.auth.token);
    const { t } = useTranslation();

    const isOwnProfile = !routeUserId;
    const myProfile = useGetMyProfileQuery(undefined, { skip: !isOwnProfile || !token });
    const userProfile = useGetUserProfileQuery(routeUserId ?? '', { skip: isOwnProfile });
    const { data: profile, isLoading, error } = isOwnProfile ? myProfile : userProfile;

    if (isOwnProfile && !token) return <Navigate to="/auth/login" replace />;
    if (isLoading) return <Loading />;
    if (error || !profile) return <ServerError />;

    return (
        <div className="min-h-screen bg-gray-50 dark:bg-gray-900 transition-colors">
            <div className="max-w-4xl mx-auto px-6 py-10">
                <header className="flex items-center gap-4 mb-8">
                    {profile.urlPhoto ? (
                        <img className="w-16 h-16 rounded-full" src={profile.urlPhoto} alt={profile.userName} />
                    ) : (
                        <div className="w-16 h-16 rounded-full bg-gray-200 dark:bg-gray-700 flex items-center justify-center text-2xl text-gray-700 dark:text-gray-200">
                            {profile.userName[0]?.toUpperCase()}
                        </div>
                    )}
                    <div>
                        <h1 className="text-3xl font-bold text-gray-900 dark:text-gray-100">{profile.userName}</h1>
                        <div className="flex gap-4 text-sm text-gray-600 dark:text-gray-400 mt-1">
                            <span>{t('profilePage.questionCount', { count: profile.questionCount })}</span>
                            <span>{t('profilePage.answerCount', { count: profile.answerCount })}</span>
                            <span>{t('profilePage.commentCount', { count: profile.commentCount })}</span>
                        </div>
                    </div>
                </header>

                {/* Keyed so each tab's page resets when moving between profiles. */}
                <div className="tabs tabs-lift" key={profile.id}>
                    <input type="radio" name="profile_tabs" className="tab" aria-label={t('profilePage.questionsTab')} defaultChecked />
                    <div className="tab-content bg-base-100 border-base-300 p-6">
                        <QuestionsTab userId={profile.id} />
                    </div>

                    <input type="radio" name="profile_tabs" className="tab" aria-label={t('profilePage.answersTab')} />
                    <div className="tab-content bg-base-100 border-base-300 p-6">
                        <AnswersTab userId={profile.id} />
                    </div>

                    <input type="radio" name="profile_tabs" className="tab" aria-label={t('profilePage.commentsTab')} />
                    <div className="tab-content bg-base-100 border-base-300 p-6">
                        <CommentsTab userId={profile.id} />
                    </div>
                </div>
            </div>
        </div>
    );
}

function QuestionsTab({ userId }: { userId: string }) {
    const { t } = useTranslation();
    const [page, setPage] = useState(1);
    const { data, isFetching, error } = useGetUserQuestionsQuery({ userId, page, take: PAGE_SIZE });

    return (
        <ActivityList
            items={data}
            isFetching={isFetching}
            error={error}
            page={page}
            setPage={setPage}
            emptyMessage={t('profilePage.noQuestions')}
            renderItem={(q) => (
                <ActivityItem key={q.id} to={`/question/${q.id}`} date={q.createdAt}>
                    <div className="font-semibold text-blue-600 dark:text-blue-400">{q.title}</div>
                    <div className="text-sm text-gray-600 dark:text-gray-400 mt-1">
                        {t('profilePage.votes', { count: q.votes })} · {t('profilePage.answerCount', { count: q.answerCount })}
                    </div>
                </ActivityItem>
            )}
        />
    );
}

function AnswersTab({ userId }: { userId: string }) {
    const { t } = useTranslation();
    const [page, setPage] = useState(1);
    const { data, isFetching, error } = useGetUserAnswersQuery({ userId, page, take: PAGE_SIZE });

    return (
        <ActivityList
            items={data}
            isFetching={isFetching}
            error={error}
            page={page}
            setPage={setPage}
            emptyMessage={t('profilePage.noAnswers')}
            renderItem={(a) => (
                <ActivityItem key={a.id} to={`/question/${a.questionId}`} date={a.createdAt}>
                    <div className="text-sm text-gray-600 dark:text-gray-400">
                        {t('profilePage.on')} <span className="font-semibold text-blue-600 dark:text-blue-400">{a.questionTitle}</span>
                    </div>
                    <p className="text-gray-700 dark:text-gray-300 text-sm line-clamp-2 mt-1">{a.content}</p>
                    <div className="text-sm text-gray-600 dark:text-gray-400 mt-1">{t('profilePage.votes', { count: a.votes })}</div>
                </ActivityItem>
            )}
        />
    );
}

function CommentsTab({ userId }: { userId: string }) {
    const { t } = useTranslation();
    const [page, setPage] = useState(1);
    const { data, isFetching, error } = useGetUserCommentsQuery({ userId, page, take: PAGE_SIZE });

    return (
        <ActivityList
            items={data}
            isFetching={isFetching}
            error={error}
            page={page}
            setPage={setPage}
            emptyMessage={t('profilePage.noComments')}
            renderItem={(c) => (
                <ActivityItem key={c.id} to={`/question/${c.questionId}`} date={c.createdAt}>
                    <p className="text-gray-700 dark:text-gray-300 text-sm">{c.content}</p>
                    <div className="text-sm text-gray-600 dark:text-gray-400 mt-1">
                        {c.answerId ? t('profilePage.onAnswerTo') : t('profilePage.on')}{' '}
                        <span className="font-semibold text-blue-600 dark:text-blue-400">{c.questionTitle}</span>
                    </div>
                </ActivityItem>
            )}
        />
    );
}

interface ActivityListProps<T> {
    items: T[] | undefined;
    isFetching: boolean;
    error: unknown;
    page: number;
    setPage: (page: number) => void;
    emptyMessage: string;
    renderItem: (item: T) => ReactNode;
}

function ActivityList<T>({ items, isFetching, error, page, setPage, emptyMessage, renderItem }: ActivityListProps<T>) {
    const { t } = useTranslation();

    if (error) return <ServerError />;
    if (!items) return <Loading />;

    // A short page means there is nothing after it.
    const hasNext = items.length === PAGE_SIZE;

    return (
        <div className={isFetching ? 'opacity-60 transition-opacity' : undefined}>
            {items.length === 0 ? (
                <p className="text-center text-gray-500 dark:text-gray-400">{emptyMessage}</p>
            ) : (
                <ul className="grid gap-3">{items.map(renderItem)}</ul>
            )}

            {(page > 1 || hasNext) && (
                <div className="join flex justify-center mt-6">
                    <button className="join-item btn" disabled={page === 1 || isFetching} onClick={() => setPage(page - 1)}>
                        {t('profilePage.previous')}
                    </button>
                    <button className="join-item btn btn-disabled pointer-events-none">{t('profilePage.page', { page })}</button>
                    <button className="join-item btn" disabled={!hasNext || isFetching} onClick={() => setPage(page + 1)}>
                        {t('profilePage.next')}
                    </button>
                </div>
            )}
        </div>
    );
}

function ActivityItem({ to, date, children }: { to: string, date: string, children: ReactNode }) {
    return (
        <li>
            <Link
                to={to}
                className="flex justify-between gap-4 bg-white dark:bg-gray-800 rounded-xl border dark:border-gray-700 p-4 hover:shadow-md transition"
            >
                <div className="min-w-0">{children}</div>
                <div className="text-sm text-gray-500 dark:text-gray-400 whitespace-nowrap">{formatDate(date)}</div>
            </Link>
        </li>
    );
}
