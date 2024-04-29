import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { OrderOption, useDocumentTitle } from '../client';
import { useCmsSuggestionsSearch, useSearchPublisher } from '../cms';
import Breadcrumbs from '../components/Breadcrumbs';
import Button from '../components/Button';
import GridColumn from '../components/GridColumn';
import GridRow from '../components/GridRow';
import MainContent from '../components/MainContent';
import SearchResultsCms from '../components/SearchResultsCms';
import { RequestCmsSuggestionsQuery } from '../interface/cms.interface';
import SuggestionListItem from './SuggestionListItem';

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
        orderBy: 'created'
    });

    const orderByOptions: OrderOption[] = [
        { name: t('byDateModified'), value: 'updated' },
        { name: t('byDateCreated'), value: 'created' },
        { name: t('byName'), value: 'title' },
        { name: t('byPopularity'), value: 'popularity' }
    ];

    return (
        <>
            <Breadcrumbs items={[{ title: t('nkod'), link: '/' }, { title: t('suggestionList.headerTitle') }]} />
            <MainContent>
                <SearchResultsCms<RequestCmsSuggestionsQuery>
                    header={t('suggestionList.title')}
                    query={query}
                    customHeading={
                        <GridRow data-testid="sr-add-new-row">
                            <GridColumn widthUnits={1} totalUnits={1} data-testid="sr-add-new" flexEnd>
                                <Button onClick={() => navigate('/podnet/pridat')}>{t('addSuggestion.headerTitle')}</Button>
                            </GridColumn>
                        </GridRow>
                    }
                    setQueryParameters={setQueryParameters}
                    loading={loading}
                    error={error}
                    totalCount={suggestions?.paginationMetadata?.totalItemCount ?? 0}
                    orderOptions={orderByOptions}
                    filters={['publishers', 'suggestion-types', 'suggestion-statutes']}
                >
                    {suggestions?.items?.map((suggestion, i) => (
                        <SuggestionListItem
                            key={i}
                            suggestion={suggestion}
                            publisher={publishers?.find((publisher) => publisher.value === suggestion.orgToUri)}
                            isLast={i === suggestions?.items?.length - 1}
                        />
                    ))}
                </SearchResultsCms>
            </MainContent>
        </>
    );
};

export default SuggestionList;
