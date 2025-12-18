import { QuestionCard } from '../Components/QuestionCard';
import { AnswerCard } from '../Components/AnswerCard';
import { useGetBookmarkedAnswersQuery, useGetBookmarkedQuestionsQuery } from '../common/reducers';
import { Loading } from '../Components/Loading';
import { ServerError } from '../Components/ServerError';



export default function BookmarksScreen() {
    const {data: questions, isLoading: loadingQuestions, error: errorQuestions} = useGetBookmarkedQuestionsQuery();
    const {data: answers, isLoading: loadingAnswers, error: errorAnswers} = useGetBookmarkedAnswersQuery();

    return (
        <div className="tabs tabs-lift">
            <input type="radio" name="bookmark_tabs" className="tab" aria-label="Questions" />
            <div className="tab-content bg-base-100 border-base-300 p-6">
                {loadingQuestions && <Loading />}
                {errorQuestions && <ServerError />}
                {questions && (
                    <div className="grid gap-4">
                        {questions.map((q) => (
                            <QuestionCard question={q} />
                        ))}
                    </div>
                )}
            </div>

            <input type="radio" name="bookmark_tabs" className="tab" aria-label="Answers" defaultChecked />
            <div className="tab-content bg-base-100 border-base-300 p-6">
                {loadingAnswers && <Loading />}
                {errorAnswers && <ServerError />}
                {answers && (
                    <div className="grid gap-4">
                        {answers.map((a) => (
                            <AnswerCard
                                key={a.id}
                                answer={a}
                                onUpvote={() => console.log('upvote', a.id)}
                                onBookmark={() => console.log('bookmark toggle', a.id)}
                            />
                        ))}
                    </div>
                )}
            </div>
        </div>
    );
}