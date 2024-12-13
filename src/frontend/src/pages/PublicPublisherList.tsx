import { Link } from 'react-router-dom';
import { Fragment } from 'react';
import { useDocumentTitle, usePublishers } from '../client';
import Breadcrumbs from '../components/Breadcrumbs';
import GridColumn from '../components/GridColumn';
import GridRow from '../components/GridRow';
import MainContent from '../components/MainContent';
import SearchResults from '../components/SearchResults';
import { useTranslation } from 'react-i18next';

type OrderOption = {
    name: string;
    value: string;
};

type Theme = {
    name: string;
    count: number;
};

const codelistsKeys: [] = [];

export default function PublicPublisherList() {
    const [publishers, query, setQueryParameters, loading, error] = usePublishers();
    const { t } = useTranslation();
    useDocumentTitle(t('publishers'));

    const orderByOptions: OrderOption[] = [
        { name: t('byRelevance'), value: 'relevance' },
        { name: t('byName'), value: 'name' }
    ];

    return (
        <>
            <Breadcrumbs items={[{ title: t('nkod'), link: '/' }, { title: t('publishers') }]} />
            <MainContent>
                <SearchResults
                    header={t('publishers')}
                    query={query}
                    setQueryParameters={setQueryParameters}
                    loading={loading}
                    error={error}
                    totalCount={publishers?.totalCount ?? 0}
                    orderOptions={orderByOptions}
                    filters={codelistsKeys}
                    facets={publishers?.facets ?? []}
                >
                    {publishers?.items.map((c, i) => {
                        const themes: Theme[] = c.themes
                            ? Object.entries(c.themes)
                                  .filter((c) => c[1] > 0)
                                  .sort((a, b) => b[1] - a[1])
                                  .slice(0, 5)
                                  .map((v) => ({ name: v[0], count: v[1] }))
                            : [];

                        return (
                            <Fragment key={c.id}>
                                <GridRow>
                                    <GridColumn widthUnits={1} totalUnits={1} data-testid="sr-result">
                                        <Link to={'/datasety?publisher=' + encodeURIComponent(c.key)} className="idsk-card-title govuk-link">
                                            {c.name} (datasetov: {c.datasetCount})
                                        </Link>
                                    </GridColumn>
                                    {themes.length > 0 ? (
                                        <GridColumn widthUnits={1} totalUnits={1}>
                                            {themes.map((t) => (
                                                <span key={t.name} style={{ marginRight: '10px' }}>
                                                    {t.name} ({t.count})
                                                </span>
                                            ))}
                                        </GridColumn>
                                    ) : null}
                                </GridRow>
                                {i < publishers.items.length - 1 ? <hr className="idsk-search-results__card__separator" /> : null}
                            </Fragment>
                        );
                    })}
                </SearchResults>
            </MainContent>
        </>
    );
}
