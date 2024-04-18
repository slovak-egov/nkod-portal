import React, { Fragment } from 'react';
import Breadcrumbs from '../components/Breadcrumbs';
import MainContent from '../components/MainContent';
import PageHeader from '../components/PageHeader';
import { useTranslation } from 'react-i18next';
import { OrderOption, useDatasets, useDocumentTitle, useSuggestions } from '../client';
import SearchResults from '../components/SearchResults';
import GridColumn from '../components/GridColumn';
import { Link } from 'react-router-dom';
import GridRow from '../components/GridRow';

const SuggestionList = () => {
    const { t } = useTranslation();
    useDocumentTitle(t('suggestionListSearch'));

    const orderByOptions: OrderOption[] = [{ name: t('byDateCreated'), value: 'created' }, { name: t('byName'), value: 'name' }];

    const [datasets, query, setQueryParameters, loading, error] = useSuggestions({
     pageSize: 20, page: 0
    });


    console.log('datasets', datasets);
    return (
        <>
            <Breadcrumbs items={[{ title: t('nkod'), link: '/' }, { title: t('suggestionList.headerTitle') }]} />
            <MainContent>
                {/*<PageHeader>{t('suggestionList.title')}</PageHeader>*/}
                <SearchResults
                    header={t('suggestionList.title')}
                    query={query}
                    setQueryParameters={setQueryParameters}
                    loading={loading}
                    error={error}
                    totalCount={datasets?.totalCount ?? 0}
                    orderOptions={orderByOptions}
                    filters={[]}
                    facets={datasets?.facets ?? []}
                >
                    {datasets?.items.map((suggestion, i) => (
                        <Fragment key={suggestion.id}>
                            <GridRow data-testid="sr-result">
                                <GridColumn widthUnits={1} totalUnits={1}>
                                    <Link
                                        to={'/datasety/' + suggestion.id}
                                        className="idsk-card-title govuk-link"
                                    >
                                        {suggestion.title}
                                    </Link>
                                </GridColumn>
                                {suggestion.description && (
                                    <GridColumn widthUnits={1} totalUnits={1}>
                                        <div style={{ WebkitLineClamp: 3, WebkitBoxOrient: 'vertical', overflow: 'hidden', textOverflow: 'ellipsis', display: '-webkit-box' }}>{suggestion.description}</div>
                                    </GridColumn>
                                )}
                                {suggestion.orgToURI && (
                                    <GridColumn widthUnits={1} totalUnits={1} data-testid="sr-result-publisher" style={{ display: 'flex', justifyContent: 'flex-end' }}>
                                        <span style={{ color: '#000', fontStyle: 'italic', fontWeight: 'bold', paddingRight: '0.2rem' }}>{t('suggestionList.investigator')}:</span>
                                        <span style={{ color: '#777', fontStyle: 'italic', paddingLeft: '0.2rem' }}>{suggestion.orgToURI}</span>
                                    </GridColumn>
                                ) }
                            </GridRow>
                        </Fragment>
                    ))}
                </SearchResults>


            </MainContent>
        </>
    );
};

export default SuggestionList;
