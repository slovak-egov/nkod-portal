import { useEffect, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router';
import { useDataset, useDatasets, useDocumentTitle } from '../client';
import { useCmsApplications, useCmsDatasets, useCmsSuggestions } from '../cms';
import Breadcrumbs from '../components/Breadcrumbs';
import DistributionRow from '../components/DistributionRow';
import ErrorAlert from '../components/ErrorAlert';
import GridColumn from '../components/GridColumn';
import GridRow from '../components/GridRow';
import Loading from '../components/Loading';
import MainContent from '../components/MainContent';
import PageHeader from '../components/PageHeader';
import Pagination from '../components/Pagination';
import RelatedContent from '../components/RelatedContent';
import SimpleList from '../components/SimpleList';
import { Application, Suggestion } from '../interface/cms.interface';
import ApplicationListItem from './ApplicationListItem';
import CommentSection from './CommentSection';
import NotFound from './NotFound';
import SuggestionListItem from './SuggestionListItem';

type Props = {
    scrollToComments?: boolean;
};

export default function DetailDataset(props: Props) {
    const commentSectionRef = useRef(null);
    const [dataset, loading, error] = useDataset();
    const { id } = useParams();
    const { scrollToComments } = props;
    const [datasetsAsSibling, datasetsAsSiblingQuery, setDatasetsAsSiblingQuery, loadingDatasetsAsSibling] = useDatasets({
        page: 0,
        pageSize: 25,
        orderBy: 'created'
    });
    const [datasetsAsParent, datasetsAsParentQuery, setDatasetsAsParentQuery, loadingDatasetsAsParent] = useDatasets({
        page: 0,
        pageSize: 25,
        orderBy: 'created'
    });

    const [applications, loadingApplications, errorApplications, loadApps] = useCmsApplications(false, dataset?.key);
    const [suggestions, loadingSuggestions, errorSuggestions, loadSuggestions] = useCmsSuggestions(false, dataset?.key);

    const [cmsDataset, loadingCmsDatasets, errorCmsDatasets, loadCmsDatasets] = useCmsDatasets(false, dataset?.key);

    useEffect(() => {
        if (dataset?.key) {
            loadApps(dataset.key);
            loadSuggestions(dataset.key);
            loadCmsDatasets(dataset.key);
        }
    }, [dataset, loadApps, loadCmsDatasets, loadSuggestions]);

    const { t } = useTranslation();
    useDocumentTitle(dataset?.name ?? '');

    if (!loading && scrollToComments) {
        setTimeout(() => (commentSectionRef.current as any)?.scrollIntoView(), 500);
    }

    useEffect(() => {
        if (id) {
            if (!datasetsAsSiblingQuery.filters || Object.keys(datasetsAsSiblingQuery.filters).length === 0) {
                setDatasetsAsSiblingQuery({ filters: { sibling: [id] }, page: 1 });
            }
            if (!datasetsAsParentQuery.filters || Object.keys(datasetsAsParentQuery.filters).length === 0) {
                setDatasetsAsParentQuery({ filters: { parent: [id] }, page: 1 });
            }
        }
    }, [id, datasetsAsSiblingQuery, setDatasetsAsSiblingQuery, datasetsAsParentQuery, setDatasetsAsParentQuery]);

    const loadingDatasets = loadingDatasetsAsParent || loadingDatasetsAsSibling;

    const path = [
        { title: t('nkod'), link: '/' },
        { title: t('search'), link: '/datasety' }
    ];
    if (dataset?.name) {
        path.push({ title: dataset.name, link: '/datasety/' + dataset.id });
    }

    return (
        <>
            {loading ? (
                <Loading />
            ) : error ? (
                <ErrorAlert error={error} />
            ) : dataset ? (
                <>
                    <Breadcrumbs items={path} />
                    <MainContent>
                        <div className="nkod-entity-detail">
                            <PageHeader>{dataset.name}</PageHeader>
                            {dataset.publisher ? (
                                <p className="govuk-body nkod-publisher-name" data-testid="publisher-name">
                                    {dataset.publisher.name}
                                </p>
                            ) : null}
                            {dataset.description ? (
                                <p className="govuk-body nkod-entity-description" data-testid="description">
                                    {dataset.description}
                                </p>
                            ) : null}
                            {dataset.keywords.length > 0 ? (
                                <div className="nkod-entity-detail-tags govuk-clearfix" data-testid="keywords">
                                    {dataset.keywords.map((t) => (
                                        <div key={t} className="govuk-body nkod-entity-detail-tag">
                                            {t}
                                        </div>
                                    ))}
                                </div>
                            ) : null}
                            <GridRow>
                                {dataset.themeValues.length > 0 || dataset.euroVocThemeValues.length > 0 ? (
                                    <GridColumn widthUnits={1} totalUnits={4}>
                                        <div className="nkod-detail-attribute">
                                            <div className="govuk-body nkod-detail-attribute-name">{t('theme')}</div>
                                            <div className="govuk-body nkod-detail-attribute-value" data-testid="themes" style={{ wordBreak: 'break-word' }}>
                                                {dataset.themeValues.map((l) => (
                                                    <div key={l.label}>{l.label}</div>
                                                ))}
                                                {dataset.euroVocThemeValues.map((l) => (
                                                    <div key={l}>{l}</div>
                                                ))}
                                            </div>
                                        </div>
                                    </GridColumn>
                                ) : null}
                                {dataset.typeValues.length > 0 ? (
                                    <GridColumn widthUnits={1} totalUnits={4}>
                                        <div className="nkod-detail-attribute">
                                            <div className="govuk-body nkod-detail-attribute-name">{t('datasetType')}</div>
                                            <div className="govuk-body nkod-detail-attribute-value" data-testid="types" style={{ wordBreak: 'break-word' }}>
                                                {dataset.typeValues.map((l) => (
                                                    <div key={l.label}>{l.label}</div>
                                                ))}
                                            </div>
                                        </div>
                                    </GridColumn>
                                ) : null}
                                {dataset.landingPage ? (
                                    <GridColumn widthUnits={1} totalUnits={4}>
                                        <div className="nkod-detail-attribute">
                                            <div className="govuk-body nkod-detail-attribute-name">{t('landingPage')}</div>
                                            <div
                                                className="govuk-body nkod-detail-attribute-value"
                                                data-testid="landing-page"
                                                style={{ wordBreak: 'break-word' }}
                                            >
                                                <a href={dataset.landingPage} className="govuk-link">
                                                    {t('show')}
                                                </a>
                                            </div>
                                        </div>
                                    </GridColumn>
                                ) : null}
                                {dataset.isPartOf ? (
                                    <GridColumn widthUnits={1} totalUnits={4}>
                                        <div className="nkod-detail-attribute">
                                            <div className="govuk-body nkod-detail-attribute-name">{t('dataSerie')}</div>
                                            <div
                                                className="govuk-body nkod-detail-attribute-value"
                                                data-testid="data-serie"
                                                style={{ wordBreak: 'break-word' }}
                                            >
                                                <a href={'/datasety/' + dataset.isPartOf} className="govuk-link">
                                                    {t('show')}
                                                </a>
                                            </div>
                                        </div>
                                    </GridColumn>
                                ) : null}
                                {dataset.specification ? (
                                    <GridColumn widthUnits={1} totalUnits={4}>
                                        <div className="nkod-detail-attribute">
                                            <div className="govuk-body nkod-detail-attribute-name">{t('specificationLink')}</div>
                                            <div
                                                className="govuk-body nkod-detail-attribute-value"
                                                data-testid="specification"
                                                style={{ wordBreak: 'break-word' }}
                                            >
                                                <a href={dataset.specification} className="govuk-link">
                                                    {t('show')}
                                                </a>
                                            </div>
                                        </div>
                                    </GridColumn>
                                ) : null}
                                {dataset.documentation ? (
                                    <GridColumn widthUnits={1} totalUnits={4}>
                                        <div className="nkod-detail-attribute">
                                            <div className="govuk-body nkod-detail-attribute-name">{t('documentationLink')}</div>
                                            <div
                                                className="govuk-body nkod-detail-attribute-value"
                                                data-testid="documentation"
                                                style={{ wordBreak: 'break-word' }}
                                            >
                                                <a href={dataset.documentation} className="govuk-link">
                                                    {t('show')}
                                                </a>
                                            </div>
                                        </div>
                                    </GridColumn>
                                ) : null}
                                {dataset.relation ? (
                                    <GridColumn widthUnits={1} totalUnits={4}>
                                        <div className="nkod-detail-attribute">
                                            <div className="govuk-body nkod-detail-attribute-name">{t('relation')}</div>
                                            <div className="govuk-body nkod-detail-attribute-value" data-testid="relation" style={{ wordBreak: 'break-word' }}>
                                                <a href={dataset.relation} className="govuk-link">
                                                    {t('show')}
                                                </a>
                                            </div>
                                        </div>
                                    </GridColumn>
                                ) : null}
                                {dataset.hvdCategoryValue ? (
                                    <GridColumn widthUnits={1} totalUnits={4}>
                                        <div className="nkod-detail-attribute">
                                            <div className="govuk-body nkod-detail-attribute-name">{t('hvdCategory')}</div>
                                            <div
                                                className="govuk-body nkod-detail-attribute-value"
                                                data-testid="hvd-category"
                                                style={{ wordBreak: 'break-word' }}
                                            >
                                                {dataset.hvdCategoryValue.label}
                                            </div>
                                        </div>
                                    </GridColumn>
                                ) : null}
                                {dataset.applicableLegislations.length > 0 ? (
                                    <GridColumn widthUnits={1} totalUnits={4}>
                                        <div className="nkod-detail-attribute">
                                            <div className="govuk-body nkod-detail-attribute-name">{t('applicableLegislations')}</div>
                                            <div
                                                className="govuk-body nkod-detail-attribute-value"
                                                data-testid="applicable-legislations"
                                                style={{ wordBreak: 'break-word' }}
                                            >
                                                {dataset.applicableLegislations.map((l) => (
                                                    <div key={l} style={{ marginBottom: '10px' }}>
                                                        {l}
                                                    </div>
                                                ))}
                                            </div>
                                        </div>
                                    </GridColumn>
                                ) : null}
                                {dataset.contactPoint?.name || dataset.contactPoint?.email ? (
                                    <GridColumn widthUnits={1} totalUnits={4}>
                                        <div className="nkod-detail-attribute">
                                            <div className="govuk-body nkod-detail-attribute-name">{t('contactPoint')}</div>
                                            <div className="govuk-body nkod-detail-attribute-value" style={{ wordBreak: 'break-word' }}>
                                                {dataset.contactPoint?.name ? (
                                                    <div>
                                                        <span data-testid="contact-name">{dataset.contactPoint.name}</span>
                                                    </div>
                                                ) : null}
                                                {dataset.contactPoint?.email ? (
                                                    <div>
                                                        <span data-testid="contact-email">{dataset.contactPoint.email}</span>
                                                    </div>
                                                ) : null}
                                            </div>
                                        </div>
                                    </GridColumn>
                                ) : null}
                                {dataset.key ? (
                                    <GridColumn widthUnits={1} totalUnits={4}>
                                        <div className="nkod-detail-attribute">
                                            <div className="govuk-body nkod-detail-attribute-name">{t('datasetUri')}</div>
                                            <div className="govuk-body nkod-detail-attribute-value" data-testid="uri" style={{ wordBreak: 'break-word' }}>
                                                {dataset.key}
                                            </div>
                                        </div>
                                    </GridColumn>
                                ) : null}
                            </GridRow>
                            <GridRow>
                                <GridColumn widthUnits={1} totalUnits={4}>
                                    {dataset.accrualPeriodicityValue ? (
                                        <div className="nkod-detail-attribute">
                                            <div className="govuk-body nkod-detail-attribute-name">{t('updateFrequency')}</div>
                                            <div className="govuk-body nkod-detail-attribute-value" data-testid="update-frequency">
                                                {dataset.accrualPeriodicityValue.label}
                                            </div>
                                        </div>
                                    ) : null}
                                    {dataset.spatialValues.length > 0 ? (
                                        <div className="nkod-detail-attribute">
                                            <div className="govuk-body nkod-detail-attribute-name">{t('spatialValidity')}</div>
                                            <div className="govuk-body nkod-detail-attribute-value" data-testid="spatial">
                                                {dataset.spatialValues.map((l) => (
                                                    <div key={l.label}>{l.label}</div>
                                                ))}
                                            </div>
                                        </div>
                                    ) : null}
                                    {dataset.spatialResolutionInMeters ? (
                                        <div className="nkod-detail-attribute">
                                            <div className="govuk-body nkod-detail-attribute-name">{t('spatialResolution')}</div>
                                            <div className="govuk-body nkod-detail-attribute-value" style={{ wordBreak: 'break-word' }}>
                                                <span data-testid="spatial-resolution">{dataset.spatialResolutionInMeters}</span>
                                            </div>
                                        </div>
                                    ) : null}
                                    {dataset.temporal?.startDate || dataset.temporal?.endDate || dataset.temporalResolution ? (
                                        <div className="nkod-detail-attribute">
                                            <div className="govuk-body nkod-detail-attribute-name">{t('timeValidity')}</div>
                                            <div className="govuk-body nkod-detail-attribute-value" style={{ wordBreak: 'break-word' }}>
                                                {dataset.temporal?.startDate ? (
                                                    <div>
                                                        <span style={{ fontWeight: 'bold' }}>{t('from')}: </span>{' '}
                                                        <span data-testid="temporal-start">{dataset.temporal.startDate}</span>
                                                    </div>
                                                ) : null}
                                                {dataset.temporal?.endDate ? (
                                                    <div>
                                                        <span style={{ fontWeight: 'bold' }}>{t('to')}: </span>{' '}
                                                        <span data-testid="temporal-end">{dataset.temporal.endDate}</span>
                                                    </div>
                                                ) : null}
                                                {dataset.temporalResolution ? (
                                                    <div>
                                                        <span style={{ fontWeight: 'bold' }}>{t('timeResolution')}: </span>{' '}
                                                        <span data-testid="temporal-resolution">{dataset.temporalResolution}</span>
                                                    </div>
                                                ) : null}
                                            </div>
                                        </div>
                                    ) : null}
                                    {dataset.issued ? (
                                        <div className="nkod-detail-attribute">
                                            <div className="govuk-body nkod-detail-attribute-name">{t('issuedDate')}</div>
                                            <div className="govuk-body nkod-detail-attribute-value" style={{ wordBreak: 'break-word' }}>
                                                <span data-testid="date-issued">{dataset.issued}</span>
                                            </div>
                                        </div>
                                    ) : null}
                                    {dataset.lastUpdated ? (
                                        <div className="nkod-detail-attribute">
                                            <div className="govuk-body nkod-detail-attribute-name">{t('lastModifiedDate')}</div>
                                            <div className="govuk-body nkod-detail-attribute-value" style={{ wordBreak: 'break-word' }}>
                                                <span data-testid="data-last-updated">{dataset.lastUpdated}</span>
                                            </div>
                                        </div>
                                    ) : null}
                                </GridColumn>
                                {dataset.distributions.length > 0 ? (
                                    <GridColumn widthUnits={3} totalUnits={4}>
                                        <div className="govuk-body nkod-detail-distribution-count" data-testid="distributions-count">
                                            {dataset.distributions.length}{' '}
                                            {dataset.distributions.length === 1
                                                ? t('distribution')
                                                : dataset.distributions.length < 5
                                                ? t('distribution2-4')
                                                : t('distribution5')}
                                        </div>
                                        <hr className="govuk-line" aria-hidden="true" />
                                        {dataset.distributions.map((distribution) => (
                                            <DistributionRow key={distribution.id} distribution={distribution} dataset={dataset} />
                                        ))}
                                    </GridColumn>
                                ) : null}
                            </GridRow>
                            {datasetsAsParent && datasetsAsParent.items.length > 0 ? (
                                <GridRow>
                                    <GridColumn widthUnits={1} totalUnits={1} data-testid="related">
                                        <RelatedContent
                                            header={t('datasetsFromList')}
                                            links={datasetsAsParent.items.map((d) => ({
                                                title: d.name ?? t('noName'),
                                                url: '/datasety/' + d.id
                                            }))}
                                        />
                                        {loadingDatasets ? <Loading /> : null}
                                        <Pagination
                                            currentPage={datasetsAsParentQuery.page}
                                            totalItems={datasetsAsParent.totalCount}
                                            pageSize={25}
                                            onPageChange={(p) => {
                                                setDatasetsAsParentQuery({ page: p });
                                            }}
                                        />
                                    </GridColumn>
                                </GridRow>
                            ) : null}
                            {datasetsAsSibling && datasetsAsSibling.items.length > 0 ? (
                                <GridRow>
                                    <GridColumn widthUnits={1} totalUnits={1} data-testid="related">
                                        <RelatedContent
                                            header={t('otherDatasetsFromList')}
                                            links={datasetsAsSibling.items.map((d) => ({
                                                title: d.name ?? t('noName'),
                                                url: '/datasety/' + d.id
                                            }))}
                                        />
                                        {loadingDatasets ? <Loading /> : null}
                                        <Pagination
                                            currentPage={datasetsAsSiblingQuery.page}
                                            totalItems={datasetsAsSibling.totalCount}
                                            pageSize={25}
                                            onPageChange={(p) => {
                                                setDatasetsAsSiblingQuery({ page: p });
                                            }}
                                        />
                                    </GridColumn>
                                </GridRow>
                            ) : null}
                            {applications && applications?.length > 0 && (
                                <GridRow>
                                    <GridColumn widthUnits={1} totalUnits={1} data-testid="applications">
                                        <h2 className="govuk-heading-m suggestion-subtitle">{t('applicationList.headerTitle')}</h2>
                                        <SimpleList loading={loadingApplications} error={errorApplications}>
                                            {applications?.map((app: Application, i: number) => (
                                                <ApplicationListItem key={i} app={app} isLast={i === applications?.length - 1} editable={false} />
                                            ))}
                                        </SimpleList>
                                    </GridColumn>
                                </GridRow>
                            )}
                            {suggestions && suggestions?.length > 0 && (
                                <GridRow>
                                    <GridColumn widthUnits={1} totalUnits={1} data-testid="suggestions">
                                        <h2 className="govuk-heading-m suggestion-subtitle">{t('suggestionList.headerTitle')}</h2>
                                        <SimpleList loading={loadingSuggestions} error={errorSuggestions}>
                                            {suggestions?.map((suggestion: Suggestion, i: number) => (
                                                <SuggestionListItem key={i} suggestion={suggestion} isLast={i === suggestions?.length - 1} editable={false} />
                                            ))}
                                        </SimpleList>
                                    </GridColumn>
                                </GridRow>
                            )}
                            {!loadingCmsDatasets && (
                                <div ref={commentSectionRef}>
                                    <CommentSection contentId={cmsDataset?.[0]?.id} datasetUri={dataset.key} />
                                </div>
                            )}
                        </div>
                    </MainContent>
                </>
            ) : (
                <NotFound />
            )}
        </>
    );
}
