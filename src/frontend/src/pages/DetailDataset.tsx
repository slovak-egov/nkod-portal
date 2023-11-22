import PageHeader from "../components/PageHeader";
import Breadcrumbs from "../components/Breadcrumbs";
import MainContent from "../components/MainContent";
import GridRow from "../components/GridRow";
import GridColumn from "../components/GridColumn";
import RelatedContent from "../components/RelatedContent";

import csvIcon from '../icons/csv.png';
import docIcon from '../icons/doc.png';
import htmlIcon from '../icons/html.png';
import jsonIcon from '../icons/json.png';
import mdbIcon from '../icons/mdb.png';
import odsIcon from '../icons/ods.png';
import pdfIcon from '../icons/pdf.png';
import rdfIcon from '../icons/rdf.png';
import sqlIcon from '../icons/sql.png';
import txtIcon from '../icons/txt.png';
import xlsIcon from '../icons/xls.png';
import xlsxIcon from '../icons/xlsx.png';
import xmlIcon from '../icons/xml.png';

import Loading from "../components/Loading";
import ErrorAlert from "../components/ErrorAlert";
import { useDataset, useDatasets, useDocumentTitle } from "../client";
import { useParams } from "react-router";
import { useTranslation } from "react-i18next";
import { useEffect } from "react";

export default function DetailDataset()
{
    const [dataset, loading, error] = useDataset();
    const { id } = useParams();
    const [datasetsAsSibling, datasetsAsSiblingQuery, setDatasetsAsSiblingQuery] = useDatasets({page: 0});
    const [datasetsAsParent, datasetsAsParentQuery, setDatasetsAsParentQuery] = useDatasets({page: 0});
    const {t} = useTranslation();
    useDocumentTitle(dataset?.name ?? '');

    useEffect(() => {
        if (id)
        {
            if (!datasetsAsSiblingQuery.filters || Object.keys(datasetsAsSiblingQuery.filters).length === 0) {
                setDatasetsAsSiblingQuery({filters: {sibling: [id]}, page: 1, pageSize: 100})
            }
            if (!datasetsAsParentQuery.filters || Object.keys(datasetsAsParentQuery.filters).length === 0) {
                setDatasetsAsParentQuery({filters: {parent: [id]}, page: 1, pageSize: 100})
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

    const path = [{title: t('nkod'), link: '/'}, {title: t('search'), link: '/datasety'}];
    if (dataset?.name) {
        path.push({title: dataset.name, link: '/datasety/' + dataset.id});
    }

    return  <>{loading ? <Loading /> : error ? <ErrorAlert error={error} /> : dataset ?
        <><Breadcrumbs items={path} />
        <MainContent>
            <div className="nkod-entity-detail">
                <PageHeader>{dataset.name}</PageHeader>
                {dataset.publisher ? <p className="govuk-body nkod-publisher-name" data-testid="publisher-name">
                    {dataset.publisher.name}
                </p> : null}
                {dataset.description ? <p className="govuk-body nkod-entity-description" data-testid="description">
                    {dataset.description}
                </p> : null}
                {dataset.keywords.length > 0 ? <div className="nkod-entity-detail-tags govuk-clearfix" data-testid="keywords">
                    {dataset.keywords.map(t => <div key={t} className="govuk-body nkod-entity-detail-tag">
                        {t}
                    </div>)}
                </div> : null}
                <GridRow>
                    {(dataset.themeValues.length > 0 || dataset.euroVocThemeValues.length > 0) ? <GridColumn widthUnits={1} totalUnits={4}>
                        <div className="nkod-detail-attribute">
                            <div className="govuk-body nkod-detail-attribute-name">
                                {t('theme')}
                            </div>
                            <div className="govuk-body nkod-detail-attribute-value" data-testid="themes">
                                {dataset.themeValues.map(l => <div key={l.label}>{l.label}</div>)}
                                {dataset.euroVocThemeValues.map(l => <div key={l}>{l}</div>)}
                            </div>
                        </div>                        
                    </GridColumn> : null}          
                    {dataset.documentation ? <GridColumn widthUnits={1} totalUnits={4}>
                        <div className="nkod-detail-attribute">
                            <div className="govuk-body nkod-detail-attribute-name">
                                {t('documentation')}
                            </div>
                            <div className="govuk-body nkod-detail-attribute-value" data-testid="documentation">
                                <a href={dataset.documentation} className="govuk-link">{t('show')}</a>
                            </div>
                        </div>                        
                    </GridColumn> : null}          
                    {dataset.accrualPeriodicityValue ? <GridColumn widthUnits={1} totalUnits={4}>
                        <div className="nkod-detail-attribute">
                            <div className="govuk-body nkod-detail-attribute-name">
                                {t('updateFrequency')}
                            </div>
                            <div className="govuk-body nkod-detail-attribute-value" data-testid="update-frequency">
                                {dataset.accrualPeriodicityValue.label}
                            </div>
                        </div>
                    </GridColumn> : null}
                    {dataset.contactPoint?.name || dataset.contactPoint?.email ? <GridColumn widthUnits={1} totalUnits={4}>
                        <div className="nkod-detail-attribute">
                            <div className="govuk-body nkod-detail-attribute-name">
                                {t('contactPoint')}
                            </div>
                            <div className="govuk-body nkod-detail-attribute-value">
                                {dataset.contactPoint?.name ? <div>
                                    <span data-testid="contact-name">{dataset.contactPoint.name}</span>
                                </div> : null}
                                {dataset.contactPoint?.email ? <div>
                                    <span data-testid="contact-email">{dataset.contactPoint.email}</span>
                                </div> : null}
                            </div>
                        </div>                        
                    </GridColumn> : null}
                </GridRow>
                <GridRow>
                    <GridColumn widthUnits={1} totalUnits={4}>
                        {dataset.spatialValues.length > 0 ? <div className="nkod-detail-attribute">
                            <div className="govuk-body nkod-detail-attribute-name">
                                {t('spatialValidity')}
                            </div>
                            <div className="govuk-body nkod-detail-attribute-value" data-testid="spatial">
                                {dataset.spatialValues.map(l => <div key={l.label}>{l.label}</div>)}
                            </div>
                        </div> : null}
                        {dataset.temporal?.startDate || dataset.temporal?.endDate ? <div className="nkod-detail-attribute">
                            <div className="govuk-body nkod-detail-attribute-name">
                                {t('timeValidity')}
                            </div>
                            <div className="govuk-body nkod-detail-attribute-value">
                                {dataset.temporal?.startDate ? <div>
                                    <span style={{fontWeight: 'bold'}}>od: </span> <span data-testid="temporal-start">{dataset.temporal.startDate}</span>
                                </div> : null}
                                {dataset.temporal?.endDate ? <div>
                                    <span style={{fontWeight: 'bold'}}>do: </span> <span data-testid="temporal-end">{dataset.temporal.endDate}</span>
                                </div> : null}
                            </div>
                        </div> : null}
                    </GridColumn>
                    {dataset.distributions.length > 0 ? <GridColumn widthUnits={3} totalUnits={4}>
                        <div className="govuk-body nkod-detail-distribution-count" data-testid="distributions-count">
                            {dataset.distributions.length} {dataset.distributions.length === 1 ? 'distribúcia' : dataset.distributions.length < 5 ? 'distribúcie' : 'distribúcií'}
                        </div>
                        <hr className="idsk-crossroad-line" aria-hidden="true"/>
                        {dataset.distributions.map(distrubution => <div key={distrubution.id} className="govuk-body nkod-detail-distribution-row" data-testid="distribution">
                            <span className="govuk-body nkod-detail-distribution-format">
                                {
                                    distrubution.formatValue?.label?.toLowerCase() === 'csv' ? <img src={csvIcon} alt="csv" /> :
                                    distrubution.formatValue?.label?.toLowerCase() === 'doc' ? <img src={docIcon} alt="doc" /> :
                                    distrubution.formatValue?.label?.toLowerCase() === 'html' ? <img src={htmlIcon} alt="html" /> :
                                    distrubution.formatValue?.label?.toLowerCase() === 'json' ? <img src={jsonIcon} alt="json" /> :
                                    distrubution.formatValue?.label?.toLowerCase() === 'mdb' ? <img src={mdbIcon} alt="mdb" /> :
                                    distrubution.formatValue?.label?.toLowerCase() === 'ods' ? <img src={odsIcon} alt="ods" /> :
                                    distrubution.formatValue?.label?.toLowerCase() === 'pdf' ? <img src={pdfIcon} alt="pdf" /> :
                                    distrubution.formatValue?.label?.toLowerCase() === 'rdf' ? <img src={rdfIcon} alt="rdf" /> :
                                    distrubution.formatValue?.label?.toLowerCase() === 'sql' ? <img src={sqlIcon} alt="sql" /> :
                                    distrubution.formatValue?.label?.toLowerCase() === 'txt' ? <img src={txtIcon} alt="txt" /> :
                                    distrubution.formatValue?.label?.toLowerCase() === 'xls' ? <img src={xlsIcon} alt="xls" /> :
                                    distrubution.formatValue?.label?.toLowerCase() === 'xlsx' ? <img src={xlsxIcon} alt="xlsx" /> :
                                    distrubution.formatValue?.label?.toLowerCase() === 'xml' ? <img src={xmlIcon} alt="xml" /> : null
                                }
                            </span>
                            <span className="govuk-body nkod-detail-distribution-url">
                                {distrubution.downloadUrl ? <a href={distrubution.downloadUrl} className="govuk-link">
                                    {(distrubution.title && distrubution.title.trim().length > 0) ? distrubution.title : dataset.name}
                                </a> : null}
                            </span>
                            <hr className="idsk-crossroad-line" aria-hidden="true"/>
                        </div>)}
                    </GridColumn> : null}
                </GridRow>
                {(datasets.length > 0) ? <GridRow>
                    <GridColumn widthUnits={1} totalUnits={1} data-testid="related">
                        <RelatedContent header={t('otherDatasetsFromList')} links={datasets.map(d => ({
                            title: d.name ?? 'Bez názvu',
                            url: '/datasety/' + d.id
                        }))}  />
                    </GridColumn>
                </GridRow> : null}
            </div>
        </MainContent></> : null}
    </>
}