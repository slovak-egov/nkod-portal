import moment from 'moment';
import { useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router';
import { useDocumentTitle } from '../client';
import { useCmsSuggestion } from '../cms';
import Breadcrumbs from '../components/Breadcrumbs';
import GridRow from '../components/GridRow';
import Loading from '../components/Loading';
import MainContent from '../components/MainContent';
import PageHeader from '../components/PageHeader';
import { DATE_FORMAT_NO_SECONDS } from '../helpers/helpers';
import CommentSection from './CommentSection';
import DetailItemElement from './DetailItemElement';
import NotFound from './NotFound';

type Props = {
    scrollToComments?: boolean;
};

export default function SuggestionDetail(props: Props) {
    const commentSectionRef = useRef(null);
    const { id } = useParams();
    const { scrollToComments } = props;
    const [suggestion, loading] = useCmsSuggestion(id);
    const { t } = useTranslation();
    useDocumentTitle(suggestion?.title ?? '');

    if (!loading && scrollToComments) {
        setTimeout(() => (commentSectionRef.current as any)?.scrollIntoView(), 500);
    }

    return (
        <>
            {loading ? (
                <Loading />
            ) : suggestion ? (
                <>
                    <Breadcrumbs
                        items={[{ title: t('nkod'), link: '/' }, { title: t('suggestionList.headerTitle'), link: '/podnet' }, { title: suggestion.title }]}
                    />
                    <MainContent>
                        <div className="nkod-entity-detail">
                            <PageHeader>{suggestion.title}</PageHeader>
                            <GridRow>
                                <DetailItemElement value={suggestion.description} labelKey="" />

                                <DetailItemElement value={suggestion.orgName} labelKey="addSuggestion.fields.orgToUriSingle" />

                                <DetailItemElement value={t(`codelists.suggestionType.${suggestion.type}`) ?? ''} labelKey="addSuggestion.fields.type" />

                                {suggestion.datasetUri && (
                                    <DetailItemElement
                                        value={
                                            <a href={suggestion.datasetUri} className="govuk-link">
                                                {suggestion.datasetName ?? suggestion.datasetUri}
                                            </a>
                                        }
                                        labelKey="addSuggestion.fields.datasetUri"
                                    />
                                )}

                                <DetailItemElement value={t(`codelists.suggestionStatus.${suggestion.status}`) ?? ''} labelKey="addSuggestion.fields.status" />

                                <DetailItemElement value={suggestion.userEmail ?? suggestion.userId} labelKey="common.author" />

                                <DetailItemElement
                                    value={moment.utc(suggestion.created).local().format(DATE_FORMAT_NO_SECONDS)}
                                    labelKey="addSuggestion.fields.created"
                                />

                                <DetailItemElement
                                    value={moment.utc(suggestion.updated).local().format(DATE_FORMAT_NO_SECONDS)}
                                    labelKey="addSuggestion.fields.updated"
                                />
                            </GridRow>
                        </div>
                    </MainContent>
                    {id && (
                        <div ref={commentSectionRef}>
                            <CommentSection contentId={id} />
                        </div>
                    )}
                </>
            ) : (
                <NotFound />
            )}
        </>
    );
}

SuggestionDetail.defaultProps = {
    scrollToComments: false
};
