import { Fragment } from 'react';

import Breadcrumbs from '../components/Breadcrumbs';
import MainContent from '../components/MainContent';
import GridRow from '../components/GridRow';
import GridColumn from '../components/GridColumn';
import { Link } from 'react-router-dom';
import { useDocumentTitle, useLocalCatalogs } from '../client';
import SearchResults from '../components/SearchResults';
import { useTranslation } from 'react-i18next';

type OrderOption = {
    name: string;
    value: string;
};

const codelistsKeys: [] = [];

const orderByOptions: OrderOption[] = [{ name: 'Názvu', value: 'name' }];

export default function PublicDatasetList() {
    const [catalogs, query, setQueryParameters, loading, error] = useLocalCatalogs({
        orderBy: 'name'
    });
    const {t} = useTranslation();
    useDocumentTitle(t('localCatalogs'));

    return (
        <>
            <Breadcrumbs items={[{ title: t('nkod'), link: '/' }, { title: t('localCatalogs') }]} />
            <MainContent>
                <SearchResults
                    header={t('localCatalogs')}
                    query={query}
                    setQueryParameters={setQueryParameters}
                    loading={loading}
                    error={error}
                    totalCount={catalogs?.totalCount ?? 0}
                    orderOptions={orderByOptions}
                    filters={codelistsKeys}
                    facets={catalogs?.facets ?? []}
                >
                    {catalogs?.items.map((c, i) => (
                        <Fragment key={c.id}>
                            <GridRow>
                                <GridColumn widthUnits={1} totalUnits={1}>
                                    <Link to={'/lokalne-katalogy/' + c.id} className="idsk-card-title govuk-link">
                                        {c.name}
                                    </Link>
                                </GridColumn>
                                {c.publisher != null ? (
                                    <GridColumn widthUnits={1} totalUnits={1}>
                                        {c.publisher.name}
                                    </GridColumn>
                                ) : null}
                            </GridRow>
                            {i < catalogs.items.length - 1 ? <hr className="idsk-search-results__card__separator" /> : null}
                        </Fragment>
                    ))}
                </SearchResults>
            </MainContent>
        </>
    );
}
