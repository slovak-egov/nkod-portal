import PageHeader from '../components/PageHeader';
import Breadcrumbs from '../components/Breadcrumbs';
import MainContent from '../components/MainContent';
import GridRow from '../components/GridRow';
import GridColumn from '../components/GridColumn';
import RelatedContent from '../components/RelatedContent';
import FileIcon from '../components/FileIcon';

import Loading from '../components/Loading';
import ErrorAlert from '../components/ErrorAlert';
import { useDataset, useDatasets, useDocumentTitle } from '../client';
import { useParams } from 'react-router';
import { useTranslation } from 'react-i18next';
import { useEffect } from 'react';

export default function DetailDataset() {
    const [dataset, loading, error] = useDataset();
    const { id } = useParams();
    const [datasetsAsSibling, datasetsAsSiblingQuery, setDatasetsAsSiblingQuery] = useDatasets({ page: 0 });
    const [datasetsAsParent, datasetsAsParentQuery, setDatasetsAsParentQuery] = useDatasets({ page: 0 });
    const { t } = useTranslation();
    useDocumentTitle(dataset?.name ?? '');

    useEffect(() => {
        if (id) {
            if (!datasetsAsSiblingQuery.filters || Object.keys(datasetsAsSiblingQuery.filters).length === 0) {
                setDatasetsAsSiblingQuery({ filters: { sibling: [id] }, page: 1, pageSize: 100 });
            }
            if (!datasetsAsParentQuery.filters || Object.keys(datasetsAsParentQuery.filters).length === 0) {
                setDatasetsAsParentQuery({ filters: { parent: [id] }, page: 1, pageSize: 100 });
            }
        }
    }, [id, datasetsAsSiblingQuery, setDatasetsAsSiblingQuery, datasetsAsParentQuery, setDatasetsAsParentQuery]);

    const datasets = [];
    if (datasetsAsSibling) {
        datasets.push(...datasetsAsSibling.items);
    }
    if (datasetsAsParent) {
        datasets.push(...datasetsAsParent.items);
    }

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
                                            <div className="govuk-body nkod-detail-attribute-value" data-testid="themes" style={{ wordBreak: 'break-all' }}>
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
                                            <div className="govuk-body nkod-detail-attribute-value" data-testid="types" style={{ wordBreak: 'break-all' }}>
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
                                                style={{ wordBreak: 'break-all' }}
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
                                            <div className="govuk-body nkod-detail-attribute-value" data-testid="data-serie" style={{ wordBreak: 'break-all' }}>
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
                                            <div className="govuk-body nkod-detail-attribute-name">{t('specification')}</div>
                                            <div
                                                className="govuk-body nkod-detail-attribute-value"
                                                data-testid="specification"
                                                style={{ wordBreak: 'break-all' }}
                                            >
                                                <a href={dataset.specification} className="govuk-link">
                                                    {t('show')}
                                                </a>
                                            </div>
                                        </div>
                                    </GridColumn>
                                ) : null}
                                {dataset.accrualPeriodicityValue ? (
                                    <GridColumn widthUnits={1} totalUnits={4}>
                                        <div className="nkod-detail-attribute">
                                            <div className="govuk-body nkod-detail-attribute-name">{t('updateFrequency')}</div>
                                            <div className="govuk-body nkod-detail-attribute-value" data-testid="update-frequency">
                                                {dataset.accrualPeriodicityValue.label}
                                            </div>
                                        </div>
                                    </GridColumn>
                                ) : null}
                                {dataset.contactPoint?.name || dataset.contactPoint?.email ? (
                                    <GridColumn widthUnits={1} totalUnits={4}>
                                        <div className="nkod-detail-attribute">
                                            <div className="govuk-body nkod-detail-attribute-name">{t('contactPoint')}</div>
                                            <div className="govuk-body nkod-detail-attribute-value" style={{ wordBreak: 'break-all' }}>
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
                            </GridRow>
                            <GridRow>
                                <GridColumn widthUnits={1} totalUnits={4}>
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
                                            <div className="govuk-body nkod-detail-attribute-value" style={{ wordBreak: 'break-all' }}>
                                                <span data-testid="spatial-resolution">{dataset.spatialResolutionInMeters}</span>
                                            </div>
                                        </div>
                                    ) : null}
                                    {dataset.temporal?.startDate || dataset.temporal?.endDate || dataset.temporalResolution ? (
                                        <div className="nkod-detail-attribute">
                                            <div className="govuk-body nkod-detail-attribute-name">{t('timeValidity')}</div>
                                            <div className="govuk-body nkod-detail-attribute-value" style={{ wordBreak: 'break-all' }}>
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
                                        <hr className="idsk-crossroad-line" aria-hidden="true" />
                                        {dataset.distributions.map((distrubution) => (
                                            <div key={distrubution.id} className="govuk-body nkod-detail-distribution-row" data-testid="distribution">
                                                <div style={{ display: 'flex' }}>
                                                    <FileIcon format={distrubution.formatValue?.label ?? ''} />
                                                    <span
                                                        className="govuk-body nkod-detail-distribution-url"
                                                        style={{ lineHeight: '20px', paddingTop: '20px' }}
                                                    >
                                                        {distrubution.downloadUrl ? (
                                                            <a href={distrubution.downloadUrl} className="govuk-link">
                                                                {distrubution.title && distrubution.title.trim().length > 0 ? distrubution.title : dataset.name}
                                                            </a>
                                                        ) : null}
                                                    </span>
                                                </div>
                                                <hr className="idsk-crossroad-line" aria-hidden="true" />
                                            </div>
                                        ))}
                                    </GridColumn>
                                ) : null}
                            </GridRow>
                            {datasets.length > 0 ? (
                                <GridRow>
                                    <GridColumn widthUnits={1} totalUnits={1} data-testid="related">
                                        <RelatedContent
                                            header={dataset.isSerie ? t('datasetsFromList') : t('otherDatasetsFromList')}
                                            links={datasets.map((d) => ({
                                                title: d.name ?? t('noName'),
                                                url: '/datasety/' + d.id
                                            }))}
                                        />
                                    </GridColumn>
                                </GridRow>
                            ) : null}
                        </div>
                    </MainContent>
                </>
            ) : null}
        </>
    );
}
