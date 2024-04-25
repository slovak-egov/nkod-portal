import { Fragment } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useNavigate } from 'react-router-dom';
import { useDocumentTitle } from '../client';
import { useCmsSuggestions, useSearchPublisher } from '../cms';
import Breadcrumbs from '../components/Breadcrumbs';
import Button from '../components/Button';
import CommentButton from '../components/CommentButton';
import GridColumn from '../components/GridColumn';
import GridRow from '../components/GridRow';
import LikeButton from '../components/LikeButton';
import MainContent from '../components/MainContent';
import PageHeader from '../components/PageHeader';
import SimpleList from '../components/SimpleList';

const SuggestionList = () => {
    const { t } = useTranslation();
    const navigate = useNavigate();
    const [publishers] = useSearchPublisher({
        pageSize: -1,
        language: 'sk',
        query: ''
    });
    useDocumentTitle(t('suggestionList.headerTitle'));

    const [suggestions, loading, error, refresh] = useCmsSuggestions();

    return (
        <>
            <Breadcrumbs items={[{ title: t('nkod'), link: '/' }, { title: t('suggestionList.headerTitle') }]} />
            <MainContent>
                <div className="idsk-search-results__title">
                    <PageHeader size="l">{t('suggestionList.title')}</PageHeader>
                </div>
                <GridRow data-testid="sr-add-new-row">
                    <GridColumn widthUnits={1} totalUnits={1} data-testid="sr-add-new" flexEnd>
                        <Button onClick={() => navigate('/podnet/pridat')}>{t('addSuggestion.headerTitle')}</Button>
                    </GridColumn>
                </GridRow>
                <SimpleList loading={loading} error={error} totalCount={suggestions?.length ?? 0}>
                    {suggestions?.map((suggestion, i) => (
                        <Fragment key={suggestion.id}>
                            <GridRow data-testid="sr-result">
                                <GridColumn widthUnits={1} totalUnits={1}>
                                    <GridRow>
                                        <GridColumn widthUnits={1} totalUnits={2}>
                                            <Link to={'/podnet/' + suggestion.id} className="idsk-card-title govuk-link">
                                                {suggestion.title}
                                            </Link>
                                        </GridColumn>
                                        <GridColumn widthUnits={1} totalUnits={2} flexEnd>
                                            <Link to={`/podnet/${suggestion.id}/upravit`} className="idsk-card-title govuk-link govuk-!-padding-right-3">
                                                {t('common.edit')}
                                            </Link>
                                            <LikeButton count={suggestion.likeCount} contentId={suggestion.id} url={`cms/suggestions/likes`} />
                                            <CommentButton count={suggestion.commentCount} />
                                        </GridColumn>
                                    </GridRow>
                                </GridColumn>
                                {suggestion.description && (
                                    <GridColumn widthUnits={1} totalUnits={1}>
                                        <div
                                            style={{
                                                WebkitLineClamp: 3,
                                                WebkitBoxOrient: 'vertical',
                                                overflow: 'hidden',
                                                textOverflow: 'ellipsis',
                                                display: '-webkit-box'
                                            }}
                                        >
                                            {suggestion.description}
                                        </div>
                                    </GridColumn>
                                )}
                                {suggestion.orgToUri && (
                                    <GridColumn widthUnits={1} totalUnits={1} data-testid="sr-result-publisher" flexEnd>
                                        <span style={{ color: '#000', fontStyle: 'italic', fontWeight: 'bold', paddingRight: '0.2rem' }}>
                                            {t('suggestionList.resolver')}:
                                        </span>
                                        <span style={{ color: '#777', fontStyle: 'italic', paddingLeft: '0.2rem' }}>
                                            {publishers?.find((publisher) => publisher.value === suggestion.orgToUri)?.label}
                                        </span>
                                    </GridColumn>
                                )}
                            </GridRow>
                            {i < suggestions.length - 1 ? <hr className="idsk-search-results__card__separator" /> : null}
                        </Fragment>
                    ))}
                </SimpleList>
            </MainContent>
        </>
    );
};

export default SuggestionList;
