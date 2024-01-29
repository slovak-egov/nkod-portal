import { Fragment } from 'react';
import Breadcrumbs from '../components/Breadcrumbs';
import MainContent from '../components/MainContent';
import GridRow from '../components/GridRow';
import GridColumn from '../components/GridColumn';
import { Link } from 'react-router-dom';
import { useDatasets, useDocumentTitle } from '../client';
import SearchResults from '../components/SearchResults';
import { useTranslation } from 'react-i18next';

type OrderOption = {
    name: string;
    value: string;
};

const codelistsKeys = [
    'publishers',
    'https://data.gov.sk/set/codelist/dataset-type',
    'http://publications.europa.eu/resource/authority/data-theme',
    'http://publications.europa.eu/resource/authority/file-type',
    'http://publications.europa.eu/resource/authority/frequency',
    'keywords'
];

export default function PublicDatasetList() {
    const [datasets, query, setQueryParameters, loading, error] = useDatasets({
        requiredFacets: codelistsKeys
    });
    const { t } = useTranslation();
    useDocumentTitle(t('search'));

    const orderByOptions: OrderOption[] = [
        { name: t('byDateCreated'), value: 'created' },
        { name: t('byName'), value: 'name' }
    ];

    return (
        <>
            <Breadcrumbs items={[{ title: t('nkod'), link: '/' }, { title: t('search') }]} />
            <MainContent>
                <SearchResults
                    header={t('search')}
                    query={query}
                    setQueryParameters={setQueryParameters}
                    loading={loading}
                    error={error}
                    totalCount={datasets?.totalCount ?? 0}
                    orderOptions={orderByOptions}
                    filters={codelistsKeys}
                    facets={datasets?.facets ?? []}
                >
                    {datasets?.items.map((c, i) => (
                        <Fragment key={c.id}>
                            <GridRow data-testid="sr-result">
                                <GridColumn widthUnits={1} totalUnits={1}>
                                    <Link to={'/datasety/' + c.id} className="idsk-card-title govuk-link">
                                        {c.name}
                                        {c.isSerie ? (
                                            <svg xmlns="http://www.w3.org/2000/svg" width="32px" height="40px">
                                                <image
                                                    x="0px"
                                                    y="0px"
                                                    width="316px"
                                                    height="402px"
                                                    href="data:img/png;base64,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"
                                                />
                                            </svg>
                                        ) : null}
                                    </Link>
                                </GridColumn>
                                {c.description ? (
                                    <GridColumn widthUnits={1} totalUnits={1} data-testid="sr-result-description">
                                        <div
                                            style={{
                                                WebkitLineClamp: 3,
                                                WebkitBoxOrient: 'vertical',
                                                overflow: 'hidden',
                                                textOverflow: 'ellipsis',
                                                display: '-webkit-box'
                                            }}
                                        >
                                            {c.description}
                                        </div>
                                    </GridColumn>
                                ) : null}
                                <GridColumn widthUnits={1} totalUnits={2}>
                                    {c.distributions.map((distribution) => {
                                        if (distribution.downloadUrl && distribution.formatValue) {
                                            return (
                                                <Fragment key={distribution.id}>
                                                    <a href={distribution.downloadUrl} className="govuk-link">
                                                        {distribution.formatValue.label}
                                                    </a>{' '}
                                                </Fragment>
                                            );
                                        }
                                        return null;
                                    })}
                                </GridColumn>
                                {c.publisher ? (
                                    <GridColumn widthUnits={1} totalUnits={2} data-testid="sr-result-publisher">
                                        <span style={{ color: '#777', fontStyle: 'italic' }}>{c.publisher.name}</span>
                                    </GridColumn>
                                ) : null}
                            </GridRow>
                            {i < datasets.items.length - 1 ? <hr className="idsk-search-results__card__separator" /> : null}
                        </Fragment>
                    ))}
                </SearchResults>
            </MainContent>
        </>
    );
}
