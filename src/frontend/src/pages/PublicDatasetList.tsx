import { Fragment } from 'react';
import Breadcrumbs from '../components/Breadcrumbs';
import MainContent from '../components/MainContent';
import GridRow from '../components/GridRow';
import GridColumn from '../components/GridColumn';
import { Link } from 'react-router-dom';
import { knownCodelists, OrderOption, useDatasets, useDocumentTitle } from '../client';
import SearchResults from '../components/SearchResults';
import { useTranslation } from 'react-i18next';
import multiIcon from '../icons/multi.png';
import LikeButton from '../components/LikeButton';
import CommentButton from '../components/CommentButton';
import { useCmsDatasets } from '../cms';

const codelistsKeys = [
    'publishers',
    'https://data.gov.sk/set/codelist/dataset-type',
    'http://publications.europa.eu/resource/authority/data-theme',
    'http://publications.europa.eu/resource/authority/file-type',
    'http://publications.europa.eu/resource/authority/frequency',
    'keywords',
    knownCodelists.distribution.license,
    knownCodelists.distribution.personalDataContainmentType
];

export default function PublicDatasetList() {
    const [datasets, query, setQueryParameters, loading, error] = useDatasets({
        requiredFacets: codelistsKeys,
        orderBy: 'relevance'
    });

    const [datasetsCms] = useCmsDatasets();
    const cmsCounts = (datasetUri: string) => {
        const found = datasetsCms?.find((d) => d.datasetUri === datasetUri);
        return { likeCount: found?.likeCount ?? 0, commentCount: found?.commentCount ?? 0, cmsDatasetId: found?.id };
    };
    const { t } = useTranslation();
    useDocumentTitle(t('search'));

    const orderByOptions: OrderOption[] = [
        { name: t('byRelevance'), value: 'relevance' },
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
                    {datasets?.items.map((c, i) => {
                        const { likeCount, commentCount, cmsDatasetId } = cmsCounts(c.key);
                        return (
                            <Fragment key={c.id}>
                                <GridRow data-testid="sr-result">
                                    <GridColumn widthUnits={1} totalUnits={1}>
                                        <GridRow>
                                            <GridColumn widthUnits={3} totalUnits={4}>
                                                <Link to={'/datasety/' + c.id} className="idsk-card-title govuk-link">
                                                    {c.name}
                                                    {c.isSerie ? (
                                                        <img
                                                            src={multiIcon}
                                                            alt={t('dataSerie')}
                                                            style={{ marginLeft: '5px', width: 'auto', height: '20px' }}
                                                        />
                                                    ) : null}
                                                </Link>
                                            </GridColumn>
                                            <GridColumn widthUnits={1} totalUnits={4} flexEnd>
                                                <LikeButton count={likeCount} contentId={cmsDatasetId} datasetUri={c.key} url={`datasets/likes`} />
                                                <Link to={`/datasety/${c.id}/komentare`} className="no-link">
                                                    <CommentButton count={commentCount} />
                                                </Link>
                                            </GridColumn>
                                        </GridRow>
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
                                    <GridColumn widthUnits={2} totalUnits={3}>
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
                                        <GridColumn widthUnits={1} totalUnits={3} data-testid="sr-result-publisher">
                                            <span style={{ color: '#777', fontStyle: 'italic' }}>{c.publisher.name}</span>
                                        </GridColumn>
                                    ) : null}
                                </GridRow>
                                {i < datasets.items.length - 1 ? <hr className="idsk-search-results__card__separator" /> : null}
                            </Fragment>
                        );
                    })}
                </SearchResults>
            </MainContent>
        </>
    );
}
