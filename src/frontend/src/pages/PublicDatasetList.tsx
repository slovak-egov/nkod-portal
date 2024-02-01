import { Fragment } from 'react';
import Breadcrumbs from '../components/Breadcrumbs';
import MainContent from '../components/MainContent';
import GridRow from '../components/GridRow';
import GridColumn from '../components/GridColumn';
import { Link } from 'react-router-dom';
import { useDatasets, useDocumentTitle } from '../client';
import SearchResults from '../components/SearchResults';
import { useTranslation } from 'react-i18next';
import multiIcon from '../icons/multi.png';

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
        { name: t('byDateModified'), value: 'modified' },
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
                                            <img src={multiIcon} alt={t('dataSerie')} style={{ marginLeft: '5px', width: 'auto', height: '20px' }} />
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
