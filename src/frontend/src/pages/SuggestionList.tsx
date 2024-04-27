import { Fragment } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useNavigate } from 'react-router-dom';
import { OrderOption, useDocumentTitle } from '../client';
import { RequestCmsSuggestionsQuery, useCmsSuggestionsSearch, useSearchPublisher } from '../cms';
import { suggestionStatusList } from '../codelist/SuggestionCodelist';
import Breadcrumbs from '../components/Breadcrumbs';
import Button from '../components/Button';
import CommentButton from '../components/CommentButton';
import GridColumn from '../components/GridColumn';
import GridRow from '../components/GridRow';
import LikeButton from '../components/LikeButton';
import MainContent from '../components/MainContent';
import SearchResultsCms from '../components/SearchResultsCms';

const SuggestionList = () => {
    const { t } = useTranslation();
    const navigate = useNavigate();
    const [publishers] = useSearchPublisher({
        pageSize: -1,
        language: 'sk',
        query: ''
    });
    useDocumentTitle(t('suggestionList.headerTitle'));

    const [suggestions, query, setQueryParameters, loading, error] = useCmsSuggestionsSearch({
        orderBy: 'title'
    });

    const orderByOptions: OrderOption[] = [
        { name: t('byDateModified'), value: 'updated' },
        { name: t('byDateCreated'), value: 'created' },
        { name: t('byName'), value: 'title' }
    ];

    return (
        <>
            <Breadcrumbs items={[{ title: t('nkod'), link: '/' }, { title: t('suggestionList.headerTitle') }]} />
            <MainContent>
                <GridRow data-testid="sr-add-new-row">
                    <GridColumn widthUnits={1} totalUnits={1} data-testid="sr-add-new" flexEnd>
                        <Button onClick={() => navigate('/podnet/pridat')}>{t('addSuggestion.headerTitle')}</Button>
                    </GridColumn>
                </GridRow>

                <SearchResultsCms<RequestCmsSuggestionsQuery>
                    header={t('suggestionList.title')}
                    query={query}
                    setQueryParameters={setQueryParameters}
                    loading={loading}
                    error={error}
                    totalCount={suggestions?.paginationMetadata?.totalItemCount ?? 0}
                    orderOptions={orderByOptions}
                    filters={['publishers', 'suggestion-types', 'suggestion-statutes']}
                >
                    {suggestions?.items?.map((suggestion, i) => (
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

                                <GridColumn widthUnits={1} totalUnits={4}>
                                    <span className="govuk-body-m govuk-!-font-weight-bold">
                                        {suggestionStatusList?.find((status) => status.id === suggestion.status)?.label}
                                    </span>
                                </GridColumn>
                                {suggestion.orgToUri && (
                                    <GridColumn widthUnits={3} totalUnits={4} data-testid="sr-result-publisher" flexEnd>
                                        <span style={{ color: '#000', fontStyle: 'italic', fontWeight: 'bold', paddingRight: '0.2rem' }}>
                                            {t('suggestionList.resolver')}:
                                        </span>
                                        <span style={{ color: '#777', fontStyle: 'italic', paddingLeft: '0.2rem' }}>
                                            {publishers?.find((publisher) => publisher.value === suggestion.orgToUri)?.label}
                                        </span>
                                    </GridColumn>
                                )}
                            </GridRow>
                            {i < suggestions?.items?.length - 1 ? <hr className="idsk-search-results__card__separator" /> : null}
                        </Fragment>
                    ))}
                </SearchResultsCms>
            </MainContent>
        </>
    );
};

export default SuggestionList;
